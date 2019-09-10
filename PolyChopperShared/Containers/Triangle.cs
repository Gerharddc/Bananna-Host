using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathUtils;
using ClipperLib;

namespace PolyChopper.Containers
{
    public class Triangle
    {
        Vector3 point1, point2, point3;

        public Vector3 Point1 { get { return point1; } }
        public Vector3 Point2 { get { return point2; } }
        public Vector3 Point3 { get { return point3; } }

        public Vector3 pointA, pointB, pointC;

        public int index = -1;

        public int[] indexTouchingSide = new int[3];

        public int p1Idx = -1;
        public int p2Idx = -1;
        public int p3Idx = -1;

        /// <summary>
        /// Creates a new triangle with the given 3 points
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <param name="p2">Point 2</param>
        /// <param name="p3">Point 3</param>
        public Triangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            point1 = p1;
            point2 = p2;
            point3 = p3;
        }
        
        /// <summary>
        /// Creates a new triangle from an existing one
        /// </summary>
        /// <param name="triangle">The existing one that should be cloned</param>
        public Triangle(Triangle triangle)
        {
            point1 = triangle.Point1;
            point2 = triangle.Point2;
            point3 = triangle.Point3;

            //calculateOrganisedPoints();
        }

        /// <summary>
        /// This function checks if a given z coordinate can be found inside the triangle
        /// </summary>
        /// <param name="zPosition">The z coordinate to check</param>
        /// <returns>True if z position is inside triangle, false otherwise</returns>
        public bool checkIfContainsZ(long zPosition)
        {
            //First check the the entire triangle is not flat on the z plane because then it is not valid for slicing
            if (point1.Z == point2.Z && point2.Z == point3.Z)
                return false;

            return calculateOrganisedPoints(zPosition);
        }

        /// <summary>
        /// This method calculates and returns the two points where the specified z axis intersects with the tringle
        /// 
        /// NB: Please check if the Z coordinate can be found within the triangle before using calling method
        /// </summary>
        /// <param name="zPosition">The z coordinate to check intersection with</param>
        /// <returns>The two coordinates where the z axis intersects with the triangle</returns>
        public LineSegment calculateZSLicePoints(long zPosition)
        {   
            
            //First calculate the relationship of z to x on the one side of the triangle
            double zToX1 = (pointA.Z != pointB.Z) ? ((double)(pointA.X - pointB.X) / (double)(pointA.Z - pointB.Z)) : 0;
            //Then calculate the relationship of z to y on the one side of the triangle
            double zToY1 = (pointA.Z != pointB.Z) ? ((double)(pointA.Y - pointB.Y) / (double)(pointA.Z - pointB.Z)) : 0;
            //Then calculate the relationship of z to x on the other side of the triangle
            double zToX2 = (pointA.Z != pointC.Z) ? ((double)(pointA.X - pointC.X) / (double)(pointA.Z - pointC.Z)) : 0;
            //Then calculate the relationship of z to y on the other side of the triangle
            double zToY2 = (pointA.Z != pointC.Z) ? ((double)(pointA.Y - pointC.Y) / (double)(pointA.Z - pointC.Z)) : 0;

            //Now calculate the z rise above pointB
            double zRise1 = zPosition - pointB.Z;
            //And also the z rise above pointC
            double zRise2 = zPosition - pointC.Z;

            //We can now calculatet the x and y points on both sides of the triangle using the z rises that were calculated above
            long xPoint1 = (long)(pointB.X + zToX1 * zRise1);
            long yPoint1 = (long)(pointB.Y + zToY1 * zRise1);
            long xPoint2 = (long)(pointC.X + zToX2 * zRise2);
            long yPoint2 = (long)(pointC.Y + zToY2 * zRise2);

            return new LineSegment(new IntPoint(xPoint1, yPoint1), new IntPoint(xPoint2, yPoint2));
            /*IntPoint start = new IntPoint();
            IntPoint end = new IntPoint();

            start.X = pointA.X + ((pointB.Z != pointA.Z) ? ((pointB.X - pointA.X) * (zPosition - pointA.Z) / (pointB.Z - pointA.Z)) : 0);
            start.Y = pointA.Y + ((pointB.Z != pointA.Z) ? ((pointB.Y - pointA.X) * (zPosition - pointA.Z) / (pointB.Z - pointA.Z)) : 0);
            end.X = pointA.X + ((pointC.Z != pointA.Z) ? ((pointC.X - pointA.X) * (zPosition - pointA.Z) / (pointC.Z - pointA.Z)) : 0);
            end.Y = pointA.Y + ((pointC.Z != pointA.Z) ? ((pointC.Y - pointA.X) * (zPosition - pointA.Z) / (pointC.Z - pointA.Z)) : 0);

            return new LineSegment(start, end);*/
        }

