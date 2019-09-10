using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PolyChopper.Containers;
using MathUtils;
using ClipperLib;
using System.Xml;
using System.Runtime.CompilerServices;
using System.Globalization;

#if WINDOWS_APP || WINDOWS_PHONE_APP
using Windows.Storage;
#else
using System.Windows.Media;
using System.IO.IsolatedStorage;
#endif

namespace PolyChopper
{
    static class ModelImporter
    {
        /// <summary>
        /// This method imports and stl model from a given filePath
        /// </summary>
        /// <param name="filePath"></param>        
#if WINDOWS_APP || WINDOWS_PHONE_APP
        public static async void importModelFile(StorageFile modelFile)
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void importModelFile(string filePath)
#endif
        {
            Logger.logProgress("Importing model");

            /*if (Global.Values.materialType == MaterialType.Pen)
            {
                importSvg(filePath);
                return;
            }*/

#if WINDOWS_PHONE
            IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(filePath, FileMode.Open, IsolatedStorageFile.GetUserStoreForApplication());
#elif WINDOWS_APP || WINDOWS_PHONE_APP
            Stream fileStream = await modelFile.OpenStreamForReadAsync();
#else
            FileStream fileStream = new FileStream(filePath, FileMode.Open);//the file stream used to read the stl file
#endif
            BinaryReader binaryReader = new BinaryReader(fileStream);//the binary reader used to check the stl is binary and read it if it is

            binaryReader.ReadBytes(80);//read the binary hearder
            uint faceCount = binaryReader.ReadUInt32();//read the amount of faces

            if (fileStream.Length != 84 + faceCount * 50)//check if the file is binary by comparing the expected length for one to the actual length
            {
                //we are now reading an ascii stl
                binaryReader.Dispose();//dispose of the binary reader because it is not needed anymore
                fileStream.Dispose();//dispode of the filestream because it is not needed anymore

                //load the ascii stl into the vertex list
#if WINDOWS_APP || WINDOWS_PHONE_APP
                importAsciiStl(modelFile);
#else
                importAsciiStl(filePath);
#endif
            }
            else
            {
                //we are now reading a binary stl
                importBinaryStl(binaryReader, faceCount);//load the binary stl into the vertex list
                fileStream.Dispose();//we dispose the filestream after it has been used
            }
        }

        /// <summary>
        /// This method imports and Svg file as a list of polygons
        /// </summary>  
        /// <param name="filePath">The path of the svg</param>
        private static void importSvg(string filePath)
        {/*
            //First load the svg file
            XmlDocument document = new XmlDocument();
            document.Load(filePath);

            XmlNodeList elements = document.GetElementsByTagName("svg");


            string a = "sdjghfjkdh";

            foreach (XmlNode element in elements)
            {
                foreach (XmlNode child in element.ChildNodes)
                {
                    if (child.Name != "path")
                        continue;

                    string childText = child.OuterXml;

                    StringBuilder text = new StringBuilder();

                    bool started = false;
                    bool done = false;

                    char lastChar = ' ';
                    char lastChar2 = ' ';
                    char lastChar3 = ' ';

                    for (int i = 0; i < childText.Length && !done; i++)
                    {
                        char curChar = childText[i];

                        if (!started)
                        {
                            if ((lastChar3 == ' ' || lastChar3 == '\n') && lastChar2 == 'd' && lastChar == '=' && curChar == '"')
                            {
                                started = true;
                                i++;

                                while (childText[i] != '"')
                                {
                                    text.Append(childText[i]);

                                    i++;
                                }
                            }

                            lastChar3 = lastChar2;
                            lastChar2 = lastChar;
                            lastChar = curChar;
                        }
                    }

                    List<List<IntPoint>> lines = new List<List<IntPoint>>();

                    var data = text.ToString();

                    var geometry = Geometry.Parse(data);

                    var pathGeometry = PathGeometry.CreateFromGeometry(geometry);

                    var polygon = pathGeometry.GetFlattenedPathGeometry(0.01, ToleranceType.Absolute);

                    foreach (var figure in polygon.Figures)
                    {
                        List<IntPoint> line = new List<IntPoint>();

                        foreach (var segment in figure.Segments)
                        {
                            if (!segment.IsSealed)
                                continue;

                            //No idea why but the y coordinates become inverted so we invert them again to fix this

                            if (segment.GetType() == typeof(PolyLineSegment))
                            {
                                var polyLine = segment as PolyLineSegment;

                                foreach (var point in polyLine.Points)
                                {
                                    line.Add(new IntPoint(point.X * 1000000, -point.Y * 1000000));
                                }
                            }
                            else if (segment.GetType() == typeof(System.Windows.Media.LineSegment))
                            {
                                var polyLine = segment as System.Windows.Media.LineSegment;

                                line.Add(new IntPoint(polyLine.Point.X * 1000000, -polyLine.Point.Y * 1000000));
                            }
                        }

                        lines.Add(line);
                    }

                    Global.Values.svgPolygons.AddRange(Clipper.SimplifyPolygons(lines, PolyFillType.pftEvenOdd));
                }
            }

            //We now need to move the model so that the smallest x and y are 0

            long minX = long.MaxValue;
            long minY = long.MaxValue;

            foreach (List<IntPoint> polygon in Global.Values.svgPolygons)
            {
                foreach (IntPoint point in polygon)
                {
                    if (point.X < minX)
                        minX = point.X;

                    if (point.Y < minY)
                        minY = point.Y;
                }
            }

            for (int i = 0; i < Global.Values.svgPolygons.Count; i++)
            {
                List<IntPoint> polygon = Global.Values.svgPolygons[i];

                for (int j = 0; j < polygon.Count; j++)
                {
                    //polygon[j].X -= minX;
                    //polygon[j].Y -= minY;
                    polygon[j] = new IntPoint(polygon[j].X - minX, polygon[j].Y - minY);
                }
            }*/
        }

