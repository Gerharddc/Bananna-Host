#define DTP_OUT
#define GCODE_OUT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if WINDOWS_APP || WINDOWS_PHONE_APP
using Windows.Storage;
#endif

namespace PolyChopper
{
    /// <summary>
    /// This class is responsible for executing the slicing modules int the correct sequence
    /// </summary>
    public class PolyChopper
    {
        public Logger.logEventHandler logEvent;

        public PolyChopper()
        {
            Logger.logEvent += Logger_logEvent;
        }

        /// <summary>
        /// This method slices a specified model, with a specified output and config file by applying the slicing
        /// modules in the correct order
        /// </summary>
        /// <param name="inputPath">The filepath of the input stl model</param>
        /// <param name="outputPath">The filepath of the output gcode file</param>
        /// <param name="settingsPath">The filepath of the config file</param>
#if WINDOWS_APP || WINDOWS_PHONE_APP
        public void sliceFile(StorageFile inputFile, StorageFile outputFile, StorageFile settingsFile)
#else
        public void sliceFile(string inputFile, string outputFile, string settingsFile)
#endif
        {
            //Logger.logEvent += Logger_logEvent;

            Global.Values = new GlobalValues();

            SettingsImporter.importSettingsFromFile(settingsFile);
           
            ModelImporter.importModelFile(inputFile);

            ModelImporter.calculateModelSize();

            ModelImporter.optimiseOriginalTriangles();
            
            Global.Values.layerCount = ModelImporter.calculateAmountOfLayer();

            SettingsImporter.setupLayerValues();
           
            LayerSlicer.sliceTrianglesIntoLayers(); //Werk kosher
                      
            LayerAnaliser.calculateIslandsFromOriginalLines(); //Werk kosher

            Global.Values.initialTriangleList = null;

            PolygonOptimiser.optimiseOutlinePolygons(); //Werk kosher ma kan dalk optimize

            LayerAnaliser.generateOutlineSegments();
            
            InfillGenerator.generateInfillGrids();

            //The top and bottom segments need to calculated before the infill outlines otherwise the infill will be seen as top or bottom
            
            LayerAnaliser.calculateToBottomSegments();
            
            LayerAnaliser.calculateInfillSegments();
            
            LayerAnaliser.calculateSupportSegments();

            LayerAnaliser.combineInfillSegments();
            
            InfillGenerator.trimInfillGridsToFillSegments();
            
            RaftGenerator.generateRaft();

            //Calculate skirt here sothat it does not affect the above operations
            SkirtCreator.generateSkirt();

            ToolpathGenerator.calculateToolPath();

            AccelerationCalculator.calculateAcceleration();

            //GcodeWriter.writeGCode(outputFile);

            DTPWriter.writeDTPFile(outputFile);

            /*GcodeGenerator.calculateMoveSegments();            

            ValueEstimator.estimateLayerTimesAndFix();

            ValueEstimator.estimateValues();

            GcodeWriter gcodeWriter = new GcodeWriter(outputFile);

            gcodeWriter.writeStartCode();           
            
            gcodeWriter.writeMoveSegments();

            gcodeWriter.writeEndCode();*/

            Logger.logProgress("Finished");
        }

        void Logger_logEvent(string message)
        {
            if (logEvent != null)
                logEvent(message);
        }
    }
}
