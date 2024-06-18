using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
// using System.Drawing;
using System.Linq;

using Objects.Geometry;
using Objects.Other;
using Objects.Primitive;
using Objects.Utils;

using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;

namespace SpeckleHypar;

public static class VectorConversion
{
    /// <summary>
    /// Converts a Speckle Vector to Elements. 
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static Vector3 FromSpeckle(this Objects.Geometry.Vector vector)
    {
        return new Vector3(vector.x, vector.y, vector.z);
    }

    /// <summary>
    /// Converts a Vector to Speckle. 
    /// </summary>
    /// <param name="vector3"></param>
    /// <returns></returns>
    public static Objects.Geometry.Vector ToSpeckle(this Vector3 vector3)
    {
        return new Objects.Geometry.Vector(vector3.X, vector3.Y, vector3.Z);
    }
}

public static class PointConversion
{
    public static Vector3 FromSpeckle(this Objects.Geometry.Point point)
    {
        return new Vector3(point.x, point.y, point.z);
    }

    /// <summary>
    /// Converts a Vector to a Speckle Point. 
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static Objects.Geometry.Point ToSpecklePoint(this Vector3 vector3)
    {
        return new Objects.Geometry.Point(vector3.X, vector3.Y, vector3.Z);
    }

    /// <summary>
    /// Flattens a Vector to an array of its coordinates.
    /// </summary>
    /// <param name="vector3"></param>
    /// <returns></returns>
    public static double[] ToArray(this Vector3 vector3)
    {
        return new[] { vector3.X, vector3.Y, vector3.Z };
    }

    /// <summary>
    /// Flattens Vector3s to their coordinates.
    /// </summary>
    /// <param name="vector3s"></param>
    /// <returns></returns>
    public static IEnumerable<double> ToArray(this IEnumerable<Vector3> vector3s)
    {
        return vector3s.SelectMany(ToArray);
    }

    public static IEnumerable<Vector3> FromSpeckle(this IList<double> array)
    {
        var count = array.Count;
        if (count % 3 != 0)
        {
            throw new SpeckleException("Array malformed: length%3 != 0.");
        }

        var vectors = new List<Vector3>(count / 3);

        for (int i = 2; i < count; i += 3)
        {
            vectors.Add(new Vector3(array[i - 2], array[i - 1], array[i]));
        }

        return vectors;
    }
}

public static class PlaneConversion
{
    /// <summary>
    /// Converts an Elements Plane to Speckle. 
    /// </summary>
    /// <param name="plane"></param>
    /// <returns></returns>
    public static Objects.Geometry.Plane ToSpeckle(this Elements.Geometry.Plane plane, Vector3 xAxis, Vector3 yAxis)
    {
        return new Objects.Geometry.Plane(
            plane.Origin.ToSpecklePoint(),
            plane.Normal.ToSpeckle(),
            xAxis.ToSpeckle(),
            yAxis.ToSpeckle()
        );
    }

    /// <summary>
    /// Converts an Elements Transform to Speckle. 
    /// </summary>
    /// <param name="transform"></param>
    /// <returns></returns>
    public static Objects.Geometry.Plane ToSpeckle(this Elements.Geometry.Transform transform)
    {
        return new Objects.Geometry.Plane(
            transform.Origin.ToSpecklePoint(),
            transform.ZAxis.ToSpeckle(),
            transform.XAxis.ToSpeckle(),
            transform.YAxis.ToSpeckle()
        );
    }

    /// <summary>
    /// Converts a Speckle Plane to Hypar, along with its associated Transform. 
    /// </summary>
    /// <param name="plane"></param>
    /// <returns></returns>
    public static Elements.Geometry.Plane FromSpeckle(this Objects.Geometry.Plane plane, out Elements.Geometry.Transform transform)
    {
        var result = new Elements.Geometry.Plane(
            plane.origin.FromSpeckle(),
            plane.normal.FromSpeckle()
        );

        transform = new Elements.Geometry.Transform(
            result.Origin,
            plane.xdir.FromSpeckle(),
            plane.ydir.FromSpeckle(),
            result.Normal);

        return result;
    }
}


public static class LineConversion
{
    /// <summary>
    /// Converts a Speckle Line to Elements. 
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static Elements.Geometry.Line FromSpeckle(this Objects.Geometry.Line line)
    {
        return new Elements.Geometry.Line(line.start.FromSpeckle(), line.end.FromSpeckle());
    }

    /// <summary>
    /// Converts an Elements Line to Speckle.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static Objects.Geometry.Line ToSpeckle(this Elements.Geometry.Line line)
    {
        return new Objects.Geometry.Line(line.Start.ToSpecklePoint(), line.End.ToSpecklePoint());
    }
}