        /// <summary>
        /// This method imports a binary stl file with a gvien amount of faces from a given binaryreader instance
        /// </summary>
        /// <param name="binaryReader">The binary reader that has already read the heading and facecount of a binary stl file</param>
        /// <param name="faceCount">The amount of faces in the stl model</param>
        private static void importBinaryStl(BinaryReader binaryReader, uint faceCount)
        {
            Vector3 p1;
            Vector3 p2;
            Vector3 p3;

            for (uint i = 0; i < faceCount; i++)
            {
                //Read past the normals
                for (ushort j = 0; j < 3; j++)
                    binaryReader.ReadSingle();

                //Read the 3 points of the triangle
                p1 = new Vector3(binaryReader.ReadSingle() * 1000000,
                    binaryReader.ReadSingle() * 1000000, binaryReader.ReadSingle() * 1000000);
                p2 = new Vector3(binaryReader.ReadSingle() * 1000000,
                    binaryReader.ReadSingle() * 1000000, binaryReader.ReadSingle() * 1000000);
                p3 = new Vector3(binaryReader.ReadSingle() * 1000000,
                    binaryReader.ReadSingle() * 1000000, binaryReader.ReadSingle() * 1000000);

                //Add the triangle to the list
                Global.Values.initialTriangleList.Add(new Triangle(p1, p2, p3));

                binaryReader.ReadUInt16();//read the end of the facet
            }

            binaryReader.Dispose();
        }

