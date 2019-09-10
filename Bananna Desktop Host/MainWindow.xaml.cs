using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HostShared;

using System.Threading;
//using System.Reflection;

using Microsoft.Win32;

namespace Bananna_Desktop_Host
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PolyChopper.PolyChopper chopper = new PolyChopper.PolyChopper();

        public MainWindow()
        {
            InitializeComponent();

            chopper.logEvent += log;
        }

        private void log(string message)
        {
            var delg = new Action(() => { logLabel.Content = message; });
            Dispatcher.BeginInvoke(delg, null);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Main.addModelViewerToGrid(sender as Grid);
        }

        private void Grid_Unloaded(object sender, RoutedEventArgs e)
        {
            Main.removeModelViewerFromGrid(sender as Grid);
        }

        private void inBtn_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".stl";
            dlg.Filter = "STL File (.stl)|*.stl";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                inputTextbox.Text = filename;
            }
        }

        private void outBtn_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".scode";
            dlg.Filter = "SCode File (.scode)|*.scode";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                outputTextbox.Text = filename;
            }
        }

        private void confBtn_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".conf";
            dlg.Filter = "Config file (.conf)|*.conf";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                configTextbox.Text = filename;
            }
        }

        private void sliceBtn_Click(object sender, RoutedEventArgs e)
        {
            var input = inputTextbox.Text;
            var output = outputTextbox.Text;
            var conf = configTextbox.Text;

            var delg = new Action(() => { HostShared.Toolpath.ToolPathImporter.importToolpathFile(outputTextbox.Text, Main.modelViewer.webView); });

            new Thread(new ThreadStart(() =>
                {
                    chopper.sliceFile(input, output, conf);
                    Dispatcher.BeginInvoke(delg, null);
                })).Start();                      
        }
    }
}
