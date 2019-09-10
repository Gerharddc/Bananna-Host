using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;

namespace PolyChopper.Containers
{
    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;

    public class InfillGrid
    {
        /// <summary>
        /// This list contains all the line segments for the grid angled to the right
        /// </summary>
        public Polygons rightLines;

        /// <summary>
        /// This list contains all the line segments for the grid angled to the left
        /// </summary>
        public Polygons leftLines;

        /// <summary>
        /// The method creates a new grid from two lists of line segments
        /// </summary>
        /// <param name="right">The list of line segments angled to the right</param>
        /// <param name="left">The list of line segments angled to the left</param>
        public InfillGrid(Polygons right, Polygons left)
        {
            rightLines = right;
            leftLines = left;
        }
    }
}
