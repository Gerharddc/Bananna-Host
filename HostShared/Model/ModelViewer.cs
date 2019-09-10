using System;
using System.IO;
using System.Collections.Generic;
using HostShared.Html;

using System.Runtime.InteropServices;

#if DESKTOP
using System.Windows.Controls;
using System.Windows;
#elif WINDOWS_APP || WINDOWS_PHONE_APP
using Windows.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.System.Threading;
using Windows.Foundation;
using System.Threading.Tasks;
#endif

#if DEBUG
using System.Diagnostics;
#endif

namespace HostShared.Model
{
    [ComVisible(true)]
    public class ScriptManager
    {
        public void log(object message)
        {
            Debug.WriteLine(message);
        }
    }

    /// <summary>
    /// This class represents an stl model viewer
    /// </summary>
    class ModelViewer
    {
        /// <summary>
        /// The server that will be used to communicate with the ThreeJS
        /// </summary>
        private ThreeJSServer server;

        // Url of Home page
#if DESKTOP
        private const string PagePath = "/Html/modelviewer.html";

        public readonly WebBrowser webView;
#elif WINDOWS_APP || WINDOWS_PHONE_APP
        private const string MainUri = "http://localhost:8000/modelviewer.html";//"ms-appx-web:///Html/modelviewer.html";

        private WebView webView;
#endif

        /// <summary>
        /// This method initialses the model viewer
        /// </summary>
        public ModelViewer()
        {
            //Start the server on port 8000
            server = new ThreeJSServer(8000);
            server.Started += server_Started;

#if DESKTOP
            webView = new WebBrowser();
#elif WINDOWS_APP || WINDOWS_PHONE_APP
            webView = new WebView();
            webView.DefaultBackgroundColor = Windows.UI.Colors.Transparent;
#endif
            
            server.Start();
        }

        /// <summary>
        /// This method is run when the system has finished loading
        /// </summary>
        private void server_Started()
        {
#if DESKTOP
            //string sourcePath = Directory.GetCurrentDirectory();

            //Get the current drive character
            //string curDrive = sourcePath[0].ToString();

            //Remove the first 3 charcters (usually "C:\")
            //sourcePath = sourcePath.Remove(0, 3);

            //Now replace the type of slashes
            //sourcePath = sourcePath.Replace(@"\", "/");

            //Now add the correct start and page path
            //sourcePath = "file://127.0.0.1/" + curDrive.ToLower() + "$/" + sourcePath + PagePath;
            try
            {
                string sourcePath = "http://localhost:8000/modelviewer.html";

                webView.LoadCompleted += webView_LoadCompleted;
                webView.Source = new Uri(sourcePath);
                webView.ObjectForScripting = new ScriptManager();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
#elif WINDOWS_APP || WINDOWS_PHONE_APP
            webView.NavigationCompleted += webView_LoadCompleted;
            webView.Navigate(new Uri(MainUri, UriKind.Absolute));
#endif
        }
        
        /// <summary>
        /// This method is run when the webbrowser controller has finsihed loading
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The parameters</param>
#if DESKTOP
        private void webView_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            Debug.WriteLine("Loaded");
            //ModelImporter.importModelFile("Model/frog.stl", webView);
        }
#elif WINDOWS_APP || WINDOWS_PHONE_APP
        private async void webView_LoadCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            /*FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".stl");


            StorageFile file = await openPicker.PickSingleFileAsync();*/
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(@"Model\frog.stl");

            if (file != null)
            {
                // Application now has read/write access to the picked file
                Debug.WriteLine("Picked: " + file.Name);
                ModelImporter.importModelFile(file, webView);
            }
            else
            {
                Debug.WriteLine("Cancelled");
            }
        }
#elif WINDOWS_PHONE_APP
        public void continueFileOpen(IReadOnlyList<StorageFile> files)
        {
            System.Diagnostics.Debug.WriteLine("Continue to open");

            if (files != null && files.Count > 0)
            {
                StorageFile file = files[0];

                // Application now has read/write access to the picked file
                Debug.WriteLine("Picked: " + file.Name);
                //ModelImporter.importModelFile(file, webView);

                IAsyncAction asyncAction = ThreadPool.RunAsync((workitem) =>
                    {
                        /*try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(30));
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                        Debug.WriteLine("Hello after delay");*/
                        ModelImporter.importModelFile(file, webView);
                    });
            }
            else
            {
                Debug.WriteLine("Cancelled");
            }
        }

        private async void webView_LoadCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            Debug.WriteLine("loadComplete");
            /*FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.ContinuationData["Operation"] = "OpenModel";
            openPicker.FileTypeFilter.Add(".stl");
            openPicker.PickSingleFileAndContinue();*/
            //StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            //StorageFile frogFile = await localFolder.GetFileAsync("frog.stl");
            //ModelImporter.importModelFile(frogFile, webView);
            /*var folders = await localFolder.GetFoldersAsync();
            var files = await localFolder.GetFilesAsync();

            foreach (StorageFolder folder in folders)
                Debug.WriteLine("Folder: " + folder.Name);
            foreach (StorageFile file in files)
                Debug.WriteLine("File: " + file.Name);*/
            //Debug.WriteLine(localFolder.Path);
            //localFolder = new StorageFolder()
            //"ms-appx-web:///Html/modelviewer.html"
            /*var uri = new System.Uri("ms-appx:///Model/frog.stl");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            if (file != null)
                Debug.WriteLine(file.Name);
            else
                Debug.WriteLine("null");

            IAsyncAction asyncAction = ThreadPool.RunAsync((workitem) =>
            {
                //ModelImporter.importModelFile(file, webView);
            });*/
        }
#endif

        /// <summary>
        /// This method adds this viewer to the specified grid
        /// </summary>
        /// <param name="grid">The grid that this viewer should be added to</param>
        public void addToGrid(Grid grid)
        {
            grid.Children.Add(webView);
        }

        /// <summary>
        /// This method removes this viewer from the specified grid
        /// </summary>
        /// <param name="grid">The grid that this viewer should be removed from</param>
        public void removeFromGrid(Grid grid)
        {
            grid.Children.Remove(webView);
        }
    }
}
