using RockwellAutomation.LogixDesigner;
using System;
using System.IO;
using System.Threading.Tasks;
//using RockwellAutomation.FactoryTalkLogixEcho.Api.Client;
//using RockwellAutomation.FactoryTalkLogixEcho.Api.Interfaces;

namespace LogixSDKDemoApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // MODIFY THE TWO STRINGS BELOW BASED ON PROJECT APPLICATION


            if (args.Length != 2)
            {
                Console.WriteLine("Correct Command Example: .\\TestStage_CICDExample filePath commPath");
            }
            //string filePath = args[0];                                                                            // comment out this string if TESTING cicd pipeline
            //string commPath = args[1];                                                                            // comment out this string if TESTING cicd pipeline
            string filePath = @"C:\Users\ASYost\source\repos\ra-cicd-test-old\DEVELOPMENT-files\CICD_test.ACD";     // comment out this string if RUNNING cicd pipeline
            string commPath = @"EmulateEthernet\127.0.0.1";                                                         // comment out this string if RUNNING cicd pipeline

            // Create new report name. Check if file name already exists and if yes, delete it. Then create the new report text file.
            string textFileReportName = Path.Combine(@"C:\Users\ASYost\source\repos\ra-cicd-test-old\cicd-config\stage-test\test-reports\",
                DateTime.Now.ToString("yyyyMMddHHmmss") + "_testfile.txt");
            if (File.Exists(textFileReportName))
            {
                File.Delete(textFileReportName);
            }
            using (StreamWriter sw = File.CreateText(textFileReportName)) ;

            // Start process of sending console printouts to a text file 
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                ostrm = new FileStream(textFileReportName, FileMode.OpenOrCreate, FileAccess.Write);
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
            Console.WriteLine("         CI/CD TEST STAGE | " + DateTime.Now + " " + TimeZoneInfo.Local);
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
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START opening ACD file...");
            LogixProject myProject = await LogixProject.OpenLogixProjectAsync(filePath);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS opening ACD file\n---");

            // Change controller mode to program & verify
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START changing controller to PROGRAM...");
            ChangeControllerModeAsync(commPath, 0, myProject).GetAwaiter().GetResult();
            if (ReadControllerModeAsync(commPath, myProject).GetAwaiter().GetResult() == "PROGRAM")
            {
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS changing controller to PROGRAM\n---");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] FAILURE changing controller to PROGRAM\n---");
            }

            // Download project
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START downloading ACD file...");
            DownloadProjectAsync(commPath, myProject).GetAwaiter().GetResult();
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS downloading ACD file\n---");

            // Change controller mode to run
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START Changing controller to RUN...");
            ChangeControllerModeAsync(commPath, 1, myProject).GetAwaiter().GetResult();
            if (ReadControllerModeAsync(commPath, myProject).GetAwaiter().GetResult() == "RUN")
            {

                Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS changing controller to RUN");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] FAILURE changing controller to RUN");
            }

            // Begin Test Banner
            Console.WriteLine("-----------------------------------------BEGIN TEST------------------------------------------");

            // Initialize variables to be tested
            string test_DINT_1_offline;
            string test_DINT_1_online;
            string TOGGLE_WetBulbTempCalc_online;
            string TOGGLE_WetBulbTempCalc_offline;
            string TEST_AOI_WetBulbTemp_isFahrenheit_online;
            string TEST_AOI_WetBulbTemp_isFahrenheit_offline;
            string TEST_AOI_WetBulbTemp_RelativeHumidity_online;
            string TEST_AOI_WetBulbTemp_RelativeHumidity_offline;
            string TEST_AOI_WetBulbTemp_Temperature_online;
            string TEST_AOI_WetBulbTemp_Temperature_offline;

            // Get initial tag values
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START getting initial project start-up tag values...");
            test_DINT_1_offline = CallGetTagValueAsyncAndWaitOnResult("test_DINT_1", "offline", "DINT", myProject);
            Console.WriteLine(test_DINT_1_offline);
            test_DINT_1_online = CallGetTagValueAsyncAndWaitOnResult("test_DINT_1", "online", "DINT", myProject);
            Console.WriteLine(test_DINT_1_online);

            TOGGLE_WetBulbTempCalc_online = CallGetTagValueAsyncAndWaitOnResult("TOGGLE_WetBulbTempCalc", "online", "BOOL", myProject);
            Console.WriteLine(TOGGLE_WetBulbTempCalc_online);
            TOGGLE_WetBulbTempCalc_offline = CallGetTagValueAsyncAndWaitOnResult("TOGGLE_WetBulbTempCalc", "offline", "BOOL", myProject);
            Console.WriteLine(TOGGLE_WetBulbTempCalc_offline);

            TEST_AOI_WetBulbTemp_isFahrenheit_online = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_isFahrenheit", "online", "BOOL", myProject);
            Console.WriteLine(TEST_AOI_WetBulbTemp_isFahrenheit_online);
            TEST_AOI_WetBulbTemp_isFahrenheit_offline = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_isFahrenheit", "offline", "BOOL", myProject);
            Console.WriteLine(TEST_AOI_WetBulbTemp_isFahrenheit_offline);

            TEST_AOI_WetBulbTemp_RelativeHumidity_online = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_RelativeHumidity", "online", "REAL", myProject);
            Console.WriteLine(TEST_AOI_WetBulbTemp_RelativeHumidity_online);
            TEST_AOI_WetBulbTemp_RelativeHumidity_offline = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_RelativeHumidity", "offline", "REAL", myProject);
            Console.WriteLine(TEST_AOI_WetBulbTemp_RelativeHumidity_offline);

            TEST_AOI_WetBulbTemp_Temperature_online = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_Temperature", "online", "REAL", myProject);
            Console.WriteLine(TEST_AOI_WetBulbTemp_Temperature_online);
            TEST_AOI_WetBulbTemp_Temperature_offline = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_Temperature", "offline", "REAL", myProject);
            Console.WriteLine(TEST_AOI_WetBulbTemp_Temperature_offline);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS getting initial project start-up tag values\n---");

            // Verify that offline and online values are the same
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START verifying whether offline and online values are the same...");
            int failure_condition = 0;
            CompareTwoTags(test_DINT_1_online, test_DINT_1_offline);
            CompareTwoTags(TOGGLE_WetBulbTempCalc_online, TOGGLE_WetBulbTempCalc_offline);
            CompareTwoTags(TEST_AOI_WetBulbTemp_isFahrenheit_online, TEST_AOI_WetBulbTemp_isFahrenheit_offline);
            CompareTwoTags(TEST_AOI_WetBulbTemp_RelativeHumidity_online, TEST_AOI_WetBulbTemp_RelativeHumidity_offline);
            CompareTwoTags(TEST_AOI_WetBulbTemp_Temperature_online, TEST_AOI_WetBulbTemp_Temperature_offline);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE verifying whether offline and online values are the same\n---");

            // Set tag values
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START setting tag values...");
            CallSetTagValueAsyncAndWaitOnResult("test_DINT_1", 111, "online", "DINT", myProject);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS setting tag values\n---");

            // Get online tag values
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START getting tag values...");
            Console.WriteLine(CallGetTagValueAsyncAndWaitOnResult("test_DINT_1", "online", "DINT", myProject));
            Console.WriteLine(CallGetTagValueAsyncAndWaitOnResult("test_DINT_2", "online", "DINT", myProject));
            Console.WriteLine(CallGetTagValueAsyncAndWaitOnResult("test_DINT_3", "online", "DINT", myProject));
            Console.WriteLine(CallGetTagValueAsyncAndWaitOnResult("test_DINT_1", "offline", "DINT", myProject));
            Console.WriteLine(CallGetTagValueAsyncAndWaitOnResult("test_DINT_2", "offline", "DINT", myProject));
            Console.WriteLine(CallGetTagValueAsyncAndWaitOnResult("test_DINT_3", "offline", "DINT", myProject));
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS getting tag values");

            // Test Complete Banner
            Console.WriteLine("---------------------------------------TEST COMPLETE-----------------------------------------");

            // Finish process of sending console printouts to the text file specified earlier
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
        }
        // ======================
        //        METHODS
        // ======================

        // Get Tag Value Method
        static async Task<string> GetTagValueAsync(string tag_name, string online_or_offline, string data_type, LogixProject project)
        {
            var tagPath = $"Controller/Tags/Tag[@Name='{tag_name}']";
            try
            {
                if (online_or_offline == "online")
                {

                    if (data_type == "DINT")
                    {
                        int tagValue_online = await project.GetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Online);
                        return $"{tag_name} {online_or_offline} value: {tagValue_online}";
                    }
                    else if (data_type == "BOOL")
                    {
                        bool tagValue_online = await project.GetTagValueBOOLAsync(tagPath, LogixProject.TagOperationMode.Online);
                        return $"{tag_name} {online_or_offline} value: {tagValue_online}";
                    }
                    else if (data_type == "REAL")
                    {
                        float tagValue_online = await project.GetTagValueREALAsync(tagPath, LogixProject.TagOperationMode.Online);
                        return $"{tag_name} {online_or_offline} value: {tagValue_online}";
                    }
                    else
                    {
                        Console.WriteLine($"ERROR executing command: The data type {data_type} cannot be handled. Select either DINT, BOOL, or REAL.");
                    }
                }
                else if (online_or_offline == "offline")
                {
                    if (data_type == "DINT")
                    {
                        int tagValue_offline = await project.GetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Offline);
                        return $"{tag_name} {online_or_offline} value: {tagValue_offline}";
                    }
                    else if (data_type == "BOOL")
                    {
                        bool tagValue_offline = await project.GetTagValueBOOLAsync(tagPath, LogixProject.TagOperationMode.Offline);
                        return $"{tag_name} {online_or_offline} value: {tagValue_offline}";
                    }
                    else if (data_type == "REAL")
                    {
                        float tagValue_offline = await project.GetTagValueREALAsync(tagPath, LogixProject.TagOperationMode.Offline);
                        return $"{tag_name} {online_or_offline} value: {tagValue_offline}";
                    }
                    else
                    {
                        Console.WriteLine($"ERROR executing command: The tag {tag_name} of data type {data_type} cannot be handled. \n" +
                            $"Select either DINT, BOOL, or REAL.");
                    }
                }
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"ERROR getting {online_or_offline} tag {tag_name} of data type {data_type}");
                Console.WriteLine(ex.Message);
            }
            return "";
        }

        // Get Tag Value Wait On Result Method
        public static string CallGetTagValueAsyncAndWaitOnResult(string tag_name, string online_or_offline, string data_type, LogixProject project)
        {
            var task = GetTagValueAsync(tag_name, online_or_offline, data_type, project);
            task.Wait();
            var result = task.Result;
            return result;
        }

        // Set Tag Value Method
        static async Task SetTagValueAsync(string tag_name, int tag_value_in, string online_or_offline, string data_type, LogixProject project)
        {
            //int tagValue = int.Parse(tag_value_in);
            var tagPath = $"Controller/Tags/Tag[@Name='{tag_name}']";
            try
            {
                if (online_or_offline == "online")
                {
                    if (data_type == "DINT")
                    {
                        await project.SetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Online, tag_value_in);
                        Console.WriteLine($"{tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else if (data_type == "BOOL")
                    {
                        await project.SetTagValueBOOLAsync(tagPath, LogixProject.TagOperationMode.Online, Convert.ToBoolean(tag_value_in));
                        Console.WriteLine($"{tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else if (data_type == "REAL")
                    {
                        await project.SetTagValueREALAsync(tagPath, LogixProject.TagOperationMode.Online, tag_value_in);
                        Console.WriteLine($"{tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else
                    {
                        Console.WriteLine($"ERROR executing command: The data type {data_type} cannot be handled. Select either DINT, BOOL, or REAL.");
                    }
                }
                else if (online_or_offline == "offline")
                {
                    if (data_type == "DINT")
                    {
                        await project.SetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Offline, tag_value_in);
                        Console.WriteLine($"{tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else if (data_type == "BOOL")
                    {
                        await project.SetTagValueBOOLAsync(tagPath, LogixProject.TagOperationMode.Offline, Convert.ToBoolean(tag_value_in));
                        Console.WriteLine($"{tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else if (data_type == "REAL")
                    {
                        await project.SetTagValueREALAsync(tagPath, LogixProject.TagOperationMode.Offline, tag_value_in);
                        Console.WriteLine($"{tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else
                    {
                        Console.WriteLine($"ERROR executing command: The data type {data_type} cannot be handled. Select either DINT, BOOL, or REAL.");
                    }
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
        public static void CallSetTagValueAsyncAndWaitOnResult(string tag_name, int tag_value_in, string online_or_offline, string data_type, LogixProject project)
        {
            var task = SetTagValueAsync(tag_name, tag_value_in, online_or_offline, data_type, project);
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

        // Compare Two Tags Method
        private static void CompareTwoTags(string input1, string input2)
        {
            if (input1 != input2)
            {
                Console.WriteLine($"FAILURE: {input1} and {input2} NOT equal.");
            }
            else
            {
                Console.WriteLine($"SUCCESS: {input1} and {input2} are EQUAL.");
            }
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