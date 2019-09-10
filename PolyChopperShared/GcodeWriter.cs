//#if GCODE_OUT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PolyChopper.Containers;
using ClipperLib;
using System.Runtime.CompilerServices;
using System.Globalization;

#if WINDOWS_APP || WINDOWS_PHONE_APP
using Windows.Storage;
#else
using System.IO.IsolatedStorage;
#endif

namespace PolyChopper
{
    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;
    
    /// <summary>
    /// This class is responsible for writing all gcode information to the specified file
    /// </summary>
    public static class GcodeWriter
    {
        private static float currentE = 0;

        private static float prevX = 0;
        private static float prevY = 0;
        private static float prevZ = 0;
        private static int prev0F = 0;
        private static int prev1F = 0;

        private static bool zLifted = false;
        private static bool retracted = false;

        private static StreamWriter streamWriter;

        private static void writeMove(MoveSegment segment)
        {
            if (segment.isRetraction)
            {
                //If in plotting mode we want to lift up the pen rather than retract
                if (Global.Values.materialType == MaterialType.Pen)
                {
                    zLifted = true;
                    streamWriter.WriteLine("G0 Z1");
                    return;
                }

                //The e position should always change so there is no need to check if it changed
                var e = " E" + (currentE - (float)segment.extrusionDistance / 1000000f).ToString(CultureInfo.InvariantCulture);
                var f = "";

                if (segment.feedrate != prev1F)
                {
                    prev1F = segment.feedrate;
                    f = " F" + prev1F / 1000000f * 60f;
                }

                streamWriter.WriteLine("G1" + e + f);

                retracted = true;
            }
            else if (segment.moveDistance > 0)
            {
                if (!segment.isExtruded)
                {
                    var x = "";
                    var y = "";
                    var z = "";
                    var f = "";

                    var newX = (float)segment.endPoint.X / 1000000f;
                    var newY = (float)segment.endPoint.Y / 1000000f;
                    var newZ = (float)segment.endPoint.Z / 1000000f;

                    if (newX != prevX)
                    {
                        prevX = newX;
                        x = " X" + prevX.ToString(CultureInfo.InvariantCulture);
                    }

                    if (newY != prevY)
                    {
                        prevY = newY;
                        y = " Y" + prevY.ToString(CultureInfo.InvariantCulture);
                    }

                    if (newZ != prevZ && !zLifted)
                    {
                        prevZ = newZ;
                        z = " Z" + prevZ.ToString(CultureInfo.InvariantCulture);
                    }

                    if (segment.feedrate != prev0F)
                    {
                        prev0F = segment.feedrate;
                        f = " F" + prev0F / 1000000f * 60f;
                    }

                    streamWriter.WriteLine("G0" + x + y + z + f);
                }
                else
                {
                    //If the printhead has retracted then we first need to get it back at the correct e before continuing
                    if (retracted)
                    {
                        streamWriter.WriteLine("G1 E" + currentE);
                        retracted = false;
                    }

                    //The e position should always change so there is no need to check if it changed
                    int layerNumber = (int)(segment.startPoint.Z / Global.Values.layerHeight) - 1;
                    currentE += (float)segment.extrusionDistance / 1000000f * Global.Values.layerComponentList[layerNumber].flowrate;//Global.Values.flowrates[layerNumber];

                    var x = "";
                    var y = "";
                    var z = "";
                    var e = " E" + currentE.ToString(CultureInfo.InvariantCulture);
                    var f = "";

                    var newX = (float)segment.endPoint.X / 1000000f;
                    var newY = (float)segment.endPoint.Y / 1000000f;
                    var newZ = (float)segment.endPoint.Z / 1000000f;

                    //Put the pen down again if it was lifted
                    if (zLifted)
                    {
                        streamWriter.WriteLine("G1 Z" + newZ.ToString(CultureInfo.InvariantCulture));
                        zLifted = false;
                    }

                    if (newX != prevX)
                    {
                        prevX = newX;
                        x = " X" + prevX.ToString(CultureInfo.InvariantCulture);
                    }

                    if (newY != prevY)
                    {
                        prevY = newY;
                        y = " Y" + prevY.ToString(CultureInfo.InvariantCulture);
                    }

                    if (newZ != prevZ/* || zLifted*/)
                    {
                        prevZ = newZ;
                        z = " Z" + prevZ.ToString(CultureInfo.InvariantCulture);
                    }

                    if (segment.feedrate != prev1F)
                    {
                        prev1F = segment.feedrate;
                        f = " F" + prev1F / 1000000f * 60f;
                    }

                    streamWriter.WriteLine("G1" + x + y + z + e + f);

                    //zLifted = false;
                }
            }
        }

