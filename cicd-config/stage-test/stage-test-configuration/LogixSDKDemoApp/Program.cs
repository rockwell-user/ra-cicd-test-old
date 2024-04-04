﻿using RockwellAutomation.FactoryTalkLogixEcho.Api.Client;
using RockwellAutomation.FactoryTalkLogixEcho.Api.Interfaces;
using RockwellAutomation.LogixDesigner;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestStage_CICDExample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // MODIFY THE TWO STRINGS BELOW BASED ON PROJECT APPLICATION
            if (args.Length != 2)
            {
                Console.WriteLine(@"Correct Command: .\TestStage_CICDExample github_RepositoryDirectory acd_filename");
                Console.WriteLine(@"Example Format:  .\TestStage_CICDExample C:\Users\TestUser\Desktop\example-github-repo\ acd_filename.ACD");
            }
            //string github_directory = args[0];
            //string controllerFile = args[1];
            //string filePath = github_directory + "\\DEVELOPMENT-files\\" + controllerFile;     // comment out if TESTING
            string filePath = @"C:\Users\ASYost\source\repos\ra-cicd-test-old\DEVELOPMENT-files\CICD_test.ACD";


            // Set up emulated controller (based on the specified ACD file path) if one does not yet exist. If not, continue.
            var serviceClient = ClientFactory.GetServiceApiClientV2("CLIENT_TestStage_CICDExample");
            serviceClient.Culture = new CultureInfo("en-US");


            string[] testControllerInfo = await GetControllerInfo("CICDtest_chassis", "CICD_test", serviceClient); /////////////////////////////////////////////////////////////
            Console.WriteLine("TEST COMMPATH: " + testControllerInfo[0]);
            Console.WriteLine("TEST COMMPATH: " + testControllerInfo[1]);
            Console.WriteLine("TEST COMMPATH: " + testControllerInfo[2]);

            string commPath = @"EmulateEthernet\" + testControllerInfo[1];

            // Create new report name. Check if file name already exists and if yes, delete it. Then create the new report text file.
            //string textFileReportName = Path.Combine(github_directory + @"cicd-config\stage-test\test-reports\", DateTime.Now.ToString("yyyyMMddHHmmss") + "_testfile.txt");
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

            // PUT STUFF BACK HERE


            if (CallCheckCurrentChassisAsyncAndWaitOnResult("CICDtest_chassis", "CICD_test", serviceClient) == false)
            {
                var chassisUpdate = new ChassisUpdate
                {
                    Name = "CICDtest_chassis",
                    Description = "Test chassis for CI/CD demonstration."
                };
                ChassisData chassis_CICD = await serviceClient.CreateChassis(chassisUpdate);
                using (var fileHandle = await serviceClient.SendFile(filePath))
                {
                    var controllerUpdate = await serviceClient.GetControllerInfoFromAcd(fileHandle);
                    controllerUpdate.ChassisGuid = chassis_CICD.ChassisGuid;
                    var controllerData = await serviceClient.CreateController(controllerUpdate);
                }
            }

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
            string[] test_DINT_1;
            string[] TOGGLE_WetBulbTempCalc;
            string[] TEST_AOI_WetBulbTemp_isFahrenheit;
            string[] TEST_AOI_WetBulbTemp_RelativeHumidity;
            string[] TEST_AOI_WetBulbTemp_Temperature;
            string[] TEST_AOI_WetBulbTemp_WetBulbTemp;

            // Get initial project start-up tag values
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START getting initial project start-up tag values...");
            test_DINT_1 = CallGetTagValueAsyncAndWaitOnResult("test_DINT_1", "DINT", myProject);
            TOGGLE_WetBulbTempCalc = CallGetTagValueAsyncAndWaitOnResult("TOGGLE_WetBulbTempCalc", "BOOL", myProject);
            TEST_AOI_WetBulbTemp_isFahrenheit = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_isFahrenheit", "BOOL", myProject);
            TEST_AOI_WetBulbTemp_RelativeHumidity = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_RelativeHumidity", "REAL", myProject);
            TEST_AOI_WetBulbTemp_Temperature = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_Temperature", "REAL", myProject);
            TEST_AOI_WetBulbTemp_WetBulbTemp = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_WetBulbTemp", "REAL", myProject);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE getting initial project start-up tag values\n---");

            // Verify whether offline and online values are the same
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START verifying whether offline and online values are the same...");
            int failure_condition = 0;
            failure_condition = failure_condition + CompareOnlineOffline(test_DINT_1);
            failure_condition = failure_condition + CompareOnlineOffline(TOGGLE_WetBulbTempCalc);
            failure_condition = failure_condition + CompareOnlineOffline(TEST_AOI_WetBulbTemp_isFahrenheit);
            failure_condition = failure_condition + CompareOnlineOffline(TEST_AOI_WetBulbTemp_RelativeHumidity);
            failure_condition = failure_condition + CompareOnlineOffline(TEST_AOI_WetBulbTemp_Temperature);
            failure_condition = failure_condition + CompareOnlineOffline(TEST_AOI_WetBulbTemp_WetBulbTemp);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE verifying whether offline and online values are the same\n---");

            // Set tag values
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START setting tag values...");
            CallSetTagValueAsyncAndWaitOnResult("test_DINT_1", 111, "online", "DINT", myProject);
            CallSetTagValueAsyncAndWaitOnResult("TOGGLE_WetBulbTempCalc", 1, "online", "BOOL", myProject);
            CallSetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_isFahrenheit", 1, "online", "BOOL", myProject);
            CallSetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_RelativeHumidity", 30, "online", "REAL", myProject);
            CallSetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_Temperature", 70, "online", "REAL", myProject);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE setting tag values\n---");

            // Verify expected output
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START verifying expected tag outputs...");
            TEST_AOI_WetBulbTemp_WetBulbTemp = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_WetBulbTemp", "REAL", myProject);
            if (TEST_AOI_WetBulbTemp_WetBulbTemp[0] == "52.997536")
            {
                Console.WriteLine($"SUCCESS: tag TEST_AOI_WetBulbTemp_WetBulbTemp returned expected result {TEST_AOI_WetBulbTemp_WetBulbTemp[0]}");
            }
            else
            {
                Console.WriteLine($"FAILURE: : tag TEST_AOI_WetBulbTemp_WetBulbTemp returned result {TEST_AOI_WetBulbTemp_WetBulbTemp[0]} when expected 52.997536");
                ++failure_condition;
            }
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE verifying expected tag outputs\n---");


            // Show final tag values
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START showing final test tag values...");
            test_DINT_1 = CallGetTagValueAsyncAndWaitOnResult("test_DINT_1", "DINT", myProject);
            Console.WriteLine($"{test_DINT_1[2]}\n{test_DINT_1[5]}");
            TOGGLE_WetBulbTempCalc = CallGetTagValueAsyncAndWaitOnResult("TOGGLE_WetBulbTempCalc", "BOOL", myProject);
            Console.WriteLine($"{TOGGLE_WetBulbTempCalc[2]}\n{TOGGLE_WetBulbTempCalc[5]}");
            TEST_AOI_WetBulbTemp_isFahrenheit = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_isFahrenheit", "BOOL", myProject);
            Console.WriteLine($"{TEST_AOI_WetBulbTemp_isFahrenheit[2]}\n{TEST_AOI_WetBulbTemp_isFahrenheit[5]}");
            TEST_AOI_WetBulbTemp_RelativeHumidity = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_RelativeHumidity", "REAL", myProject);
            Console.WriteLine($"{TEST_AOI_WetBulbTemp_RelativeHumidity[2]}\n{TEST_AOI_WetBulbTemp_RelativeHumidity[5]}");
            TEST_AOI_WetBulbTemp_Temperature = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_Temperature", "REAL", myProject);
            Console.WriteLine($"{TEST_AOI_WetBulbTemp_Temperature[2]}\n{TEST_AOI_WetBulbTemp_Temperature[5]}");
            Console.WriteLine($"{TEST_AOI_WetBulbTemp_WetBulbTemp[2]}\n{TEST_AOI_WetBulbTemp_WetBulbTemp[5]}");
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE showing final test tag values");

            // Final Banner
            if (failure_condition > 0)
            {
                Console.WriteLine("---------------------------------------TEST FAILURE------------------------------------------");
            }
            else
            {
                Console.WriteLine("---------------------------------------TEST SUCCESS------------------------------------------");
            }

            // Finish process of sending console printouts to the text file specified earlier
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
        }
        // ======================
        //        METHODS
        // ======================

        // Get Controller Info Method
        // return_array[0] = controller name
        // return_array[1] = controller IP address
        // return_array[2] = controller project file path
        private static async Task<string[]> GetControllerInfo(string chassis_name, string controller_name, IServiceApiClientV2 serviceClient)
        {
            string[] return_array = new string[3];
            var chassisList = (await serviceClient.ListChassis()).ToList();
            for (int i = 0; i < chassisList.Count; i++)
            {
                if (chassisList[i].Name == chassis_name)
                {
                    var chassisGuid = chassisList[i].ChassisGuid;
                    var controllerList = (await serviceClient.ListControllers(chassisGuid)).ToList();
                    for (int j = 0; j < controllerList.Count; j++)
                    {
                        if (controllerList[j].ControllerName == controller_name)
                        {
                            return_array[0] = controllerList[j].ControllerName;
                            return_array[1] = controllerList[j].IPConfigurationData.Address.ToString() ?? "";
                            return_array[2] = controllerList[j].ProjectPath;
                        }
                    }
                }
            }
            return return_array;
        }

        // Check Current Chassis For A Specific Chassis Name Method
        private static async Task<bool> CheckCurrentChassisAsync(string chassis_name, string controller_name, IServiceApiClientV2 serviceClient)
        {
            var chassisList = (await serviceClient.ListChassis()).ToList();
            for (int i = 0; i < chassisList.Count; i++)
            {
                if (chassisList[i].Name == chassis_name)
                {
                    var chassisGuid = chassisList[i].ChassisGuid;
                    var controllerList = (await serviceClient.ListControllers(chassisGuid)).ToList();
                    for (int j = 0; j < controllerList.Count; j++)
                    {
                        if (controllerList[j].ControllerName == controller_name)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Check Current Chassis For A Specific Chassis Name Wait On Result Method
        private static bool CallCheckCurrentChassisAsyncAndWaitOnResult(string chassis_name, string controller_name, IServiceApiClientV2 serviceClient)
        {
            var task = CheckCurrentChassisAsync(chassis_name, controller_name, serviceClient);
            task.Wait();
            var result = task.Result;
            return result;
        }

        // Get Tag Value Method
        // return_array[0] = $"{tagValue_online}";
        // return_array[1] = $"{tag_name} online value";
        // return_array[2] = $"{tag_name} online value: {tagValue_online}";
        // return_array[3] = $"{tagValue_offline}";
        // return_array[4] = $"{tag_name} offline value";
        // return_array[5] = $"{tag_name} offline value: {tagValue_offline}";
        private static async Task<string[]> GetTagValueAsync(string tag_name, string data_type, LogixProject project)
        {
            var tagPath = $"Controller/Tags/Tag[@Name='{tag_name}']";
            // $"Controller/Tags/AOI_TAG_VAL/Tag[@Name='{tag_name}']"
            // $"Controller/Tags/AOI_TAG_VAL/'{tag_name}']"
            string[] return_array = new string[6];
            try
            {
                if (data_type == "DINT")
                {
                    int tagValue_online = await project.GetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Online);
                    return_array[0] = $"{tagValue_online}";
                    return_array[1] = $"{tag_name} online value";
                    return_array[2] = $"{tag_name} online value: {tagValue_online}";
                }
                else if (data_type == "BOOL")
                {
                    bool tagValue_online = await project.GetTagValueBOOLAsync(tagPath, LogixProject.TagOperationMode.Online);
                    return_array[0] = $"{tagValue_online}";
                    return_array[1] = $"{tag_name} online value";
                    return_array[2] = $"{tag_name} online value: {tagValue_online}";
                }
                else if (data_type == "REAL")
                {
                    float tagValue_online = await project.GetTagValueREALAsync(tagPath, LogixProject.TagOperationMode.Online);
                    return_array[0] = $"{tagValue_online}";
                    return_array[1] = $"{tag_name} online value";
                    return_array[2] = $"{tag_name} online value: {tagValue_online}";
                }
                else
                {
                    Console.WriteLine($"ERROR executing command: The data type {data_type} cannot be handled. Select either DINT, BOOL, or REAL.");
                }
                if (data_type == "DINT")
                {
                    int tagValue_offline = await project.GetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Offline);
                    return_array[3] = $"{tagValue_offline}";
                    return_array[4] = $"{tag_name} offline value";
                    return_array[5] = $"{tag_name} offline value: {tagValue_offline}";
                }
                else if (data_type == "BOOL")
                {
                    bool tagValue_offline = await project.GetTagValueBOOLAsync(tagPath, LogixProject.TagOperationMode.Offline);
                    return_array[3] = $"{tagValue_offline}";
                    return_array[4] = $"{tag_name} offline value";
                    return_array[5] = $"{tag_name} offline value: {tagValue_offline}";
                }
                else if (data_type == "REAL")
                {
                    float tagValue_offline = await project.GetTagValueREALAsync(tagPath, LogixProject.TagOperationMode.Offline);
                    return_array[3] = $"{tagValue_offline}";
                    return_array[4] = $"{tag_name} offline value";
                    return_array[5] = $"{tag_name} offline value: {tagValue_offline}";
                }
                else
                {
                    Console.WriteLine($"ERROR executing command: The tag {tag_name} of data type {data_type} cannot be handled. \n" +
                        $"Select either DINT, BOOL, or REAL.");
                }
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"ERROR getting tag {tag_name}.");
                Console.WriteLine(ex.Message);
            }
            return return_array;
        }

        // Get Tag Value Wait On Result Method
        private static string[] CallGetTagValueAsyncAndWaitOnResult(string tag_name, string data_type, LogixProject project)
        {
            var task = GetTagValueAsync(tag_name, data_type, project);
            task.Wait();
            var result = task.Result;
            return result;
        }

        // Set Tag Value Method
        private static async Task SetTagValueAsync(string tag_name, int tag_value_in, string online_or_offline, string data_type, LogixProject project)
        {
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
        private static void CallSetTagValueAsyncAndWaitOnResult(string tag_name, int tag_value_in, string online_or_offline, string data_type, LogixProject project)
        {
            var task = SetTagValueAsync(tag_name, tag_value_in, online_or_offline, data_type, project);
            task.Wait();
        }

        // Download Project Method
        private static async Task DownloadProjectAsync(string comm_path, LogixProject project)
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
        private static async Task ChangeControllerModeAsync(string comm_path, int mode_in, LogixProject project)
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

        // Return Logix Project Mode (supporting Change Controller Mode Method)
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
        private static async Task<string> ReadControllerModeAsync(string comm_path, LogixProject project)
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
        private static int CompareOnlineOffline(string[] input_string)
        {
            if (input_string[0] != input_string[3])
            {
                Console.WriteLine($"FAILURE: {input_string[1]} ({input_string[0]}) & {input_string[4]} ({input_string[3]}) NOT equal.");
                return 1;

            }
            else
            {
                Console.WriteLine($"SUCCESS: {input_string[1]} ({input_string[0]}) & {input_string[4]} ({input_string[3]}) are EQUAL.");
                return 0;
            }
        }
    }
}