public static class PolylineConversion
{
    /// <summary>
    /// Converts a Speckle Polyline to Elements. Also returns a Polygon if the polyline is closed
    /// </summary>
    /// <param name="polyline"></param>
    /// <returns></returns>
    public static Elements.Geometry.Polyline FromSpeckle(this Objects.Geometry.Polyline polyline)
    {
        if (polyline.closed)
        {
            return new Elements.Geometry.Polygon(polyline.value.FromSpeckle().ToList());
        }
        else
        {
            return new Elements.Geometry.Polyline(polyline.value.FromSpeckle().ToList());
        }
    }

    /// <summary>
    /// Converts a Polyline to Speckle.
    /// </summary>
    /// <param name="polyline"></param>
    /// <returns></returns>
    public static Objects.Geometry.Polyline ToSpeckle(this Elements.Geometry.Polyline polyline)
    {
        return new Objects.Geometry.Polyline(polyline.Vertices.ToArray().ToList());
    }

    /// <summary>
    /// Converts a Speckle Polyline to either a specified Elements Polygon and/or Elements Polyline.
    /// </summary>
    /// <param name="polyline"></param>
    /// <param name="polygon"></param>
    /// <param name="elementsPolyline"></param>
    /// <returns></returns>
    public static bool TryGetPolygon(this Objects.Geometry.Polyline polyline, out Elements.Geometry.Polygon? polygon, out Elements.Geometry.Polyline elementsPolyline)
    {
        elementsPolyline = polyline.FromSpeckle();
        if (polyline.closed)
        {
            polygon = new Elements.Geometry.Polygon(polyline.value.FromSpeckle().ToList());
            return true;
        }
        polygon = null;
        return false;
    }


    /// <summary>
    /// Converts a Polygon to Speckle.
    /// </summary>
    /// <param name="polygon"></param>
    /// <returns></returns>
    public static Objects.Geometry.Polyline ToSpeckle(this Elements.Geometry.Polygon polygon)
    {
        var result = new Objects.Geometry.Polyline(polygon.Vertices.ToArray().ToList());
        result.closed = true;
        return result;
    }
}

public static class ArcConversion
{
    /// <summary>
    /// Converts a Speckle Circle to Elements. 
    /// </summary>
    /// <param name="circle"></param>
    /// <returns></returns>
    public static Objects.Geometry.Circle ToSpeckle(this Elements.Geometry.Circle circle)
    {
        var plane = new Objects.Geometry.Plane(
            circle.Center.ToSpecklePoint(),
            circle.Normal.ToSpeckle(),
            circle.Transform.XAxis.ToSpeckle(),
            circle.Transform.YAxis.ToSpeckle()
        );
        return new Objects.Geometry.Circle(plane, circle.Radius);
    }

    /// <summary>
    /// Converts a Speckle Circle to Elements. 
    /// </summary>
    /// <param name="circle"></param>
    /// <returns></returns>
    public static Elements.Geometry.Circle FromSpeckle(this Objects.Geometry.Circle circle)
    {
        circle.plane.FromSpeckle(out var transform);
        var radius = circle.radius ?? 0;
        return new Elements.Geometry.Circle(transform, radius);
    }

    /// <summary>
    /// Converts an Elements Arc to Speckle. 
    /// </summary>
    /// <param name="arc"></param>
    /// <returns></returns>
    public static Objects.Geometry.Arc ToSpeckle(this Elements.Geometry.Arc arc)
    {
        var plane = new Objects.Geometry.Plane(
            arc.Plane().Origin.ToSpecklePoint(),
            arc.Plane().Normal.ToSpeckle(),
            arc.TransformAt(0).XAxis.ToSpeckle(),
            arc.TransformAt(0).YAxis.ToSpeckle()
        );
        return new Objects.Geometry.Arc(
            plane,
            arc.Radius,
            arc.StartAngle,
            arc.EndAngle,
            arc.Domain.Length
        );
    }

    /// <summary>
    /// Converts a Speckle Arc to Elements. 
    /// </summary>
    /// <param name="arc"></param>
    /// <returns></returns>
    /// <exception cref="SpeckleException"></exception>
    public static Elements.Geometry.Arc FromSpeckle(this Objects.Geometry.Arc arc)
    {
        arc.plane.FromSpeckle(out var transform);
        var radius = arc.radius.GetValueOrDefault();
        var startParam = arc.domain.start.GetValueOrDefault();
        var endParam = arc.domain.end.GetValueOrDefault();
        return new Elements.Geometry.Arc(
            transform,
            radius,
            startParam,
            endParam
        );
    }
}