        public static void writeGCode(string filePath)
        {
            streamWriter = new StreamWriter(filePath);

            //Determine the hours minutes and seconds in the printing time estimate
            //long hours, mins;
            long secs = Global.Values.printingTimeEstimate;
            long hours = secs / 3600;
            secs -= hours * 3600;
            long mins = secs / 60;
            secs -= mins * 60;

            //streamWriter.WriteLine(Global.Values.startCode);
            streamWriter.WriteLine(";Total amount of layers: " + Global.Values.layerCount);
            streamWriter.WriteLine(";Estimated time: " + hours + "h " + mins + "m " + secs + "s");
            streamWriter.WriteLine(";Estimated filament: " + ((float)Global.Values.filamentUsageEstimate / 1000f) + "kg");
            streamWriter.WriteLine(";Estimated cost: " + Global.Values.printingCostEstimate + "c");
            streamWriter.WriteLine("G21");
            streamWriter.WriteLine("G90");
            streamWriter.WriteLine("G28 X0 Y0 Z0");
            if (Global.Values.printingTemperature != -1)
                streamWriter.WriteLine("M109 T0 S" + Global.Values.printingTemperature);
            streamWriter.WriteLine("G92 E0");
            streamWriter.WriteLine("G1 F600");



            currentE = 0;
            prevX = 0;
            prevY = 0;
            prevZ = 0;
            prev0F = 0;
            prev1F = 0;
            zLifted = false;
            retracted = false;

            int layerNum = 0;
            float ex = 0;

            foreach (LayerComponent layer in Global.Values.layerComponentList)
            {
                streamWriter.WriteLine(";Layer: " + layer.layerNumber);
                layerNum++;

                /*foreach (Island island in layer.islandList)
                {
                    foreach (Polygon polygon in island.outlinePolygons)
                    {
                        streamWriter.Write("G1 X" + polygon[0].X / 1000000.0);
                        streamWriter.Write(" Y" + polygon[0].Y / 1000000.0);
                        streamWriter.Write(" Z" + Global.Values.layerHeight / 1000000.0 * layerNum);
                        streamWriter.WriteLine(" F100");

                        for (int i = 1; i < polygon.Count; i++)
                        {
                            ex += 0.1f;
                            streamWriter.Write("G1 X" + polygon[i].X / 1000000.0);
                            streamWriter.Write(" Y" + polygon[i].Y / 1000000.0);
                            streamWriter.Write(" Z" + Global.Values.layerHeight / 1000000.0 * layerNum);
                            streamWriter.Write(" E" + ex);
                            streamWriter.WriteLine(" F100");
                        }

                        ex += 0.1f;
                        streamWriter.Write("G1 X" + polygon[0].X / 1000000.0);
                        streamWriter.Write(" Y" + polygon[0].Y / 1000000.0);
                        streamWriter.Write(" Z" + Global.Values.layerHeight / 1000000.0 * layerNum);
                        streamWriter.Write(" E" + ex);
                        streamWriter.WriteLine(" F100");
                    }
                }*/

                /*foreach (LineSegment line in layer.initialLineList)
                {
                    streamWriter.Write("G1 X" + line.Point1.X / 1000000.0);
                    streamWriter.Write(" Y" + line.Point1.Y / 1000000.0);
                    streamWriter.Write(" Z" + Global.Values.layerHeight / 1000000.0 * layerNum);
                    streamWriter.WriteLine(" F100");
                    
                    ex += 0.1f;
                    streamWriter.Write("G1 X" + line.Point2.X / 1000000.0);
                    streamWriter.Write(" Y" + line.Point2.Y / 1000000.0);
                    streamWriter.Write(" Z" + Global.Values.layerHeight / 1000000.0 * layerNum);
                    streamWriter.Write(" E" + ex);
                    streamWriter.WriteLine(" F100");
                }*/

                foreach (MoveSegment move in layer.intialLayerMoves)
                {
                    writeMove(move);
                }

                foreach (Island island in layer.islandList)
                {
                    streamWriter.WriteLine(";Island");

                    foreach (LayerSegment segment in island.segmentList)
                    {
                        streamWriter.WriteLine(";Segment: " + segment.segmentType.ToString());

                        foreach (MoveSegment move in segment.moveSegments)
                        {
                            writeMove(move);
                        }
                    }
                }
            }


            //streamWriter.WriteLine(Global.Values.endCode);
            //streamWriter.Flush();
            streamWriter.WriteLine("M104 S0");
            streamWriter.WriteLine("G91");
            streamWriter.WriteLine("G1 Z+0.5 E-5 X-15 Y-15 F4800");
            streamWriter.WriteLine("G28 X0 Y0");

            streamWriter.Flush();
            streamWriter.Dispose();
        }
    }
}

//#endif //GCODE_OUT