using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using System.Globalization;

#if WINDOWS_APP || WINDOWS_PHONE_APP
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Core;
#elif DESKTOP
using System.Windows.Controls;
#endif

namespace HostShared.Model
{
    static class ModelImporter
    {
        /// <summary>
        /// This method runs a specified javascript function inside the specified the specified webbrowser
        /// </summary>
        /// <param name="webView">The webbrowser to run the script in</param>
        /// <param name="name">The name of the function to execute</param>
        /// <param name="arguments">The list of arguments to run the function with</param>
#if WINDOWS_APP || WINDOWS_PHONE_APP
        private static async void runScript(WebView webView, string name, string[] arguments)
        {
            await webView.InvokeScriptAsync(name, arguments);
        }
#elif DESKTOP
        private static void runScript(WebBrowser webView, string name, string[] arguments)
        {
            //TODO: async
            webView.InvokeScript(name, arguments);
        }
#endif

#if WINDOWS_APP || WINDOWS_PHONE_APP
        private static Stream fileStream;
        private static WebView _webView;
#elif DESKTOP
        private static FileStream fileStream;
        private static WebBrowser _webView;
#endif

        private static bool isBinary = false;
        private static BinaryReader binaryReader;
        private static uint faceCount = 0;
        private static uint currentFace = 0;
        private static StreamReader streamReader;

        public static string trigForJs = "";

        //The amount of triangles to shift at once
        private const int trigsShiftAmount = 500;

        //This method reads the following bunch of triangles
        public static void readNextTrigs()
        {
            //Tell the server to wait until we are done loading
            trigForJs = "wait";

            string newString = "";

            //Keep track of the amount of triangles that were added
            int amount = 0;

            if (isBinary)
            {
                if (currentFace == faceCount)
                {
                    trigForJs = "done";
                    binaryReader.Dispose();
                    return;
                }

                while (amount < trigsShiftAmount && currentFace < faceCount)
                {
                    newString += readBinaryTrig();
                    currentFace++;
                    amount++;
                }
            }
            else
            {
                if (streamReader.EndOfStream)
                {
                    trigForJs = "done";
                    streamReader.Dispose();
                }

                while (amount < trigsShiftAmount && !streamReader.EndOfStream)
                {
                    newString += readAsciiTrig();
                    amount++;
                }
            }

            newString += amount;
            System.Diagnostics.Debug.WriteLine(newString);
            trigForJs = newString;
        }

        public static string readBinaryTrig()
        {
            string toReturn = "";

            //Read past the normals
            binaryReader.ReadSingle(); binaryReader.ReadSingle(); binaryReader.ReadSingle();

            trigForJs = "";

            //Read the points
            for (int i = 0; i < 9; i++)
                toReturn += binaryReader.ReadSingle().ToString(CultureInfo.InvariantCulture) + ";";

            binaryReader.ReadUInt16();//read the end of the facet

            return toReturn;
        }

        public static string readAsciiTrig()
        {
            string toReturn = "";

            while (!streamReader.ReadLine().Contains("outer loop")) ;

            string readLine = streamReader.ReadLine();
            readLine = readLine.Replace(@"vertex", " ");//remove the word "vertex" from the line
            string[] lineElements = readLine.Split(' ');//split the line at each space
            //the first 7 segments are just empty characters
            toReturn = lineElements[5] + ";";
            toReturn += lineElements[6] + ";";
            toReturn += lineElements[7] + ";";

            readLine = streamReader.ReadLine();
            readLine = readLine.Replace(@"vertex", " ");//remove the word "vertex" from the line
            lineElements = readLine.Split(' ');//split the line at each space
            //the first 7 segments are just empty characters
            toReturn += lineElements[5] + ";";
            toReturn += lineElements[6] + ";";
            toReturn += lineElements[7] + ";";

            readLine = streamReader.ReadLine();
            readLine = readLine.Replace(@"vertex", " ");//remove the word "vertex" from the line
            lineElements = readLine.Split(' ');//split the line at each space
            //the first 7 segments are just empty characters
            toReturn += lineElements[5] + ";";
            toReturn += lineElements[6] + ";";
            toReturn += lineElements[7] + ";";

            return toReturn;
        }

