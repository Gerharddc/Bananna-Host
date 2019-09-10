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

    public class Island
    {
        public Polygons outlinePolygons = new Polygons();
        public List<LayerSegment> segmentList = new List<LayerSegment>();
        public float IslandSpeed = 0;

        /// <summary>
        /// This method returns all segments in the local segment list which are of the required type
        /// </summary>
        /// <param name="type">The type of segment that should be returned</param>
        /// <returns>The list of layersegments in the local list which are of the correct type</returns>
        public List<LayerSegment> specialSegmentList(SegmentType type)
        {
            List<LayerSegment> returnList = new List<LayerSegment>();

            foreach (LayerSegment segment in segmentList)
                if (segment.segmentType == type)
                    returnList.Add(segment);

            return returnList;
        }
    }
}
