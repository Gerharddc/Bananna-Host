using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolyChopper.Containers;
using System.Runtime.CompilerServices;
using MathUtils;

namespace PolyChopper
{
    static class AccelerationCalculator
    {
        private static void acclerateMoves(ref MoveSegment prevMove, MoveSegment move, ref double prev_vA, ref double prev_vB)
        {
            //prevMove.gedoen = true;
            //prevMove.doen();
            //Calculate the change in X and Y positions
            long dX = move.endPoint.X - move.startPoint.X;
            long dY = move.endPoint.Y - move.startPoint.Y;

            /*if (dX == 0 && dY == 0)
            {
                prevMove.convertToCoreXY();
                prevMove = move;
                prev_vA = 0;
                prev_vB = 0;
                return;
            }*/

            //Calculate the target velocities in the X and Y axis
            double vX = (double)move.feedrate / move.moveDistance * dX;
            double vY = (double)move.feedrate / move.moveDistance * dY;

            //Calculate the target velocities for the A and B motors
            double vA = vX + vY;
            double vB = vX - vY;

            //Convert the coordinates to CoreXY
            prevMove.convertToCoreXY();

            //Calculate the change in position on the A and B axis
            double dA = prevMove.endPoint.X - prevMove.startPoint.X;
            double dB = prevMove.endPoint.Y - prevMove.startPoint.Y;

            double max_vA, max_vB, dif_vA, dif_vB;

            if (dA == 0 && dB == 0)
            {
                dif_vA = 0;
                dif_vB = 0;
                max_vA = 0;
                max_vB = 0;
            }
            else if (Math.Abs(dA) > Math.Abs(dB))
            {              
                var squared = (prevMove.startVelocity.X > 0) ? (prevMove.startVelocity.X * prevMove.startVelocity.X) : -(prevMove.startVelocity.X * prevMove.startVelocity.X);
                var other = (2 * Global.Values.maxAccel * dA);
                var result = Math.Sqrt(Math.Abs(squared + other));

                if (prev_vA > prevMove.startVelocity.X)
                {
                    prevMove.acceleration = new Vector2(Global.Values.maxAccel, (long)(dB / dA * Global.Values.maxAccel));
                    max_vA = Math.Min(result, prev_vA);
                }
                else
                {
                    prevMove.acceleration = new Vector2(-Global.Values.maxAccel, (long)(dB / dA * -Global.Values.maxAccel));
                    max_vA = Math.Max(-result, prev_vA);
                }

                //Calculate the maximum achieveable velocity in each axis or the target velocity if it can be reached
                max_vB = dB / dA * max_vA;

                //Calculate the difference between the target velocities for this move and the max achievable ones for the previous one
                dif_vA = Math.Abs(vA - max_vA);
                dif_vB = Math.Abs(vB - max_vB);
            }
            else
            {
                var squared = (prevMove.startVelocity.Y > 0) ? (prevMove.startVelocity.Y * prevMove.startVelocity.Y) : -(prevMove.startVelocity.Y * prevMove.startVelocity.Y);
                var other = (2 * Global.Values.maxAccel * dB);
                var result = Math.Sqrt(Math.Abs(squared + other));

                if (prev_vB > prevMove.startVelocity.Y)
                {
                    prevMove.acceleration = new Vector2(Global.Values.maxAccel, (long)(dA / dB * Global.Values.maxAccel));
                    max_vB = Math.Min(result, prev_vB);
                }
                else
                {
                    prevMove.acceleration = new Vector2(-Global.Values.maxAccel, (long)(dA / dB * -Global.Values.maxAccel));
                    max_vB = Math.Max(-result, prev_vB);
                }

                //Calculate the maximum achieveable velocity in each axis or the target velocity if it can be reached
                max_vA = dA / dB * max_vB;

                //Calculate the difference between the target velocities for this move and the max achievable ones for the previous one
                dif_vA = Math.Abs(vA - max_vA);
                dif_vB = Math.Abs(vB - max_vB);
            }

            //Calculate the jumping point by scaling the max velocities
            double maxDif = Math.Max(dif_vA, dif_vB);
            double scaler = (maxDif != 0) ? (Global.Values.maxJump / maxDif) : 0;
            prevMove.endVelocity = new Vector2((long)(max_vA * scaler), (long)(max_vB * scaler));
            move.startVelocity = new Vector2((long)(vA * scaler), (long)(vB * scaler));

            //Now calculate the peak velocity by calculating the intesection between the velocity line from the start and the invert one from the jumping point

            ///Lines defined in terms of Ax + By = C where:
            ///A = y2 - y1
            ///B = x1 - x2
            ///C = A * x1 + B * y1
            
            if (max_vA == 0 && max_vB == 0)
            {
                prevMove.peakVelocity = new Vector2(0, 0);
            }
            else if (Math.Abs(max_vA) > Math.Abs(max_vB))
            {
                var A1 = max_vA - prevMove.startVelocity.X;
                var B1 = -dA; //0 - dA;
                var C1 = B1 * prevMove.startVelocity.X; //A1 * 0 + B1 * prevMove.startVelocity.X;

                var A2 = max_vA; //(max_vA + prevMove.endVelocity.X) - prevMove.endVelocity.X;
                var B2 = dA; //dA - 0;
                var C2 = A2 * dA + B2 * prevMove.endVelocity.X;

                var det = A1 * B2 - A2 * B1;
                var peak_vA = (A1 * C2 - A2 * C1) / det;

                //Because the A and B points are in a fixed ratio to each other, we only do the long calculation for A and then us the ratio for B
                prevMove.peakVelocity = new Vector2((long)(peak_vA), (long)(dB / dA * peak_vA));
            }
            else
            {
                var A1 = max_vB - prevMove.startVelocity.Y;
                var B1 = -dB; //0 - dB;
                var C1 = B1 * prevMove.startVelocity.Y; //A1 * 0 + B1 * prevMove.startVelocity.Y;

                var A2 = max_vB; //(max_vA + prevMove.endVelocity.X) - prevMove.endVelocity.X;
                var B2 = dB; //dA - 0;
                var C2 = A2 * dB + B2 * prevMove.endVelocity.Y;

                var det = A1 * B2 - A2 * B1;
                var peak_vB = (A1 * C2 - A2 * C1) / det;

                //Because the A and B points are in a fixed ratio to each other, we only do the long calculation for A and then us the ratio for B
                prevMove.peakVelocity = new Vector2((long)(dA / dB * peak_vB), (long)(peak_vB));
            }

            prevMove = move;
            prev_vA = vA;
            prev_vB = vB;
        }