        /// <summary>
        /// This method calculates point a, b and c from point 1, 2 and 3
        /// </summary>
        private bool calculateOrganisedPoints(long zPosition)
        {
            //We need to determine which two sides of the triangle intersect with the z point
            //point A should be the point where these two sides connect
            //point B and C should therefore be the other two points

            if (zPosition == point1.Z && zPosition == point2.Z)
            {
                pointA = point3;
                pointB = point1;
                pointC = point2;
                return true;
            }
            else if (zPosition == point1.Z && zPosition == point3.Z)
            {
                pointA = point2;
                pointB = point3;
                pointC = point1;
                return true;
            }
            else if (zPosition == point2.Z && zPosition == point3.Z)
            {
                pointA = point1;
                pointB = point2;
                pointC = point3;
                return true;
            }
            
            bool oneTwo = false;
            bool oneThree = false;
            bool twoThree = false;

            if ((zPosition <= point1.Z && zPosition >= point2.Z) || (zPosition >= point1.Z && zPosition <= point2.Z))
            {
                oneTwo = true;
            }
            if ((zPosition <= point1.Z && zPosition >= point3.Z) || (zPosition >= point1.Z && zPosition <= point3.Z))
            {
                oneThree = true;
            }
            if ((zPosition <= point2.Z && zPosition >= point3.Z) || (zPosition >= point2.Z && zPosition <= point3.Z))
            {
                twoThree = true;
            }

            if (oneTwo && oneThree && twoThree)
            {
                if (point1.Z < zPosition)
                    twoThree = false;
                else if (point2.Z < zPosition)
                    oneThree = false;
                else if (point3.Z < zPosition)
                    oneTwo = false;
            }

            if (oneTwo && oneThree)
            {
                pointA = point1;
                pointB = point2;
                pointC = point3;
                return true;
            }
            else if (oneTwo && twoThree)
            {
                pointA = point2;
                pointB = point3;
                pointC = point1;
                return true;
            }
            else if (oneThree && twoThree)
            {
                pointA = point3;
                pointB = point1;
                pointC = point2;
                return true;
            }
            /*if (point1.Z < zPosition && point2.Z >= zPosition && point3.Z >= zPosition)
            {
                pointA = point1;
                pointB = point3;
                pointC = point2;
                return true;
            }
            else if (point1.Z > zPosition && point2.Z <= zPosition && point3.Z <= zPosition)
            {
                pointA = point1;
                pointB = point2;
                pointC = point3;
                return true;
            }

            if (point2.Z < zPosition && point1.Z >= zPosition && point3.Z >= zPosition)
            {
                pointA = point2;
                pointB = point1;
                pointC = point3;
                return true;
            }
            else if (point2.Z > zPosition && point1.Z <= zPosition && point3.Z <= zPosition)
            {
                pointA = point2;
                pointB = point3;
                pointC = point1;
                return true;
            }

            if (point3.Z < zPosition && point2.Z >= zPosition && point1.Z >= zPosition)
            {
                pointA = point3;
                pointB = point2;
                pointC = point1;
                return true;
            }
            else if (point3.Z > zPosition && point2.Z <= zPosition && point1.Z <= zPosition)
            {
                pointA = point3;
                pointB = point1;
                pointC = point2;
                return true;
            }*/
            
            return false;
        }

        /// <summary>
        /// This method returns the point of the triangle with the lowest Z coordinate
        /// </summary>
        /// <returns>The lowest point of the triangle</returns>
        public Vector3 lowestPoint
        {
            get
            {
                Vector3 lowPoint = (Math.Min(point1.Z, point2.Z) == point1.Z) ? point1 : point2;
                lowPoint = (Math.Min(lowPoint.Z, point3.Z) == lowPoint.Z) ? lowPoint : point3;
                return lowPoint;
            }
        }

        /// <summary>
        /// This method returns the point of the triangle with the highest Z coordinate
        /// </summary>
        /// <returns>The highest point of the triangle</returns>
        public Vector3 highestPoint
        {
            get
            {
                Vector3 highPoint = (Math.Max(point1.Z, point2.Z) == point1.Z) ? point1 : point2;
                highPoint = (Math.Max(highPoint.Z, point3.Z) == highPoint.Z) ? highPoint : point3;
                return highPoint;
            }
        }
    }
}
