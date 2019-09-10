using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using PolyChopper.Containers;

#if WINDOWS_PHONE
using System.IO.IsolatedStorage;
#elif WINDOWS_APP || WINDOWS_PHONE_APP
using Windows.Storage;
#endif

namespace PolyChopper
{
    /// <summary>
    /// This class is responsible for importing the slicer settings from a specified config file
    /// </summary>
    static class SettingsImporter
    {
        /// <summary>
        /// This method imports all the settings from a specified file
        /// </summary>
        /// <param name="settingsPath">The path of the file to import the settings from</param>
#if WINDOWS_APP || WINDOWS_PHONE_APP
        public static async void importSettingsFromFile(StorageFile settingsFile)
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void importSettingsFromFile(string settingsPath)
#endif
        {
            Logger.logProgress("Importing settings");

#if WINDOWS_PHONE
            IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream("chopperconf.txt", FileMode.Open, IsolatedStorageFile.GetUserStoreForApplication());

            StreamReader settingsReader = new StreamReader(isoStream);
#elif WINDOWS_APP || WINDOWS_PHONE_APP
            StreamReader settingsReader = new StreamReader(await settingsFile.OpenStreamForReadAsync());
#else
            StreamReader settingsReader = new StreamReader(settingsPath);
#endif
                       
            string readText = settingsReader.ReadLine();
            readText = readText.Replace("bedWidth: ", "");
            long.TryParse(readText, out Global.Values.bedWidth);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("bedLength: ", "");
            long.TryParse(readText, out Global.Values.bedLength);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("bedHeigth: ", "");
            long.TryParse(readText, out Global.Values.bedHeigth);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("printingTemperature: ", "");
            short.TryParse(readText, out Global.Values.printingTemperature);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("bottomLayerCount: ", "");
            ushort.TryParse(readText, out Global.Values.bottomLayerCount);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("topLayerCount: ", "");
            ushort.TryParse(readText, out Global.Values.topLayerCount);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("flowPercentage: ", "");
            float.TryParse(readText, out Global.Values.flowPercentage);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("initialLayerSpeed: ", "");
            int.TryParse(readText, out Global.Values.initialLayerSpeed);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("normalLayerSpeed: ", "");
            int.TryParse(readText, out Global.Values.normalLayerSpeed);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("normalInfillSpeed: ", "");
            int.TryParse(readText, out Global.Values.normalInfillSpeed);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("bridgeingSpeed: ", "");
            int.TryParse(readText, out Global.Values.bridgeingSpeed);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("moveSpeed: ", "");
            int.TryParse(readText, out Global.Values.moveSpeed);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("normalInfillDensity: ", "");
            float.TryParse(readText, out Global.Values.normalInfillDensity);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("specialLayerSpeeds: ", "");
            Global.Values.specialLayerSpeeds = readText;

            readText = settingsReader.ReadLine();
            readText = readText.Replace("specialInfillSpeeds: ", "");
            Global.Values.specialInfillSpeeds = readText;

            readText = settingsReader.ReadLine();
            readText = readText.Replace("specialLayerDensities: ", "");
            Global.Values.specialLayerDensities = readText;

            readText = settingsReader.ReadLine();
            readText = readText.Replace("nozzleWidth: ", "");
            int.TryParse(readText, out Global.Values.nozzleWidth);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("filamentWidth: ", "");
            int.TryParse(readText, out Global.Values.filamentWidth);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("layerHeight: ", "");
            int.TryParse(readText, out Global.Values.layerHeight);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("materialType: ", "");
            MaterialTypeParser.TryParse(readText, out Global.Values.materialType);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("costPerKg: ", "");
            float.TryParse(readText, out Global.Values.costPerKg);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("costPerMinute: ", "");
            float.TryParse(readText, out Global.Values.costPerMinute);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("skirtSpeed: ", "");
            int.TryParse(readText, out Global.Values.skirtSpeed);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("skirtCount: ", "");
            ushort.TryParse(readText, out Global.Values.skirtCount);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("skirtDistance: ", "");
            uint.TryParse(readText, out Global.Values.skirtDistance);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("shouldSkirt: ", "");
            bool.TryParse(readText, out Global.Values.shouldSkirt);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("shellThickness: ", "");
            ushort.TryParse(readText, out Global.Values.shellThickness);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("shouldSupportMaterial: ", "");
            bool.TryParse(readText, out Global.Values.shouldSupportMaterial);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("supportMaterialDesnity: ", "");
            float.TryParse(readText, out Global.Values.supportMaterialDesnity);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("supportSpeed: ", "");
            int.TryParse(readText, out Global.Values.supportSpeed);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("retractionSpeed: ", "");
            int.TryParse(readText, out Global.Values.retractionSpeed);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("retractionDistance: ", "");
            uint.TryParse(readText, out Global.Values.retractionDistance);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("shouldRaft: ", "");
            bool.TryParse(readText, out Global.Values.shouldRaft);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("raftDensity: ", "");
            float.TryParse(readText, out Global.Values.raftDensity);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("raftDistance: ", "");
            uint.TryParse(readText, out Global.Values.raftDistance);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("raftCount: ", "");
            ushort.TryParse(readText, out Global.Values.raftCount);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("raftSpeed: ", "");
            int.TryParse(readText, out Global.Values.raftSpeed);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("infillCombinationCount: ", "");
            ushort.TryParse(readText, out Global.Values.infillCombinationCount);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("specialFlowrates: ", "");
            Global.Values.specialFlowrates = readText;

            readText = settingsReader.ReadLine();
            readText = readText.Replace("specialMoveSpeeds: ", "");
            Global.Values.specialMoveSpeeds = readText;

            readText = settingsReader.ReadLine();
            readText = readText.Replace("specialBridgeingSpeeds: ", "");
            Global.Values.specialBridgeingSpeeds = readText;

            readText = settingsReader.ReadLine();
            readText = readText.Replace("specialSupportDensities: ", "");
            Global.Values.specialSupportDensities = readText;

            readText = settingsReader.ReadLine();
            readText = readText.Replace("specialSupportSpeeds: ", "");
            Global.Values.specialSupportSpeeds = readText;

            readText = settingsReader.ReadLine();
            readText = readText.Replace("minimumLayerTime: ", "");
            float.TryParse(readText, out Global.Values.minimumLayerTime);

            readText = settingsReader.ReadLine();
            readText = readText.Replace("minimumLayerSpeed: ", "");
            int.TryParse(readText, out Global.Values.minimumLayerSpeed);

            //TODO: implement a way of parsing multi-layer start and end code

            settingsReader.Dispose();
        }

