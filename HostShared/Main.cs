using System;
using System.Collections.Generic;
using HostShared.Model;

#if DESKTOP
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
#elif WINDOWS_APP || WINDOWS_PHONE_APP
using Windows.UI.Xaml.Controls;
using Windows.UI.Core;
#endif

namespace HostShared
{
    static class Main
    {
        /// <summary>
        /// The modelviewer of the program
        /// </summary>
        public static ModelViewer modelViewer = new ModelViewer();

#if DESKTOP
        /// <summary>
        /// This method fixes the Windows Registry so that WebBrowser components function correctly with Three.js and Hammer.js
        /// in this application
        /// </summary>
        private static void fixRegistry()
        {
            RegistryKey[] keys = new RegistryKey[4];

            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                //The key that will disable legacy input emulation
                keys[0] = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_NINPUT_LEGACYMODE");

                //The key that will try to disable activex warnings
                keys[1] = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BLOCK_LMZ_SCRIPT");

                //The key that will try to help disable activex warnings
                keys[2] = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_LOCALMACHINE_LOCKDOWN");

                //The key that will enable hardware acceleration
                keys[3] = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_GPU_RENDERING");
            }
            else
            {
                keys[0] = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_NINPUT_LEGACYMODE");
                keys[1] = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BLOCK_LMZ_SCRIPT");
                keys[2] = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_LOCALMACHINE_LOCKDOWN");
                keys[3] = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_GPU_RENDERING");
            }

            string procName = Process.GetCurrentProcess().ProcessName + ".exe";

            bool shouldFix = false;

            for (int i = 0; i < 4 && !shouldFix; i++)
            {
                if (keys[i] == null || keys[i].GetValue(procName) == null)
                    shouldFix = true;
            }

            if (!shouldFix)
                return;

            MessageBox.Show("It seems that your system has not yet been configured to run embedded Internet Explorer correctly for this application." +
                "We will temporarily need admin rights to fix this in the registry. Sorry for the inconvenience but it should not happen again...", 
                "Initialization needed");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "registryfixer.exe",
                    Verb = "runas",
                    Arguments = '"' + procName + '"',
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// This method initialiases the stl model viewer
        /// </summary>
        /// <param name="webview">The webbrowser that the viewer should be placed in</param>
        public static void initStl(WebBrowser webview)
#elif WINDOWS_APP || WINDOWS_PHONE_APP
        public static void initStl(WebView webview)
#endif
        {
#if DESKTOP
            //The registry needs to be fixed before the desktop browser element works correctly
            fixRegistry();
#endif
        }

        /// <summary>
        /// This method adds the model viewer to the specified grid
        /// </summary>
        /// <param name="grid">The grid to add the viewer to</param>
        public static void addModelViewerToGrid(Grid grid)
        {
            modelViewer.addToGrid(grid);
        }

        /// <summary>
        /// This method removes the model viewer from the specified grid
        /// </summary>
        /// <param name="grid">The grid to remove the viewer from</param>
        public static void removeModelViewerFromGrid(Grid grid)
        {
            modelViewer.removeFromGrid(grid);
        }

#if WINDOWS_PHONE_APP
        public static void continueModelOpening(IReadOnlyList<Windows.Storage.StorageFile> files)
        {
            //modelViewer.continueFileOpen(files);
        }
#endif
    }
}
