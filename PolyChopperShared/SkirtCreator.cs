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

    class SkirtCreator
    {
        /// <summary>
        /// This method generates the skirt according to the imported settings
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void generateSkirt()
        {
            Logger.logProgress("Generating skirt");

            if (!Global.Values.shouldSkirt || Global.Values.skirtCount < 1)
                return;

            Polygons skirtpolygons = new Polygons();

            //Skirt should only be calculated for the first layer
            //We start by combining all the islands in the first layer into one polygon

            Clipper clipper = new Clipper();

            foreach (Island island in Global.Values.layerComponentList[0].islandList)
            {
                //No idea why but sometimes the island has an outline segment but the value of that segment is not stored in
                //in the island itself, we should then fix that before we create the skirt
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

                if (island.outlinePolygons.Count < 1)
                    continue;

                clipper.AddPaths(island.outlinePolygons, PolyType.ptClip, true);
            }

            Polygons combinedIslands = new Polygons();
            clipper.Execute(ClipType.ctUnion, combinedIslands);

            var offset = (Global.Values.shouldRaft && Global.Values.raftCount > 0) ? 0 : Global.Values.shellThickness * Global.Values.nozzleWidth;

            Polygons initialSkirt = new Polygons();
            ClipperOffset clipperOffset = new ClipperOffset();
            clipperOffset.AddPaths(combinedIslands, JoinType.jtMiter, EndType.etClosedPolygon);
            clipperOffset.Execute(ref initialSkirt, Global.Values.skirtDistance + offset);

            Island tempIsland = new Island();
            LayerSegment segment = new LayerSegment(SegmentType.SkirtSegment);
            segment.outlinePolygons = new Polygons(initialSkirt);
            segment.segmentSpeed = Global.Values.skirtSpeed;

            for (ushort i = 1; i < Global.Values.skirtCount; i++)
            {
                clipperOffset = new ClipperOffset();
                clipperOffset.AddPaths(combinedIslands, JoinType.jtMiter, EndType.etClosedPolygon);
                clipperOffset.Execute(ref initialSkirt, Global.Values.skirtDistance + offset + (Global.Values.nozzleWidth * i));
                segment.outlinePolygons.AddRange(new Polygons(initialSkirt));
            }

            tempIsland.segmentList.Add(segment);

            //Add the skirt island to the bottom layer
            Global.Values.layerComponentList[0].islandList.Insert(0, tempIsland);
        }
    }
}
