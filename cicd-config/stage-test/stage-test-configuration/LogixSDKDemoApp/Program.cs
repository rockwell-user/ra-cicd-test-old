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
using DataType = RockwellAutomation.LogixDesigner.LogixProject.DataType;
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


            //string sint_string = Convert.ToString(value_in, 2);
            //byteArray[1] = Convert.ToByte(sint_string.Substring(sint_string.Length - 8), 2);
            //private static ByteString ModifyByteString_UDT_AllAtomicDataTypesValues(long value_in, DataType type, string[] string_UDT_AllAtomicDataTypes)
            //{

            //    if (type == DataType.BOOL)
            //        byteArray[0] = Convert.ToByte($"{value_in}", 2);
            //    else if (type == DataType.SINT)
            //    {
            //        string sint_string = Convert.ToString(value_in, 2);
            //        byteArray[1] = Convert.ToByte(sint_string.Substring(Math.Max(0, sint_string.Length - 8)), 2);
            byte[] byteArray = new byte[1];
            long value = -24;
            string sint_string = Convert.ToString(value, 2);
            sint_string = sint_string.Substring(sint_string.Length - 8);
            //Console.WriteLine(sint_string);
            //Console.WriteLine(sint_string.Length);
            //foreach (char c in sint_string)
            //    Console.WriteLine((int)c + ": " + c);
            //Console.WriteLine("NEXT");
            byteArray[0] = Convert.ToByte(sint_string, 2);
            string testString = CreateBinaryString(byteArray, "backward");
            string flippedString = "";
            for (int i = testString.Length - 1; i >= 0; i--)
                flippedString += testString[i];
            Console.WriteLine(testString);
            foreach (char c in testString)
                Console.WriteLine((int)c + ": " + c);
            int sign = testString[0] == '1' ? -1 : 1;
            Console.WriteLine(sign);
            int integerValue;
            if (sign == 1)
            {
                integerValue = Convert.ToInt16(("00000000" + testString), 2);
                Console.WriteLine("RESULT1: " + integerValue);
            }

            else if (sign == -1)
            {
                integerValue = Convert.ToInt16(("11111111" + testString), 2);
                Console.WriteLine("RESULT2: " + integerValue);
            }
            //int integerValue = Convert.ToInt16(testString.Substring(1), 2);
            //Console.WriteLine(testString.Substring(1));

            //Console.WriteLine(testString);
            //Console.WriteLine(BinaryStringToInteger(CreateBinaryString(byteArray, "forward")));
            //BinaryStringToInteger
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
            Console.WriteLine(".NET Framework version:           " + Environment.Version);

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
            string[] UDT_AllAtomicDataTypes_Online = FormatUDT_AllAtomicDataTypes(CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", TagOperationMode.Online, DataType.BYTE_ARRAY, filePath_ControllerScope, myProject));
            string[] UDT_AllAtomicDataTypes_Offline = FormatUDT_AllAtomicDataTypes(CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", TagOperationMode.Offline, DataType.BYTE_ARRAY, filePath_ControllerScope, myProject));
            bool ex_BOOL1 = GetBool(UDT_AllAtomicDataTypes_Online[0]);
            bool ex_BOOL2 = GetBool(UDT_AllAtomicDataTypes_Online[1]);
            bool ex_BOOL3 = GetBool(UDT_AllAtomicDataTypes_Online[2]);
            bool ex_BOOL4 = GetBool(UDT_AllAtomicDataTypes_Online[3]);
            bool ex_BOOL5 = GetBool(UDT_AllAtomicDataTypes_Online[4]);
            bool ex_BOOL6 = GetBool(UDT_AllAtomicDataTypes_Online[5]);
            bool ex_BOOL7 = GetBool(UDT_AllAtomicDataTypes_Online[6]);
            bool ex_BOOL8 = GetBool(UDT_AllAtomicDataTypes_Online[7]);
            int ex_SINT = int.Parse(UDT_AllAtomicDataTypes_Online[8]); // c# bytes have range 0 to 255 so INT accounts for negatives (Studio 5k SINT has range -128 to 127)
            int ex_INT = int.Parse(UDT_AllAtomicDataTypes_Online[9]);
            double ex_DINT = double.Parse(UDT_AllAtomicDataTypes_Online[10]);
            long ex_LINT = long.Parse(UDT_AllAtomicDataTypes_Online[11]);
            float ex_REAL = float.Parse(UDT_AllAtomicDataTypes_Online[12]);
            string ex_STRING = UDT_AllAtomicDataTypes_Online[13];
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE getting initial project start-up tag values\n---");

            // Verify whether offline and online values are the same
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START verifying whether offline and online values are the same...");
            int failure_condition = 0;
            failure_condition += CompareOnlineOffline(TEST_DINT_1[6], TEST_DINT_1[0], TEST_DINT_1[3]);
            failure_condition += CompareOnlineOffline(TEST_TOGGLE_WetBulbTempCalc[6], TEST_TOGGLE_WetBulbTempCalc[0], TEST_TOGGLE_WetBulbTempCalc[3]);
            failure_condition += CompareOnlineOffline(TEST_AOI_WetBulbTemp_isFahrenheit[6], TEST_AOI_WetBulbTemp_isFahrenheit[0], TEST_AOI_WetBulbTemp_isFahrenheit[3]);
            failure_condition += CompareOnlineOffline(TEST_AOI_WetBulbTemp_RelativeHumidity[6], TEST_AOI_WetBulbTemp_RelativeHumidity[0], TEST_AOI_WetBulbTemp_RelativeHumidity[3]);
            failure_condition += CompareOnlineOffline(TEST_AOI_WetBulbTemp_Temperature[6], TEST_AOI_WetBulbTemp_Temperature[0], TEST_AOI_WetBulbTemp_Temperature[3]);
            failure_condition += CompareOnlineOffline(TEST_AOI_WetBulbTemp_WetBulbTemp[6], TEST_AOI_WetBulbTemp_WetBulbTemp[0], TEST_AOI_WetBulbTemp_WetBulbTemp[3]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL1", UDT_AllAtomicDataTypes_Online[0], UDT_AllAtomicDataTypes_Offline[0]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL2", UDT_AllAtomicDataTypes_Online[1], UDT_AllAtomicDataTypes_Offline[1]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL3", UDT_AllAtomicDataTypes_Online[2], UDT_AllAtomicDataTypes_Offline[2]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL4", UDT_AllAtomicDataTypes_Online[3], UDT_AllAtomicDataTypes_Offline[3]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL5", UDT_AllAtomicDataTypes_Online[4], UDT_AllAtomicDataTypes_Offline[4]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL6", UDT_AllAtomicDataTypes_Online[5], UDT_AllAtomicDataTypes_Offline[5]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL7", UDT_AllAtomicDataTypes_Online[6], UDT_AllAtomicDataTypes_Offline[6]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL8", UDT_AllAtomicDataTypes_Online[7], UDT_AllAtomicDataTypes_Offline[7]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_SINT", UDT_AllAtomicDataTypes_Online[8], UDT_AllAtomicDataTypes_Offline[8]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_INT", UDT_AllAtomicDataTypes_Online[9], UDT_AllAtomicDataTypes_Offline[9]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_DINT", UDT_AllAtomicDataTypes_Online[10], UDT_AllAtomicDataTypes_Offline[10]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_LINT", UDT_AllAtomicDataTypes_Online[11], UDT_AllAtomicDataTypes_Offline[11]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_REAL", UDT_AllAtomicDataTypes_Online[12], UDT_AllAtomicDataTypes_Offline[12]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_STRING", UDT_AllAtomicDataTypes_Online[13], UDT_AllAtomicDataTypes_Offline[13]);
            Console.Write($"[{DateTime.Now.ToString("T")}] DONE verifying whether offline and online values are the same\n---\n");

            // Set tag values
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START setting tag values...");
            CallSetTagValueAsyncAndWaitOnResult(TEST_DINT_1[6], 111, "online", TEST_DINT_1[7], filePath_ControllerScope, myProject);
            CallSetTagValueAsyncAndWaitOnResult(TEST_TOGGLE_WetBulbTempCalc[6], 1, "online", TEST_TOGGLE_WetBulbTempCalc[7], filePath_MainProgram, myProject);
            CallSetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_isFahrenheit[6], 1, "online", TEST_AOI_WetBulbTemp_isFahrenheit[7], filePath_MainProgram, myProject);
            CallSetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_RelativeHumidity[6], 30, "online", TEST_AOI_WetBulbTemp_RelativeHumidity[7], filePath_MainProgram, myProject);
            CallSetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_Temperature[6], 70, "online", TEST_AOI_WetBulbTemp_Temperature[7], filePath_MainProgram, myProject);
            //byte[] byteArray_UDT_AllAtomicDataTypes = new byte[int.Parse(UDT_AllAtomicDataTypes_Online[14])];
            //byteArray_UDT_AllAtomicDataTypes[0] = Convert.ToByte("10010010", 2);   // new boolean values
            //byteArray_UDT_AllAtomicDataTypes[1] = Convert.ToByte(Convert.ToInt16("-24"));
            //ByteString byteString_UDT_AllAtomicDataTypes = ByteString.CopyFrom(byteArray_UDT_AllAtomicDataTypes);
            ByteString newValues_UDT_AllAtomicDataTypes = ModifyByteString_UDT_AllAtomicDataTypesValues(10010010, DataType.BOOL, UDT_AllAtomicDataTypes_Online);
            newValues_UDT_AllAtomicDataTypes = ModifyByteString_UDT_AllAtomicDataTypesValues(-24, DataType.SINT, UDT_AllAtomicDataTypes_Online);
            SetUDT_AllAtomicDataTypes("UDT_AllAtomicDataTypes", newValues_UDT_AllAtomicDataTypes, TagOperationMode.Online, filePath_ControllerScope, myProject);
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


        }
        #region METHODS --------------------------------------------------------------------------------------------------------------------------------------------------

        private static bool GetBool(string input_string)
        {
            if (input_string == "1")
                return true;
            return false;
        }

        // Wrap Text Method
        // https://stackoverflow.com/questions/10541124/wrap-text-to-the-next-line-when-it-exceeds-a-certain-length
        private static string WrapText(string input_string)
        {
            int myLimit = 100;
            string[] words = input_string.Split(' ');
            StringBuilder newSentence = new StringBuilder();
            string line = "";
            int numberOfNewLines = 0;
            foreach (string word in words)
            {
                word.Trim();
                if ((line + word).Length > myLimit)
                {
                    newSentence.AppendLine(line);
                    line = "";
                    numberOfNewLines++;
                }
                line += string.Format($"{word} ");
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




        // Get Complex Data Type Tag Value Method
        private static string[] FormatUDT_AllAtomicDataTypes(ByteString byte_string)
        {

            string[] return_string = new string[15];
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
            string sint_string = CreateBinaryString(ex_SINT, "forward");
            int sign = sint_string[0] == '1' ? -1 : 1;
            if (sign == 1)
                return_string[8] = Convert.ToString(Convert.ToInt16(("00000000" + sint_string), 2));

            else if (sign == -1)
                return_string[8] = Convert.ToString(Convert.ToInt16(("11111111" + sint_string), 2));

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
            return_string[13] = Encoding.ASCII.GetString(ex_STRING).Replace("\0", "");

            return_string[14] = Convert.ToString(byte_string.Length);
            return return_string;
        }

        // Get ByteString From UDT_AllAtomicDataTypes Tag Method
        private static async Task<ByteString> GetUDT_AllAtomicDataTypes(string tag_name, TagOperationMode mode, DataType type, string tagPath, LogixProject project)
        {
            tagPath = tagPath + $"[@Name='{tag_name}']";
            ByteString byte_string = await project.GetTagValueAsync(tagPath, mode, type);
            return byte_string;
        }

        // Get ByteString From UDT_AllAtomicDataTypes Tag Method
        private static ByteString CallGetUDT_AllAtomicDataTypesAndWaitOnResult(string tag_name, TagOperationMode mode, DataType type, string tagPath, LogixProject project)
        {
            var task = GetUDT_AllAtomicDataTypes(tag_name, mode, type, tagPath, project);
            task.Wait();
            return task.Result;
        }

        // Set Complex Data Type Tag Value Method
        private static void SetUDT_AllAtomicDataTypes(string tag_name, ByteString input_byte, TagOperationMode mode, string tagPath, LogixProject project)
        {
            tagPath = tagPath + $"[@Name='{tag_name}']";
            string[] UDT_AllAtomicDataTypes_Online = FormatUDT_AllAtomicDataTypes(CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", TagOperationMode.Online, DataType.BYTE_ARRAY, tagPath, project));
            string[] UDT_AllAtomicDataTypes_Offline = FormatUDT_AllAtomicDataTypes(CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", TagOperationMode.Offline, DataType.BYTE_ARRAY, tagPath, project));
            string[] new_UDT_AllAtomicDataTypes = FormatUDT_AllAtomicDataTypes(input_byte);
            project.SetTagValueAsync(tagPath, mode, input_byte.ToByteArray(), LogixProject.DataType.BYTE_ARRAY);


            if (mode == TagOperationMode.Online)
            {
                Console.WriteLine($"SUCCESS: {tag_name} online values: ");
                for (int i = 0; i < UDT_AllAtomicDataTypes_Online.Length; i++)
                    Console.WriteLine("         " + UDT_AllAtomicDataTypes_Online[i] + "  -->  " + new_UDT_AllAtomicDataTypes[i]);
            }
            else if (mode == TagOperationMode.Offline)
            {
                Console.WriteLine($"SUCCESS: {tag_name} offline values: ");
                for (int i = 0; i < UDT_AllAtomicDataTypes_Offline.Length; i++)
                    Console.WriteLine("         " + UDT_AllAtomicDataTypes_Offline[i] + "  -->  " + new_UDT_AllAtomicDataTypes[i]);
            }
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
                        Console.WriteLine($"SUCCESS: {tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else if (data_type == "BOOL")
                    {
                        await project.SetTagValueBOOLAsync(tagPath, LogixProject.TagOperationMode.Online, Convert.ToBoolean(tag_value_in));
                        Console.WriteLine($"SUCCESS: {tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else if (data_type == "REAL")
                    {
                        await project.SetTagValueREALAsync(tagPath, LogixProject.TagOperationMode.Online, tag_value_in);
                        Console.WriteLine($"SUCCESS: {tag_name} {online_or_offline} new value: {tag_value_in}");
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
                        Console.WriteLine($"SUCCESS: {tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else if (data_type == "BOOL")
                    {
                        await project.SetTagValueBOOLAsync(tagPath, LogixProject.TagOperationMode.Offline, Convert.ToBoolean(tag_value_in));
                        Console.WriteLine($"SUCCESS: {tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else if (data_type == "REAL")
                    {
                        await project.SetTagValueREALAsync(tagPath, LogixProject.TagOperationMode.Offline, tag_value_in);
                        Console.WriteLine($"SUCCESS: {tag_name} {online_or_offline} new value: {tag_value_in}");
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
        private static int CompareOnlineOffline(string tag_name, string online_value, string offline_value)
        {
            if (online_value != offline_value)
            {
                Console.WriteLine(WrapText($"FAILURE: {tag_name} online value ({online_value}) & offline value ({offline_value}) NOT equal."));
                return 1;
            }
            else
            {
                String newString = "SUCCESS: " + tag_name + "online value (" + online_value + ") & offline value (" + offline_value + ") are EQUAL.";
                Console.Write(WrapText(newString));
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


        // Set UDT Values
        private static ByteString ModifyByteString_UDT_AllAtomicDataTypesValues(long value_in, DataType type, string[] string_UDT_AllAtomicDataTypes)
        {
            byte[] byteArray = new byte[int.Parse(string_UDT_AllAtomicDataTypes[14])];
            if (type == DataType.BOOL)
                byteArray[0] = Convert.ToByte($"{value_in}", 2);
            else if (type == DataType.SINT)
            {
                string sint_string = Convert.ToString(value_in, 2);
                sint_string = sint_string.Substring(sint_string.Length - 8);
                byteArray[1] = Convert.ToByte(sint_string, 2);
                foreach (char c in sint_string)
                    Console.WriteLine((int)c + ": " + c);
            }

            ByteString returnBytes = ByteString.CopyFrom(byteArray);
            return returnBytes;
        }

        //byte[] byteArray = new byte[20];
        //long value = -24;
        //string sint_string = Convert.ToString(value, 2);
        //Console.WriteLine(sint_string.Substring(sint_string.Length - 8), 2);
        //byteArray[1] = Convert.ToByte(sint_string.Substring(sint_string.Length - 8), 2);



        #endregion
    }
}