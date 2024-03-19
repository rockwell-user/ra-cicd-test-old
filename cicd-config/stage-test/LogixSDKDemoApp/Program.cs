using RockwellAutomation.LogixDesigner;
using System.Threading.Tasks;
using System.Diagnostics;
using System;
using System.IO;
using static RockwellAutomation.LogixDesigner.LogixProject;

namespace LogixSDKDemoApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // MODIFY THE TWO STRINGS BELOW BASED ON PROJECT APPLICATION
            string filePath = @"C:\Users\ASYost\Desktop\s5k_cicd_testfiles\CICD_test.ACD";
            string commPath = @"EmulateEthernet\127.0.0.1";

            // Title Banner 
            DateTime currentTime = DateTime.Now;
            Console.WriteLine("  =========================================================================================  ");
            Console.WriteLine("=============================================================================================\n");
            Console.WriteLine("                             CI/CD TEST STAGE | " + currentTime);
            Console.WriteLine("\n=============================================================================================");
            Console.WriteLine("  =========================================================================================  \n\n");

            // Printout relevant test information
            Console.WriteLine("Project dependencies:");
            Console.WriteLine("---------------------------------------------------------------------------------------------");
            Console.WriteLine($"ACD file path specified:          {filePath}");
            Console.WriteLine($"Communication path specified:     {commPath}");
            Console.WriteLine("Common language runtime version:  " + typeof(string).Assembly.ImageRuntimeVersion);
            Console.WriteLine("---------------------------------------------------------------------------------------------\n\n");

            // Begin Test Banner
            Console.WriteLine("BEGIN TESTING:");
            Console.WriteLine("-----------------");

            // Open the ACD project file and store the reference as myProject.
            Console.WriteLine("Opening ACD file...");
            LogixProject myProject = await LogixProject.OpenLogixProjectAsync(filePath);
            Console.WriteLine("Opening ACD file... SUCCESS\n---");

            // Download project
            Console.WriteLine("Downloading ACD file...");
            DP(commPath, myProject).GetAwaiter().GetResult();
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine("Downloading ACD file...  SUCCESS\n---");

            // Change controller mode to program & verify
            Console.WriteLine("Changing controller to PROGRAM...");
            CCM(commPath, 0, myProject).GetAwaiter().GetResult();
            if (RCM(commPath, myProject).GetAwaiter().GetResult() == "PROGRAM")
            {
                Console.WriteLine("Changing controller to PROGRAM...  SUCCESS\n---");
            }
            else
            {
                Console.WriteLine("Changing controller to PROGRAM...  FAILURE\n---");
            }

            // Change controller mode to run
            Console.WriteLine("Changing controller to RUN...");
            CCM(commPath, 1, myProject).GetAwaiter().GetResult();
            if (RCM(commPath, myProject).GetAwaiter().GetResult() == "RUN")
            {

                Console.WriteLine("Changing controller to RUN...  SUCCESS\n---");
            }
            else
            {
                Console.WriteLine("Changing controller to RUN...  FAILURE\n---");
            }


            //// Get tag value
            //Console.WriteLine("Getting Tag Values...");
            //string test_DINT_1 = GTV("test_DINT_1", myProject).GetAwaiter().GetResult();
            //Console.WriteLine(test_DINT_1);
            //string test_DINT_2 = GTV("test_DINT_2", myProject).GetAwaiter().GetResult();
            //Console.WriteLine(test_DINT_2);
            //string test_DINT_3 = GTV("test_DINT_3", myProject).GetAwaiter().GetResult();
            //Console.WriteLine(test_DINT_3);
            //Console.Write("Getting Tag Values...  COMPLETE\n\n");

            //// Set tag value
            //Console.WriteLine("Setting Tag Values...");
            //STV(test_DINT_1, 111, myProject).GetAwaiter().GetResult();
            //STV(test_DINT_2, 222, myProject).GetAwaiter().GetResult();
            //STV(test_DINT_1, 333, myProject).GetAwaiter().GetResult();
            //Console.Write("Setting Tag Values...  COMPLETE\n\n");

            //// Get tag value
            //Console.WriteLine("Getting Tag Values...");
            //Console.WriteLine(GTV("test_DINT_1", myProject).GetAwaiter().GetResult());
            //Console.WriteLine(GTV("test_DINT_2", myProject).GetAwaiter().GetResult());
            //Console.WriteLine(GTV("test_DINT_3", myProject).GetAwaiter().GetResult());
            //Console.Write("Getting Tag Values...  COMPLETE\n\n");
        }


        // ======================
        //        METHODS
        // ======================

        // (GTV) Get Tag Value Method
        static public async Task<string> GTV(string tag_name, LogixProject project)
        {
            var tagPath = $"Controller/Tags/Tag[@Name='{tag_name}']";
            try
            {
                //int tagValue_online = await project.GetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Online);
                int tagValue_offline = await project.GetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Offline);
                return $"{tag_name} value: {tagValue_offline}";
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine("Unable to get tag value DINT");
                Console.WriteLine(ex.Message);
            }
            return "";
        }

        // (DP) Download Project Method
        static public async Task DP(string comm_path, LogixProject project)
        {
            try
            {
                await project.SetCommunicationsPathAsync(comm_path);
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"Unable to set commpath to {comm_path}");
                Console.WriteLine(ex.Message);
            }

            try
            {
                LogixProject.ControllerMode controllerMode = await project.ReadControllerModeAsync();
                if (controllerMode != LogixProject.ControllerMode.Program)
                {
                    Console.WriteLine($"Controller mode is {controllerMode}. Downloading is possible only if the controller is in 'Program' mode");
                }
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"Unable to read ControllerMode");
                Console.WriteLine(ex.Message);
            }

            try
            {
                await project.DownloadAsync();
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"Unable to download");
                Console.WriteLine(ex.Message);
            }

            // Download modifies the project.
            // Without saving, if used file will be opened again, commands which need correlation
            // between program in the controller and opened project like LoadImageFromSDCard or StoreImageOnSDCard
            // may won't be able to succeed because project in the controller won't match opened project.
            try
            {
                await project.SaveAsync();
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"Unable to save project");
                Console.WriteLine(ex.Message);
            }
        }

        // (CCM) Change Controller Mode Method
        static public async Task CCM(string comm_path, int mode_in, LogixProject project)
        {
            uint mode = Convert.ToUInt32(mode_in);

            try
            {
                await project.SetCommunicationsPathAsync(comm_path);
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"Unable to set commpath to {comm_path}");
                Console.WriteLine(ex.Message);
            }

            try
            {
                LogixProject.RequestedControllerMode requestedControllerMode = ToRequestedMode(mode);
                await project.ChangeControllerModeAsync(requestedControllerMode);
                //Console.WriteLine($"Controller mode changed to: {requestedControllerMode}({mode}).");
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"Unable to set mode. Requested mode was {mode}");
                Console.WriteLine(ex.Message);
            }
        }

        // Return Logix Project Mode (supporting CCM method)
        private static LogixProject.RequestedControllerMode ToRequestedMode(uint mode)
        {
            switch (mode)
            {
                case 0:
                    {
                        return LogixProject.RequestedControllerMode.Program;
                    }
                case 1:
                    {
                        return LogixProject.RequestedControllerMode.Run;
                    }
                case 2:
                    {
                        return LogixProject.RequestedControllerMode.Test;
                    }
                default:
                    {
                        var error = $"{mode} is not supported.";
                        Console.WriteLine(error);
                        throw new ArgumentException(error);
                    }
            }
        }

        // (RCM) Read Controller Mode Method
        static public async Task<string> RCM(string comm_path, LogixProject project)
        {
            try
            {
                await project.SetCommunicationsPathAsync(comm_path);
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"Unable to set commpath to {comm_path}");
                Console.WriteLine(ex.Message);
            }

            try
            {
                LogixProject.ControllerMode result = await project.ReadControllerModeAsync();
                switch (result)
                {
                    case LogixProject.ControllerMode.Faulted:
                        return "FAULTED";
                    case LogixProject.ControllerMode.Program:
                        return "PROGRAM";
                    case LogixProject.ControllerMode.Run:
                        return "RUN";
                    case LogixProject.ControllerMode.Test:
                        return "TEST";
                    default:
                        throw new ArgumentOutOfRangeException("Controller mode is unrecognized");
                }
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"Unable to read controller mode");
                Console.WriteLine(ex.Message);
            }

            return "";
        }

        // (STV) Set Tag Value Method
        static public async Task STV(string tag_name, int tag_value_in, LogixProject project)
        {
            string tagPath = $"Controller/Tags/Tag[@Name='{tag_name}']";
            try
            {
                await project.SetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Online, tag_value_in);
                Console.WriteLine("Tag value was set.");
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine("Unable to set tag value DINT");
                Console.WriteLine(ex.Message);
            }

            try
            {
                await project.SaveAsync();
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine("Unable to save project");
                Console.WriteLine(ex.Message);
            }
        }
    }
}









