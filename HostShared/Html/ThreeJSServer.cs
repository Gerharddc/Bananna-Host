using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using HostShared.Model;
using HostShared.Toolpath;

#if DESKTOP
using System.Net;
using System.Net.Sockets;
#elif WINDOWS_APP || WINDOWS_PHONE_APP
using Windows.Networking.Sockets;
using Windows.Storage;
#endif

namespace HostShared.Html
{
    /// <summary>
    /// This class represents a barebones http server that will be used to communicate with ThreeJS renderer
    /// </summary>
    class ThreeJSServer
    {
        public delegate void StartedEventHandler();
        /// <summary>
        /// This event will be called when the server has started
        /// </summary>
        public event StartedEventHandler Started;

        /// <summary>
        /// A listerner that will listen for requests from the JS
        /// </summary>
#if DESKTOP
        private readonly TcpListener listener;

        /// <summary>
        /// The program will keep trying to read requests while this bool is true
        /// </summary>
        private bool active = false;
#elif WINDOWS_APP || WINDOWS_PHONE_APP       
        private readonly StreamSocketListener listener;

        /// <summary>
        /// The port that will be listened on
        /// </summary>
        private readonly string _port;
#endif

        /// <summary>
        /// Initialize the barebones http server
        /// </summary>
        /// <param name="port">To port to listen on</param>
        public ThreeJSServer(int port)
        {
#if DESKTOP
            listener = new TcpListener(IPAddress.Loopback, port);
#elif WINDOWS_APP || WINDOWS_PHONE_APP
            listener = new StreamSocketListener();
            listener.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);
            _port = port.ToString();
#endif
        }

        /// <summary>
        /// Start the http server
        /// </summary>
        public async void Start()
        {
#if DESKTOP
            listener.Start();
            active = true;
            ProcessRequestsAsync();
#elif WINDOWS_APP || WINDOWS_PHONE_APP
            await listener.BindServiceNameAsync(_port);
#endif

            if (Started != null)
                Started();
        }

        /// <summary>
        /// Dispose of the http server
        /// </summary>
        public void Dispose()
        {
#if DESKTOP
            active = false;
            listener.Stop();
#elif WINDOWS_APP || WINDOWS_PHONE_APP
            listener.Dispose();
#endif
        }

#if DESKTOP
        /// <summary>
        /// This method keeps trying to read requests from the connected client
        /// </summary>
        private async void ProcessRequestsAsync()
        {
            //Continue checking for connections for connection
            while (active)
            {
                //Setup the tcp listener
                TcpClient client = await listener.AcceptTcpClientAsync();
                NetworkStream stream = client.GetStream();
                StreamReader reader = new StreamReader(stream);
                reader.BaseStream.ReadTimeout = 250;

                //Read the entire request
                string request = "";
                string curline = "start";
                
                //First read the first line and then the others
                while (curline != "")
                {                  
                    try
                    {
                        curline = reader.ReadLine();
                        request += curline + "\n";
                    }
                    catch (Exception)
                    {
                        curline = "";
                    }
                }

                //The header response message
                string responseHeader = "";
                string responseText = "";

                //If the request is empty then we should send a timeout response (this happens even here)
                if (request != "")
                {
                    //The actual request part of the message
                    string getMessage = "";

                    try
                    {
                        //Extract the message
                        getMessage = request.Split('\n')[0];
                        getMessage = getMessage.Split(' ')[1];
                        getMessage = getMessage.Remove(0, 1); //Remove the trailing '/'

                        Debug.WriteLine(getMessage);

                        //Remove timestamp
                        if (getMessage.Contains("?"))
                            getMessage = getMessage.Split('?')[0];
                    }
                    catch (Exception) { }

                    //string sendText = "";
                    string filePath = "";

                    //Respond approprietly to each type of request
                    switch (getMessage)
                    {
                        case "random":
                            responseText = "Yes yes";
                            break;
                        case "readTrigs":
                            //Read the next triangle in the currently open model
                            while (ModelImporter.trigForJs == "wait") ;                            
                            responseText = ModelImporter.trigForJs;
                            //Debug.WriteLine(responseText.Split(';').Length);
                            ModelImporter.readNextTrigs();
                            break;
                        case "readSegment":
                            //Read the next segment in the currently open toolpath
                            while (ToolPathImporter.linesForJs == "wait" /*|| ToolPathImporter.linesForJs == ""*/) ;
                            responseText = ToolPathImporter.linesForJs;
                            if (responseText == "")
                                responseText = "none";
                            //Debug.WriteLine(responseText.Split(';').Length);
                            //Debug.WriteLine(responseText);
                            ToolPathImporter.readNextSegment();
                            break;
                        case "readLayer":
                            while (ToolPathImporter.linesForJs == "wait") ;
                            responseText = ToolPathImporter.linesForJs;
                            if (responseText == "")
                                responseText = "none";
                            ToolPathImporter.readNextLayer();
                            break;
                        case "getLayerHeight":
                            //Return the height of the current layer in the toolpath
                            responseText = ToolPathImporter.layerHeight.ToString();
                            break;
                        case "readPaths":
                            throw new NotImplementedException("Nogi geduni");
                        default:
                            //Otherwise load the specified file
                            filePath = "Html/" + getMessage;
                            break;
                    }

                    if (filePath != "" && File.Exists(filePath))
                    {
                        //Try reading the specified file if we have a path
                        //Only read the file if it exists
                        StreamReader fileReader = new StreamReader(filePath);
                        responseText = fileReader.ReadToEnd();
                        fileReader.Dispose();

                        //Respond to the request
                        responseHeader = "200 OK";
                    }
                    else if (responseText == "")
                    {
                        //Send 404
                        responseHeader = "404 Not Found";

                        throw new Exception("404 Not Found");
                    }
                }
                else
                {
                    //Timeout response
                    responseHeader = "408 Request Time-out";
                }

                byte[] sendMessage = Encoding.UTF8.GetBytes(responseText);

                string header = String.Format("HTTP/1.1 " + responseHeader + "\r\n" +
                    "Content-Length: {0}\r\n" +
                    "Connection: close\r\n\r\n",
                    sendMessage.Length);

                byte[] headerArray = Encoding.UTF8.GetBytes(header);

                //Get the writer and write the response
                var writer = stream;
                await writer.WriteAsync(headerArray, 0, headerArray.Length);
                await writer.WriteAsync(sendMessage, 0, sendMessage.Length);
                await writer.FlushAsync();

                //Close the listener
                stream.Dispose();
                client.Close();
            }
        }

#elif WINDOWS_APP || WINDOWS_PHONE_APP
        private static StorageFolder LocalFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;