        /// <summary>
        /// This method imports an ascii stl model from a given filepath
        /// </summary>
        /// <param name="filePath">The filepath of the model to import</param>
#if WINDOWS_APP || WINDOWS_PHONE_APP
        private static async void importAsciiStl(StorageFile file)
#else
        private static void importAsciiStl(string filePath)
#endif
        {
#if WINDOWS_PHONE
            IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream("chopperconf.txt", FileMode.Open, IsolatedStorageFile.GetUserStoreForApplication());

            StreamReader streamReader = new StreamReader(isoStream);
#elif WINDOWS_APP || WINDOWS_PHONE_APP
            StreamReader streamReader = new StreamReader(await file.OpenStreamForReadAsync());
#else
            StreamReader streamReader = new StreamReader(filePath);
#endif

            string readLine = "";

            Vector3 p1, p2, p3;

            string[] lineElements;

            float v1, v2, v3;

            while (!streamReader.EndOfStream)
            {
                readLine = streamReader.ReadLine();

                if (readLine.Contains("outer loop"))
                {
                    readLine = streamReader.ReadLine();
                    readLine = readLine.Replace(@"vertex", " ");//remove the word "vertex" from the line
                    lineElements = readLine.Split(' ');//split the line at each space
                    //the first 7 segments are just empty characters
                    v1 = float.Parse(lineElements[5], CultureInfo.InvariantCulture);//We need to use US formatting because the strings
                    v2 = float.Parse(lineElements[6], CultureInfo.InvariantCulture);//contain "."s which represent the decimal points
                    v3 = float.Parse(lineElements[7], CultureInfo.InvariantCulture);
                    p1 = new Vector3(v1 * 1000000, v2 * 1000000, v3 * 1000000);//store the vertex value

                    readLine = streamReader.ReadLine();
                    readLine = readLine.Replace(@"vertex", " ");//remove the word "vertex" from the line
                    lineElements = readLine.Split(' ');//split the line at each space
                    //the first 7 segments are just empty characters
                    v1 = float.Parse(lineElements[5], CultureInfo.InvariantCulture);//We need to use US formatting because the strings
                    v2 = float.Parse(lineElements[6], CultureInfo.InvariantCulture);//contain "."s which represent the decimal points
                    v3 = float.Parse(lineElements[7], CultureInfo.InvariantCulture);
                    p2 = new Vector3(v1 * 1000000, v2 * 1000000, v3 * 1000000);//store the vertex value

                    readLine = streamReader.ReadLine();
                    readLine = readLine.Replace(@"vertex", " ");//remove the word "vertex" from the line
                    lineElements = readLine.Split(' ');//split the line at each space
                    //the first 7 segments are just empty characters
                    v1 = float.Parse(lineElements[5], CultureInfo.InvariantCulture);//We need to use US formatting because the strings
                    v2 = float.Parse(lineElements[6], CultureInfo.InvariantCulture);//contain "."s which represent the decimal points
                    v3 = float.Parse(lineElements[7], CultureInfo.InvariantCulture);
                    p3 = new Vector3(v1 * 1000000, v2 * 1000000, v3 * 1000000);//store the vertex value

                    //Add the triangle to the list
                    Global.Values.initialTriangleList.Add(new Triangle(p1, p2, p3));
                }
            }
        }

        /// <summary>
        /// This method calculates the amount of layers in the imported triangles
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int calculateAmountOfLayer()
        {
            //If plotting, we only want 1 layer
            if (Global.Values.materialType == MaterialType.Pen)
                return 1;

            return (int)(Math.Floor((double)Global.Values.modelZSize / Global.Values.layerHeight));
        }
        
        /// <summary>
        /// This method calculates the minimum, maximum and variation values of each axis
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void calculateModelSize()
        {
            //We do not need to calculate the size of a plotting model because we will not have to do any infill calculations
            if (Global.Values.materialType == MaterialType.Pen)
                return;

            foreach (Triangle triangle in Global.Values.initialTriangleList)
            {
                if (triangle.Point1.X < Global.Values.modelMinX)
                    Global.Values.modelMinX = triangle.Point1.X;
                else if (triangle.Point1.X > Global.Values.modelMaxX)
                    Global.Values.modelMaxX = triangle.Point1.X;

                if (triangle.Point2.X < Global.Values.modelMinX)
                    Global.Values.modelMinX = triangle.Point2.X;
                else if (triangle.Point2.X > Global.Values.modelMaxX)
                    Global.Values.modelMaxX = triangle.Point2.X;

                if (triangle.Point3.X < Global.Values.modelMinX)
                    Global.Values.modelMinX = triangle.Point3.X;
                else if (triangle.Point3.X > Global.Values.modelMaxX)
                    Global.Values.modelMaxX = triangle.Point3.X;

                if (triangle.Point1.Y < Global.Values.modelMinY)
                    Global.Values.modelMinY = triangle.Point1.Y;
                else if (triangle.Point1.Y > Global.Values.modelMaxY)
                    Global.Values.modelMaxY = triangle.Point1.Y;

                if (triangle.Point2.Y < Global.Values.modelMinY)
                    Global.Values.modelMinY = triangle.Point2.Y;
                else if (triangle.Point2.Y > Global.Values.modelMaxY)
                    Global.Values.modelMaxY = triangle.Point2.Y;

                if (triangle.Point3.Y < Global.Values.modelMinY)
                    Global.Values.modelMinY = triangle.Point3.Y;
                else if (triangle.Point3.Y > Global.Values.modelMaxY)
                    Global.Values.modelMaxY = triangle.Point3.Y;

                if (triangle.Point1.Z < Global.Values.modelMinZ)
                    Global.Values.modelMinZ = triangle.Point1.Z;
                else if (triangle.Point1.Z > Global.Values.modelMaxZ)
                    Global.Values.modelMaxZ = triangle.Point1.Z;

                if (triangle.Point2.Z < Global.Values.modelMinZ)
                    Global.Values.modelMinZ = triangle.Point2.Z;
                else if (triangle.Point2.Z > Global.Values.modelMaxZ)
                    Global.Values.modelMaxZ = triangle.Point2.Z;

                if (triangle.Point3.Z < Global.Values.modelMinZ)
                    Global.Values.modelMinZ = triangle.Point3.Z;
                else if (triangle.Point3.Z > Global.Values.modelMaxZ)
                    Global.Values.modelMaxZ = triangle.Point3.Z;
            }

            Global.Values.modelXSize = Global.Values.modelMaxX - Global.Values.modelMinX;
            Global.Values.modelYSize = Global.Values.modelMaxY - Global.Values.modelMinY;
            Global.Values.modelZSize = Global.Values.modelMaxZ - Global.Values.modelMinZ;
        }