        /// <summary>
        /// This method imports and stl model from a given filePath
        /// </summary>        
#if WINDOWS_APP || WINDOWS_PHONE_APP
        public static async void importModelFile(StorageFile modelFile, WebView webView)
#elif DESKTOP
        public static void importModelFile(string filePath, WebBrowser webView)
#endif
        {
#if WINDOWS_APP || WINDOWS_PHONE_APP          
            /*Stream */fileStream = await modelFile.OpenStreamForReadAsync();
#elif DESKTOP
            /*FileStream */fileStream = new FileStream(filePath, FileMode.Open);//the file stream used to read the stl file
#endif

            binaryReader = new BinaryReader(fileStream);//the binary reader used to check the stl is binary and read it if it is

            binaryReader.ReadBytes(80);//read the binary hearder
            faceCount = binaryReader.ReadUInt32();//read the amount of faces

            if (fileStream.Length != 84 + faceCount * 50)//check if the file is binary by comparing the expected length for one to the actual length
            {
                //we are now reading an ascii stl
                binaryReader.Dispose();//dispose of the binary reader because it is not needed anymore
                fileStream.Dispose();//dispode of the filestream because it is not needed anymore

                //load the ascii stl into the vertex list
#if WINDOWS_APP || WINDOWS_PHONE_APP
                //importAsciiStl(modelFile, webView);
#elif DESKTOP
                //importAsciiStl(filePath, webView);
                StreamReader streamReader = new StreamReader(filePath);
                isBinary = false;
#endif
            }
            else
            {
                //we are now reading a binary stl
                //importBinaryStl(binaryReader, faceCount, webView);//load the binary stl into the vertex list
                //fileStream.Dispose();//we dispose the filestream after it has been used
                isBinary = true;
                currentFace = 0;
            }

            //Create the model
            //runScript(webView, "addModel", new string[] { "ding" });
            readNextTrigs();
            runScript(webView, "startImport", null);
            System.Diagnostics.Debug.WriteLine("Started import");

            //runScript(webView, "startImport", null);
        }
        /*
        /// <summary>
        /// This method imports a binary stl file with a gvien amount of faces from a given binaryreader instance
        /// </summary>
        /// <param name="binaryReader">The binary reader that has already read the heading and facecount of a binary stl file</param>
        /// <param name="faceCount">The amount of faces in the stl model</param>
#if DESKTOP
        private static void importBinaryStl(BinaryReader binaryReader, uint faceCount, WebBrowser webView)
#elif WINDOWS_APP || WINDOWS_PHONE_APP
        private static void importBinaryStl(BinaryReader binaryReader, uint faceCount, WebView webView)
#endif
        {
            string v1x, v1y, v1z, v2x, v2y, v2z, v3x, v3y, v3z;

            for (uint i = 0; i < faceCount; i++)
            {
                //Read past the normals
                binaryReader.ReadSingle(); binaryReader.ReadSingle(); binaryReader.ReadSingle();

                //Read the points
                v1x = binaryReader.ReadSingle().ToString(CultureInfo.InvariantCulture);
                v1y = binaryReader.ReadSingle().ToString(CultureInfo.InvariantCulture);
                v1z = binaryReader.ReadSingle().ToString(CultureInfo.InvariantCulture);
                v2x = binaryReader.ReadSingle().ToString(CultureInfo.InvariantCulture);
                v2y = binaryReader.ReadSingle().ToString(CultureInfo.InvariantCulture);
                v2z = binaryReader.ReadSingle().ToString(CultureInfo.InvariantCulture);
                v3x = binaryReader.ReadSingle().ToString(CultureInfo.InvariantCulture);
                v3y = binaryReader.ReadSingle().ToString(CultureInfo.InvariantCulture);
                v3z = binaryReader.ReadSingle().ToString(CultureInfo.InvariantCulture);

                //Create the triangle
                runScript(webView, "addTriangle", new string[] { v1x, v1y, v1z, v2x, v2y, v2z, v3x, v3y, v3z });

                binaryReader.ReadUInt16();//read the end of the facet
            }

            binaryReader.Dispose();
        }

        /// <summary>
        /// This method imports an ascii stl model from a given filepath
        /// </summary>
        /// <param name="filePath">The filepath of the model to import</param>
#if WINDOWS_APP || WINDOWS_PHONE_APP
        private static async void importAsciiStl(StorageFile file, WebView webView)
#elif DESKTOP
        private static void importAsciiStl(string filePath, WebBrowser webView)
#endif
        {
#if WINDOWS_APP || WINDOWS_PHONE_APP
            StreamReader streamReader = new StreamReader(await file.OpenStreamForReadAsync());
#elif DESKTOP
            StreamReader streamReader = new StreamReader(filePath);
#endif

            string readLine = "";

            string[] lineElements;

            string v1x, v1y, v1z, v2x, v2y, v2z, v3x, v3y, v3z;

            while (!streamReader.EndOfStream)
            {
                readLine = streamReader.ReadLine();

                if (readLine.Contains("outer loop"))
                {
                    readLine = streamReader.ReadLine();
                    readLine = readLine.Replace(@"vertex", " ");//remove the word "vertex" from the line
                    lineElements = readLine.Split(' ');//split the line at each space
                    //the first 7 segments are just empty characters
                    v1x = lineElements[5];
                    v1y = lineElements[6];
                    v1z = lineElements[7];

                    readLine = streamReader.ReadLine();
                    readLine = readLine.Replace(@"vertex", " ");//remove the word "vertex" from the line
                    lineElements = readLine.Split(' ');//split the line at each space
                    //the first 7 segments are just empty characters
                    v2x = lineElements[5];
                    v2y = lineElements[6];
                    v2z = lineElements[7];

                    readLine = streamReader.ReadLine();
                    readLine = readLine.Replace(@"vertex", " ");//remove the word "vertex" from the line
                    lineElements = readLine.Split(' ');//split the line at each space
                    //the first 7 segments are just empty characters
                    v3x = lineElements[5];
                    v3y = lineElements[6];
                    v3z = lineElements[7];

                    //Create the triangle
                    runScript(webView, "addTriangle", new string[] { v1x, v1y, v1z, v2x, v2y, v2z, v3x, v3y, v3z });
                }
            }
        }
        */
    }
}