        /// <summary>
        /// This method sets up the dictionaries containing the layerspeeds, infillspeeds and infilldensities for each layer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void setupLayerValues()
        {
            //If there should not be a raft then the raft layer count should be 0
            if (!Global.Values.shouldRaft)
                Global.Values.raftCount = 0;

            setupLayerSpeeds();
            setupInfillSpeeds();
            setupInfillDensities();
            setupFlowrates();
            setupMoveSpeeds();
            setupBridgeingSpeeds();
            setupSupportDensities();
            setupSupportSpeeds();

            //NOTE: all custom speed and flowrate settings should take raft layers into account
        }

        /// <summary>
        /// This method determines the flowrate for each layer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void setupFlowrates()
        {
            //Create flowrate for raft layers 
            //for (int i = 0; i < Global.Values.raftCount; i++)
              //  Global.Values.flowrates[i] = Global.Values.flowPercentage; //TODO: implement raf specific flow rates

            //First we setup every layer with the default flow rate
            //for (int i = 0; i < Global.Values.layerCount; i++)
             //   Global.Values.layerComponentList[i].flowrate = Global.Values.flowPercentage;
                //Global.Values.flowrates[i + Global.Values.raftCount] = Global.Values.flowPercentage;

            //We then determine the custom infill densities and set them up
            string[] specialFlowrates = Global.Values.specialFlowrates.Split(';');

            string[] commandComponents;
            int layerNumber;
            float flowrate;

            foreach (string specialFlowrate in specialFlowrates)
            {
                commandComponents = specialFlowrate.Split(':');
                if (int.TryParse(commandComponents[0], out layerNumber) && float.TryParse(commandComponents[1], out flowrate) && layerNumber <= Global.Values.layerCount)
                {
                    Global.Values.layerComponentList[layerNumber].flowrate = flowrate;
                }
            }
        }

