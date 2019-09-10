using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolyChopper.Containers;
using ClipperLib;
using MathUtils;

namespace PolyChopper
{
    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;
    using System.Runtime.CompilerServices;

    public static class LayerSlicer
    {
        /// <summary>
        /// This method slices all the initial triangles into 2d layers
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sliceTrianglesIntoLayers()
        {
            Logger.logProgress("Slicing triangles into layers");

            //If in plotting mode we do not work with triangles
            if (Global.Values.materialType == MaterialType.Pen)
                return;

            long zPoint;
            List<LineSegment> lineList;
            Dictionary<int, int> faceToLineIndex ;

            for (int i = 0; i < Global.Values.layerCount; i++)
            {
                lineList = new List<LineSegment>();
                faceToLineIndex = new Dictionary<int, int>();

                zPoint = i * Global.Values.layerHeight + (Global.Values.layerHeight / 2);

                for (int j = 0; j < Global.Values.initialTriangleList.Count; j++)
                {
                    var triangle = Global.Values.initialTriangleList[j];

                    if (triangle.checkIfContainsZ(zPoint))
                    {
                        //We do not want dots
                        var temp = triangle.calculateZSLicePoints(zPoint);
                        if (!temp.Point1.Equals(temp.Point2))
                        {
                            faceToLineIndex.Add(j, lineList.Count);
                            temp.triangleIndex = j;
                            lineList.Add(temp);
                        }
                    }
                }
                
                var layerComponent = new LayerComponent(i);
                layerComponent.initialLineList = lineList;
                layerComponent.faceToLineIndex = faceToLineIndex;
                Global.Values.layerComponentList.Add(layerComponent);
            }
        }
    }
}
