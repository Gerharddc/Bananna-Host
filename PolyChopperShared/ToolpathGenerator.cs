using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using PolyChopper.Containers;
using MathUtils;

namespace PolyChopper
{
    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;
    using System.Runtime.CompilerServices;

    static class ToolpathGenerator
    {
        public static void calculateToolPath()
        {
            Logger.logProgress("Calculating Toolpath");

            IntPoint lastPoint = new IntPoint(0, 0);
            long lastZ = 0;

            foreach (LayerComponent currentLayer in Global.Values.layerComponentList)
            {
                bool firstInLayer = true;

                //Move to the new z position
                long newZ = lastZ + Global.Values.layerHeight;//i * Global.Values.layerHeight + Global.Values.layerHeight;
                Vector3 v1 = new Vector3(lastPoint.X, lastPoint.Y, lastZ);
                Vector3 v2 = new Vector3(lastPoint.X, lastPoint.Y, newZ);
                currentLayer.intialLayerMoves.Enqueue(new MoveSegment(v1, v2, currentLayer.moveSpeed));
                lastZ = newZ;

                foreach (Island island in currentLayer.islandList)
                {
                    bool firstInIsland = true;

                    //The outline segments of am island should also come before all infill type segments, we can therefore store the oultine
                    //linesegments for use of the infill move calculations

                    List<List<LineSegment>> outlineSegments = new List<List<LineSegment>>();

                    foreach (LayerSegment segment in island.segmentList)
                    {
                        //We need to convert all the polygons in the segment into lines sothat we can do our calculations
                        List<LineSegment> lineList = convertSegmentToLines(segment);

                        //Store the outline linesegments for later use
                        if (segment.segmentType == SegmentType.OutlineSegment)
                            outlineSegments.Add(new List<LineSegment>(lineList));

                        //Move to the next segment if this one does not have any lines
                        if (lineList.Count < 1)
                            continue;

                        //We now have to determine if we still need to move to the island
                        if (firstInIsland && !firstInLayer)
                        {
                            //If we still have to move to this island then we should retract filament if needed and then create a
                            //direct move to the first point of the segment

                            if (Global.Values.retractionSpeed > 0 && Global.Values.retractionDistance > 0)
                            {
                                //Create a retraction because we are now moving to a new island only if we have moved more than the miniumum distance (5mm)
                                const double minDist = 10 * 1000000;
                                const double minDistSquared = minDist * minDist;

                                double moveDistSquared = LineSegment.squaredDistanceBetweenPoints(lastPoint, lineList[0].Point1);

                                if (moveDistSquared > minDistSquared)
                                    segment.moveSegments.Enqueue(new MoveSegment());
                            }

                            //If in plotting mode this move should be done in the air
                            if (Global.Values.materialType != MaterialType.Pen)
                            {
                                //Now create a move from the last point to the first point
                                v1 = new Vector3(lastPoint.X, lastPoint.Y, lastZ);
                                v2 = new Vector3(lineList[0].Point1.X, lineList[0].Point1.Y, lastZ);
                                segment.moveSegments.Enqueue(new MoveSegment(v1, v2, currentLayer.moveSpeed));
                            }
                            else
                            {
                                //Now create a move from the last point to the first point
                                v1 = new Vector3(lastPoint.X, lastPoint.Y, lastZ + 1);
                                v2 = new Vector3(lineList[0].Point1.X, lineList[0].Point1.Y, lastZ + 1);
                                segment.moveSegments.Enqueue(new MoveSegment(v1, v2, currentLayer.moveSpeed));

                                //Move the pen down again
                                v1 = new Vector3(lineList[0].Point1.X, lineList[0].Point1.Y, lastZ + 1);
                                v2 = new Vector3(lineList[0].Point1.X, lineList[0].Point1.Y, lastZ);
                            }

                            lastPoint = lineList[0].Point1;

                            firstInIsland = false;
                        }
                        else
                        {
                            //If we were already in this island then we need to move inside the island from the last point to the first
                            //point in the segment

                            generateMovesBetweenPoints(lastPoint, lineList[0].Point1, lastZ, outlineSegments, currentLayer.moveSpeed, ref segment.moveSegments);

                            lastPoint = lineList[0].Point1;

                            if (firstInLayer)
                                firstInLayer = false;
                        }

                        //Now we need to start going through each line segment and creating the neccesary moves and retractions
                        while (lineList.Count > 0)
                        {
                            //Firstly create a move from the last point to the start of the last point to the point closest to it remaining in the list
                            var closestLine = findClosestLineInList(lastPoint, lineList);

                            //We now have to move from the lastPoint to the first point on the closest line segment
                            generateMovesBetweenPoints(lastPoint, closestLine.Point1, lastZ, outlineSegments, currentLayer.moveSpeed, ref segment.moveSegments);

                            //We then need to create an extruded move for the line segment
                            v1 = new Vector3(closestLine.Point1.X, closestLine.Point1.Y, lastZ);
                            v2 = new Vector3(closestLine.Point2.X, closestLine.Point2.Y, lastZ);
                            segment.moveSegments.Enqueue(new MoveSegment(v1, v2, Global.Values.layerHeight * segment.infillHeightMultiplier, segment.segmentSpeed));

                            //Update the last point
                            lastPoint = closestLine.Point2;

                            //Finally we remove the line from the list
                            lineList.Remove(closestLine);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method finds the distance from a given point to a line segment (which is the same as from this point to anywhere
        /// on an infinite line). Also returns the closest point.
        /// </summary>
        /// <param name="p1">The first point of the linesegment</param>
        /// <param name="p2">The second point of the line segment</param>
        /// <param name="op">The point of which the closest point should be found</param>
        /// <param name="cp">The closest point to the starting point</param>
        /// <returns>The distance from the starting point to the closest point squared</returns>
        private static double distanceToLineSegmentSquared(Vector2 p1, Vector2 p2, Vector2 op, out Vector2 cp)
        {
            // Compute length of line segment (squared) and handle special case of coincident points
            double segmentLengthSquared = Vector2.DistanceSquared(p1, p2);
            if (segmentLengthSquared < 1E-7f)  // Arbitrary "close enough for government work" value
            {
                cp = p1;
                return Vector2.DistanceSquared(op, cp);
            }

            // Use the magic formula to compute the "projection" of this point on the infinite line
            Vector2 lineSegment = p2 - p1;
            double t = Vector2.Dot(op - p1, lineSegment) / segmentLengthSquared;

            // Handle the two cases where the projection is not on the line segment, and the case where 
            //  the projection is on the segment
            if (t <= 0)
                cp = p1;
            else if (t >= 1)
                cp = p2;
            else
                cp = p1 + (lineSegment * t);
            return Vector2.DistanceSquared(op, cp);
        }

        /// <summary>
        /// This method calculates the shortest distance from a given point to a given line
        /// </summary>
        /// <param name="line">The linesegment</param>
        /// <param name="c">The point of which the distance should be measured from</param>
        /// <param name="d">The closest point to point c on linesegment ab</param>
        /// <param name="squared">Determines if the distance can be squared or not. Leaving it squared is faster.</param>
        /// <returns></returns>
        private static double distanceToLine(LineSegment line, IntPoint c, out IntPoint d, bool squared = true)
        {
            Vector2 pA = new Vector2(line.Point1.X, line.Point1.Y);
            Vector2 pB = new Vector2(line.Point2.X, line.Point2.Y);
            Vector2 oP = new Vector2(c.X, c.Y);
            Vector2 cP;

            double dist = (squared) ? distanceToLineSegmentSquared(pA, pB, oP, out cP) : Math.Sqrt(distanceToLineSegmentSquared(pA, pB, oP, out cP));

            d = new IntPoint(cP.X, cP.Y);
            return dist;
        }

        /// <summary>
        /// This method is used to find a point in a polygon. A polygon is a list of point which should be loopable as end tied end to end.
        /// This method allows to find a point in a polygon with an identifier larger than the amount of points in the polygon. If a point is larger then it
        /// starts from the begining
        /// </summary>
        /// <param name="polygon">The polygon where you want to find the point in</param>
        /// <param name="n">The how manyeth point that you want to retrieve</param>
        /// <returns>The point at the given location in a looping polygon</returns>
        private static IntPoint pointInPolygon(Polygon polygon, int n)
        {
            //First we need to eliminate any full rotations in the number count
            while (n >= polygon.Count)
                n -= polygon.Count;

            while (n < 0)
                n += polygon.Count;

            //Now n is withing range of the list and we can return the point
            return polygon[n];
        }

        /// <summary>
        /// This method converts the polygons in a layersegment into a list of linesegments
        /// </summary>
        /// <param name="layerSegment">The layersegment whose polygons should be converted to line segments</param>
        /// <returns>The list of linesegments</returns>
        private static List<LineSegment> convertSegmentToLines(LayerSegment layerSegment)
        {
            List<LineSegment> lineList = new List<LineSegment>();

            if (layerSegment.segmentType == SegmentType.OutlineSegment || layerSegment.segmentType == SegmentType.SkirtSegment)
            {
                //This segment contains its linesegments in its outline polygons

                foreach (Polygon polygon in layerSegment.outlinePolygons)
                {
                    if (polygon.Count < 3)
                        continue;

                    for (int i = 0; i < polygon.Count - 1; i++)
                        lineList.Add(new LineSegment(polygon[i], polygon[i + 1]));

                    lineList.Add(new LineSegment(polygon[polygon.Count - 1], polygon[0]));
                }
            }
            else
            {
                //This segment containsits linesegments in its fill polygons
                lineList.AddRange(layerSegment.fillLines);
            }

            return lineList;
        }

        /// <summary>
        /// This method finds the line closest to the base / starting point. The line with the closest point will be swapped if needed so that
        /// the closest point is p1
        /// </summary>
        /// <param name="basePoint">The base point of which the closest line should be found</param>
        /// <param name="lineList">The list of possible lines to move to</param>
        /// <returns>The line with the closest point in the correct orientation</returns>
        private static LineSegment findClosestLineInList(IntPoint basePoint, List<LineSegment> lineList)
        {
            double closestDist = double.MaxValue;
            LineSegment closestLine = lineList[0];

            foreach (LineSegment line in lineList)
            {
                var distance = LineSegment.squaredDistanceBetweenPoints(basePoint, line.Point1);

                if (distance == 0)
                    return line;

                if (distance < closestDist)
                {
                    closestDist = distance;
                    closestLine = line;
                }

                distance = LineSegment.squaredDistanceBetweenPoints(basePoint, line.Point2);

                if (distance == 0)
                {
                    closestLine = line;
                    closestLine.swapPoints();
                    return line;
                }

                if (distance < closestDist)
                {
                    closestLine = line;
                    closestLine.swapPoints();
                    closestDist = distance;
                }
            }

            return closestLine;
        }

        /// <summary>
        /// This method generates move commands ehich try to get from a given point to another whilst staying on the outline
        /// of an island
        /// </summary>s
        /// <param name="p1">The starting point</param>
        /// <param name="p2">The ending point</param>
        /// <param name="zPos">The z position of the layer</param>
        /// <param name="outlineLines">The outline linesegments of the island</param>
        private static void generateMovesBetweenPoints(IntPoint p1, IntPoint p2, long zPos, List<List<LineSegment>> outlineLines, int moveSpeed, ref Queue<MoveSegment> moveQueue)
        {
            //TODO: sometimes it appears as if though when there are two points on the smae line a move if first genertated from p1 to the end (or start) of the
            //line and then back, there sould instead be a direct move from p1 to p2

            //If the two points are already equal then a move between them is not neccesary
            if (p1.Equals(p2))
                return;

            /*//If we are in plotting mode then we only need to lift up the pen and create a direct move
            if (Global.Values.materialType == MaterialType.Pen)
            {
                //Retract / lift up the pen
                Global.Values.MoveSegmentList.Add(new MoveSegment());

                //Make a direct move
                var vec1 = new Vector3(p1.X, p1.Y, zPos + 1);
                var vec2 = new Vector3(p2.X, p2.Y, zPos + 1);
                Global.Values.MoveSegmentList.Add(new MoveSegment(vec1, vec2, moveSpeed));

                //Move the pen down again
                vec1 = new Vector3(p2.X, p2.Y, zPos + 1);
                vec2 = new Vector3(p2.X, p2.Y, zPos);
                Global.Values.MoveSegmentList.Add(new MoveSegment(vec1, vec2, moveSpeed));

                return;
            }*/


            //If there are no outline linesegments then we need to generate a direct move
            if (outlineLines.Count < 1)
            {
                var vec1 = new Vector3(p1.X, p1.Y, zPos + 1);//);
                var vec2 = new Vector3(p2.X, p2.Y, zPos + 1);//);
                moveQueue.Enqueue(new MoveSegment(vec1, vec2, moveSpeed));
                return;
            }

            //First find out which point on which polygon is closest to the first point
            List<LineSegment> closestOutline = new List<LineSegment>();
            IntPoint closestPoint = new IntPoint();
            LineSegment closestLine = new LineSegment(new IntPoint(), new IntPoint());
            double closesDistance = double.MaxValue;

            foreach (List<LineSegment> lines in outlineLines)
            {
                foreach (LineSegment line in lines)
                {
                    IntPoint newPoint;
                    var distance = distanceToLine(line, p1, out newPoint);

                    if (distance < closesDistance)
                    {
                        closesDistance = distance;
                        closestPoint = newPoint;
                        closestOutline = lines;
                        closestLine = line;
                    }
                }
            }

            //We now need to create a move from the first point to the closest point on the polygon
            var v1 = new Vector3(p1.X, p1.Y, zPos);
            var v2 = new Vector3(closestPoint.X, closestPoint.Y, zPos);
            moveQueue.Enqueue(new MoveSegment(v1, v2, moveSpeed));

            //Next we need to determine which point point on the above determined polygon is closest to the second
            IntPoint closestPoint2 = new IntPoint();
            LineSegment closestLine2 = new LineSegment(new IntPoint(), new IntPoint());
            closesDistance = double.MaxValue;

            foreach (LineSegment line in closestOutline)
            {
                IntPoint newPoint;
                var distance = distanceToLine(line, p2, out newPoint);

                if (distance < closesDistance)
                {
                    closesDistance = distance;
                    closestPoint2 = newPoint;
                    closestLine2 = line;
                }
            }

            //Create an endless looping warpper for the list
            LoopingList<LineSegment> loopingList = new LoopingList<LineSegment>(ref closestOutline);

            //We now need to determine the index of the two lines
            int lineIndex = closestOutline.IndexOf(closestLine);
            int lineIndex2;// = closestOutline.IndexOf(closestLine2);

            //We now need to add move segments for each line on the outline between the two closest points

            //A while ago we moved to closestPoint so it is now our last point
            IntPoint lastPoint = closestPoint;

            //For each line between the two points we have to move from the last point to the second point on the list
            int indexDiff = loopingList.distanceBetweenElements(closestLine, closestLine2, out lineIndex2); //lineIndex2 - lineIndex;
            if (indexDiff > 0)
            {
                for (int i = lineIndex; i < lineIndex2; i++)
                {
                    v1 = new Vector3(lastPoint.X, lastPoint.Y, zPos);
                    var point2 = loopingList.elementAtIndex(i).Point2;
                    v2 = new Vector3(point2.X, point2.Y, zPos);
                    moveQueue.Enqueue(new MoveSegment(v1, v2, moveSpeed));
                    lastPoint = point2;//closestOutline[i].Point2;
                }
            }
            else if (indexDiff < 0)
            {
                for (int i = lineIndex; i > lineIndex2; i--)
                {
                    v1 = new Vector3(lastPoint.X, lastPoint.Y, zPos);
                    var point1 = loopingList.elementAtIndex(i).Point1;
                    //v2 = new Vector3(closestOutline[i].Point1.X, closestOutline[i].Point1.Y, zPos);
                    v2 = new Vector3(point1.X, point1.Y, zPos);
                    moveQueue.Enqueue(new MoveSegment(v1, v2, moveSpeed));
                    //Global.Values.MoveSegmentList.Add(new MoveSegment(v1, v2, moveSpeed));
                    lastPoint = point1;//closestOutline[i].Point1;
                }
            }

            //Next we have to move from the last point to the closest point 2
            v1 = new Vector3(lastPoint.X, lastPoint.Y, zPos);
            v2 = new Vector3(closestPoint2.X, closestPoint2.Y, zPos);
            moveQueue.Enqueue(new MoveSegment(v1, v2, moveSpeed));

            //We then finally have to move from the last point 2 to the closest point 2
            v1 = new Vector3(closestPoint2.X, closestPoint2.Y, zPos);
            v2 = new Vector3(p2.X, p2.Y, zPos);
            moveQueue.Enqueue(new MoveSegment(v1, v2, moveSpeed));
        }    
    }
}
