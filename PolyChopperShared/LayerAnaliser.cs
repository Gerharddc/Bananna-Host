using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolyChopper.Containers;
using ClipperLib;
using MathUtils;

using System.Diagnostics;

namespace PolyChopper
{
    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// This class is responsible for generating the islands for the model from the original lines
    /// as well as calculating the outlines of all the types of segments
    /// </summary>
    public static class LayerAnaliser
    {
        /// <summary>
        /// This method calculates the islands for each layercomponent from the initial linesegments that were calculated for it
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void calculateIslandsFromOriginalLines()
        {
            Logger.logProgress("Calculating islands");

            Debug.WriteLine("Total: " + Global.Values.layerCount);

            //If in plotter mode we only have to calculate the islands for one layer from the imported polygons
            /*if (Global.Values.materialType == MaterialType.Pen)
            {
                Logger.logProgress("Calculating islands");

                LayerComponent component = new LayerComponent(0);
                component.islandList = calculateIslandFromPolygons(Global.Values.svgPolygons);

                Global.Values.layerComponentList = new List<LayerComponent>();
                Global.Values.layerComponentList.Add(component);

                return;
            }*/

            for (int i = 0; i < Global.Values.layerComponentList.Count; i++)
            {
                Debug.WriteLine(i);
                Logger.logProgress("Island: " + (i + 1));

                Global.Values.layerComponentList[i].islandList = calculateIslandsFromLineList(Global.Values.layerComponentList[i].initialLineList, Global.Values.layerComponentList[i].faceToLineIndex);
            }
        }

