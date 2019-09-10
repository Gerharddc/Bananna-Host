using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using MathUtils;
using PolyChopper.Containers;

namespace PolyChopper
{
    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;

    /// <summary>
    /// This enumurator contains the currently supported printing materials
    /// </summary>
    public enum MaterialType
    {
        Default,
        PLA,
        ABS,
        Pen
    }

    /// <summary>
    /// This class allows for the parsing of text into a material type identifier
    /// </summary>
    public static class MaterialTypeParser
    {
        public static bool TryParse(string text, out MaterialType output)
        {
            try
            {
                if (text == "Default")
                {
                    output = MaterialType.Default;
                    return true;
                }
                else if (text == "PLA")
                {
                    output = MaterialType.PLA;
                    return true;
                }
                else if (text == "ABS")
                {
                    output = MaterialType.ABS;
                    return true;
                }
                else if (text == "Pen")
                {
                    output = MaterialType.Pen;
                    return true;
                }
                else
                {
                    output = MaterialType.Default;
                    return false;
                }
            }
            catch (Exception)
            {
                output = MaterialType.Default;
                return false;
            }
        }
    }

    /// <summary>
    /// This class simply allows for the regeneration of the Global Values
    /// </summary>
    public static class Global
    {
        public static GlobalValues Values = new GlobalValues();
    }

    /// <summary>
    /// This is a class contains all the global values and settings that will be used by all modules
    /// </summary>
    public class GlobalValues
    {
        public long bedWidth = 0; //Measured in nanometre
        public long bedLength = 0; //Measured in nanometre
        public long bedHeigth = 0; //Measured in nanometre

        public ushort bottomLayerCount = 0;
        public ushort topLayerCount = 0;

        /// <summary>
        /// Value out of 1.0f representing percentage out of 100%
        /// Amount of extruded filament will be multiplied by this
        /// </summary>
        public float flowPercentage = 0;

        public int initialLayerSpeed = 0; //Measured in nanometre per second
        public int normalLayerSpeed = 0; //Measured in nanometre per second
        public int normalInfillSpeed = 0; //Measured in nanometre per second
        public int bridgeingSpeed = 0; //Measured in nanometre per second
        public int moveSpeed = 0; //Measured in nanometre per second

        /// <summary>
        /// Value out of 1.0f representing % solidity of infill
        /// </summary>
        public float normalInfillDensity = 0;

        /// <summary>
        /// String containing special layer speed instrcutions
        /// </summary>
        public string specialLayerSpeeds = "";

        /// <summary>
        /// String containing special layer infill speed instrcutions
        /// </summary>
        public string specialInfillSpeeds = "";

        /// <summary>
        /// String containing special layer density instrcutions
        /// </summary>
        public string specialLayerDensities = "";

        public int nozzleWidth = 0; //Measured in nanometre
        public int filamentWidth = 0; //Measured in nanometre
        public int layerHeight = 0; //Measured in nanometre

        public MaterialType materialType = MaterialType.Default;

        public float costPerKg = 0;
        public float costPerMinute = 0;

        public int skirtSpeed = 0;  //Measured in nanometre per second
        public ushort skirtCount = 0;
        public uint skirtDistance = 0;  //Measured in nanometre
        public bool shouldSkirt = false;

        public ushort shellThickness = 0; //Amount of shells

        public bool shouldSupportMaterial = false;
        /// <summary>
        /// Value out of 1.0f representing density of support material out of 100%
        /// </summary>
        public float supportMaterialDesnity = 0;

        public int supportSpeed = 0;

        public string startCode = "";
        public string endCode = "";

        /// <summary>
        /// List that intially contains all triangles loaded from file
        /// </summary>
        public List<Triangle> initialTriangleList = new List<Triangle>();

        public int layerCount = 0;
        /// <summary>
        /// List containing layerComponents for each layer
        /// </summary>
        public List<LayerComponent> layerComponentList = new List<LayerComponent>();

        /// <summary>
        /// Estimated printing time in seconds
        /// </summary>
        public long printingTimeEstimate = 0;
        /// <summary>
        /// Estimated filament usage in milligram
        /// </summary>
        public long filamentUsageEstimate = 0;
        /// <summary>
        /// Estimated printing cost in cents
        /// </summary>
        public long printingCostEstimate = 0;

        public Dictionary<float, InfillGrid> infillGrids = new Dictionary<float, InfillGrid>();

        public int retractionSpeed = 0; //Retraction feedrate in nanometre per second
        public uint retractionDistance = 0; //Retraction distance in nanometre

        public bool shouldRaft = false;
        public float raftDensity = 0; //Density of raft as % out of 1
        public uint raftDistance = 0; //Distance of raft outline from first layer outline
        public ushort raftCount = 0; //Amount of raft layers
        public int raftSpeed = 0; //Raft print speed in mm per minute

        public ushort infillCombinationCount = 1;

        public string specialFlowrates = "";
        public string specialMoveSpeeds = "";
        public string specialBridgeingSpeeds = "";
        public string specialSupportDensities = "";
        public string specialSupportSpeeds = "";

        public long modelXSize = 0;
        public long modelYSize = 0;
        public long modelZSize = 0;

        public long modelMinX = long.MaxValue;
        public long modelMinY = long.MaxValue;
        public long modelMinZ = long.MaxValue;

        public long modelMaxX = long.MinValue;
        public long modelMaxY = long.MinValue;
        public long modelMaxZ = long.MinValue;

        public float minimumLayerTime = 3; //seconds
        public int minimumLayerSpeed = 10000; //nanometre per second

        public Polygons svgPolygons = new Polygons();

        public short printingTemperature = 0;

        public int maxAccel = 1000000; // nanometre / s / s
        public int maxJump = 100000; // nanometre / s
    }
}
