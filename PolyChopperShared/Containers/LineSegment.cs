using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using MathUtils;

namespace PolyChopper.Containers
{
    public class LineSegment
    {
        private IntPoint point1, point2;

        public IntPoint Point1 { get { return point1; } }
        public IntPoint Point2 { get { return point2; } }

        /// <summary>
        /// This value is used to indicate if this line has already been used in the creation of a polygon or not
        /// </summary>
        public bool usedInPolygon = false; 

        /// <summary>
        /// This value is used to store the index of the triangle that this line has been created from
        /// </summary>
        public int triangleIndex = -1;

        /// <summary>
        /// This method creates a new LineSegment form the 2 given points
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        public LineSegment(IntPoint p1, IntPoint p2)
        {
            point1 = p1;
            point2 = p2;
        }

        /// <summary>
        /// This method creates a new LineSegment form the 2 given points
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        public LineSegment(Vector2 p1, Vector2 p2)
        {
            point1 = new IntPoint(p1.X, p1.Y);
            point2 = new IntPoint(p2.X, p2.Y);
        }

        /// <summary>
        /// This method creates a new linesegment with the same points as an existing one
        /// </summary>
        /// <param name="oldLine">The existing line segment</param>
        public LineSegment(LineSegment oldLine)
        {
            point1 = oldLine.Point1;
            point2 = oldLine.Point2;
        }

        /// <summary>
        /// This method swaps the two points of the linesegment
        /// </summary>
        public void swapPoints()
        {
            var temp = point1;
            point1 = point2;
            point2 = temp;
        }

        /// <summary>
        /// This property returns the distance between the two points of the linesegment
        /// </summary>
        public long length
        {
            get
            {
                return (long)distanceBetweenPoints(point1, point2);
            }
        }

        /// <summary>
        /// This method returns the distance between two points that has not been rooted yet, as in still squared.
        /// It is faster to use this value for comparison purposes
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>The squared distance between the two points</returns>
        public static double squaredDistanceBetweenPoints(IntPoint p1, IntPoint p2)
        {
            var xDist = p2.X - p1.X;
            var yDist = p2.Y - p1.Y;
            return Math.Pow(xDist, 2) + Math.Pow(yDist, 2);
        }

        /// <summary>
        /// This method returns the square rooted (real) distance between two given points
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <returns>The real distance between two points</returns>
        public static double distanceBetweenPoints(IntPoint p1, IntPoint p2)
        {
            return Math.Sqrt(squaredDistanceBetweenPoints(p1, p2));
        }
    }
}
