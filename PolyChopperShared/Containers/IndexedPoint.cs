using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using MathUtils;

namespace PolyChopper.Containers
{
    class IndexedPoint
    {
        /// <summary>
        /// This value contains the physical point of the indexed point
        /// </summary>
        public Vector3 p;

        /// <summary>
        /// This value contains the index of the point in a the list. Only points in a hashtable should use this
        /// </summary>
        public int index;

        /// <summary>
        /// This list specifies the indices of all the triangles that contain this point
        /// </summary>
        public List<int> indexList;

        /// <summary>
        /// This method creates new indexed point with the specified index being that of the first triangle that contains this point
        /// </summary>
        /// <param name="_p">The physical point</param>
        /// <param name="_index">The index of the first triangle that contains the point</param>
        public IndexedPoint(Vector3 _p, int _index)
        {
            p = _p;
            indexList = new List<int>();
            indexList.Add(_index);
        }

        /// <summary>
        /// This operator compares only the physical points of two indexed points
        /// </summary>
        /// <param name="p1">The first point to compare</param>
        /// <param name="p2">The second point to compare</param>
        /// <returns>If the physical points are equal</returns>
        public static bool operator ==(IndexedPoint p1, IndexedPoint p2)
        {
            if (p1.p.X == p2.p.X && p1.p.Y == p2.p.Y && p1.p.Z == p2.p.Z)
                return true;
            else
                return false;
        }

        public static bool operator !=(IndexedPoint p1, IndexedPoint p2)
        {
            return !(p1 == p2);
        }
    }
}
