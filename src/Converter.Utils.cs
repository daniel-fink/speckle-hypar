using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;


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

    // /// <summary>
    // /// Join a collection of arcs, lines, or polylines into polycurves.
    // /// Note: de-duplicates any duplicate fragments.
    // /// </summary>
    // /// <param name="curves"></param>
    // /// <returns></returns>
    // /// <exception cref="NotImplementedException"></exception>
    // internal static IList<Elements.Geometry.IndexedPolycurve> Join(this ICollection<Elements.Geometry.BoundedCurve> curves)
    // {
    //     if (curves.Count == 0) return new List<Elements.Geometry.IndexedPolycurve>();
    //     var fragments = new List<Elements.Geometry.BoundedCurve>();
    //     foreach (var curve in curves)
    //     {
    //         if (curve is Elements.Geometry.Line line)
    //         {
    //             if (!fragments.Any(f => f is Elements.Geometry.Line l && l.IsAlmostEqualTo(line, false)))
    //                 fragments.Add(line);
    //         }
    //         else if (curve is Elements.Geometry.Arc arc)
    //         {
    //             if (!fragments.Any(f => f is Elements.Geometry.Arc a && a.Equals(arc)))
    //                 fragments.Add(arc);
    //         }
    //         else if (curve is Elements.Geometry.Polyline polyline)
    //         {
    //             foreach (var segment in polyline.Segments())
    //             {
    //                 if (!fragments.Any(f => f is Elements.Geometry.Line l && l.IsAlmostEqualTo(segment, false)))
    //                     fragments.Add(segment);
    //             }
    //         }
    //         else
    //         {
    //             throw new NotImplementedException("Error: Can only sort Line and Arc curve types. Curve type encountered: " + curve.GetType());
    //         }
    //     }
    //
    //     foreach (var f in fragments)
    //     {
    //         Console.WriteLine("Fragment: " + f.ToString());
    //     }
    //
    //     var polycurves = new List<Elements.Geometry.IndexedPolycurve>();
    //     var queue = new Queue<Elements.Geometry.BoundedCurve>(fragments);
    //
    //     while (queue.Count > 0)
    //     {
    //         Console.WriteLine("Init While Loop Queue Count: " + queue.Count + " " + string.Join(", ", queue.Select(s => s.ToString())));
    //         var current = queue.Dequeue();
    //         Console.WriteLine("Dequeue Current: " + queue.Count + " " + string.Join(", ", queue.Select(s => s.ToString())));
    //
    //         var segments = new List<Elements.Geometry.BoundedCurve>() { current };
    //
    //         var i = 0;
    //         while (i < queue.Count)
    //         {
    //             Console.WriteLine("Inner While Loop Queue Count: " + queue.Count + " " + string.Join(", ", queue.Select(s => s.ToString())));
    //             Console.WriteLine("i: " + i);
    //
    //             if (!queue.TryDequeue(out var next)) continue;
    //             if (segments.Last().End.IsAlmostEqualTo(next.Start))
    //             {
    //                 segments.Add(next);
    //             }
    //             else if (segments.Last().End.IsAlmostEqualTo(next.End))
    //             {
    //                 if (next is Elements.Geometry.Line line) segments.Add(line.Reversed());
    //                 else if (next is Elements.Geometry.Arc arc) segments.Add(arc.Reversed());
    //             }
    //             else if (segments.First().Start.IsAlmostEqualTo(next.Start))
    //             {
    //                 if (next is Elements.Geometry.Line line) segments.Insert(0, line.Reversed());
    //                 else if (next is Elements.Geometry.Arc arc) segments.Insert(0, arc.Reversed());
    //             }
    //             else if (segments.First().Start.IsAlmostEqualTo(next.End))
    //             {
    //                 segments.Insert(0, next);
    //             }
    //             else
    //             {
    //                 Console.WriteLine("Enqueue Next: " + next.ToString());
    //                 queue.Enqueue(next);
    //                 i++;
    //             }
    //         }
    //         Console.WriteLine("Segments after sorting: " + segments.Count + " " + string.Join(", ", segments.Select(s => s.ToString())));
    //         var polycurve = new Elements.Geometry.IndexedPolycurve(segments);
    //         polycurves.Add(new IndexedPolycurve(segments));
    //     }
    //     return polycurves;
    // }

    public static string ToLongString(this Elements.Geometry.Vector3 vector3)
    {
        return "X:" + vector3.X.ToString("F12") + ", Y:" + vector3.Y.ToString("F12") + ", Z:" + vector3.Z.ToString("F12");
    }

    public static string ToLongString(this Elements.Geometry.BoundedCurve boundedCurve)
    {
        return boundedCurve.GetType().Name + " Start: " + boundedCurve.Start.ToLongString() + " End: " + boundedCurve.End.ToLongString();
    }

    internal static Elements.Geometry.Vector3 TolerancePoint(this Elements.Geometry.Vector3 vector3, double tolerance = Vector3.EPSILON)
    {
        var decimalPlaces = Convert.ToInt32(-Math.Log10(tolerance));
        return new Elements.Geometry.Vector3(
            Math.Round(vector3.X, decimalPlaces),
            Math.Round(vector3.Y, decimalPlaces),
            Math.Round(vector3.Z, decimalPlaces)
            );
    }

    internal static bool JoinSegments(this ICollection<Elements.Geometry.BoundedCurve> segments, out Elements.Geometry.IndexedPolycurve polyCurve)
    {
        Console.WriteLine("----------\n Joining Segments...");
        Console.WriteLine("Segments: \n" + string.Join("\n", segments.Select(segment => segment.ToLongString())));

        if (segments.Any(segment => segment is not Line && segment is not Arc))
            throw new NotImplementedException("Error: Can only join Line and Arc segment types.");

        var nodes = new Dictionary<Elements.Geometry.Vector3, List<Elements.Geometry.BoundedCurve>>();
        foreach (var segment in segments)
        {
            if (nodes.ContainsKey(segment.Start.TolerancePoint())) nodes[segment.Start.TolerancePoint()].Add(segment);
            else nodes.Add(segment.Start.TolerancePoint(), new List<Elements.Geometry.BoundedCurve> { segment });

            if (nodes.ContainsKey(segment.End.TolerancePoint())) nodes[segment.End.TolerancePoint()].Add(segment);
            else nodes.Add(segment.End.TolerancePoint(), new List<Elements.Geometry.BoundedCurve> { segment });
        }
        Console.WriteLine("Nodes: \n" + string.Join("\n", nodes.Select(node => node.Key.ToLongString() + ": " + string.Join(" | ", node.Value.Select(s => s.ToLongString())))));
        Console.WriteLine("\n");

        if (nodes.Values.Any(node => node.Count > 2)) // If more than two segments meet at a point, we can't join the segments into a singular polycurve.
        {
            Console.WriteLine("Error: Unable to join segments. More than two segments meet at a point.");
            polyCurve = null;
            return false;
        }

        var termini = nodes.Where(node => node.Value.Count == 1);
        var terminiCount = termini.Count();
        if (terminiCount > 2) // If there are more than two termini, we can't join the segments into a singular polycurve.
        {
            Console.WriteLine("Error: Unable to join segments. More than two termini found.");
            polyCurve = null;
            return false;
        }

        var ordered = new List<BoundedCurve>();

        if (terminiCount == 2) ordered.Add(termini.First(node => node.Key == node.Value.First().Start.TolerancePoint()).Value.First()); // Segments form an open polycurve. Choose first segment whose start is a node's key.
        else if (terminiCount == 0) ordered.Add(nodes.Values.First().First()); // Segments form a closed polycurve. Choose any segment to start.
        else throw new Exception("Error: Unable to determine polycurve start. Singular terminus found.");
        nodes.Remove(ordered.Last().Start.TolerancePoint());

        Console.WriteLine("Begin While Loop\n");
        while (nodes.Count > 0)
        {
            var current = ordered.Last();
            Console.WriteLine("Current: \n" + current.ToString());
            Console.WriteLine("Nodes: \n" + string.Join("\n", nodes.Select(node => node.Key.ToString() + ": " + string.Join(" | ", node.Value.Select(s => s.ToString())))));
            Console.WriteLine("Ordered: \n" + string.Join(", ", ordered.Select(s => s.ToString())));

            var nexts = nodes[current.End.TolerancePoint()];
            Console.WriteLine("Nexts: \t" + string.Join(" | ", nexts.Select(s => s.ToString())));

            var next = nexts.First(curve => curve != current && curve.End.TolerancePoint() != current.Start.TolerancePoint()); // Get the next segment that isn't the current segment.
            Console.WriteLine("Next pre-parsing: \t" + next.ToString());

            if (next.End.IsAlmostEqualTo(current.End))
            {
                if (next is Elements.Geometry.Line line) next = line.Reversed();
                else if (next is Elements.Geometry.Arc arc) next = arc.Reversed();
            }
            Console.WriteLine("Next post-parsing: \t" + next.ToString());

            ordered.Add(next);
            Console.WriteLine("Adding Next...");
            nodes.Remove(next.Start.TolerancePoint());

            Console.WriteLine("Nodes: \n" + string.Join("\n", nodes.Select(node => node.Key.ToString() + ": " + string.Join(" | ", node.Value.Select(s => s.ToString())))));
            Console.WriteLine("Ordered: \n" + string.Join(", ", ordered.Select(s => s.ToString())));
            Console.WriteLine("\n");
        }

        polyCurve = new IndexedPolycurve(ordered);
        return true;
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