        /// <summary>
        /// This method will process all responses received
        /// </summary>
        /// <param name="socket">The streamsocket to read the requests from</param>
        private async void ProcessRequestAsync(StreamSocket socket)
        {
            //Create a reader
            var reader = new StreamReader(socket.InputStream.AsStreamForRead());

            //Read the entire request
            string request = "";
            string curline = "start";

            //First read the first line and then the others
            while (curline != "")
            {
                try
                {
                    curline = reader.ReadLine();
                    request += curline + "\n";
                }
                catch (Exception)
                {
                    curline = "";
                }
            }

            //The header response message
            string responseHeader = "";
            string responseText = "";

            //If the request is empty then we should send a timeout response (this happens even here)
            if (request != "")
            {
                //The actual request part of the message
                string getMessage = "";

                try
                {
                    //Extract the message
                    getMessage = request.Split('\n')[0];
                    getMessage = getMessage.Split(' ')[1];
                    getMessage = getMessage.Remove(0, 1); //Remove the trailing '/'

                    Debug.WriteLine(getMessage);

                    //Remove timestamp
                    if (getMessage.Contains("?"))
                        getMessage = getMessage.Split('?')[0];
                }
                catch (Exception) { }

                //string sendText = "";
                string filePath = "";

                //Respond approprietly to each type of request
                switch (getMessage)
                {
                    case "random":
                        responseText = "Yes yes";
                        break;
                    case "readTrigs":
                        //Read the next triangle in the currently open model
                        while (ModelImporter.trigForJs == "wait") ;
                        responseText = ModelImporter.trigForJs;
                        Debug.WriteLine(responseText.Split(';').Length);
                        ModelImporter.readNextTrigs();
                        break;
                    case "readPaths":
                        throw new NotImplementedException("Nogi geduni");
                    default:
                        //Otherwise load the specified file
                        filePath = @"Html\" + getMessage.Replace('/', '\\'); //Winr reuires the backslash operator for file access
                        break;
                }

                Stream fs = null;

                if (filePath != "")
                {
                    try
                    {
                        fs = await LocalFolder.OpenStreamForReadAsync(filePath);
                    }
                    catch (Exception) { }
                }

                if (fs != null)
                {
                    //Try reading the specified file if we have a path
                    //Only read the file if it exists
                    StreamReader fileReader = new StreamReader(fs);
                    responseText = fileReader.ReadToEnd();
                    fileReader.Dispose();

                    //Respond to the request
                    responseHeader = "200 OK";
                }
                else if (responseText == "")
                {
                    //Send 404
                    responseHeader = "404 Not Found";
                }
            }
            else
            {
                //Timeout response
                responseHeader = "408 Request Time-out";
            }

            byte[] sendMessage = Encoding.UTF8.GetBytes(responseText);

            string header = String.Format("HTTP/1.1 " + responseHeader + "\r\n" +
                "Content-Length: {0}\r\n" +
                "Connection: close\r\n\r\n",
                sendMessage.Length);

            byte[] headerArray = Encoding.UTF8.GetBytes(header);

            //Get the writer and write the response
            var writer = socket.OutputStream.AsStreamForWrite();
            await writer.WriteAsync(headerArray, 0, headerArray.Length);
            await writer.WriteAsync(sendMessage, 0, sendMessage.Length);
            await writer.FlushAsync();

            //Close the listener
            socket.Dispose();

            /*byte[] sendMessage = Encoding.UTF8.GetBytes("Jha tjom");
            string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                    "Content-Length: {0}\r\n" +
                                    "Connection: close\r\n\r\n",
                                    sendMessage.Length);
            byte[] headerArray = Encoding.UTF8.GetBytes(header);

            //Get the writer and write the response
            var writer = socket.OutputStream.AsStreamForWrite();
            await writer.WriteAsync(headerArray, 0, headerArray.Length);
            await writer.WriteAsync(sendMessage, 0, sendMessage.Length);
            await writer.FlushAsync();*/



        }
#endif
    }
}
