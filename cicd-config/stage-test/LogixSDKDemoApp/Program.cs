using RockwellAutomation.LogixDesigner;
using System.Threading.Tasks;
using System.IO;
using System;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;

namespace LogixSDKDemoApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            DateTime currentTime = DateTime.Now;

            // MODIFY THE TWO STRINGS BELOW BASED ON PROJECT APPLICATION
            string filePath = @"C:\Users\ASYost\Desktop\s5k_cicd_testfiles\CICD_test.ACD";
            string commPath = @"EmulateEthernet\127.0.0.1";
            //string textFileName = @"C:\Users\ASYost\source\repos\ra-cicd-test-old\cicd-config\stage-test\testfile.txt";
            string textFileName = Path.Combine(@"C:\Users\ASYost\source\repos\ra-cicd-test-old\cicd-config\stage-test\", 
                DateTime.Now.ToString("yyyyMMddHHmmss") + "_testfile.txt");
            
            // Create new text file

            
            // Check if file already exists. If yes, delete it.
            if (File.Exists(textFileName))
            {
                File.Delete(textFileName);
            }
            using (StreamWriter sw = File.CreateText(textFileName)) ;

            // Start process of sending console printouts to a text file 
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
                {
                    ostrm = new FileStream(textFileName, FileMode.OpenOrCreate, FileAccess.Write);
                    writer = new StreamWriter(ostrm);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Cannot open Redirect.txt for writing");
                    Console.WriteLine(e.Message);
                    return;
                }
            Console.SetOut(writer);

            // Title Banner 
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

            // Staging Test Banner
            Console.WriteLine("----------------------------------------STAGING TEST-----------------------------------------");

            // Open the ACD project file and store the reference as myProject.
            Console.WriteLine("Opening ACD file...");
            LogixProject myProject = await LogixProject.OpenLogixProjectAsync(filePath);
            Console.WriteLine("Opening ACD file... SUCCESS\n---");

            // Change controller mode to program & verify
            Console.WriteLine("Changing controller to PROGRAM...");
            ChangeControllerModeAsync(commPath, 0, myProject).GetAwaiter().GetResult();
            if (ReadControllerModeAsync(commPath, myProject).GetAwaiter().GetResult() == "PROGRAM")
            {
                Console.WriteLine("Changing controller to PROGRAM...  SUCCESS\n---");
            }
            else
            {
                Console.WriteLine("Changing controller to PROGRAM...  FAILURE\n---");
            }

            // Download project
            Console.WriteLine("Downloading ACD file...");
            DownloadProjectAsync(commPath, myProject).GetAwaiter().GetResult();
            Console.WriteLine("Downloading ACD file...  SUCCESS\n---");

            // Change controller mode to run
            Console.WriteLine("Changing controller to RUN...");
            ChangeControllerModeAsync(commPath, 1, myProject).GetAwaiter().GetResult();
            if (ReadControllerModeAsync(commPath, myProject).GetAwaiter().GetResult() == "RUN")
            {

                Console.WriteLine("Changing controller to RUN...  SUCCESS\n---");
            }
            else
            {
                Console.WriteLine("Changing controller to RUN...  FAILURE\n\n");
            }

            // Begin Test Banner
            Console.WriteLine("-----------------------------------------BEGIN TEST------------------------------------------");

            // Get offline tag values
            Console.WriteLine("Getting Tag Values...");
            string test_DINT_1 = CallGetTagValueAsyncAndWaitOnResult("test_DINT_1", "offline", myProject);
            Console.WriteLine(test_DINT_1);
            string test_DINT_2 = CallGetTagValueAsyncAndWaitOnResult("test_DINT_2", "offline", myProject);
            Console.WriteLine(test_DINT_2);
            string test_DINT_3 = CallGetTagValueAsyncAndWaitOnResult("test_DINT_3", "offline", myProject);
            Console.WriteLine(test_DINT_3);
            Console.WriteLine("Getting Tag Values...  COMPLETE\n---");

            // Set tag values
            Console.WriteLine("Setting Tag Values...");
            CallSetTagValueAsyncAndWaitOnResult("test_DINT_1", 111, "online", myProject);
            CallSetTagValueAsyncAndWaitOnResult("test_DINT_2", 222, "online", myProject);
            CallSetTagValueAsyncAndWaitOnResult("test_DINT_3", 333, "online",  myProject);
            Console.WriteLine("Setting Tag Values...  COMPLETE\n---");

            // Get online tag values
            Console.WriteLine("Getting Tag Values...");
            Console.WriteLine(CallGetTagValueAsyncAndWaitOnResult("test_DINT_1", "online", myProject));
            Console.WriteLine(CallGetTagValueAsyncAndWaitOnResult("test_DINT_2", "online", myProject));
            Console.WriteLine(CallGetTagValueAsyncAndWaitOnResult("test_DINT_3", "online", myProject));
            Console.WriteLine(CallGetTagValueAsyncAndWaitOnResult("test_DINT_1", "offline", myProject));
            Console.WriteLine(CallGetTagValueAsyncAndWaitOnResult("test_DINT_2", "offline", myProject));
            Console.WriteLine(CallGetTagValueAsyncAndWaitOnResult("test_DINT_3", "offline", myProject));
            Console.WriteLine("Getting Tag Values...  COMPLETE");

            // Test Complete Banner
            Console.WriteLine("----------------------------------------TEST COMPLETE----------------------------------------");

            // Finish process of sending console printouts to a text file
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
        }


        // ======================
        //        METHODS
        // ======================

        // Get Tag Value Method
        static async Task<string> GetTagValueAsync(string tag_name, string online_or_offline, LogixProject project)
        {
            var tagPath = $"Controller/Tags/Tag[@Name='{tag_name}']";
            try
            {
                if (online_or_offline == "online")
                {
                    int tagValue_offline = await project.GetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Online);
                    return $"{tag_name} {online_or_offline} value: {tagValue_offline}";
                }
                else if (online_or_offline == "offline")
                {
                    int tagValue_offline = await project.GetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Offline);
                    return $"{tag_name} {online_or_offline} value: {tagValue_offline}";
                }                
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine("Unable to get tag value DINT");
                Console.WriteLine(ex.Message);
            }
            return "";
        }

        // Get Tag Value Wait On Result Method
        public static string CallGetTagValueAsyncAndWaitOnResult(string tag_name, string online_or_offline, LogixProject project)
        {
            var task = GetTagValueAsync(tag_name, online_or_offline, project);
            task.Wait();
            var result = task.Result;
            return result;
        }

        // Set Tag Value Method
        static async Task SetTagValueAsync(string tag_name, int tag_value_in, string online_or_offline, LogixProject project)
        {
            //int tagValue = int.Parse(tag_value_in);
            var tagPath = $"Controller/Tags/Tag[@Name='{tag_name}']";
            try
            {
                if (online_or_offline == "online")
                {
                    await project.SetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Online, tag_value_in);
                    Console.WriteLine($"{tag_name} {online_or_offline} new value: {tag_value_in}");
                }
                else if (online_or_offline == "offline")
                {
                    await project.SetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Offline, tag_value_in);
                    Console.WriteLine($"{tag_name} {online_or_offline} new value: {tag_value_in}");
                }

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

        // Set Tag Value Wait On Result Method
        public static void CallSetTagValueAsyncAndWaitOnResult(string tag_name, int tag_value_in, string online_or_offline, LogixProject project)
        {
            var task = SetTagValueAsync(tag_name, tag_value_in, online_or_offline, project);
            task.Wait();
        }

        // Download Project Method
        static async Task DownloadProjectAsync(string comm_path, LogixProject project)
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

        // Change Controller Mode Method
        static async Task ChangeControllerModeAsync(string comm_path, int mode_in, LogixProject project)
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

        // Read Controller Mode Method
        static async Task<string> ReadControllerModeAsync(string comm_path, LogixProject project)
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
    }
}









// CODE GRAVEYARD

//Console.SetCursorPosition(0, Console.CursorTop - 1);
// Thread.Sleep(2000);

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