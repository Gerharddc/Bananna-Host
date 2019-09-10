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

    /// <summary>
    /// Determines the content of the segment
    /// </summary>
    public enum SegmentType
    {
        BottomSegment,
        TopSegment,
        SupportSegment,
        OutlineSegment,
        SkirtSegment,
        InfillSegment,
        RaftSegment
    }

    /// <summary>
    /// A layer segment is a part of a layer which contains information that will be translated into gcode
    /// </summary>
    public class LayerSegment
    {
        //public bool isSolid = false;
        public SegmentType segmentType = SegmentType.OutlineSegment;
        public int segmentSpeed = 0; // in nanometre per second
        public Polygons outlinePolygons = new Polygons();
        public List<LineSegment> fillLines = new List<LineSegment>();
        public float fillDensity = 0;
        public Queue<MoveSegment> moveSegments = new Queue<MoveSegment>();

        /// <summary>
        /// This value is used to determine the how manieth infill segment this is
        /// </summary>
        public short infillLayerNumber = -1;

        /// <summary>
        /// This value determines how many layers worth of filament should be extruded for this segment
        /// </summary>
        public short infillHeightMultiplier = 1;

        public LayerSegment(SegmentType type)
        {
            segmentType = type;
        }
    }
}