        /// <summary>
        /// This method determines the layer speed for each layer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void setupLayerSpeeds()
        {
            /*//Create layer speeds for raft layers 
            for (int i = 0; i < Global.Values.raftCount; i++)
                Global.Values.layerSpeeds[i] = Global.Values.raftSpeed;

            Global.Values.layerSpeeds[Global.Values.raftCount] = (!Global.Values.shouldRaft) ? Global.Values.initialLayerSpeed : Global.Values.normalLayerSpeed;

            //First we setup every layer with the default layer speed
            for (int i = 1; i < Global.Values.layerCount; i++)
                Global.Values.layerSpeeds[i] = Global.Values.normalLayerSpeed;*/

            //We then determine the custom layer speeds and set them up
            string[] specialLayerSpeedCommands = Global.Values.specialLayerSpeeds.Split(';');

            string[] commandComponents;
            int layerNumber;
            int layerSpeed;

            foreach (string specialLayerSpeedCommand in specialLayerSpeedCommands)
            {
                commandComponents = specialLayerSpeedCommand.Split(':');
                if (int.TryParse(commandComponents[0], out layerNumber) && int.TryParse(commandComponents[1], out layerSpeed) && layerNumber <= Global.Values.layerCount)
                {
                    Global.Values.layerComponentList[layerNumber].layerSpeed = layerSpeed;
                }
            }
        }

        /// <summary>
        /// This method determines the infill speed for each layer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void setupInfillSpeeds()
        {
            /*//Create infill speeds for raft layers 
            for (int i = 0; i < Global.Values.raftCount; i++)
                Global.Values.infillSpeeds[i] = Global.Values.raftSpeed;

            Global.Values.infillSpeeds[Global.Values.raftCount] = (!Global.Values.shouldRaft) ? Global.Values.initialLayerSpeed : Global.Values.normalInfillSpeed;

            //First we setup every layer with the default infill speed
            for (int i = 1; i < Global.Values.layerCount; i++)
                Global.Values.infillSpeeds[i + Global.Values.raftCount] = Global.Values.normalInfillSpeed;*/

            //We then determine the custom infill speeds and set them up
            string[] specialInfillSpeedCommands = Global.Values.specialInfillSpeeds.Split(';');

            string[] commandComponents;
            int layerNumber;
            int infillSpeed;

            //for (uint i = 0; i < specialInfillSpeedCommands.Length; i++)
            foreach (string specialInfillSpeedCommand in specialInfillSpeedCommands)
            {
                commandComponents = specialInfillSpeedCommand.Split(':');
                if (int.TryParse(commandComponents[0], out layerNumber) && int.TryParse(commandComponents[1], out infillSpeed) && layerNumber <= Global.Values.layerCount)
                {
                    Global.Values.layerComponentList[layerNumber].infillSpeed = infillSpeed;
                }
            }
        }

        /// <summary>
        /// This method determines the support speed for each layer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void setupSupportSpeeds()
        {
            /*//Create support speeds for raft layers 
            for (int i = 0; i < Global.Values.raftCount; i++)
                Global.Values.supportSpeeds[i] = Global.Values.raftSpeed;

            Global.Values.supportSpeeds[Global.Values.raftCount] = (!Global.Values.shouldRaft) ? Global.Values.initialLayerSpeed : Global.Values.supportSpeed;

            //First we setup every layer with the default flow rate
            for (int i = 1; i < Global.Values.layerCount; i++)
                Global.Values.supportSpeeds[i + Global.Values.raftCount] = Global.Values.supportSpeed;*/

            //We then determine the custom infill densities and set them up
            string[] specialSupportSpeeds = Global.Values.specialSupportSpeeds.Split(';');

            string[] commandComponents;
            int layerNumber;
            int supportSpeed;

            //for (int i = 0; i < specialSupportSpeeds.Length; i++)
            foreach (string specialSupportSpeed in specialSupportSpeeds)
            {
                commandComponents = specialSupportSpeed.Split(':');
                if (int.TryParse(commandComponents[0], out layerNumber) && int.TryParse(commandComponents[1], out supportSpeed) && layerNumber <= Global.Values.layerCount)
                {
                    Global.Values.layerComponentList[layerNumber].supportSpeed = supportSpeed;
                }
            }
        }

