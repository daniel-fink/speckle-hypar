using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.BuiltElements.Revit.Curve;
using Objects.Geometry;
using Objects.Other;
using Objects.Primitive;
using Objects.Structural.Geometry;

using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

using Elements;
using Elements.Geometry;
using Polyline = Elements.Geometry.Polyline;


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
                    else throw new NotImplementedException("Error: Can only sort Line and Arc curve types.");
                }
                else if (segments.First().Start.IsAlmostEqualTo(next.Start))
                {
                    if (next is Elements.Geometry.Line line) segments.Insert(0, line.Reversed());
                    else if (next is Elements.Geometry.Arc arc) segments.Insert(0, arc.Reversed());
                    else throw new NotImplementedException("Error: Can only sort Line and Arc curve types.");
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
            polycurves.Add(new IndexedPolycurve(segments));
        }
        return polycurves;
    }
}
//
// public static class UnitsConversion
// {
//     public static string ToSpeckle(this Elements.Units.LengthUnit lengthUnit)
//     {
//         return lengthUnit switch
//         {
//             Elements.Units.LengthUnit.Millimeter => "mm",
//             Elements.Units.LengthUnit.Centimeter => "cm",
//             Elements.Units.LengthUnit.Meter => "m",
//             Elements.Units.LengthUnit.Kilometer => "km",
//
//             Elements.Units.LengthUnit.Inch => "in",
//             Elements.Units.LengthUnit.Foot => "ft",
//             _ => "m"
//         };
//     }
//
//     public static Elements.Units.LengthUnit ToNative(this string units)
//     {
//         return units switch
//         {
//             "mm" => Elements.Units.LengthUnit.Millimeter,
//             "cm" => Elements.Units.LengthUnit.Centimeter,
//             "m" => Elements.Units.LengthUnit.Meter,
//             "km" => Elements.Units.LengthUnit.Kilometer,
//
//             "in" => Elements.Units.LengthUnit.Inch,
//             "ft" => Elements.Units.LengthUnit.Foot,
//             _ => Elements.Units.LengthUnit.Meter
//         };
//     }
// }
// public partial class ConverterHypar
// {
//     private double ScaleToNative(double value, string units)
//     {
//         var f = Speckle.Core.Kits.Units.GetConversionFactor(units, this.ModelUnits.ToSpeckle());
//         return value * f;
//     }
// }
//
//
