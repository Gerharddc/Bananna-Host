using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using MathUtils;

namespace PolyChopper.Containers
{
    /// <summary>
    /// This class contains methods and properties related to a segment of gcode
    /// </summary>
    public class MoveSegment
    {
        /// <summary>
        /// This is the point where the segment starts, in other words where the previous segment stopped
        /// </summary>
        public Vector3 startPoint;
        /// <summary>
        /// This is the point where the segment stops
        /// </summary>
        public Vector3 endPoint;

        public bool isCoreXY = false;       

        /// <summary>
        /// This method converts the coordinates from cartesian X Y Z to coreXY A B Z ones
        /// </summary>
        public void convertToCoreXY()
        {
            VectorToCoreXY(ref startPoint);
            VectorToCoreXY(ref endPoint);

            isCoreXY = true;
        }

        private void VectorToCoreXY(ref Vector3 vector)
        {
            //A = X + Y
            //B = X - Y
            var A = vector.X + vector.Y;
            var B = vector.X - vector.Y;

            vector.X = A;
            vector.Y = B;
        }

        public Vector2 startVelocity;
        public Vector2 endVelocity;
        public Vector2 peakVelocity;
        public Vector2 acceleration;
        //public Vector2 minVelocity;
        //public Vector2 acc

        //The extruder to whic the move applies, default is 1
        public byte extruderNmbr = 1;

        //extruded determines if the segment is just a move or an extruded move
        //retraction determines if the segment is a retraction
        private bool extruded, retraction;

        /// <summary>
        /// If this segment is merely a comment then this value will be the desired comment
        /// </summary>
        //public string comment;

        /// <summary>
        /// This property determines the height of the extrusion in nanometres
        /// </summary>
        public int height;
        /// <summary>
        /// This property determines the feedrate of the segments in nanometre per second
        /// </summary>
        public int feedrate;

        public bool isExtruded { get { return extruded; } }
        public bool isRetraction { get { return retraction; } }

        /// <summary>
        /// This property returns the distance in nanometres from the startpoint to the endpoint
        /// </summary>
        public uint moveDistance
        {
            get
            {
                if (startPoint == null || endPoint == null)
                    return 0;

                var xDif = Math.Pow(endPoint.X - startPoint.X, 2);
                var yDif = Math.Pow(endPoint.Y - startPoint.Y, 2);
                var zDif = Math.Pow(endPoint.Z - startPoint.Z, 2);

                return (uint)Math.Sqrt(xDif + yDif + zDif);
            }
        }

        /// <summary>
        /// This property returns the length of filament needed to extrude the amount required for the segment
        /// </summary>
        public uint extrusionDistance
        {
            get
            {
                if (height == 0 && !retraction)
                    return 0;

                //First we need to calculate the volume of the segment
                var volume = (!retraction) ? (moveDistance * height / Global.Values.nozzleWidth) : Global.Values.retractionDistance;

                //We then need to calculate how much smaller the extrusion is from the filament so that we know how much filament to use
                //to get the desired amount of extrusion
                var filamentToTip = Global.Values.filamentWidth / Global.Values.nozzleWidth;

                //We can then return the amount of filament needed for the extrusion of the move
                return (uint)(volume / filamentToTip / 5); //Not sure why the 5 is needed
                //TODO: the above is not 100% correct
            }
        }

        /// <summary>
        /// This property calculates the estimated printing time for the specified gcode segment in milliseconds
        /// </summary>
        public float estimatedTime
        {
            get
            {
                /*//First we need to convert the mm per minute feedrate into nanometre per millisecond
                var nanometrePerMilli = feedrate / 60;*/

                if (moveDistance == 0 || feedrate == 0 /* || nanometrePerMilli == 0*/)
                    return 0;

                //We then need to determine if the movement or the extrusion distance is the largest
                //and then use the above speed to calculate time in milliseconds
                //return (uint)(moveDistance / nanometrePerMilli);
                return moveDistance / feedrate;
                //TODO: the above is not 100% correct
            }
        }

        /// <summary>
        /// This method creates an extruded from a given startpoint to a given endpoint with a given
        /// extrusion height at a given feedrate
        /// </summary>
        /// <param name="sPoint">The starting point of the segment</param>
        /// <param name="ePoint">The ending point of the segment</param>
        /// <param name="height">The height of the extrusion</param>
        /// <param name="feedrate">The feedrate of the segment in mm per minute</param>
        public MoveSegment(Vector3 sPoint, Vector3 ePoint, int _height, int _feedrate)
        {
            startPoint = sPoint;
            endPoint = ePoint;
            height = _height;
            feedrate = _feedrate;
            extruded = true;
            retraction = false;
        }

        /// <summary>
        /// This method creates an unextruded gcode move from a given startpoint to a given endpoint
        /// at a given feedrate
        /// </summary>
        /// <param name="sPoint">The starting point of the move</param>
        /// <param name="ePoint">The ending point of the move</param>
        /// <param name="feedrate">The feedrate of the move in mm per minute</param>
        public MoveSegment(Vector3 sPoint, Vector3 ePoint, int _feedrate)
        {
            startPoint = sPoint;
            endPoint = ePoint;
            feedrate = _feedrate;
            extruded = false;
            retraction = false;
        }

        /// <summary>
        /// This method creates a new reteaction segment with the parameter stored in Global.Values
        /// </summary>
        public MoveSegment()
        {
            extruded = false;
            retraction = true;
            feedrate = Global.Values.retractionSpeed;
        }

        /*
        /// <summary>
        /// This method creates a new gcode comment section with the specified text
        /// </summary>
        /// <param name="_comment">The text of the comment</param>
        public MoveSegment(string _comment)
        {
            extruded = false;
            retraction = false;

            comment = _comment;
        }*/
    }
}
