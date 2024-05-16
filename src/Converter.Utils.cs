using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Csg;

// using Objects.BuiltElements;
// using Objects.BuiltElements.Revit;
// using Objects.BuiltElements.Revit.Curve;
// using Objects.Geometry;
// using Objects.Other;
// using Objects.Primitive;
// using Objects.Structural.Geometry;
//
// using Speckle.Core.Kits;
// using Speckle.Core.Logging;
// using Speckle.Core.Models;

using Elements;
using Elements.Geometry;


namespace SpeckleHypar;

public static class GeometryProcessing
{
    /// <summary>
    /// Remove sequential duplicates from a list of points.
    /// </summary>
    /// <param name="vertices"></param>
    /// <param name="wrap">Whether or not to assume a closed shape like a polygon. If true, the last vertex will be compared to the first, and deleted if identical.</param>
    /// <param name="tolerance">An optional distance tolerance for the comparison.</param>
    /// <returns></returns>
    internal static IList<Vector3> RemoveSequentialDuplicates(this IList<Vector3> vertices, bool wrap = false, double tolerance = Vector3.EPSILON)
    {
        List<Vector3> newList = new List<Vector3> { vertices[0] };
        for (int i = 1; i < vertices.Count; i++)
        {
            var vertex = vertices[i];
            var prevVertex = newList[newList.Count - 1];
            if (!vertex.IsAlmostEqualTo(prevVertex, tolerance))
            {
                // if we wrap, and we're at the last vertex, also check for a zero-length segment between first and last.
                if (wrap && i == vertices.Count - 1)
                {
                    if (!vertex.IsAlmostEqualTo(vertices[0], tolerance))
                    {
                        newList.Add(vertex);
                    }
                }
                else
                {
                    newList.Add(vertex);
                }
            }
        }
        return newList;
    }

    internal static IList<Elements.Geometry.IndexedPolycurve> Join(this IList<Elements.Geometry.BoundedCurve> curves)
    {
        if (curves.Count == 0) return new List<Elements.Geometry.IndexedPolycurve>();
        if (!curves.All(curve => curve is Elements.Geometry.Line || curve is Elements.Geometry.Arc))
            throw new NotImplementedException("Error: Can only sort Line and Arc curve types.");

        var queue = new Queue<Elements.Geometry.BoundedCurve>(curves);
        var polycurves = new List<Elements.Geometry.IndexedPolycurve>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var segments = new List<Elements.Geometry.BoundedCurve>() { current };

            var i = 0;
            while (i < queue.Count)
            {
                if (!queue.TryDequeue(out Elements.Geometry.BoundedCurve next)) continue;
                if (segments.Last().End.IsAlmostEqualTo(next.Start))
                {
                    segments.Add(next);
                }
                else if (segments.Last().End.IsAlmostEqualTo(next.End))
                {
                    if (next is Elements.Geometry.Line line) segments.Add(line.Reversed());
                    else if (next is Elements.Geometry.Arc arc) segments.Add(arc.Reversed());
                    // else throw new NotImplementedException("Error: Can only sort Line and Arc curve types.");
                }
                else if (segments.First().Start.IsAlmostEqualTo(next.Start))
                {
                    if (next is Elements.Geometry.Line line) segments.Insert(0, line.Reversed());
                    else if (next is Elements.Geometry.Arc arc) segments.Insert(0, arc.Reversed());
                    // else throw new NotImplementedException("Error: Can only sort Line and Arc curve types.");
                }
                else if (segments.First().Start.IsAlmostEqualTo(next.End))
                {
                    segments.Insert(0, next);
                }
                else
                {
                    queue.Enqueue(next);
                    i++;
                }
            }
            var polycurve = new Elements.Geometry.IndexedPolycurve(segments);

            polycurves.Add(new IndexedPolycurve(segments));
        }
        return polycurves;
    }

    /// <summary>
    /// Converts a Solid's Face to a Profile. NOTE: Assumes the face is planar.
    /// </summary>
    /// <param name="face"></param>
    /// <returns></returns>
    public static Elements.Geometry.Profile ToProfile(this Elements.Geometry.Solids.Face face)
    {
        var outer = face.Outer.ToPolygon();
        var inner = face.Inner?.Select(loop => loop.ToPolygon()).ToList() ?? new List<Elements.Geometry.Polygon>();
        return new Elements.Geometry.Profile(outer, inner);
    }

    public static bool FindEqual(this Elements.Geometry.Line line, IEnumerable<Elements.Geometry.Line> lines, out IList<Elements.Geometry.Line> equals, bool strictly = false)
    {
        equals = new List<Elements.Geometry.Line>();
        foreach (var other in lines)
        {
            if (line.IsAlmostEqualTo(other, strictly)) equals.Add(other);
        }
        return (equals.Count > 0);
    }

    public static bool GetAdjacent(this Elements.Geometry.Polygon polygon, IEnumerable<Elements.Geometry.Polygon> polygons, out IList<Elements.Geometry.Polygon> adjacent)
    {
        adjacent = new List<Elements.Geometry.Polygon>();
        foreach (var line in polygon.OfType<Elements.Geometry.Line>())
        {
            var adjacentLines = new List<Elements.Geometry.Polygon>();
            foreach (var other in polygons)
            {
                if (line.FindEqual(other.OfType<Elements.Geometry.Line>(), out var equals, false))
                {
                    adjacentLines.Add(other);
                }
            }
            if (adjacentLines.Count > 1) throw new Exception("Error: Edge has more than one adjacent polygon.");
            else if (adjacentLines.Count == 1) adjacent.Add(adjacentLines.First());
        }
        return (adjacent.Count > 0);
    }

    public static bool GetAdjacent(this Elements.Geometry.Profile profile, IEnumerable<Elements.Geometry.Profile> profiles, out IList<Elements.Geometry.Profile> adjacent)
    {
        adjacent = new List<Elements.Geometry.Profile>();
        foreach (var other in profiles)
        {
            if (other.Equals(profile)) continue;

            var otherPolygons = other.Voids.Prepend(other.Perimeter);
            if (profile.Perimeter.GetAdjacent(otherPolygons, out var adjacentsToPerimeter))
            {
                adjacent.Add(other);
            }
            else if (profile.Voids.Any(hole => hole.GetAdjacent(otherPolygons, out var adjacentsToHole)))
            {
                adjacent.Add(other);
            }
        }
        return (adjacent.Count != 0);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="polygon"></param>
    /// <param name="size"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public static ModelArrows NormalArrow(this Elements.Geometry.Polygon polygon, double size = 1, Color? color = null)
    {
        color = color ?? new Color("Red");
        var vectors = new List<(Vector3 location, Vector3 direction, double magnitude, Color? color)>();
        var centroid = polygon.Centroid();
        var normal = polygon.Normal();

        vectors.Add((centroid, normal, size, color));
        return new ModelArrows(vectors, false, true);
    }

    public static ModelText CentroidText(this Elements.Geometry.Polygon polygon, string text, FontSize size = FontSize.PT36, Color? color = null, double scale = 1)
    {
        color = color ?? new Color("Black");
        var texts = new List<(Vector3 location, Vector3 facingDirection, Vector3 lineDirection, string text, Color? color)>();
        var centroid = polygon.Centroid();

        texts.Add((centroid, Vector3.ZAxis, Vector3.XAxis, text, color));
        return new ModelText(texts, size, scale);
    }
}