        /// <summary>
        /// This method calculates a list of islands from a list of polygons
        /// </summary>
        /// <param name="polygons">The list of polygons to calculate the islands from</param>
        /// <returns>The calculated list of islands</returns>
        private static List<Island> calculateIslandFromPolygons(Polygons polygons)
        {
            List<Island> islandList = new List<Island>();

            //We now need to put the polygons through clipper sothat it can detect holes for us
            //and then make proper islands with the returned data
            PolyTree resultTree = new PolyTree();

            Clipper clipper = new Clipper();
            clipper.AddPaths(polygons, PolyType.ptClip, true);
            clipper.Execute(ClipType.ctUnion, resultTree, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

            List<PolyNode> polyNodeList = moveNodesToSameLevel(resultTree);

            foreach (PolyNode polyNode in polyNodeList)
            {
                if (!polyNode.IsHole)
                {
                    Island temp = new Island();
                    temp.outlinePolygons.Add(polyNode.Contour);
                    islandList.Add(temp);
                }
                else //IsHole
                {
                    bool shouldBreak = false;

                    foreach (Island island in islandList)
                    {
                        if (shouldBreak)
                            break;

                        foreach (Polygon islandPolygon in island.outlinePolygons)
                        {
                            if (polyNode.Parent.Contour.Equals(islandPolygon))
                            {
                                island.outlinePolygons.Add(polyNode.Contour);
                                shouldBreak = true;
                                break;
                            }
                        }
                    }
                }
            }

            //Lastly we return our list of islands
            return islandList;
        }

        /// <summary>
        /// This method calculates the outline polygons of a layer from a list of lines and a list of triangles in which it has been detremined
        /// which triangles touch each other on which side. A list containing the indices of line segment in relation to the indices of the triangles
        /// they were created from is also required.
        /// </summary>
        /// <param name="lineList">The list of linesegments that the layer is made up of.</param>
        /// <param name="faceToLineIndex">A decitionary specifiying which line is made from which triangle</param>
        /// <returns>The list of islands whith their outline polygons for the layer</returns>
        private static List<Island> calculateIslandsFromLineList(List<LineSegment> lineList, Dictionary<int, int> faceToLineIndex)
        {
            //If there are less than two lines then there cannot be any polygons
            if (lineList.Count < 2)
                return new List<Island>();

            //First we create the list of islands that will ultimately be returned
            List<Island> islandList = new List<Island>();

            //We also need a list of polygons which have already been closed and those that still need closing
            Polygons closedPolygons = new Polygons();
            Polygons openPolygons = new Polygons();

            for (int startLine = 0; startLine < lineList.Count; startLine++)
            {
                //Check if the line has lready been used
                if (lineList[startLine].usedInPolygon)
                    continue;

                Polygon polygon = new Polygon();
                polygon.Add(lineList[startLine].Point1);

                int lineIndex = startLine;
                bool closed = false;

                while (!closed)
                {
                    closed = false;
                    lineList[lineIndex].usedInPolygon = true;
                    IntPoint p1 = lineList[lineIndex].Point2;
                    polygon.Add(p1);

                    int nextIndex = -1;
                    Triangle triangle = Global.Values.initialTriangleList[lineList[lineIndex].triangleIndex];

                    for (int i = 0; i < 3; i++)
                    {
                        int touchIndex = triangle.indexTouchingSide[i];

                        if (!faceToLineIndex.ContainsKey(touchIndex))
                            continue;

                        IntPoint p2 = lineList[faceToLineIndex[touchIndex]].Point1;
                        double diff = LineSegment.squaredDistanceBetweenPoints(p1, p2);

                        if (diff < 10000) //100 * 100
                        {
                            if (faceToLineIndex[touchIndex] == startLine)
                            {
                                closed = true;
                                break;
                            }

                            if (lineList[faceToLineIndex[touchIndex]].usedInPolygon)
                                continue;

                            nextIndex = faceToLineIndex[touchIndex];
                            break;
                        }
                        
                        p2 = lineList[faceToLineIndex[touchIndex]].Point2;
                        diff = LineSegment.squaredDistanceBetweenPoints(p1, p2);

                        if (diff < 10000)
                        {
                            lineList[faceToLineIndex[touchIndex]].swapPoints();

                            if (faceToLineIndex[touchIndex] == startLine)
                            {
                                closed = true;
                                break;
                            }

                            if (lineList[faceToLineIndex[touchIndex]].usedInPolygon)
                                continue;

                            nextIndex = faceToLineIndex[touchIndex];
                            break;
                        }
                    }

                    if (nextIndex == -1)
                        break;

                    lineIndex = nextIndex;
                }

                if (closed)
                    closedPolygons.Add(polygon);
                else
                    openPolygons.Add(polygon);
            }

            //The list is no longer needed and can be removed to save memory
            lineList.Clear();

            for (int i = 0; i < openPolygons.Count; i++)
            {
                if (openPolygons[i].Count < 1)
                    continue;

                for (int j = 0; j < openPolygons.Count; j++)
                {
                    if (openPolygons[j].Count < 1)
                        continue;

                    IntPoint p1 = openPolygons[i][openPolygons[i].Count - 1];
                    IntPoint p2 = openPolygons[j][0];
                    double diff = LineSegment.squaredDistanceBetweenPoints(p1, p2);

                    if (diff < 200 * 200)
                    {
                        if (i == j)
                        {
                            closedPolygons.Add(new Polygon(openPolygons[i]));
                            openPolygons[i].Clear();
                            break;
                        }
                        else
                        {
                            openPolygons[i].AddRange(openPolygons[j]);

                            openPolygons[j].Clear();
                        }
                    }
                }
            }

            //TODO: combine the below and the above, also possibly add hashing

            while (true)
            {
                double bestDistance = double.PositiveInfinity;
                int bestA = -1;
                int bestB = -1;
                bool wrongWay = false;

                for (int i = 0; i < openPolygons.Count; i++)
                {
                    if (openPolygons[i].Count < 1)
                        continue;

                    for (int j = 0; j < openPolygons.Count; j++)
                    {
                        if (openPolygons[j].Count < 1)
                            continue;

                        IntPoint p1 = openPolygons[i][openPolygons[i].Count - 1];
                        IntPoint p2 = openPolygons[j][0];
                        double diff = LineSegment.squaredDistanceBetweenPoints(p1, p2);

                        if (diff < bestDistance)
                        {
                            bestDistance = diff;
                            bestA = i;
                            bestB = j;
                            wrongWay = false;
                        }

                        if (i != j)
                        {
                            p2 = openPolygons[j][openPolygons[j].Count - 1];
                            diff = LineSegment.squaredDistanceBetweenPoints(p1, p2);

                            if (diff < bestDistance)
                            {
                                bestDistance = diff;
                                bestA = i;
                                bestB = j;
                                wrongWay = true;
                            }
                        }
                    }
                }

                if (bestDistance == double.PositiveInfinity)
                    break;

                if (bestA == bestB)
                {
                    closedPolygons.Add(new Polygon(openPolygons[bestA]));
                    openPolygons[bestA].Clear();
                }
                else
                {
                    if (wrongWay)
                        openPolygons[bestB].Reverse();

                    openPolygons[bestA].AddRange(openPolygons[bestB]);

                    openPolygons[bestB].Clear();
                }
            }

            //We now need to put the newly created polygons through clipper sothat it can detect holes for us
            //and then make proper islands with the returned data
            return calculateIslandFromPolygons(closedPolygons);
        }
        
        /// <summary>
        /// This method moves all the polynodes inside a polytree to the same hierarchical position
        /// </summary>
        /// <param name="polyTree">The polytree that contains the polynodes</param>
        /// <returns>All the polynodes on the same hierarchical level</returns>
        private static List<PolyNode> moveNodesToSameLevel(PolyTree polyTree)
        {
            List<PolyNode> polyNodeList = new List<PolyNode>();

            foreach (PolyNode polyNode in polyTree.Childs)
            {
                polyNodeList.AddRange(moveNodesToSameLevel(polyNode));
            }

            return polyNodeList;
        }

        /// <summary>
        /// This method moves all the polynodes inside a polynode to the same hierarchical level
        /// </summary>
        /// <param name="polyNode">The PolyNode that contains the other PolyNodes</param>
        /// <returns>The polynodes on the same hierarchical level</returns>
        private static List<PolyNode> moveNodesToSameLevel(PolyNode polyNode)
        {
            List<PolyNode> polyNodeList = new List<PolyNode>();

            polyNodeList.Add(polyNode);

            foreach (PolyNode _polyNode in polyNode.Childs)
            {
                polyNodeList.AddRange(moveNodesToSameLevel(_polyNode));
            }

            return polyNodeList;
        }

        /// <summary>
        /// This method generates all the outline segments for a layer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void generateOutlineSegments()
        {
            Logger.logProgress("Generating outline segments");

            //Check if there should be at least one shell
            if (Global.Values.shellThickness < 1)
                return;

            //We need to got through every layercomponent
            //for (ushort i = 0; i < Global.Values.layerCount; i++)
            foreach (LayerComponent layer in Global.Values.layerComponentList)
            {
                Logger.logProgress("Outline: " + layer.layerNumber);

                //And every island inside the list
                foreach (Island island in layer.islandList)
                {
                    if (island.outlinePolygons.Count < 1)
                        continue;

                    //The first outline will be one that is half an extrusion thinner than the sliced outline, ths is sothat the dimensions
                    //do not change once extruded
                    Polygons outline = new Polygons();
                    ClipperOffset offset = new ClipperOffset();
                    offset.AddPaths(island.outlinePolygons, JoinType.jtMiter, EndType.etClosedPolygon);
                    offset.Execute(ref outline, -Global.Values.nozzleWidth / 2);

                    for (ushort j = 0; j < Global.Values.shellThickness; j++)
                    {
                        //Place the newly created outline in its own segment
                        LayerSegment outlineSegment = new LayerSegment(SegmentType.OutlineSegment);
                        outlineSegment.segmentType = SegmentType.OutlineSegment;
                        outlineSegment.segmentSpeed = layer.layerSpeed;
                        outlineSegment.outlinePolygons = new Polygons(outline);

                        island.segmentList.Add(outlineSegment);

                        var distance = (-Global.Values.nozzleWidth / 2) - Global.Values.nozzleWidth * (j + 1);

                        //We now shrink the outline with one extrusion width for the next shell if any
                        offset = new ClipperOffset();
                        offset.AddPaths(island.outlinePolygons, JoinType.jtMiter, EndType.etClosedPolygon);
                        offset.Execute(ref outline, distance);
                    }

                    //We now need to store the smallest outline as the new layer outline for infill trimming purposes
                    //the current outline though is just half an extrusion width to small
                    offset = new ClipperOffset();
                    offset.AddPaths(outline, JoinType.jtMiter, EndType.etClosedPolygon);
                    offset.Execute(ref island.outlinePolygons, Global.Values.nozzleWidth);
                }
            }
        }

        /// <summary>
        /// This method calculates which segments of ever layer is top or bottom
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void calculateToBottomSegments()
        {
            Logger.logProgress("Calculating top and bottom segments");

            //To calculate the top segments we need to go from the bottom up, take each island as a subject, take the outline of the above layer
            //as a clipper and perform a difference operation. The result will then be the top segment(s) for each layer

            //Go from the second layer to the second highest layer as everything on the top layer is a top segment in any case
            //and everything on the first layer is a bottom segment in any case
            if (Global.Values.topLayerCount > 0)
            {
                for (int i = 1; i < Global.Values.layerCount - Global.Values.topLayerCount; i++)
                {
                    //First we need to calculate the intersection of the top few layers above it
                    Polygons aboveIntersection = new Polygons();

                    for (int j = i + 1; j < i + Global.Values.topLayerCount + 1; j++)
                    {
                        Polygons combinedIsland = new Polygons();

                        //Combine the outlines of the islands into one
                        foreach (Island island in Global.Values.layerComponentList[j].islandList)
                        {
                            Clipper clipper = new Clipper();
                            clipper.AddPaths(combinedIsland, PolyType.ptClip, true);
                            clipper.AddPaths(island.outlinePolygons, PolyType.ptSubject, true);
                            clipper.Execute(ClipType.ctUnion, combinedIsland);
                        }

                        if (aboveIntersection.Count < 1)
                        {
                            aboveIntersection = combinedIsland;
                            continue;
                        }

                        Clipper comboClipper = new Clipper();
                        comboClipper.AddPaths(aboveIntersection, PolyType.ptSubject, true);
                        comboClipper.AddPaths(combinedIsland, PolyType.ptClip, true);
                        comboClipper.Execute(ClipType.ctIntersection, aboveIntersection);
                    }

                    foreach (Island island in Global.Values.layerComponentList[i].islandList)
                    {
                        Polygons segmentOutlines = new Polygons();

                        Clipper clipper = new Clipper();
                        clipper.AddPaths(island.outlinePolygons, PolyType.ptSubject, true);
                        clipper.AddPaths(aboveIntersection, PolyType.ptClip, true);
                        clipper.Execute(ClipType.ctDifference, segmentOutlines);

                        if (segmentOutlines.Count > 0)
                        {
                            LayerSegment topSegment = new LayerSegment(SegmentType.TopSegment);
                            //All top segments are probably bridges
                            topSegment.segmentSpeed = Global.Values.layerComponentList[i].bridgeingSpeed;
                            topSegment.outlinePolygons = segmentOutlines;
                            topSegment.infillHeightMultiplier = 2;  //Extrude more for a bridge

                            island.segmentList.Add(topSegment);
                        }
                    }
                }

                for (int i = Global.Values.layerCount - 1; i > Global.Values.layerCount - Global.Values.topLayerCount - 1; i--)
                {
                    foreach (Island island in Global.Values.layerComponentList[i].islandList)
                    {
                        LayerSegment topSegment = new LayerSegment(SegmentType.TopSegment);
                        topSegment.outlinePolygons = island.outlinePolygons;
                        //All top segments are probably bridges
                        topSegment.segmentSpeed = Global.Values.layerComponentList[i].bridgeingSpeed;
                        topSegment.infillHeightMultiplier = 2;  //Extrude more for a bridge

                        island.segmentList.Add(topSegment);
                    }
                }
            }
            
            //To calculate the bottom segments we need to go from the top down, take each island as a subject, take the outline of the layer below
            //as a clipper and perform a difference operation. The result will then be the bottom segment(s) for each layer
            
            //Go through every layer from the second highest layer to the second lowest layer
            if (Global.Values.bottomLayerCount > 0)
            {
                for (int i = Global.Values.layerCount - 2; i > Global.Values.bottomLayerCount - 1; i--)
                {
                    if (i < 1)
                        continue;

                    //First we need to calculate the intersection of the top few layers above it
                    Polygons belowIntersection = new Polygons();

                    for (int j = i - 1; j > i - 1 - Global.Values.bottomLayerCount; j--)
                    {
                        Polygons combinedIsland = new Polygons();

                        //Combine the outlines of the islands into one
                        foreach (Island island in Global.Values.layerComponentList[j].islandList)
                        {
                            Clipper clipper = new Clipper();
                            clipper.AddPaths(combinedIsland, PolyType.ptClip, true);
                            clipper.AddPaths(island.outlinePolygons, PolyType.ptSubject, true);
                            clipper.Execute(ClipType.ctUnion, combinedIsland);
                        }

                        if (belowIntersection.Count < 1)
                        {
                            belowIntersection = combinedIsland;
                            continue;
                        }

                        Clipper comboClipper = new Clipper();
                        comboClipper.AddPaths(belowIntersection, PolyType.ptSubject, true);
                        comboClipper.AddPaths(combinedIsland, PolyType.ptClip, true);
                        comboClipper.Execute(ClipType.ctIntersection, belowIntersection);
                    }

                    foreach (Island island in Global.Values.layerComponentList[i].islandList)
                    {
                        Polygons segmentOutlines = new Polygons();

                        Clipper clipper = new Clipper();
                        clipper.AddPaths(island.outlinePolygons, PolyType.ptSubject, true);
                        clipper.AddPaths(belowIntersection, PolyType.ptClip, true);
                        clipper.Execute(ClipType.ctDifference, segmentOutlines);

                        if (segmentOutlines.Count > 0)
                        {
                            LayerSegment bottomSegment = new LayerSegment(SegmentType.BottomSegment);
                            //All non initial layer bottom segments are probably bridges
                            bottomSegment.segmentSpeed = Global.Values.layerComponentList[i].bridgeingSpeed;
                            bottomSegment.outlinePolygons = segmentOutlines;
                            bottomSegment.infillHeightMultiplier = 2; //Extrude more for a bridge

                            island.segmentList.Add(bottomSegment);
                        }
                    }
                }

                //Every island in the bottom layer is obviously a bottom segment
                for (int i = 0; i < Global.Values.bottomLayerCount; i++)
                {
                    foreach (Island island in Global.Values.layerComponentList[i].islandList)
                    {
                        LayerSegment bottomSegment = new LayerSegment(SegmentType.BottomSegment);
                        bottomSegment.outlinePolygons = island.outlinePolygons;
                        //All top segments are probably bridges
                        bottomSegment.segmentSpeed = Global.Values.layerComponentList[i].bridgeingSpeed;

                        island.segmentList.Add(bottomSegment);
                    }
                }
            }
        }

        /// <summary>
        /// This method calculates the segments of each island that need normal infill
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void calculateInfillSegments()
        {
            Logger.logProgress("Calculating infill segments");

            //To calculate the segments that need normal infill we need to go through each island in each layer, we then need to subtract the
            //top or bottom segments from the outline shape polygons of the layer and we then have the segments that need normal infill

            for (int i = Global.Values.layerCount - 1; i >= 0; i--)
            {
                foreach (Island island in Global.Values.layerComponentList[i].islandList)
                {
                    Clipper islandClipper = new Clipper();

                    //Add the outline shape polygons as the subject
                    islandClipper.AddPaths(island.outlinePolygons, PolyType.ptSubject, true);

                    //Then add existing segments such as top or bottom and outline as the clip
                    foreach (LayerSegment segment in island.segmentList)
                    {
                        //We do not want to subtract outline segments because we already use the overall outline of the island
                        if (segment.segmentType == SegmentType.OutlineSegment)
                            continue;

                        islandClipper.AddPaths(segment.outlinePolygons, PolyType.ptClip, true);
                    }

                    Polygons infillSegments = new Polygons();

                    //We then need to perform a difference operation to determine the infill segments
                    islandClipper.Execute(ClipType.ctDifference, infillSegments);

                    LayerSegment infillSegment = new LayerSegment(SegmentType.InfillSegment);
                    infillSegment.outlinePolygons = infillSegments;
                    infillSegment.segmentType = SegmentType.InfillSegment;
                    infillSegment.segmentSpeed = Global.Values.layerComponentList[i].infillSpeed;

                    island.segmentList.Add(infillSegment);
                }
            }
        }

        /// <summary>
        /// This method combines the infill segments of each layer with those of the specified amount of layers higher
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void combineInfillSegments()
        {
            Logger.logProgress("Combining infill segments");

            //To combine the infill segments we have to go through each layer, for each infill segment int that layer we need to perform
            //an intersection test with all the above layers infill segments. The combined segments should then be separated from its original
            //segments in both the first and the second layer, the same operation should then be performed with the result and all the infill
            //segments above it untill it has been done for the amount of layers specified int global values. The bottom one of the infill segment
            //will be completely removed from its layer. The extrusion multiplier of each segment will be determined by how many layers of infill
            //it represents

            if (Global.Values.infillCombinationCount < 2)
                return;

            for (int i = Global.Values.layerCount - 1; i > 0; i -= Global.Values.infillCombinationCount)
            {
                Polygons belowInfill = new Polygons();

                //This will indicate how many layers have been combined
                short combinedLayerCount = 1;

                //Then figure out to what extent it intersects with the layers below
                for (int j = i - 1; j > i - Global.Values.infillCombinationCount && j >= 0; j--)
                {
                    //Fist combine all the infill segments for the layer
                    Polygons combinedInfill = new Polygons();

                    foreach (Island island in Global.Values.layerComponentList[j].islandList)
                    {
                        foreach (LayerSegment segment in island.segmentList)
                        {
                            if (segment.segmentType != SegmentType.InfillSegment)
                                continue;

                            Clipper union = new Clipper();
                            union.AddPaths(combinedInfill, PolyType.ptSubject, true);
                            union.AddPaths(segment.outlinePolygons, PolyType.ptClip, true);
                            union.Execute(ClipType.ctUnion, combinedInfill);
                        }
                    }

                    if (belowInfill.Count > 0)
                    {
                        //Them determine which part intersects with the previous layers' infill
                        Clipper intersect = new Clipper();
                        intersect.AddPaths(belowInfill, PolyType.ptClip, true);
                        intersect.AddPaths(combinedInfill, PolyType.ptSubject, true);
                        intersect.Execute(ClipType.ctIntersection, combinedInfill);
                    }
                    else
                        belowInfill = combinedInfill;

                    combinedLayerCount++;
                }

                foreach (Island mainIsland in Global.Values.layerComponentList[i].islandList)
                {
                    Polygons commonInfill = new Polygons();

                    //Start by setting all the infill in the current island as the base
                    foreach (LayerSegment layerSegment in mainIsland.segmentList)
                    {
                        if (layerSegment.segmentType != SegmentType.InfillSegment)
                            continue;

                        Clipper clipper = new Clipper();
                        clipper.AddPaths(commonInfill, PolyType.ptClip, true);
                        clipper.AddPaths(layerSegment.outlinePolygons, PolyType.ptSubject, true);
                        clipper.Execute(ClipType.ctUnion, commonInfill);
                    }

                    //Them determine which part intersects with the previous layers' infill
                    Clipper intersect = new Clipper();
                    intersect.AddPaths(commonInfill, PolyType.ptClip, true);
                    intersect.AddPaths(belowInfill, PolyType.ptSubject, true);
                    intersect.Execute(ClipType.ctIntersection, commonInfill);

                    if (commonInfill.Count < 1)
                        continue;
  
                    //We should now subtract the common infill from all the affected layers and then add it to the topmost layer
                    //again but with it thickness that will account for all the layers
                    
                    for (int j = i; j > i - Global.Values.infillCombinationCount && j >= 0; j--)
                    {
                        foreach (Island island in Global.Values.layerComponentList[j].islandList)
                        {
                            foreach (LayerSegment segment in island.segmentList)
                            {
                                if (segment.segmentType != SegmentType.InfillSegment)
                                    continue;

                                //Remove the common infill from the layersegment
                                Clipper difference = new Clipper();
                                difference.AddPaths(segment.outlinePolygons, PolyType.ptSubject, true);
                                difference.AddPaths(commonInfill, PolyType.ptClip, true);
                                difference.Execute(ClipType.ctDifference, segment.outlinePolygons);
                            }
                        }
                    }
                    
                    //Finally add the new segment to the topmost layer
                    if (commonInfill.Count < 1)
                        continue;

                    LayerSegment infillSegment = new LayerSegment(SegmentType.InfillSegment);
                    infillSegment.infillHeightMultiplier = combinedLayerCount;
                    infillSegment.segmentSpeed = Global.Values.layerComponentList[i].infillSpeed;
                    infillSegment.outlinePolygons = commonInfill;
                    mainIsland.segmentList.Add(infillSegment);
                }
            }
        }

        /// <summary>
        /// This method calculates the segments in each layer (except the lowest one) that need infill
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void calculateSupportSegments()
        {
            Logger.logProgress("Calculating support segments");

            if (!Global.Values.shouldSupportMaterial || Global.Values.supportMaterialDesnity <= 0)
                return;

            //To calculate the support segments we will keep a union of the above layers and perform a difference with them and the current layer
            Polygons topUnion = new Polygons();

            for (int i = Global.Values.layerCount - 1; i > -1; i--)
            {
                Polygons layerPolygons = new Polygons();

                foreach (Island island in Global.Values.layerComponentList[i].islandList)
                {
                    Polygons offsetResult = new Polygons();

                    ClipperOffset offset = new ClipperOffset();
                    offset.AddPaths(island.outlinePolygons, JoinType.jtMiter, EndType.etClosedPolygon);
                    offset.Execute(ref offsetResult, Global.Values.shellThickness * Global.Values.nozzleWidth);

                    Clipper clipper = new Clipper();
                    clipper.AddPaths(offsetResult, PolyType.ptClip, true);
                    clipper.AddPaths(layerPolygons, PolyType.ptSubject, true);
                    clipper.Execute(ClipType.ctUnion, layerPolygons);
                }

                Polygons supportOutline = new Polygons();
                Clipper supportClipper = new Clipper();
                supportClipper.AddPaths(topUnion, PolyType.ptSubject, true);
                supportClipper.AddPaths(layerPolygons, PolyType.ptClip, true);
                supportClipper.Execute(ClipType.ctDifference, supportOutline);
                
                //We should just offset the support slightly so that it does not touch the rest of the model
                ClipperOffset clipperOffset = new ClipperOffset();
                clipperOffset.AddPaths(supportOutline, JoinType.jtMiter, EndType.etClosedPolygon);
                clipperOffset.Execute(ref supportOutline, -Global.Values.nozzleWidth);

                Island supportIsland = new Island();
                LayerSegment segment = new LayerSegment(SegmentType.SupportSegment);
                segment.segmentSpeed = Global.Values.layerComponentList[i].supportSpeed;
                segment.outlinePolygons = supportOutline;
                supportIsland.outlinePolygons = supportOutline;
                supportIsland.segmentList.Add(segment);
                Global.Values.layerComponentList[i].islandList.Add(supportIsland);

                Clipper unionClipper = new Clipper();
                unionClipper.AddPaths(topUnion, PolyType.ptClip, true);
                unionClipper.AddPaths(layerPolygons, PolyType.ptSubject, true);
                unionClipper.Execute(ClipType.ctUnion, topUnion);
            }
        }
    }
}
