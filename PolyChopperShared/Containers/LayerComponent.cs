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

    public class LayerComponent
    {
        public int layerNumber;
        public List<Island> islandList = new List<Island>();
        public int layerSpeed = 0; //in nanometre per second
        public List<LineSegment> initialLineList = new List<LineSegment>();

        public Dictionary<int, int> faceToLineIndex = new Dictionary<int, int>();

        //Assign default values here, the settings loader will automatically apply custom ones
        public float flowrate = Global.Values.flowPercentage;
        public int moveSpeed = Global.Values.moveSpeed; // nanometre per second
        public int bridgeingSpeed = Global.Values.bridgeingSpeed; // nanometre per second
        public float supportDensity = Global.Values.supportMaterialDesnity;
        public int supportSpeed = Global.Values.supportSpeed; // nanometre per second
        public float infillDensity = Global.Values.normalInfillDensity;
        public int infillSpeed = Global.Values.normalInfillSpeed; // nanometre per second

        public LayerComponent(int _layerNumer)
        {
            layerNumber = _layerNumer;
            islandList = new List<Island>();
        }

        public LayerComponent(int _layerNumber, List<Island> _islandList)
        {
            layerNumber = _layerNumber;
            islandList = _islandList;
        }

        /// <summary>
        /// This queu contains all the moves that will need to be executed at the begining of the layer
        /// </summary>
        public Queue<MoveSegment> intialLayerMoves = new Queue<MoveSegment>();
    }
}
