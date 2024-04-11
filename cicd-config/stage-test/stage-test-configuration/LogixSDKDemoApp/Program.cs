// ---------------------------------------------------------------------------------------------------------------------------------------------------------------
//
// FileName: Program.cs
// FileType: Visual C# Source file
// Size : 31,224
// Author : Rockwell Automation
// Created : 2024
// Last Modified : 04/XX/2024
// Copy Rights : Rockwell Automation
// Description : Program to provide an example test in a CI/CD pipeline utilizing Studio 5000 Logix Designer SDK and FactoryTalk Logix Echo SDK.
//
// ---------------------------------------------------------------------------------------------------------------------------------------------------------------

#region INCLUDED PROJECT LIBRARIES ---------------------------------------------------------------------------------------------------------------------------------------
using Google.Protobuf;
using RockwellAutomation.FactoryTalkLogixEcho.Api.Client;
using RockwellAutomation.FactoryTalkLogixEcho.Api.Interfaces;
using RockwellAutomation.LogixDesigner;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RockwellAutomation.LogixDesigner.LogixProject;
#endregion

namespace TestStage_CICDExample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            #region TESTING VARIABLES TO BE COMMENTED OUT OR REMOVED WHEN NOT TROUBLESHOOTING ----------------------------------------------------------------------------
            string filePath = @"C:\Users\ASYost\source\repos\ra-cicd-test-old\DEVELOPMENT-files\CICD_test.ACD";
            string textFileReportName = Path.Combine(@"C:\Users\ASYost\source\repos\ra-cicd-test-old\cicd-config\stage-test\test-reports\",
                DateTime.Now.ToString("yyyyMMddHHmmss") + "_testfile.txt");
            #endregion

            //#region PARSING INCOMING VARIABLES WHEN RUNNING PROJECT EXECUTABLE -------------------------------------------------------------------------------------------
            //// Pass the incoming executable variables
            //if (args.Length != 2)
            //{
            //    Console.WriteLine(@"Correct Command: .\TestStage_CICDExample github_RepositoryDirectory acd_filename");
            //    Console.WriteLine(@"Example Format:  .\TestStage_CICDExample C:\Users\TestUser\Desktop\example-github-repo\ acd_filename.ACD");
            //}
            //string github_directory = args[0];
            //string controllerFile = args[1];
            //string filePath = github_directory + @"DEVELOPMENT-files\" + controllerFile;     // comment out if TESTING
            //string textFileReportName = Path.Combine(github_directory + @"cicd-config\stage-test\test-reports\", DateTime.Now.ToString("yyyyMMddHHmmss") + "_testfile.txt");
            //#endregion











            //LogixProject myProject1 = await LogixProject.OpenLogixProjectAsync(filePath);

























            //#region TEST FILE CREATION -----------------------------------------------------------------------------------------------------------------------------------
            // Create new report name. Check if file name already exists and if yes, delete it. Then create the new report text file.
            if (File.Exists(textFileReportName))
                File.Delete(textFileReportName);

            //// Start process of sending console printouts to a text file 
            //FileStream ostrm;
            //StreamWriter writer;
            //TextWriter oldOut = Console.Out;

            //try
            //{
            //    ostrm = new FileStream(textFileReportName, FileMode.OpenOrCreate, FileAccess.Write);
            //    writer = new StreamWriter(ostrm);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("Cannot open Redirect.txt for writing");
            //    Console.WriteLine(e.Message);
            //    return;
            //}
            //Console.SetOut(writer);

            // Title Banner 
            Console.WriteLine("  ========================================================================================================  ");
            Console.WriteLine("============================================================================================================\n");
            Console.WriteLine("                   CI/CD TEST STAGE | " + DateTime.Now + " " + TimeZoneInfo.Local);
            Console.WriteLine("\n============================================================================================================");
            Console.WriteLine("  ========================================================================================================  \n\n");

            // Printout relevant test information
            Console.WriteLine("--------------------------------------TEST DEPENDENCIES-----------------------------------------------------");
            Console.WriteLine($"ACD file path specified:          {filePath}");
            Console.WriteLine("Common language runtime version:  " + typeof(string).Assembly.ImageRuntimeVersion);

            // Staging Test Banner
            Console.WriteLine("----------------------------------------STAGING TEST--------------------------------------------------------");

            // Set up emulated controller (based on the specified ACD file path) if one does not yet exist. If not, continue.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START setting up FactoryTalk Logix Echo emulated controller...");
            var serviceClient = ClientFactory.GetServiceApiClientV2("CLIENT_TestStage_CICDExample");
            serviceClient.Culture = new CultureInfo("en-US");
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
            string[] testControllerInfo = await GetControllerInfo("CICDtest_chassis", "CICD_test", serviceClient);
            string commPath = @"EmulateEthernet\" + testControllerInfo[1];
            Console.WriteLine($"SUCCESS: project communication path specified is \"{commPath}\"");
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE setting up FactoryTalk Logix Echo emulated controller\n---");

            // Open the ACD project file and store the reference as myProject.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START opening ACD file...");
            LogixProject myProject = await LogixProject.OpenLogixProjectAsync(filePath);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS opening ACD file\n---");

            // Change controller mode to program & verify
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START changing controller to PROGRAM...");
            ChangeControllerModeAsync(commPath, 0, myProject).GetAwaiter().GetResult();
            if (ReadControllerModeAsync(commPath, myProject).GetAwaiter().GetResult() == "PROGRAM")
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS changing controller to PROGRAM\n---");
            else
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] FAILURE changing controller to PROGRAM\n---");

            // Download project
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START downloading ACD file...");
            DownloadProjectAsync(commPath, myProject).GetAwaiter().GetResult();
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS downloading ACD file\n---");

            // Change controller mode to run
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START Changing controller to RUN...");
            ChangeControllerModeAsync(commPath, 1, myProject).GetAwaiter().GetResult();
            if (ReadControllerModeAsync(commPath, myProject).GetAwaiter().GetResult() == "RUN")
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS changing controller to RUN");
            else
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] FAILURE changing controller to RUN");

            // Begin Test Banner
            Console.WriteLine("-----------------------------------------BEGIN TEST---------------------------------------------------------");

            // Get initial project start-up tag values
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START getting initial project start-up tag values...");
            string filePath_MainProgram = $"Controller/Programs/Program[@Name='MainProgram']/Tags/Tag";
            string filePath_ControllerScope = $"Controller/Tags/Tag";
            string[] TEST_DINT_1 = CallGetTagValueAsyncAndWaitOnResult("TEST_DINT_1", "DINT", filePath_ControllerScope, myProject);
            string[] TEST_TOGGLE_WetBulbTempCalc = CallGetTagValueAsyncAndWaitOnResult("TEST_TOGGLE_WetBulbTempCalc", "BOOL", filePath_MainProgram, myProject);
            string[] TEST_AOI_WetBulbTemp_isFahrenheit = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_isFahrenheit", "BOOL", filePath_MainProgram, myProject);
            string[] TEST_AOI_WetBulbTemp_RelativeHumidity = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_RelativeHumidity", "REAL", filePath_MainProgram, myProject);
            string[] TEST_AOI_WetBulbTemp_Temperature = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_Temperature", "REAL", filePath_MainProgram, myProject);
            string[] TEST_AOI_WetBulbTemp_WetBulbTemp = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_WetBulbTemp", "REAL", filePath_MainProgram, myProject);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE getting initial project start-up tag values\n---");

            // Verify whether offline and online values are the same
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START verifying whether offline and online values are the same...");
            int failure_condition = 0;
            failure_condition = failure_condition + CompareOnlineOffline(TEST_DINT_1);
            failure_condition = failure_condition + CompareOnlineOffline(TEST_TOGGLE_WetBulbTempCalc);
            failure_condition = failure_condition + CompareOnlineOffline(TEST_AOI_WetBulbTemp_isFahrenheit);
            failure_condition = failure_condition + CompareOnlineOffline(TEST_AOI_WetBulbTemp_RelativeHumidity);
            failure_condition = failure_condition + CompareOnlineOffline(TEST_AOI_WetBulbTemp_Temperature);
            failure_condition = failure_condition + CompareOnlineOffline(TEST_AOI_WetBulbTemp_WetBulbTemp);
            Console.Write($"[{DateTime.Now.ToString("T")}] DONE verifying whether offline and online values are the same\n---\n");

            // Set tag values
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START setting tag values...");
            CallSetTagValueAsyncAndWaitOnResult(TEST_DINT_1[6], 111, "online", TEST_DINT_1[7], filePath_ControllerScope, myProject);
            CallSetTagValueAsyncAndWaitOnResult(TEST_TOGGLE_WetBulbTempCalc[6], 1, "online", TEST_TOGGLE_WetBulbTempCalc[7], filePath_MainProgram, myProject);
            CallSetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_isFahrenheit[6], 1, "online", TEST_AOI_WetBulbTemp_isFahrenheit[7], filePath_MainProgram, myProject);
            CallSetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_RelativeHumidity[6], 30, "online", TEST_AOI_WetBulbTemp_RelativeHumidity[7], filePath_MainProgram, myProject);
            CallSetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_Temperature[6], 70, "online", TEST_AOI_WetBulbTemp_Temperature[7], filePath_MainProgram, myProject);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE setting tag values\n---");

            // Verify expected output
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START verifying expected tag outputs...");
            TEST_AOI_WetBulbTemp_WetBulbTemp = CallGetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_WetBulbTemp[6], TEST_AOI_WetBulbTemp_WetBulbTemp[7], filePath_MainProgram, myProject);
            if (TEST_AOI_WetBulbTemp_WetBulbTemp[0] == "52.997536")
                Console.WriteLine($"SUCCESS: tag TEST_AOI_WetBulbTemp_WetBulbTemp returned expected result {TEST_AOI_WetBulbTemp_WetBulbTemp[0]}");
            else
            {
                Console.WriteLine($"FAILURE: : tag TEST_AOI_WetBulbTemp_WetBulbTemp returned result {TEST_AOI_WetBulbTemp_WetBulbTemp[0]} when expected 52.997536");
                ++failure_condition;
            }
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE verifying expected tag outputs\n---");

            // Show final tag values
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START showing final test tag values...");
            TEST_DINT_1 = CallGetTagValueAsyncAndWaitOnResult(TEST_DINT_1[6], TEST_DINT_1[7], filePath_ControllerScope, myProject);
            Console.WriteLine($"{TEST_DINT_1[2]}\n{TEST_DINT_1[5]}");
            TEST_TOGGLE_WetBulbTempCalc = CallGetTagValueAsyncAndWaitOnResult(TEST_TOGGLE_WetBulbTempCalc[6], TEST_TOGGLE_WetBulbTempCalc[7], filePath_MainProgram, myProject);
            Console.WriteLine($"{TEST_TOGGLE_WetBulbTempCalc[2]}\n{TEST_TOGGLE_WetBulbTempCalc[5]}");
            TEST_AOI_WetBulbTemp_isFahrenheit = CallGetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_isFahrenheit[6], TEST_AOI_WetBulbTemp_isFahrenheit[7], filePath_MainProgram, myProject);
            Console.WriteLine($"{TEST_AOI_WetBulbTemp_isFahrenheit[2]}\n{TEST_AOI_WetBulbTemp_isFahrenheit[5]}");
            TEST_AOI_WetBulbTemp_RelativeHumidity = CallGetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_RelativeHumidity[6], TEST_AOI_WetBulbTemp_RelativeHumidity[7], filePath_MainProgram, myProject);
            Console.WriteLine($"{TEST_AOI_WetBulbTemp_RelativeHumidity[2]}\n{TEST_AOI_WetBulbTemp_RelativeHumidity[5]}");
            TEST_AOI_WetBulbTemp_Temperature = CallGetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_Temperature[6], TEST_AOI_WetBulbTemp_Temperature[7], filePath_MainProgram, myProject);
            Console.WriteLine($"{TEST_AOI_WetBulbTemp_Temperature[2]}\n{TEST_AOI_WetBulbTemp_Temperature[5]}");
            Console.WriteLine($"{TEST_AOI_WetBulbTemp_WetBulbTemp[2]}\n{TEST_AOI_WetBulbTemp_WetBulbTemp[5]}");
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE showing final test tag values");

            // Final Banner
            if (failure_condition > 0)
                Console.WriteLine("---------------------------------------TEST FAILURE---------------------------------------------------------");
            else
                Console.WriteLine("---------------------------------------TEST SUCCESS---------------------------------------------------------");

            //// Finish process of sending console printouts to the text file specified earlier
            //Console.SetOut(oldOut);
            //writer.Close();
            //ostrm.Close();
            //#endregion





            Console.WriteLine("\n\n\nSTARTING TESTING:");
            //CallSetComplexTagValueAsyncAndWaitOnResult($"Controller/Tags/Tag[@Name='TEST_DINT_1']", TagOperationMode.Online, BitConverter.GetBytes(11111), DataType.DINT, myProject);
            //var testDINT = CallGetComplexTagValueAsyncAndWaitOnResult($"Controller/Tags/Tag[@Name='TEST_DINT_1']", TagOperationMode.Online, DataType.DINT, myProject);
            //if (testDINT != null)
            //{
            //    foreach (byte b in testDINT)
            //    {
            //        Console.Write(b + " ");
            //    }
            //}
            //Console.WriteLine("\nhow many bytes: " + testDINT.Length);
            //Console.WriteLine("The 4 bytes:");
            //Console.WriteLine("0: " + testDINT[0]);
            //Console.WriteLine("1: " + testDINT[1]);
            //Console.WriteLine("2: " + testDINT[2]);
            //Console.WriteLine("3: " + testDINT[3]);
            ////string fullDINT = CreateBinaryString(testDINT);
            ////Console.WriteLine("string dint altogether: " + fullDINT);
            //Console.WriteLine("TEST NEGATIVE encoding: " + Convert.ToInt64("1111111000011001", 2));
            //Console.WriteLine("TEST NEGATIVE encoding: " + Convert.ToInt32("1111111000011001", 2));
            //Console.WriteLine("TEST NEGATIVE encoding: " + Convert.ToInt16("1111111000011001", 2));
            //var data = GetBytesFromBinaryString("0101010001100101011100110111010000100000001100010011001000110011");
            //var text = Encoding.ASCII.GetString(data);
            //Console.WriteLine("TEST ENCODING PT II: " + text);
            ////var testResultDINT = BinaryStringToInteger(fullDINT);
            ////Console.WriteLine("long representation of variable: " + testResultDINT);

            var testAOI = CallGetComplexTagValueAsyncAndWaitOnResult($"Controller/Tags/Tag[@Name='UDT_AllAtomicDataTypes']", TagOperationMode.Online, DataType.BYTE_ARRAY, myProject);
            string[] testAOI_Values = GetUDT_AllAtomicDataTypes(testAOI);
            bool ex_BOOL1 = GetBool(testAOI_Values[0]);
            bool ex_BOOL2 = GetBool(testAOI_Values[1]);
            bool ex_BOOL3 = GetBool(testAOI_Values[2]);
            bool ex_BOOL4 = GetBool(testAOI_Values[3]);
            bool ex_BOOL5 = GetBool(testAOI_Values[4]);
            bool ex_BOOL6 = GetBool(testAOI_Values[5]);
            bool ex_BOOL7 = GetBool(testAOI_Values[6]);
            bool ex_BOOL8 = GetBool(testAOI_Values[7]);
            byte ex_SINT = byte.Parse(testAOI_Values[8]);
            int ex_INT = int.Parse(testAOI_Values[9]);
            double ex_DINT = double.Parse(testAOI_Values[10]);
            long ex_LINT = long.Parse(testAOI_Values[11]);
            float ex_REAL = float.Parse(testAOI_Values[12]);
            string ex_STRING = testAOI_Values[13];
            for (int i = 0; i < testAOI_Values.Length; i++)
                Console.WriteLine(testAOI_Values[i]);

            Console.WriteLine("\nENDING TESTING\n\n\n");



        }
        #region METHODS --------------------------------------------------------------------------------------------------------------------------------------------------d

        private static bool GetBool(string input_string)
        {
            if (input_string == "1")
                return true;
            return false;
        }

        // Wrap Text Method
        private static string WrapText(string input_string)
        {
            int myLimit = 100;
            string[] words = input_string.Split(' ');
            StringBuilder newSentence = new StringBuilder();
            string line = "";
            int numberOfNewLines = 0;
            foreach (string word in words)
            {
                if ((line + word).Length > myLimit)
                {
                    newSentence.AppendLine(line);
                    line = "";
                    numberOfNewLines++;
                }
                line += string.Format("{0} ", word);
            }
            if (line.Length > 0)
            {
                if (numberOfNewLines > 0)
                    newSentence.AppendLine("         " + line);
                else
                    newSentence.AppendLine(line);
            }
            return newSentence.ToString();
        }

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

        // Get Complex Data Type Tag Value Wait On Result Method
        private static ByteString CallGetComplexTagValueAsyncAndWaitOnResult(string tagPath, TagOperationMode mode, DataType type, LogixProject project)
        {
            var task = project.GetTagValueAsync(tagPath, mode, type);
            task.Wait();
            return task.Result;
        }

        // Set Complex Data Type Tag value Wait On Result Method
        private static void CallSetComplexTagValueAsyncAndWaitOnResult(string tagPath, TagOperationMode mode, byte[] tagValue, DataType type, LogixProject project)
        {
            var task = project.SetTagValueAsync(tagPath, mode, tagValue, type);
            task.Wait();
        }

        // Summary:
        //     Returns a double-precision floating point number converted from eight bytes at
        //     a specified position in a byte array.
        //
        // Parameters:
        //   value:
        //     An array of bytes that includes the eight bytes to convert.
        //
        //   startIndex:
        //     The starting position within value.
        //
        // Returns:
        //     A double-precision floating point number formed by eight bytes beginning at startIndex.
        //
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     startIndex is greater than or equal to the length of value minus 7, and is less
        //     than or equal to the length of value minus 1.
        //
        //   T:System.ArgumentNullException:
        //     value is null.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     startIndex is less than zero or greater than the length of value minus 1.
        // Get AllAtomicDataTypes UDT values
        private static string[] GetUDT_AllAtomicDataTypes(ByteString byte_string)
        {
            string[] return_string = new string[14];
            byte[] UDT_ByteArray = new byte[byte_string.Length];
            for (int i = 0; i < UDT_ByteArray.Length; i++)
                UDT_ByteArray[i] = (byte)byte_string[i];

            // ex_BOOL1 - ex_BOOL2
            byte[] ex_BOOLs = new byte[1];
            Array.ConstrainedCopy(UDT_ByteArray, 0, ex_BOOLs, 0, 1);
            for (int i = 0; i < 8; i++)
                return_string[i] = Convert.ToString(CreateBinaryString(ex_BOOLs, "backward")[7 - i]);

            // ex_SINT
            byte[] ex_SINT = new byte[1];
            Array.ConstrainedCopy(UDT_ByteArray, 1, ex_SINT, 0, 1);
            return_string[8] = Convert.ToString(byte.Parse(CreateBinaryString(ex_SINT, "forward")));

            // ex_INT
            byte[] ex_INT = new byte[2];
            Array.ConstrainedCopy(UDT_ByteArray, 2, ex_INT, 0, 2);
            return_string[9] = Convert.ToString(BitConverter.ToInt16(ex_INT));

            // ex_DINT
            byte[] ex_DINT = new byte[4];
            Array.ConstrainedCopy(UDT_ByteArray, 4, ex_DINT, 0, 4);
            return_string[10] = Convert.ToString(BitConverter.ToInt32(ex_DINT));

            // ex_LINT
            byte[] ex_LINT = new byte[8];
            Array.ConstrainedCopy(UDT_ByteArray, 8, ex_LINT, 0, 8);
            return_string[11] = Convert.ToString(BitConverter.ToInt64(ex_LINT));

            // ex_REAL
            byte[] ex_REAL = new byte[4];
            Array.ConstrainedCopy(UDT_ByteArray, 16, ex_REAL, 0, 4);
            return_string[12] = ConvertIEEE754ToFloatString(ex_REAL);

            // ex_STRING
            byte[] ex_STRING = new byte[UDT_ByteArray.Length - 24];
            Array.ConstrainedCopy(UDT_ByteArray, 24, ex_STRING, 0, UDT_ByteArray.Length - 24);
            return_string[13] = Encoding.ASCII.GetString(ex_STRING);

            return return_string;
        }

        // Helper method to Convert IEEE 754
        private static string ConvertIEEE754ToFloatString(byte[] inputArray)
        {
            if (inputArray.Length != 4)
            {
                throw new ArgumentException("Byte array must be 4 bytes long.");
            }

            byte[] flippedArray = new byte[4];
            for (int i = 0; i < 4; i++)
                flippedArray[i] = inputArray[3 - i];

            // Interpret the bytes as per IEEE 754 single-precision format
            int sign = (flippedArray[0] & 0x80) >> 7;
            int exponent = ((flippedArray[0] & 0x7F) << 1) | ((flippedArray[1] & 0x80) >> 7);
            uint mantissa = ((uint)(flippedArray[1] & 0x7F) << 16) | ((uint)flippedArray[2] << 8) | flippedArray[3];

            // Calculate the value represented by the bits
            double value = Math.Pow(-1, sign) * (1 + mantissa / Math.Pow(2, 23)) * Math.Pow(2, exponent - 127);

            // Convert the value to a string
            return value.ToString();
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
            return task.Result;
        }

        // Get Tag Value Method
        // return_array[0] = $"{tagValue_online}";
        // return_array[1] = $"{tag_name} online value";
        // return_array[2] = $"{tag_name} online value: {tagValue_online}";
        // return_array[3] = $"{tagValue_offline}";
        // return_array[4] = $"{tag_name} offline value";
        // return_array[5] = $"{tag_name} offline value: {tagValue_offline}";
        // return_array[6] = tag_name;
        // return_array[7] = data_type;
        private static async Task<string[]> GetTagValueAsync(string tag_name, string data_type, string tagPath, LogixProject project)
        {
            string[] return_array = new string[8];
            tagPath = tagPath + $"[@Name='{tag_name}']";
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
                return_array[6] = tag_name;
                return_array[7] = data_type;
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"ERROR getting tag {tag_name}.");
                Console.WriteLine(ex.Message);
            }
            return return_array;
        }

        // Get Tag Value Wait On Result Method
        private static string[] CallGetTagValueAsyncAndWaitOnResult(string tag_name, string data_type, string tagPath, LogixProject project)
        {
            var task = GetTagValueAsync(tag_name, data_type, tagPath, project);
            task.Wait();
            return task.Result;
        }

        // Set Tag Value Method
        private static async Task SetTagValueAsync(string tag_name, int tag_value_in, string online_or_offline, string data_type, string tagPath, LogixProject project)
        {
            tagPath = tagPath + $"[@Name='{tag_name}']";
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
        private static void CallSetTagValueAsyncAndWaitOnResult(string tag_name, int tag_value_in, string online_or_offline, string data_type, string tagPath, LogixProject project)
        {
            var task = SetTagValueAsync(tag_name, tag_value_in, online_or_offline, data_type, tagPath, project);
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
                Console.WriteLine(WrapText($"FAILURE: {input_string[1]} ({input_string[0]}) & {input_string[4]} ({input_string[3]}) NOT equal."));
                return 1;
            }
            else
            {
                Console.Write(WrapText($"SUCCESS: {input_string[1]} ({input_string[0]}) & {input_string[4]} ({input_string[3]}) are EQUAL."));
                return 0;
            }
        }

        // Base Conversion With Specified Radix (2 to 36) Method
        private static string DecimalToArbitrarySystem(long decimalNumber, int radix)
        {
            const int BitsInLong = 64;
            const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            if (radix < 2 || radix > Digits.Length)
                throw new ArgumentException("The radix must be >= 2 and <= " + Digits.Length.ToString());
            if (decimalNumber == 0)
                return "0";

            int index = BitsInLong - 1;
            long currentNumber = Math.Abs(decimalNumber);
            char[] charArray = new char[BitsInLong];

            while (currentNumber != 0)
            {
                int remainder = (int)(currentNumber % radix);
                charArray[index--] = Digits[remainder];
                currentNumber = currentNumber / radix;
            }
            string result = new String(charArray, index + 1, BitsInLong - index - 1);
            if (decimalNumber < 0)
            {
                result = "-" + result;
            }
            return result;
        }

        // Convert Binary String to Output Integer Method
        private static long BinaryStringToInteger(string binary_string)
        {
            long result = 0;
            int power = 0;
            for (int i = binary_string.Length - 1; i >= 0; i--)
            {
                if (binary_string[i] == '1')
                    result += (long)Math.Pow(2, power);
                power++;
            }
            return result;
        }

        // Get Bytes From Binary String Method
        private static Byte[] GetBytesFromBinaryString(String binary)
        {
            var list = new List<Byte>();
            for (int i = 0; i < binary.Length; i += 8)
            {
                String t = binary.Substring(i, 8);
                list.Add(Convert.ToByte(t, 2));
            }
            return list.ToArray();
        }

        // Create Binary String From ByteString Method
        private static string CreateBinaryString(byte[] byteString, string forward_backward)
        {
            string returnstring = "";
            if (forward_backward == "backward")
                for (int i = byteString.Length - 1; i >= 0; --i)
                    returnstring = returnstring + Convert.ToString(byteString[i], 2).PadLeft(8, '0');
            else if (forward_backward == "forward")
                for (int i = 0; i < byteString.Length; ++i)
                    returnstring = returnstring + Convert.ToString(byteString[i], 2).PadLeft(8, '0');
            return returnstring;
        }
        #endregion
    }
}