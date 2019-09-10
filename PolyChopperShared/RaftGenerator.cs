using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using PolyChopper.Containers;

namespace PolyChopper
{
    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// This class is responsible for generating a raft according to imported parameters if needed
    /// </summary>
    static class RaftGenerator
    {
        /// <summary>
        /// This method calculates the skirt from imported parameters
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void generateRaft()
        {
            Logger.logProgress("Generating raft");

            if (!Global.Values.shouldRaft || Global.Values.raftCount < 1)
                return;

            Polygons raftOutlines = generateRaftOutline();

            //Global.Values.layerComponentList = newLayerList;
            Global.Values.layerCount += Global.Values.raftCount;

            //To calculate the raft we will trim the vertical and horisontal grids with the raft density using a calculated raft outline

            var raftGrid = generateRaftGrid(raftOutlines, Global.Values.raftDensity);//Global.Values.infillGrids[0.3f];

            var lLines = InfillGenerator.clipLinesInPolygons(raftGrid.leftLines, raftOutlines);
            var rLines = InfillGenerator.clipLinesInPolygons(raftGrid.rightLines, raftOutlines);

            for (ushort i = 0; i < Global.Values.raftCount; i++)
            {
                LayerComponent raftLayer = new LayerComponent(i);
                Island raftIsland = new Island();
                LayerSegment raftSegment = new LayerSegment(SegmentType.RaftSegment);
                raftSegment.segmentSpeed = (i == 0) ? Global.Values.initialLayerSpeed : Global.Values.raftSpeed;
                raftSegment.fillLines = new List<LineSegment>();
                raftSegment.fillLines.AddRange(lLines);
                raftSegment.fillLines.AddRange(rLines);
                raftSegment.outlinePolygons = raftOutlines;
                raftIsland.segmentList.Add(raftSegment);
                raftIsland.outlinePolygons = raftOutlines; //This is so that the skirt can still be calculated
                raftLayer.islandList.Add(raftIsland);

                Global.Values.layerComponentList.Insert(i, raftLayer);
            }
        }
        
        /// <summary>
        /// This method generates the outline of the raft that will be used to trim the grids
        /// </summary>
        /// <returns>The outline of the raft</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Polygons generateRaftOutline()
        {
            //To generate the outline of the raft we need to calculate the combined outline of all the islands of the first layer and then offset the result by
            //the raft distance

            Polygons raftOutline = new Polygons();

            //First combine the outlines of all the islands on the first layer
            foreach (Island island in Global.Values.layerComponentList[0].islandList)
            {
                //No idea why but sometimes the island has an outline segment but the value of that segment is not stored in
                //in the island itself, we should then fix that before we create the raft
                if (island.outlinePolygons.Count < 1)
                {
                    foreach (LayerSegment _segment in island.segmentList)
                    {
                        if (_segment.segmentType == SegmentType.OutlineSegment)
                        {
                            island.outlinePolygons = _segment.outlinePolygons;
                        }
                    }
                }

                Clipper islandClipper = new Clipper();
                islandClipper.AddPaths(raftOutline, PolyType.ptSubject, true);
                islandClipper.AddPaths(island.outlinePolygons, PolyType.ptClip, true);

                islandClipper.Execute(ClipType.ctUnion, raftOutline);
            }

            //The outlines on the islands are probably to small because they represent the inside outlines of each layer
            //we therefore need to increase the offset amount by the amount that they have by now already shrunk

            var offset = Global.Values.nozzleWidth * Global.Values.shellThickness + Global.Values.raftDistance;

            Polygons result = new Polygons();
            ClipperOffset clipperOffset = new ClipperOffset();
            clipperOffset.AddPaths(raftOutline, JoinType.jtMiter, EndType.etClosedPolygon);
            clipperOffset.Execute(ref result, offset);

            return result;
        }
        
        //TODO: the raft grid generation methods should somehow be combined with those of the infill grid generator

        /// <summary>
        /// This method calculates the spacing distance required to achieve a desired density with a given
        /// filament width for a two directional grid
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

            uint spacingWidth = (uint)(nozzleWidth * 2 * x);

            return spacingWidth;
        }

        /// <summary>
        /// This method generates the infill for a grid with a size determined from the specified outline polygons
        /// </summary>
        /// <param name="raftOutline">The outline of the raft</param>
        /// <param name="density">The density that the grid should have</param>
        /// <returns>The infillGrid for the raft</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InfillGrid generateRaftGrid(Polygons raftOutline, float density)
        {
            //Start by calculating the endpoints of the raft

            long raftMinX = Global.Values.modelMinX - Global.Values.raftDistance * 2;
            long raftMaxX = Global.Values.modelMaxX + Global.Values.raftDistance * 2;
            long raftMinY = Global.Values.modelMinY - Global.Values.raftDistance * 2;
            long raftMaxY = Global.Values.modelMaxY + Global.Values.raftDistance * 2;

            Polygons rightList = new Polygons();
            Polygons leftList = new Polygons();

            uint spacing = caluclateNeededSpacing(density, Global.Values.nozzleWidth);
            uint divider = (uint)(Global.Values.nozzleWidth + spacing);

            uint amountOfLines = 0;

            //2 points of line segments
            IntPoint p1 = new IntPoint();
            IntPoint p2 = new IntPoint();

            //We need to start creating diagonal lines before the min x sothat there are lines over every part of the model
            long xOffset = (long)((raftMaxY - raftMinY) / Math.Tan(MathUtils.MathHelper.ToRadians(45)));
            raftMinX -= xOffset;

            amountOfLines = (uint)((raftMaxX - raftMinX) / divider);

            //Calculate the right and left line simeltaniously
            for (uint i = 0; i < amountOfLines; i++)
            {
                //First the right angled line
                p1.X = raftMinX + i * divider +(Global.Values.nozzleWidth / 2);
                p1.Y = raftMinY;
                p2.X = p1.X;// +xOffset;
                p2.Y = raftMaxY;

                Polygon line = new Polygon();
                line.Add(p1); line.Add(p2);
                rightList.Add(line);
            }

            amountOfLines = (uint)((raftMaxY - raftMinY) / divider);

            //Calculate the right and left line simeltaniously
            for (uint i = 0; i < amountOfLines; i++)
            {
                p1.Y = raftMinY + i * divider + (Global.Values.nozzleWidth / 2);
                p1.X = raftMinX;
                p2.Y = p1.Y;// +xOffset;
                p2.X = raftMaxX;

                Polygon line = new Polygon();
                line.Add(p1); line.Add(p2);
                leftList.Add(line);
            }

            return new InfillGrid(rightList, leftList);
        }
    }
}
