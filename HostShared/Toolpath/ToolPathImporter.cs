using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;

namespace HostShared.Toolpath
{
    class ToolPathImporter
    {
        private static void runScript(WebBrowser webView, string name, string[] arguments)
        {
            //TODO: async
            webView.InvokeScript(name, arguments);
        }

        private static BinaryReader reader;

        private static int layerCount; //Amount of layers in model
        private static int islandCount; //Amount of islands in current layer
        private static int segmentCount; //Amount of segments in current island
        //private static int moveCount; //Amount of moves in current segment
        public static double layerHeight; //Layer height in mm
        private static bool busyWithInitial = true; //Busy with the initial layer moves

        public static string linesForJs = "wait";

        private static int currentLayer = 0;
        private static int currentIsland = 0;
        private static int currentSegment = 0;

        private static double currentZ = 0;

        //The amount of lines to shift at once
        //private const int linesToShift = 1500;

        private static void incrementSegment()
        {
            if (busyWithInitial)
            {
                islandCount = reader.ReadInt32();
                segmentCount = reader.ReadInt32();
                currentSegment = 0;
                currentIsland = 0;
                busyWithInitial = false;
            }
            else
            {
                currentSegment++;

                if (currentSegment == segmentCount)
                {
                    currentSegment = 0;
                    currentIsland++;

                    if (currentIsland == islandCount)
                    {
                        currentLayer++;
                        currentZ += layerHeight;
                        busyWithInitial = true;
                    }
                    else
                    {
                        segmentCount = reader.ReadInt32();
                    }
                }
            }
        }

        public static void readNextLayer()
        {
            linesForJs = "wait";

            bool isExtruded = false;
            sbyte segmentType = -1;

            if (currentLayer == layerCount)
            {
                linesForJs = "done";
                reader.Dispose();
                return;
            }

            var startLayer = currentLayer;

            var newLines = "";

            while (currentLayer == startLayer)
            {
                string newString = "";

                int moveCount = reader.ReadInt32();

                if (moveCount == 0)
                {
                    //incrementSegment();
                    //readNextSegment();
                    //return;
                    newString = "none";
                }

                if (!busyWithInitial)
                {
                    segmentType = reader.ReadSByte(); //Segment type
                }

                for (int i = 0; i < moveCount; i++)
                {
                    double sA = reader.ReadInt64() / 1000000.0;
                    double sB = reader.ReadInt64() / 1000000.0;
                    double eA = reader.ReadInt64() / 1000000.0;
                    double eB = reader.ReadInt64() / 1000000.0;

                    double sX = 0.5 * (sA + sB);
                    double sY = 0.5 * (sA - sB);
                    double eX = 0.5 * (eA + eB);
                    double eY = 0.5 * (eA - eB);

                    //Start velocity
                    reader.ReadInt64();
                    reader.ReadInt64();

                    //Peak velocity
                    reader.ReadInt64();
                    reader.ReadInt64();

                    //End velocity
                    reader.ReadInt64();
                    reader.ReadInt64();

                    //Extruded
                    isExtruded = reader.ReadBoolean();

                    if (isExtruded)
                        newString += sX + ";" + sY + ";" + eX + ";" + eY + ";" + currentZ + ";";
                }

                incrementSegment();

                if (segmentType != 5)
                {
                    newLines += newString;
                }
            }

            linesForJs = newLines;
        }

        public static void readNextSegment()
        {
            linesForJs = "wait";

            bool isExtruded = false;
            sbyte segmentType = -1;

            if (currentLayer == layerCount)
            {
                linesForJs = "done";
                reader.Dispose();
                return;
            }

            string newString = "";

            int moveCount = reader.ReadInt32();

            if (moveCount == 0)
            {
                //incrementSegment();
                //readNextSegment();
                //return;
                newString = "none";
            }

            if (!busyWithInitial)
            {
                segmentType = reader.ReadSByte(); //Segment type
            }

            for (int i = 0; i < moveCount; i++)
            {
                double sA = reader.ReadInt64() / 1000000.0;
                double sB = reader.ReadInt64() / 1000000.0;
                double eA = reader.ReadInt64() / 1000000.0;
                double eB = reader.ReadInt64() / 1000000.0;

                double sX = 0.5 * (sA + sB);
                double sY = 0.5 * (sA - sB);
                double eX = 0.5 * (eA + eB);
                double eY = 0.5 * (eA - eB);

                //Start velocity
                reader.ReadInt64();
                reader.ReadInt64();

                //Peak velocity
                reader.ReadInt64();
                reader.ReadInt64();

                //End velocity
                reader.ReadInt64();
                reader.ReadInt64();

                //Extruded
                isExtruded = reader.ReadBoolean();

                if (isExtruded)
                    newString += sX + ";" + sY + ";" + eX + ";" + eY + ";" + currentZ + ";";
            }

            //if (newString == "")
              //  throw new Exception("no moves");
            incrementSegment();

            if (segmentType != 5)
            {
                linesForJs = newString;
            }
            else
            {
                linesForJs = "none";
            }
        }

        public static void importToolpathFile(string filePath, WebBrowser webView)
        {
            FileStream fileStream = new FileStream(filePath, FileMode.Open);
            reader = new BinaryReader(fileStream);

            currentIsland = 0;
            currentLayer = 0;
            currentSegment = 0;
            currentZ = 0;
            busyWithInitial = true;

            layerHeight = reader.ReadInt32() / 1000000.0;
            layerCount = reader.ReadInt32();

            //readNextSegment();
            readNextLayer();

            runScript(webView, "startToolpathImport", null);
        }
    }
}
