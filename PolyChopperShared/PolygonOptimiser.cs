using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using MathUtils;
using PolyChopper.Containers;

namespace PolyChopper
{
    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// This class is responsible for optimising a polygon (be enforcing a minimum distance) so that the resolution
    /// does not overwhelm clipper
    /// </summary>
    public static class PolygonOptimiser
    {
        /// <summary>
        /// This method optimises all the outline polygons by ensuring the distance between two points is larger than the minimum
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void optimiseOutlinePolygons()
        {
            foreach (LayerComponent layer in Global.Values.layerComponentList)
            {
                foreach (Island island in layer.islandList)
                {
                    for (int i = 0; i < island.outlinePolygons.Count; i++)
                        island.outlinePolygons[i] = optimisePolygon(island.outlinePolygons[i]);
                }
            }
        }

        /// <summary>
        /// This method optimises a polygon by ensuring that the distance between two consecutive points is never less than specified
        /// minimum distance
        /// </summary>
        /// <param name="polygon">The polygon to optimise</param>
        /// <returns>The optimised polygon</returns>
        private static Polygon optimisePolygon(Polygon polygon)
        {
            //This method is slightly adapted from one in Cura, I am not sure how it works
            Vector2 p0 = new Vector2(polygon[polygon.Count - 1]);

            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 p1 = new Vector2(polygon[i]);

                if (Vector2.DistanceSquared(p0, p1) < 100000)
                {
                    polygon.RemoveAt(i);
                    i--;
                }
                /*else
                {
                    p0 = p1;
                }*/
                else
                {
                    Vector2 p2;

                    if (i < polygon.Count - 1)
                        p2 = new Vector2(polygon[i + 1]);
                    else
                        p2 = new Vector2(polygon[0]);

                    Vector2 diff0 = normal(p1 - p0, 100000);
                    Vector2 diff2 = normal(p1 - p2, 100000);

                    float d = Vector2.Dot(diff0, diff2);

                    if (d < -998888000000)
                    {
                        polygon.RemoveAt(i);
                        i--;
                    }
                    else
                        p0 = p1;
                }
            }

            return polygon;
        }

        /// <summary>
        /// This method has been adapted from Cura and the functionality is slightly unknown at the moment
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        private static Vector2 normal(Vector2 p0, int len)
        {
            float _len = Vector2.Distance(new Vector2(0, 0), p0);

            if (_len < 1)
                return new Vector2(len, 0);

            return p0 * len / _len;
        }
    }
}