        /// <summary>
        /// This method determines the move speed fore each layer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void setupMoveSpeeds()
        {
            /*//Create layer speeds for raft layers 
            for (int i = 0; i < Global.Values.raftCount; i++)
                Global.Values.moveSpeeds[i] = Global.Values.moveSpeed; //TODO: implement raf specific move speed

            Global.Values.moveSpeeds[Global.Values.raftCount] = (!Global.Values.shouldRaft) ? Global.Values.initialLayerSpeed : Global.Values.moveSpeed;

            //First we setup every layer with the default flow rate
            for (int i = 1; i < Global.Values.layerCount; i++)
                Global.Values.moveSpeeds[i + Global.Values.raftCount] = Global.Values.moveSpeed;*/

            //We then determine the custom infill densities and set them up
            string[] specialMoveSpeeds = Global.Values.specialMoveSpeeds.Split(';');

            string[] commandComponents;
            int layerNumber;
            int moveSpeed;

            //for (int i = 0; i < specialMoveSpeeds.Length; i++)
            foreach (string specialMoveSpeed in specialMoveSpeeds)
            {
                commandComponents = specialMoveSpeed.Split(':');
                if (int.TryParse(commandComponents[0], out layerNumber) && int.TryParse(commandComponents[1], out moveSpeed) && layerNumber <= Global.Values.layerCount)
                {
                    Global.Values.layerComponentList[layerNumber].moveSpeed = moveSpeed;
                }
            }
        }

        /// <summary>
        /// This method determines the bridgeing speed for each layer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void setupBridgeingSpeeds()
        {
            /*//Create layer speeds for raft layers 
            for (int i = 0; i < Global.Values.raftCount; i++)
                Global.Values.bridgeingSpeeds[i] = Global.Values.raftSpeed;

            Global.Values.bridgeingSpeeds[Global.Values.raftCount] = (!Global.Values.shouldRaft) ? Global.Values.initialLayerSpeed : Global.Values.bridgeingSpeed;

            //First we setup every layer with the default flow rate
            for (int i = 1; i < Global.Values.layerCount; i++)
                Global.Values.bridgeingSpeeds[i + Global.Values.raftCount] = Global.Values.bridgeingSpeed;*/

            //We then determine the custom infill densities and set them up
            string[] specialBridgeingSpeeds = Global.Values.specialBridgeingSpeeds.Split(';');

            string[] commandComponents;
            int layerNumber;
            int bridgeingSpeed;

            //for (int i = 0; i < specialBridgeingSpeeds.Length; i++)
            foreach (string specialBridgeingSpeed in specialBridgeingSpeeds)
            {
                commandComponents = specialBridgeingSpeed.Split(':');
                if (int.TryParse(commandComponents[0], out layerNumber) && int.TryParse(commandComponents[1], out bridgeingSpeed) && layerNumber <= Global.Values.layerCount)
                {
                    Global.Values.layerComponentList[layerNumber].bridgeingSpeed = bridgeingSpeed;
                }
            }
        }

        /// <summary>
        /// This method determines the support density for each layer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void setupSupportDensities()
        {
            /*//First we setup every layer with the default flow rate
            for (int i = 0; i < Global.Values.layerCount; i++)
                Global.Values.supportDensities[i] = Global.Values.supportMaterialDesnity;*/

            //We then determine the custom infill densities and set them up
            string[] specialSupportDensities = Global.Values.specialSupportDensities.Split(';');

            string[] commandComponents;
            int layerNumber;
            float supportDensity;

            //for (int i = 0; i < specialSupportDensities.Length; i++)
            foreach (string specialSupportDensity in specialSupportDensities)
            {
                commandComponents = specialSupportDensity.Split(':');
                if (int.TryParse(commandComponents[0], out layerNumber) && float.TryParse(commandComponents[1], out supportDensity) && layerNumber <= Global.Values.layerCount)
                {
                    Global.Values.layerComponentList[layerNumber].supportDensity = supportDensity;
                }
            }

            //TODO: implement usage of custom support densities
        }

        /// <summary>
        /// This method determines the infill density for each layer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void setupInfillDensities()
        {
            /*//First we setup every layer with the default infill density
            for (int i = 0; i < Global.Values.layerCount; i++)
                Global.Values.infillDensities[i] = Global.Values.normalInfillDensity;*/

            //We then determine the custom infill densities and set them up
            string[] specialInfillDensities = Global.Values.specialLayerDensities.Split(';');

            string[] commandComponents;
            int layerNumber;
            float infillDensity;

            //for (int i = 0; i < specialInfillDensities.Length; i++)
            foreach (string specialInfillDensity in specialInfillDensities)
            {
                commandComponents = specialInfillDensity.Split(':');
                if (int.TryParse(commandComponents[0], out layerNumber) && float.TryParse(commandComponents[1], out infillDensity) && layerNumber <= Global.Values.layerCount)
                {
                    Global.Values.layerComponentList[layerNumber].infillDensity = infillDensity;
                }
            }
        }
    }
}
