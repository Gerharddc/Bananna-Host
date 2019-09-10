using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using ClipperLib;

using PolyChopper.Containers;

#if WINDOWS_APP || WINDOWS_PHONE_APP
using Windows.Storage;
#endif

namespace PolyChopper
{
    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;

    abstract class ToolPathWriter
    {
        /*private enum SectionType
        {
            Layer,
            Island,
            //Layer segment types
            BottomSegment,
            TopSegment,
            SupportSegment,
            OutlineSegment,
            SkirtSegment,
            InfillSegment,
            RaftSegment
        }

        abstract void startNewSection(SectionType sectionType);
        abstract void startNewLayer();
        abstract void startNewIsland();
        abstract void startNewSegment(LayerSegment);*/

        internal static float currentE = 0;

        internal static float prevX = 0;
        internal static float prevY = 0;
        internal static float prevZ = 0;
        internal static float prev0F = 0;
        internal static float prev1F = 0;

        internal static bool zLifted = false;
        internal static bool retracted = false;

        internal abstract void writeMove(MoveSegment segment);

        internal void writeToolPath(string filePath)
        {

        }
    }

    /// <summary>
    /// This class is responsible for storing the generated toolpath in a
    /// "de Clercq Toolpath" format
    /// </summary>
    public class DTPWriter
    {
        private static BinaryWriter binaryWriter;

        private static void writeMoveSegment(MoveSegment segment)
        {
            //Write the startpoint
            binaryWriter.Write(segment.startPoint.X);
            binaryWriter.Write(segment.startPoint.Y);

            //Write the endpoint
            binaryWriter.Write(segment.endPoint.X);
            binaryWriter.Write(segment.endPoint.Y);

            //Write the start velocity
            binaryWriter.Write(segment.startVelocity.X);
            binaryWriter.Write(segment.startVelocity.Y);

            //Write the peak velocity
            binaryWriter.Write(segment.peakVelocity.X);
            binaryWriter.Write(segment.peakVelocity.Y);

            //Write the end velocity
            binaryWriter.Write(segment.endVelocity.X);
            binaryWriter.Write(segment.endVelocity.Y);

            //Write if isExtruded
            binaryWriter.Write(segment.isExtruded);
        }

        public static void writeDTPFile(string filePath)
        {
            Logger.logProgress("Writing Toolpath");

            FileStream fileStream = new FileStream(filePath, FileMode.Create);
            binaryWriter = new BinaryWriter(fileStream);

            //Write the layer height
            binaryWriter.Write(Global.Values.layerHeight);
            
            //First write the amount of layers
            binaryWriter.Write(Global.Values.layerComponentList.Count);
            System.Diagnostics.Debug.WriteLine("Outline: " + (byte)SegmentType.OutlineSegment);
            //Now we need to write through each layer
            foreach (LayerComponent layer in Global.Values.layerComponentList)
            {
                //Write the layer speed in nanometre per second
                //binaryWriter.Write(layer.layerSpeed);

                //Write the infill speed in nanometre per second
                //binaryWriter.Write(layer.infillSpeed);

                //Write the top speed in nanometre per second
                //binaryWriter.Write(layer.)

                //Write the amount of initial moves
                binaryWriter.Write(layer.intialLayerMoves.Count);

                foreach (MoveSegment move in layer.intialLayerMoves)
                    writeMoveSegment(move);

                //Write the amount of islands
                binaryWriter.Write(layer.islandList.Count);

                foreach (Island island in layer.islandList)
                {
                    binaryWriter.Write(island.segmentList.Count);

                    foreach (LayerSegment segment in island.segmentList)
                    {
                        //Write the amount of moves inside this segment
                        binaryWriter.Write(segment.moveSegments.Count);

                        if (segment.moveSegments.Count == 0)
                            System.Diagnostics.Debug.WriteLine("No moves");
                        
                        //Write the layer segment identifier so that the system knows we are starting with a new segment and what it is
                        binaryWriter.Write((sbyte)segment.segmentType);

                        foreach (MoveSegment move in segment.moveSegments)
                            writeMoveSegment(move);
                    }
                }
            }

            binaryWriter.Dispose();
            //fileStream.Dispose();
        }

        /*private BinaryWriter binaryWriter;

#if WINDOWS_APP || WINDOWS_PHONE_APP
        public async void initWriter(StorageFile file)
        {
            binaryWriter = new BinaryWriter(await file.OpenStreamForWriteAsync());
        }
#endif

#if WINDOWS_APP || WINDOWS_PHONE_APP
        public DTPWriter(StorageFile file)
#else
        public DTPWriter(string filePath)
#endif
        {
#if WINDOWS_APP || WINDOWS_PHONE_APP
            initWriter(file);
#else
            binaryWriter = new BinaryWriter(new FileStream(filePath, FileMode.CreateNew));
#endif
        }*/
    }
}