        public static void calculateAcceleration()
        {
            Logger.logProgress("Calculating Acceleration");
            
            //We need to keep a refernce to the previous segment so that we can adjust it based on the current one
            MoveSegment prevMove = new MoveSegment();
            double prev_vA = 0;
            double prev_vB = 0;

            foreach (LayerComponent layer in Global.Values.layerComponentList)
            {
                foreach (MoveSegment move in layer.intialLayerMoves)
                {
                    acclerateMoves(ref prevMove, move, ref prev_vA, ref prev_vB);
                }

                foreach (Island island in layer.islandList)
                {
                    foreach (LayerSegment segment in island.segmentList)
                    {
                        foreach (MoveSegment move in segment.moveSegments)
                        {
                            acclerateMoves(ref prevMove, move, ref prev_vA, ref prev_vB);
                        }
                    }
                }
            }

            //Finally we need to make the last move accelerate to rest
            MoveSegment stopMove = new MoveSegment(prevMove.endPoint, prevMove.endPoint, 0); //It is not needed to have positions here with the current algorithm but it might become neccesary
            acclerateMoves(ref prevMove, stopMove, ref prev_vA, ref prev_vB);
        }
    }

    /// <summary>
    /// This class is responsible for estimating information regarding the print job
    /// </summary>
    static class ValueEstimator
    {
        /// <summary>
        /// This method estimates the total priting time, filament usage and cost
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void estimateValues()
        {
            Logger.logProgress("Estimating values");

            estimateTotalPrintingTime();
            estimateTotalFilamentUsage();
            estimateTotalPrintingCost();
        }