public static class PolyCurveConversion
{
    /// <summary>
    /// Converts a Speckle PolyCurve to Elements.
    /// </summary>
    /// <param name="polyCurve"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static Elements.Geometry.IndexedPolycurve FromSpeckle(this Objects.Geometry.Polycurve polyCurve)
    {
        var boundedCurves = new List<Elements.Geometry.BoundedCurve>();
        foreach (var segment in polyCurve.segments)
        {
            boundedCurves.Add(segment.FromSpeckleICurve());
        }
        return new Elements.Geometry.IndexedPolycurve(boundedCurves);
    }

    /// <summary>
    /// Converts an Elements PolyCurve to Speckle.
    /// </summary>
    /// <param name="polyCurve"></param>
    /// <returns></returns>
    public static Objects.Geometry.Polycurve ToSpeckle(this Elements.Geometry.IndexedPolycurve polyCurve)
    {
        var result = new Objects.Geometry.Polycurve();
        result.segments = polyCurve.Select(boundedCurve => boundedCurve.ToSpeckleICurve()).ToList();
        return result;
    }
}

public static class BoundedCurveConversion
{
    /// <summary>
    /// Converts a Speckle Geometry that could be BoundedCurves to Elements.
    /// </summary>
    /// <param name="curve"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static Elements.Geometry.BoundedCurve FromSpeckleICurve(this Objects.ICurve curve)
    {
        switch (curve)
        {
            case Objects.Geometry.Line line:
                return line.FromSpeckle();
            case Objects.Geometry.Polyline polyline:
                var result = polyline.TryGetPolygon(out var polygon, out var hyparPolyline);
                if (result) return polygon;
                else return hyparPolyline;
            case Objects.Geometry.Circle circle:
                return circle.FromSpeckle();
            case Objects.Geometry.Arc arc:
                return arc.FromSpeckle();
            case Objects.Geometry.Polycurve polycurve:
                return polycurve.FromSpeckle();

            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an Elements Geometry implementing ICurve to Speckle.
    /// </summary>
    /// <param name="curve"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static Objects.ICurve ToSpeckleICurve(this Elements.Geometry.Interfaces.ICurve curve)
    {
        switch (curve)
        {
            case Elements.Geometry.Line line:
                return line.ToSpeckle();
            case Elements.Geometry.Polyline polyline:
                return polyline.ToSpeckle();
            case Elements.Geometry.Circle circle:
                return circle.ToSpeckle();
            case Elements.Geometry.Arc arc:
                return arc.ToSpeckle();
            case Elements.Geometry.IndexedPolycurve polycurve:
                return polycurve.ToSpeckle();

            default:
                throw new NotImplementedException();
        }
    }
}

public static class BoxConversion
{
    /// <summary>
    /// Converts a Speckle Box to Elements.
    /// </summary>
    /// <param name="box"></param>
    /// <returns></returns>
    public static Elements.Geometry.Box FromSpeckle(this Objects.Geometry.Box box)
    {
        var min = new Vector3(
            box.xSize.start.GetValueOrDefault(),
            box.ySize.start.GetValueOrDefault(),
            box.zSize.start.GetValueOrDefault());
        var max = new Vector3(
            box.xSize.end.GetValueOrDefault(),
            box.ySize.end.GetValueOrDefault(),
            box.zSize.end.GetValueOrDefault());
        box.basePlane.FromSpeckle(out var transform);

        var result = new Elements.Geometry.Box(min, max);
        result.TransformBox(transform);
        return result;
    }

    /// <summary>
    /// Converts an Elements Box to Speckle.
    /// </summary>
    /// <param name="box"></param>
    /// <returns></returns>
    public static Objects.Geometry.Box ToSpeckle(this Elements.Geometry.Box box)
    {
        var xSize = new Interval(box.Min.X, box.Max.X);
        var ySize = new Interval(box.Min.Y, box.Max.Y);
        var zSize = new Interval(box.Min.Z, box.Max.Z);

        return new Objects.Geometry.Box(
            box.Transform.ToSpeckle(),
            xSize,
            ySize,
            zSize);
    }
}

public static class MeshConversion
{
    /// <summary>
    /// Converts a Speckle Mesh to Elements.
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns></returns>
    public static Elements.Geometry.Mesh FromSpeckle(this Objects.Geometry.Mesh mesh)
    {
        var vertices = mesh.vertices.FromSpeckle().ToList();
        var triangles = new List<Triangle>();

        int i = 0;
        while (i < mesh.faces.Count)
        {
            int index = mesh.faces[i];
            if (index < 3)
            {
                index += 3; // 0 -> 3, 1 -> 4
            }

            if (index == 3)
            {
                // triangle
                var a = new Elements.Geometry.Vertex(vertices[mesh.faces[i + 1]]);
                var b = new Elements.Geometry.Vertex(vertices[mesh.faces[i + 2]]);
                var c = new Elements.Geometry.Vertex(vertices[mesh.faces[i + 3]]);
                triangles.Add(new Triangle(a, b, c));
            }
            else
            {
                // quad & n-gon
                var triangulateds = MeshTriangulationHelper.TriangulateFace(i, mesh, false);
                var faceIndices = new List<int>(triangulateds.Count);
                for (int t = 0; t < triangulateds.Count; t += 3)
                {
                    var a = new Elements.Geometry.Vertex(vertices[triangulateds[t]]);
                    var b = new Elements.Geometry.Vertex(vertices[triangulateds[t + 1]]);
                    var c = new Elements.Geometry.Vertex(vertices[triangulateds[t + 2]]);
                    triangles.Add(new Triangle(a, b, c));
                }
            }

            i += index + 1;
        }

        return new Elements.Geometry.Mesh(vertices.Select(vector => new Elements.Geometry.Vertex(vector)).ToList(), triangles);
    }

    /// <summary>
    /// Converts an Elements Mesh to Speckle.
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns></returns>
    public static Objects.Geometry.Mesh ToSpeckle(this Elements.Geometry.Mesh mesh)
    {
        var vertices = mesh.Vertices.Select(vertex => vertex.Position).ToArray().ToList();

        var faces = new List<int>(mesh.Triangles.Count * 3);
        foreach (var triangle in mesh.Triangles)
        {
            faces.Add(triangle.Vertices[0].Index);
            faces.Add(triangle.Vertices[1].Index);
            faces.Add(triangle.Vertices[2].Index);
        }

        return new Objects.Geometry.Mesh(vertices, faces);
    }
}

public static class BrepConversion
{
    /// <summary>
    /// Converts a Speckle Loop to Elements. Note: Polygonizes all curves.
    /// </summary>
    /// <param name="loop"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Elements.Geometry.Polygon? FromSpeckle(this Objects.Geometry.BrepLoop loop)
    {
        var trimCurves = loop.Trims
            .Select(trim => loop.Brep.Edges[trim.EdgeIndex])
            .Select(edge => loop.Brep.Curve3D[edge.Curve3dIndex])
            .Select(curve => curve.FromSpeckleICurve())
            .ToList();

        trimCurves = trimCurves.OrderBy(curve => curve.Length()).ToList();

        if (trimCurves.JoinSegments(out var polyCurve))
        {
            try
            {
                return new Polygon(polyCurve.Vertices);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
        else throw new Exception("Error: Loop could not be transformed into singular Polygon.");
    }

    public static Elements.Geometry.Profile FromSpeckle(this Objects.Geometry.BrepFace face)
    {
        List<Polygon> innerLoops = null;
        Polygon outerLoop = null;
        foreach (var loop in face.Loops)
        {
            if (loop.Type == Objects.Geometry.BrepLoopType.Outer)
            {
                outerLoop = loop.FromSpeckle();
            }
            else if (loop.Type == Objects.Geometry.BrepLoopType.Inner)
            {
                if (innerLoops == null)
                {
                    innerLoops = new List<Polygon>();
                }

                var innerLoop = loop.FromSpeckle();
                innerLoops.Add(innerLoop);
            }
        }
        return new Elements.Geometry.Profile(outerLoop, innerLoops);
    }

    public static IList<Elements.Geometry.Profile> OrientFaces(this IEnumerable<Objects.Geometry.BrepFace> faces)
    {
        var profiles = faces.Select(face => face.FromSpeckle()).ToImmutableList();
        var orienteds = new List<Elements.Geometry.Profile>();

        foreach (var profile in profiles)
        {
            var i = 0;
            var ray = new Ray(profile.Perimeter.Centroid(), profile.Perimeter.Normal());
            var others = profiles.Remove(profile);
            foreach (var other in others)
            {
                ray.Intersects(other.Perimeter, out var point, out Elements.Geometry.Containment containment);
                if (containment == Elements.Geometry.Containment.Inside) i++;
            }

            if (i % 2 != 0)
            {
                orienteds.Add(profile.Reversed());
            }
            else
            {
                orienteds.Add(profile);
            }
        }
        return orienteds;
    }


    /// <summary>
    /// Converts a Speckle Brep to an Elements Constructed Solid. Note: Currently assumes all faces are planar
    /// </summary>
    /// <param name="brep"></param>
    /// <returns></returns>
    public static Elements.Geometry.Solids.ConstructedSolid FromSpeckle(this Objects.Geometry.Brep brep)
    {
        var solid = new Elements.Geometry.Solids.Solid();
        foreach (var profile in brep.Faces.OrientFaces())
        {
            var outerLoop = profile.Perimeter;
            var innerLoops = profile.Voids;
            solid.AddFace(outerLoop, innerLoops, true);
        }
        return new Elements.Geometry.Solids.ConstructedSolid(solid);
    }
}