// CODE GRAVEYARD

//Console.SetCursorPosition(0, Console.CursorTop - 1);


//.ConfigureAwait(false) for awaits - not what i wanted

//Console.WriteLine(GTVAsync(filePath, "test_DINT", myProject));
//Directory.SetCurrentDirectory("C:\\Users\\ASYost\\source\\repos\\ra-cicd-test-old\\cicd-config\\stage-test\\GetTagValue\\bin\\Debug\\net6.0");
//Process.Start(".\\GetTagValue", $"\"{acd_path}\" {tag_name}");




//string gtv_dir = "C:\\Users\\ASYost\\source\\repos\\ra-cicd-test-old\\cicd-config\\stage-test\\GetTagValue\\bin\\Debug\\net6.0";
//Console.WriteLine("\n\nGTV directory: " + gtv_dir + "\n");
//try
//{
//    Directory.SetCurrentDirectory(gtv_dir);
//}
//catch (DirectoryNotFoundException e)
//{
//    Console.WriteLine("The specified directry does not exist. {0}", e);
//}
//Console.WriteLine("\nRoot directory: {0}", Directory.GetDirectoryRoot(gtv_dir));
//Console.WriteLine("\nCurrent directory: {0}", Directory.GetCurrentDirectory());
//Console.WriteLine(".\\GetTagValue \"" + filePath + "\" test_DINT");
//Process.Start(".\\GetTagValue", $"\"{filePath}\" test_DINT");
//Process.Start("GetTagValue" + );