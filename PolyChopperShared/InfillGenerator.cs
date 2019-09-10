using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using PolyChopper.Containers;
using System.Diagnostics;
using MathUtils;

namespace PolyChopper
{
    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// This class contains all methods and variables needed for calculating and trimming the infill grids needed for the creation of
    /// infill
    /// </summary>
    public static class InfillGenerator
    {
        /// <summary>
        /// This method generated all the required infill grids
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void generateInfillGrids()
        {
            Logger.logProgress("Generating infill grids");

            List<float> requiredDensities = new List<float>();

            //Add the normal infill density
            requiredDensities.Add(Global.Values.normalInfillDensity);

            //Add the solid infill desnity (for top and bottom) only if the value is not present yet
            if (!requiredDensities.Contains(1))
                requiredDensities.Add(1);

            //Add the raft density if there should be raft
            if (Global.Values.shouldRaft && !requiredDensities.Contains(Global.Values.raftDensity))
                requiredDensities.Add(Global.Values.raftDensity);

            foreach (LayerComponent layer in Global.Values.layerComponentList)//Global.Values.infillDensities.Values)
            {
                if (!requiredDensities.Contains(layer.infillDensity))
                    requiredDensities.Add(layer.infillDensity);
            }

            foreach (LayerComponent layer in Global.Values.layerComponentList)
            {
                if (!requiredDensities.Contains(layer.supportDensity))
                    requiredDensities.Add(layer.supportDensity);
            }

            foreach (float requiredDensity in requiredDensities)
            {
                Global.Values.infillGrids.Add(requiredDensity, generateInfillGrid(requiredDensity));
            }
        }
        
        /// <summary>
        /// This method generates an infill grid consisting of line segments accoring to a specified angle and density
        /// </summary>
        /// <param name="density">Hoe dense the grid should be</param>
        /// <returns>The infill grid consisting of line segments</returns>
        public static InfillGrid generateInfillGrid(float density, double angle = 45)
        {
            Polygons rightList = new Polygons();
            Polygons leftList = new Polygons();

            uint spacing = caluclateNeededSpacing(density, Global.Values.nozzleWidth);
            uint divider = (uint)(Global.Values.nozzleWidth + spacing);

            uint amountOfLines = 0;

            //2 points of line segments
            IntPoint p1 = new IntPoint(); 
            IntPoint p2 = new IntPoint();

            //We need to start creating diagonal lines before the min x sothat there are lines over every part of the model
            long xOffset = (long)((Global.Values.modelMaxY - Global.Values.modelMinY) / Math.Tan(angle));
            var modMinX = Global.Values.modelMinX - xOffset;

            amountOfLines = (uint)((Global.Values.modelMaxX - modMinX) / divider);

            //Calculate the right and left line simultaniously
            for (uint i = 0; i < amountOfLines; i++)
            {
                //First the right angled line
                p1.X = modMinX + i * divider + (Global.Values.nozzleWidth / 2);
                p1.Y = Global.Values.modelMinY;
                p2.X = p1.X + xOffset;
                p2.Y = Global.Values.modelMaxY;

                //We make use of duplicated points sothat the below changes do not affect this line
                Polygon line = new Polygon();
                line.Add(new IntPoint(p1)); line.Add(new IntPoint(p2));
                rightList.Add(line);

                p2.Y = Global.Values.modelMinY;
                p1.Y = Global.Values.modelMaxY;

                line = new Polygon();
                line.Add(p1); line.Add(p2);
                leftList.Add(line);
            }

            return new InfillGrid(rightList, leftList);
        }

        /// <summary>
        /// This method calculates the spacing distance required to achieve a desired density with a given
        /// filament width
        /// </summary>
        /// <param name="density">The desired desnity % as a value out of 1.0</param>
        /// <param name="nozzleWidth">The nozzle width</param>
        /// <returns>The spcaing width in the same measurement units as the specified filament width</returns>
        private static uint caluclateNeededSpacing(float density, int nozzleWidth)
        {
            if (density <= 0)
                return uint.MaxValue;

            //d% = 1 / (x% + 1)
            //d% * x + d% = 1
            //a + d% = 1
            //a = 1 - d%
            //x = a / d%

            float a = 1 - density;
            float x = a / density;

            uint spacingWidth = (uint)(nozzleWidth * x);

            return spacingWidth;
        }

        /// <summary>
        /// This method trims infill grids into all infill segments
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void trimInfillGridsToFillSegments()
        {
            Logger.logProgress("Filling segments with grids");
            bool right = false;

            //for (int i = 0; i < Global.Values.layerCount; i++)
            foreach (LayerComponent layer in Global.Values.layerComponentList)
            {
                foreach (Island island in layer.islandList)
                {
                    foreach (LayerSegment segment in island.segmentList)
                    {
                        List<LineSegment> infillSegments = new List<LineSegment>();

                        if (segment.segmentType == SegmentType.InfillSegment)
                        {
                            //If the segment is an infill segment then we need to trim the correlating infill grid to fill it
                            float density = layer.infillDensity;

                            if (right)
                                infillSegments = clipLinesInPolygons(Global.Values.infillGrids[density].rightLines, segment.outlinePolygons);
                            else
                                infillSegments = clipLinesInPolygons(Global.Values.infillGrids[density].leftLines, segment.outlinePolygons);

                            segment.fillDensity = density;
                        }
                        else if (segment.segmentType == SegmentType.BottomSegment || segment.segmentType == SegmentType.TopSegment)
                        {
                            //If this is a top or bottom segment then we need to trim the solid infill grid to fill it
                            if (right)
                                infillSegments = clipLinesInPolygons(Global.Values.infillGrids[1].rightLines, segment.outlinePolygons);
                            else
                                infillSegments = clipLinesInPolygons(Global.Values.infillGrids[1].leftLines, segment.outlinePolygons);

                            segment.fillDensity = 1;
                        }
                        else if (segment.segmentType == SegmentType.SupportSegment)
                        {
                            //If this is a support segment then we need to trim the support infill grid to fill it
                            float density = layer.supportDensity;

                            infillSegments = clipLinesInPolygons(Global.Values.infillGrids[density].leftLines, segment.outlinePolygons);

                            segment.fillDensity = density;
                        }
                        else
                            continue;

                        segment.fillLines = infillSegments;
                    }
                }

                right = !right;
            }
        }

        /// <summary>
        /// This method clips a list of lines inside polygons
        /// </summary>
        /// <param name="lines">The list of lines to clip</param>
        /// <param name="polygons">The polygons in which to clip</param>
        /// <returns>The list of lines clipped to intersect with the polygons</returns>
        public static List<LineSegment> clipLinesInPolygons(Polygons lines, Polygons polygons)
        {
            List<LineSegment> lineList = new List<LineSegment>();

            Clipper clipper = new Clipper();

            clipper.AddPaths(lines, PolyType.ptSubject, false);

            clipper.AddPaths(polygons, PolyType.ptClip, true);

            PolyTree result = new PolyTree();

            clipper.Execute(ClipType.ctIntersection, result);

            foreach (PolyNode node in result.Childs)
            {
                if (node.Contour.Count == 2)
                {
                    //Make sure the infill line is longer than at least double the nozzlewidth

                    LineSegment line = new LineSegment(node.Contour[0], node.Contour[1]);

                    if (line.length < Global.Values.nozzleWidth * 2)
                        continue;

                    lineList.Add(line);
                }
            }

            return lineList;
        }
    }
}