        /// <summary>
        /// This method estimates the total printing time for the whole gcode file
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void estimateTotalPrintingTime()
        {
            //TODO: something does not work here

            ulong totalTime = 0;

            /*foreach (MoveSegment segment in Global.Values.MoveSegmentList)
            {
                totalTime += segment.estimatedTime;
            }*/

            Global.Values.printingTimeEstimate = (long)(totalTime / 1000000 * 1.2f); //Add 10% for safety
        }

        /// <summary>
        /// This method estimates the total amount of filament usage in terms of both length
        /// and mass
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void estimateTotalFilamentUsage()
        {
            //First calculate the total extruded distance for the job

            ulong totalExtrude = 0;

            /*foreach (MoveSegment segment in Global.Values.MoveSegmentList)
            {
                totalExtrude += segment.extrusionDistance;
            }*/

            //Then multiply it with the diameter to get the volume

            double volume = (double)totalExtrude / 1000000 * Math.PI * Math.Pow(((double)Global.Values.filamentWidth / 2000000), 2);
            //TODO: the above is not 100% correct

            //We then need to determine the density of the plastic used to determine the total weight of filament used
            if (Global.Values.materialType == MaterialType.PLA)
            {
                Global.Values.filamentUsageEstimate = (long)(volume * 1.3);
            }
            else if (Global.Values.materialType == MaterialType.ABS)
            {
                Global.Values.filamentUsageEstimate = (long)(volume * 1.04);
            }
        }

        /// <summary>
        /// This method calculates the total printing cost of the printjob by taking in to account the specified
        /// cost per kg filament as well as the cost per minute of printing
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void estimateTotalPrintingCost()
        {
            Global.Values.printingCostEstimate = (long)((float)Global.Values.filamentUsageEstimate / 1000f * Global.Values.costPerKg);
        }

        /// <summary>
        /// This method estimates the printing time of every layer and makes it a bit slower if needed
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void estimateLayerTimesAndFix()
        {
            Logger.logProgress("Enforcing minimum layer time");

            //First of all we need to estimate the printing time of each layer, if the time is more than the minimum
            //then we need to make the feedrate of each segment slower accordingly but be careful not make make the
            //feedrate less than the minimum

            /*Global.Values.layerStartIndices[Global.Values.layerCount] = Global.Values.MoveSegmentList.Count;

            for (int i = 0; i < Global.Values.layerCount; i++)
            {
                long layerTime = 0;

                //First estimate the printing time for the layer
                for (int j = Global.Values.layerStartIndices[i]; j < Global.Values.layerStartIndices[i + 1]; j++)
                    layerTime += Global.Values.MoveSegmentList[j].estimatedTime;

                //Determine if the time estimate is below the minimum
                if (layerTime > Global.Values.minimumLayerTime)
                    continue;

                //Otherwise calculate how much faster it is
                float multi = (float)Global.Values.minimumLayerTime / (float)layerTime;

                for (int j = Global.Values.layerStartIndices[i]; j < Global.Values.layerStartIndices[i + 1]; j++)
                {
                    float currentSpeed = Global.Values.MoveSegmentList[j].feedrate;
                    float newSpeed = currentSpeed / multi;

                    if (newSpeed < Global.Values.minimumLayerSpeed)
                        newSpeed = Global.Values.minimumLayerSpeed;

                    Global.Values.MoveSegmentList[j].feedrate = (int)newSpeed;
                }
            }*/
        }
    }
}