        /// <summary>
        /// This method calculates a hash value for a given vector3
        /// </summary>
        /// <param name="p">The vector to hash</param>
        /// <returns>The hash</returns>
        private static int hashForPoint(Vector3 p)
        {
            //The + 2s are so that we do not get 0 or 1
            int x = (int)((double)p.X / Math.Sqrt(Global.Values.modelXSize)) + 2;
            int y = (int)((double)p.Y / Math.Sqrt(Global.Values.modelYSize)) + 2;
            int z = (int)((double)p.Z / Math.Sqrt(Global.Values.modelZSize)) + 2;

            return x * y * z;
        }

        /// <summary>
        /// This method optimises a model by removing duplicate triangles and determining which triangles touch each
        /// other on each side.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void optimiseOriginalTriangles()
        {
            //We do not work with triangles in plotting mode
            if (Global.Values.materialType == MaterialType.Pen)
                return;

            //To optimise the triangles we will firstly go through all the the triangles and check if all three of its points already exist and then check
            //if any of the existing references is to an existing triangle containg all three points

            //This list will contain the new triangles which are not duplicates and contain the indices of the triangles touching it
            List<Triangle> newTriangles = new List<Triangle>();

            //This list will contain all the points that exist in the model in an indexed form where they have the indices of all the triangles that they
            //are used in
            List<IndexedPoint> newPoints = new List<IndexedPoint>();

            //This dictionary contains a hashtable whith all the points to allow us to detect if a point already exists much faster, the points also contain
            //there index in the large list
            Dictionary<int, List<IndexedPoint>> pMap = new Dictionary<int, List<IndexedPoint>>();

            //We will now go through each triangle and add it to the new list if not a duplicate
            for (int i = 0; i < Global.Values.initialTriangleList.Count(); i++)
            {
                var triangle = Global.Values.initialTriangleList[i];

                //Calculate the hash values of the three points
                int hashA = hashForPoint(triangle.Point1);
                int hashB = hashForPoint(triangle.Point2);
                int hashC = hashForPoint(triangle.Point3);

                bool duplicate = false;

                IndexedPoint tA = new IndexedPoint(triangle.Point1, newTriangles.Count());
                IndexedPoint tB = new IndexedPoint(triangle.Point2, newTriangles.Count());
                IndexedPoint tC = new IndexedPoint(triangle.Point3, newTriangles.Count());

                //Check if points already exist
                if (!pMap.ContainsKey(hashA))
                    pMap.Add(hashA, new List<IndexedPoint>());
                if (!pMap.ContainsKey(hashB))
                    pMap.Add(hashB, new List<IndexedPoint>());
                if (!pMap.ContainsKey(hashC))
                    pMap.Add(hashC, new List<IndexedPoint>());

                //Go through each triangle refrenced by each of the points of the current riangles and check if there is already
                //a triangle that contains all three points
                for (int a = 0; a < pMap[hashA].Count && !duplicate; a++)
                {
                    var pA = pMap[hashA][a];

                    for (int b = 0; b < pMap[hashB].Count && !duplicate; b++)
                    {
                        var pB = pMap[hashB][b];

                        for (int c = 0; c < pMap[hashC].Count && !duplicate; c++)
                        {
                            var pC = pMap[hashC][c];

                            bool sameTriangle = false;

                            for (int a2 = 0; a2 < pA.indexList.Count && !sameTriangle; a2++)
                            {
                                int aIs = pA.indexList[a2];

                                for (int b2 = 0; b2 < pB.indexList.Count && !sameTriangle; b2++)
                                {
                                    int bIs = pB.indexList[b2];

                                    for (int c2 = 0; c2 < pC.indexList.Count && !sameTriangle; c2++)
                                    {
                                        int cIs = pC.indexList[c2];

                                        if (aIs == bIs && bIs == cIs)
                                        {
                                            sameTriangle = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            //If point A, B and C are not from the same triangle then continue
                            //if (pA.index != pB.index && pA.index != pC.index)
                            if (!sameTriangle)
                                continue;

                            //Check if the triangle is a duplicate
                            if (tA == pA && tB == pB && tC == pC)
                            {
                                duplicate = true;
                                break;
                            }
                        }
                    }
                }

                if (!duplicate)
                {
                    //If this is a new triangle then we should add the index of this triangle to each of its three points
                    //in the hashed and larger list

                    //Determine the index of each point in the hashtable
                    int indexA = indexOfPointInList(pMap[hashA], tA);
                    int indexB = indexOfPointInList(pMap[hashB], tB);
                    int indexC = indexOfPointInList(pMap[hashC], tC);

                    if (indexA == -1)
                    {
                        //If this is a new point then add it to the hash table and the larger list

                        tA.index = newPoints.Count;
                        pMap[hashA].Add(tA);
                        triangle.p1Idx = newPoints.Count;
                        newPoints.Add(tA);
                    }
                    else
                    {
                        //If the point already exists then add the index of the current triangle to the list of triangles
                        //that contain the point

                        pMap[hashA][indexA].indexList.Add(newTriangles.Count);
                        triangle.p1Idx = pMap[hashA][indexA].index; //Store the index of the point in the triangle
                        newPoints[pMap[hashA][indexA].index].indexList.Add(newTriangles.Count);
                    }

                    if (indexB == -1)
                    {
                        tB.index = newPoints.Count;
                        pMap[hashB].Add(tB);
                        triangle.p2Idx = newPoints.Count;
                        newPoints.Add(tB);
                    }
                    else
                    {
                        pMap[hashB][indexB].indexList.Add(newTriangles.Count);
                        triangle.p2Idx = pMap[hashB][indexB].index;
                        newPoints[pMap[hashB][indexB].index].indexList.Add(newTriangles.Count);
                    }

                    if (indexC == -1)
                    {
                        tC.index = newPoints.Count;
                        pMap[hashC].Add(tC);
                        triangle.p3Idx = newPoints.Count;
                        newPoints.Add(tC);
                    }
                    else
                    {
                        pMap[hashC][indexC].indexList.Add(newTriangles.Count);
                        triangle.p3Idx = pMap[hashC][indexC].index;
                        newPoints[pMap[hashC][indexC].index].indexList.Add(newTriangles.Count);
                    }

                    //Add the new triangle to the list
                    newTriangles.Add(triangle);
                }
            }

            //For each triangle determine if and which other triangle also contains two of its points and store the index of the other
            //triangle as the one touching each side
            for (int i = 0; i < newTriangles.Count; i++)
            {
                var triangle = newTriangles[i];

                triangle.indexTouchingSide[0] = faceWithIndexPoints(newPoints, triangle.p1Idx, triangle.p2Idx, i);
                triangle.indexTouchingSide[1] = faceWithIndexPoints(newPoints, triangle.p1Idx, triangle.p3Idx, i);
                triangle.indexTouchingSide[2] = faceWithIndexPoints(newPoints, triangle.p2Idx, triangle.p3Idx, i);
            }

            Global.Values.initialTriangleList = newTriangles;
        }

        /// <summary>
        /// This method returns the index of the face other then the one specified with the two required points
        /// </summary>
        /// <param name="points">The list of points in the polygon</param>
        /// <param name="index1">The index of the first point that the face should contain</param>
        /// <param name="index2">The index of the second point that the face should contain</param>
        /// <param name="notIndex">The index that the face can not have</param>
        /// <returns>The index of the face</returns>
        private static int faceWithIndexPoints(List<IndexedPoint> points, int index1, int index2, int notIndex)
        {
            foreach (int f1 in points[index1].indexList)
            {
                if (f1 == notIndex)
                    continue;

                foreach (int f2 in points[index2].indexList)
                {
                    if (f2 == notIndex)
                        continue;

                    if (f1 == f2)
                        return f1;
                }
            }

            return -1;
        }

        /// <summary>
        /// This method gets the index of a point in a list, if not already there then it returns -1
        /// This only checks for the vector part of the indexedpoint
        /// </summary>
        /// <param name="points">The list of indexedpoints</param>
        /// <param name="point">The point of which the index should be found</param>
        /// <returns>The index of the point, -1 if not there</returns>
        private static int indexOfPointInList(List<IndexedPoint> points, IndexedPoint point)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].p == point.p)
                    return i;
            }

            return -1;
        }
    }
}
