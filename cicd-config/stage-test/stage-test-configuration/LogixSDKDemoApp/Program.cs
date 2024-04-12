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
            string[] TEST_DINT_1 = CallGetTagValueAsyncAndWaitOnResult("TEST_DINT_1", DataType.DINT, filePath_ControllerScope, myProject, true);
            string[] TEST_TOGGLE_WetBulbTempCalc = CallGetTagValueAsyncAndWaitOnResult("TEST_TOGGLE_WetBulbTempCalc", DataType.BOOL, filePath_MainProgram, myProject, true);
            string[] TEST_AOI_WetBulbTemp_isFahrenheit = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_isFahrenheit", DataType.BOOL, filePath_MainProgram, myProject, true);
            string[] TEST_AOI_WetBulbTemp_RelativeHumidity = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_RelativeHumidity", DataType.REAL, filePath_MainProgram, myProject, true);
            string[] TEST_AOI_WetBulbTemp_Temperature = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_Temperature", DataType.REAL, filePath_MainProgram, myProject, true);
            string[] TEST_AOI_WetBulbTemp_WetBulbTemp = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_WetBulbTemp", DataType.REAL, filePath_MainProgram, myProject, true);
            ByteString[] ByteString_UDT_AllAtomicDataTypes = CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", filePath_ControllerScope, myProject);
            string[][] UDT_AllAtomicDataTypes = FormatUDT_AllAtomicDataTypes(ByteString_UDT_AllAtomicDataTypes, true);
            bool ex_BOOL1 = GetBool(UDT_AllAtomicDataTypes[1][0]);
            bool ex_BOOL2 = GetBool(UDT_AllAtomicDataTypes[1][1]);
            bool ex_BOOL3 = GetBool(UDT_AllAtomicDataTypes[1][2]);
            bool ex_BOOL4 = GetBool(UDT_AllAtomicDataTypes[1][3]);
            bool ex_BOOL5 = GetBool(UDT_AllAtomicDataTypes[1][4]);
            bool ex_BOOL6 = GetBool(UDT_AllAtomicDataTypes[1][5]);
            bool ex_BOOL7 = GetBool(UDT_AllAtomicDataTypes[1][6]);
            bool ex_BOOL8 = GetBool(UDT_AllAtomicDataTypes[1][7]);
            int ex_SINT = int.Parse(UDT_AllAtomicDataTypes[1][8]); // c# bytes have range 0 to 255 so INT accounts for negatives (Studio 5k SINT has range -128 to 127)
            int ex_INT = int.Parse(UDT_AllAtomicDataTypes[1][9]);
            double ex_DINT = double.Parse(UDT_AllAtomicDataTypes[1][10]);
            long ex_LINT = long.Parse(UDT_AllAtomicDataTypes[1][11]);
            float ex_REAL = float.Parse(UDT_AllAtomicDataTypes[1][12]);
            string ex_STRING = UDT_AllAtomicDataTypes[1][13];
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE getting initial project start-up tag values\n---");

            // Verify whether offline and online values are the same
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START verifying whether offline and online values are the same...");
            int failure_condition = 0;
            failure_condition += CompareOnlineOffline(TEST_DINT_1[0], TEST_DINT_1[1], TEST_DINT_1[2]);
            failure_condition += CompareOnlineOffline(TEST_TOGGLE_WetBulbTempCalc[0], TEST_TOGGLE_WetBulbTempCalc[1], TEST_TOGGLE_WetBulbTempCalc[2]);
            failure_condition += CompareOnlineOffline(TEST_AOI_WetBulbTemp_isFahrenheit[0], TEST_AOI_WetBulbTemp_isFahrenheit[1], TEST_AOI_WetBulbTemp_isFahrenheit[2]);
            failure_condition += CompareOnlineOffline(TEST_AOI_WetBulbTemp_RelativeHumidity[0], TEST_AOI_WetBulbTemp_RelativeHumidity[1], TEST_AOI_WetBulbTemp_RelativeHumidity[2]);
            failure_condition += CompareOnlineOffline(TEST_AOI_WetBulbTemp_Temperature[0], TEST_AOI_WetBulbTemp_Temperature[1], TEST_AOI_WetBulbTemp_Temperature[2]);
            failure_condition += CompareOnlineOffline(TEST_AOI_WetBulbTemp_WetBulbTemp[0], TEST_AOI_WetBulbTemp_WetBulbTemp[1], TEST_AOI_WetBulbTemp_WetBulbTemp[2]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL1", UDT_AllAtomicDataTypes[1][0], UDT_AllAtomicDataTypes[2][0]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL2", UDT_AllAtomicDataTypes[1][1], UDT_AllAtomicDataTypes[2][1]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL3", UDT_AllAtomicDataTypes[1][2], UDT_AllAtomicDataTypes[2][2]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL4", UDT_AllAtomicDataTypes[1][3], UDT_AllAtomicDataTypes[2][3]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL5", UDT_AllAtomicDataTypes[1][4], UDT_AllAtomicDataTypes[2][4]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL6", UDT_AllAtomicDataTypes[1][5], UDT_AllAtomicDataTypes[2][5]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL7", UDT_AllAtomicDataTypes[1][6], UDT_AllAtomicDataTypes[2][6]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL8", UDT_AllAtomicDataTypes[1][7], UDT_AllAtomicDataTypes[2][7]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_SINT", UDT_AllAtomicDataTypes[1][8], UDT_AllAtomicDataTypes[2][8]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_INT", UDT_AllAtomicDataTypes[1][9], UDT_AllAtomicDataTypes[2][9]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_DINT", UDT_AllAtomicDataTypes[1][10], UDT_AllAtomicDataTypes[2][10]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_LINT", UDT_AllAtomicDataTypes[1][11], UDT_AllAtomicDataTypes[2][11]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_REAL", UDT_AllAtomicDataTypes[1][12], UDT_AllAtomicDataTypes[2][12]);
            failure_condition += CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_STRING", UDT_AllAtomicDataTypes[1][13], UDT_AllAtomicDataTypes[2][13]);
            Console.Write($"[{DateTime.Now.ToString("T")}] DONE verifying whether offline and online values are the same\n---\n");

            // Set tag values
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START setting tag values...");
            CallSetTagValueAsyncAndWaitOnResult(TEST_DINT_1[0], 111, "online", DataType.DINT, filePath_ControllerScope, myProject);
            CallSetTagValueAsyncAndWaitOnResult(TEST_TOGGLE_WetBulbTempCalc[0], 1, "online", DataType.BOOL, filePath_MainProgram, myProject);
            CallSetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_isFahrenheit[0], 1, "online", DataType.BOOL, filePath_MainProgram, myProject);
            CallSetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_RelativeHumidity[0], 30, "online", DataType.REAL, filePath_MainProgram, myProject);
            CallSetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_Temperature[0], 70, "online", DataType.REAL, filePath_MainProgram, myProject);

            ByteString[] newValues_UDT_AllAtomicDataTypes = CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", filePath_ControllerScope, myProject);
            newValues_UDT_AllAtomicDataTypes[0] = ModifyByteString_UDT_AllAtomicDataTypesValues("10010010", DataType.BOOL, newValues_UDT_AllAtomicDataTypes);
            newValues_UDT_AllAtomicDataTypes[0] = ModifyByteString_UDT_AllAtomicDataTypesValues("-24", DataType.SINT, newValues_UDT_AllAtomicDataTypes);
            newValues_UDT_AllAtomicDataTypes[0] = ModifyByteString_UDT_AllAtomicDataTypesValues("20500", DataType.INT, newValues_UDT_AllAtomicDataTypes);
            newValues_UDT_AllAtomicDataTypes[0] = ModifyByteString_UDT_AllAtomicDataTypesValues("-2000111000", DataType.DINT, newValues_UDT_AllAtomicDataTypes);
            newValues_UDT_AllAtomicDataTypes[0] = ModifyByteString_UDT_AllAtomicDataTypesValues("-9000111000111000111", DataType.LINT, newValues_UDT_AllAtomicDataTypes);
            newValues_UDT_AllAtomicDataTypes[0] = ModifyByteString_UDT_AllAtomicDataTypesValues("10555.888", DataType.REAL, newValues_UDT_AllAtomicDataTypes);
            newValues_UDT_AllAtomicDataTypes[0] = ModifyByteString_UDT_AllAtomicDataTypesValues("New String!", DataType.STRING, newValues_UDT_AllAtomicDataTypes);
            SetUDT_AllAtomicDataTypes("UDT_AllAtomicDataTypes", newValues_UDT_AllAtomicDataTypes, TagOperationMode.Online, filePath_ControllerScope, myProject);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE setting tag values\n---");

            // Verify expected output
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START verifying expected tag outputs...");
            TEST_AOI_WetBulbTemp_WetBulbTemp = CallGetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_WetBulbTemp[0], DataType.REAL, filePath_MainProgram, myProject, false);
            failure_condition += CompareForExpectedValue("TEST_AOI_WetBulbTemp_WetBulbTemp", "52.997536", TEST_AOI_WetBulbTemp_WetBulbTemp[1]);
            ByteString_UDT_AllAtomicDataTypes = CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", filePath_ControllerScope, myProject);
            UDT_AllAtomicDataTypes = FormatUDT_AllAtomicDataTypes(ByteString_UDT_AllAtomicDataTypes, false);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_INT", "20500", UDT_AllAtomicDataTypes[1][9]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_STRING", "New String!", UDT_AllAtomicDataTypes[1][13]);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE verifying expected tag outputs\n---");

            // Show final tag values
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START showing final test tag values...");
            CallGetTagValueAsyncAndWaitOnResult(TEST_DINT_1[0], DataType.DINT, filePath_ControllerScope, myProject, true);
            CallGetTagValueAsyncAndWaitOnResult(TEST_TOGGLE_WetBulbTempCalc[0], DataType.BOOL, filePath_MainProgram, myProject, true);
            CallGetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_isFahrenheit[0], DataType.BOOL, filePath_MainProgram, myProject, true);
            CallGetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_RelativeHumidity[0], DataType.REAL, filePath_MainProgram, myProject, true);
            CallGetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_Temperature[0], DataType.REAL, filePath_MainProgram, myProject, true);
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
        private static string[][] FormatUDT_AllAtomicDataTypes(ByteString[] byte_string, bool printout)
        {

            string[][] return_string = new string[3][];
            return_string[0] = new string[] { "UDT_AllAtomicDataTypes.ex_BOOL1", "UDT_AllAtomicDataTypes.ex_BOOL2", "UDT_AllAtomicDataTypes.ex_BOOL3",
                "UDT_AllAtomicDataTypes.ex_BOOL4", "UDT_AllAtomicDataTypes.ex_BOOL5", "UDT_AllAtomicDataTypes.ex_BOOL6", "UDT_AllAtomicDataTypes.ex_BOOL7",
                "UDT_AllAtomicDataTypes.ex_BOOL8", "UDT_AllAtomicDataTypes.ex_SINT", "UDT_AllAtomicDataTypes.ex_INT", "UDT_AllAtomicDataTypes.ex_DINT",
                "UDT_AllAtomicDataTypes.ex_LINT", "UDT_AllAtomicDataTypes.ex_REAL", "UDT_AllAtomicDataTypes.ex_STRING" };
            return_string[1] = new string[15];
            return_string[2] = new string[15];

            for (int i = 0; i < 2; i++)
            {
                var UDT_ByteArray = new byte[byte_string[0].Length];
                for (int j = 0; j < UDT_ByteArray.Length; j++)
                    UDT_ByteArray[j] = (byte)byte_string[i][j];

                var ex_BOOLs = new byte[1];
                Array.ConstrainedCopy(UDT_ByteArray, 0, ex_BOOLs, 0, 1);
                for (int j = 0; j < 8; j++)
                    return_string[i + 1][j] = Convert.ToString(CreateBinaryString(ex_BOOLs, "backward")[7 - j]);

                var ex_SINT = new byte[1];
                Array.ConstrainedCopy(UDT_ByteArray, 1, ex_SINT, 0, 1);
                var sint_string_online = CreateBinaryString(ex_SINT, "forward");
                var sign_online = sint_string_online[0] == '1' ? -1 : 1;
                if (sign_online == 1)
                    return_string[i + 1][8] = Convert.ToString(Convert.ToInt16(("00000000" + sint_string_online), 2));
                else if (sign_online == -1)
                    return_string[i + 1][8] = Convert.ToString(Convert.ToInt16(("11111111" + sint_string_online), 2));

                var ex_INT = new byte[2];
                Array.ConstrainedCopy(UDT_ByteArray, 2, ex_INT, 0, 2);
                return_string[i + 1][9] = Convert.ToString(BitConverter.ToInt16(ex_INT));

                var ex_DINT = new byte[4];
                Array.ConstrainedCopy(UDT_ByteArray, 4, ex_DINT, 0, 4);
                return_string[i + 1][10] = Convert.ToString(BitConverter.ToInt32(ex_DINT));

                var ex_LINT = new byte[8];
                Array.ConstrainedCopy(UDT_ByteArray, 8, ex_LINT, 0, 8);
                return_string[i + 1][11] = Convert.ToString(BitConverter.ToInt64(ex_LINT));

                var ex_REAL = new byte[4];
                Array.ConstrainedCopy(UDT_ByteArray, 16, ex_REAL, 0, 4);
                return_string[i + 1][12] = ConvertIEEE754ToFloatString(ex_REAL);

                var ex_STRING = new byte[UDT_ByteArray.Length - 24];
                Array.ConstrainedCopy(UDT_ByteArray, 24, ex_STRING, 0, UDT_ByteArray.Length - 24);
                return_string[i + 1][13] = Encoding.ASCII.GetString(ex_STRING).Replace("\0", "");

                return_string[i + 1][14] = Convert.ToString(byte_string[i].Length);
            }
            if (printout)
            {
                for (int i = 0; i < return_string[0].Length; i++)
                {
                    var online_message = $"online value: {return_string[1][i]}";
                    var offline_message = $"offline value: {return_string[2][i]}";
                    Console.WriteLine($"SUCCESS: " + return_string[0][i].PadRight(40, ' ') + online_message.PadRight(35, ' ') + offline_message.PadRight(35, ' '));
                }
            }
            return return_string;
        }

        // Get ByteString From UDT_AllAtomicDataTypes Tag Method
        private static async Task<ByteString[]> GetUDT_AllAtomicDataTypes(string tag_name, string tagPath, LogixProject project)
        {
            tagPath = tagPath + $"[@Name='{tag_name}']";
            ByteString[] return_byteString = new ByteString[2];
            return_byteString[0] = await project.GetTagValueAsync(tagPath, TagOperationMode.Online, DataType.BYTE_ARRAY);
            return_byteString[1] = await project.GetTagValueAsync(tagPath, TagOperationMode.Offline, DataType.BYTE_ARRAY);
            return return_byteString;
        }

        // Get ByteString From UDT_AllAtomicDataTypes Tag Method
        private static ByteString[] CallGetUDT_AllAtomicDataTypesAndWaitOnResult(string tag_name, string tagPath, LogixProject project)
        {
            var task = GetUDT_AllAtomicDataTypes(tag_name, tagPath, project);
            task.Wait();
            return task.Result;
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
        // return_array[0] = tag name
        // return_array[1] = online tag value
        // return_array[2] = offline tag value
        private static async Task<string[]> GetTagValueAsync(string tag_name, DataType type, string tagPath, LogixProject project, bool printout)
        {
            string[] return_array = new string[3];
            tagPath = tagPath + $"[@Name='{tag_name}']";
            return_array[0] = tag_name;
            try
            {
                if (type == DataType.DINT)
                {
                    int tagValue_online = await project.GetTagValueDINTAsync(tagPath, TagOperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    int tagValue_offline = await project.GetTagValueDINTAsync(tagPath, TagOperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";
                }
                else if (type == DataType.BOOL)
                {
                    bool tagValue_online = await project.GetTagValueBOOLAsync(tagPath, TagOperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    bool tagValue_offline = await project.GetTagValueBOOLAsync(tagPath, TagOperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";

                }
                else if (type == DataType.REAL)
                {
                    float tagValue_online = await project.GetTagValueREALAsync(tagPath, TagOperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    float tagValue_offline = await project.GetTagValueREALAsync(tagPath, TagOperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";
                }
                else
                    Console.WriteLine(WrapText($"ERROR executing command: The tag {tag_name} cannot be handled. Select either DINT, BOOL, or REAL."));
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"ERROR getting tag {tag_name}");
                Console.WriteLine(ex.Message);
            }
            if (printout)
            {
                string online_message = $"online value: {return_array[1]}";
                string offline_message = $"offline value: {return_array[2]}";
                Console.WriteLine($"SUCCESS: " + tag_name.PadRight(40, ' ') + online_message.PadRight(35, ' ') + offline_message.PadRight(35, ' '));
            }
            return return_array;
        }

        // Get Tag Value Wait On Result Method
        private static string[] CallGetTagValueAsyncAndWaitOnResult(string tag_name, DataType type, string tagPath, LogixProject project, bool printout)
        {
            var task = GetTagValueAsync(tag_name, type, tagPath, project, printout);
            task.Wait();
            return task.Result;
        }

        // Set Tag Value Method
        private static async Task SetTagValueAsync(string tag_name, int tag_value_in, string online_or_offline, DataType type, string tagPath, LogixProject project)
        {
            tagPath = tagPath + $"[@Name='{tag_name}']";
            try
            {
                if (online_or_offline == "online")
                {
                    if (type == DataType.DINT)
                    {
                        await project.SetTagValueDINTAsync(tagPath, TagOperationMode.Online, tag_value_in);
                        Console.WriteLine($"SUCCESS: {tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else if (type == DataType.BOOL)
                    {
                        await project.SetTagValueBOOLAsync(tagPath, TagOperationMode.Online, Convert.ToBoolean(tag_value_in));
                        Console.WriteLine($"SUCCESS: {tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else if (type == DataType.REAL)
                    {
                        await project.SetTagValueREALAsync(tagPath, TagOperationMode.Online, tag_value_in);
                        Console.WriteLine($"SUCCESS: {tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else
                    {
                        Console.WriteLine($"ERROR executing command: The data type cannot be handled. Select either DINT, BOOL, or REAL.");
                    }
                }
                else if (online_or_offline == "offline")
                {
                    if (type == DataType.DINT)
                    {
                        await project.SetTagValueDINTAsync(tagPath, TagOperationMode.Offline, tag_value_in);
                        Console.WriteLine($"SUCCESS: {tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else if (type == DataType.BOOL)
                    {
                        await project.SetTagValueBOOLAsync(tagPath, TagOperationMode.Offline, Convert.ToBoolean(tag_value_in));
                        Console.WriteLine($"SUCCESS: {tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else if (type == DataType.REAL)
                    {
                        await project.SetTagValueREALAsync(tagPath, TagOperationMode.Offline, tag_value_in);
                        Console.WriteLine($"SUCCESS: {tag_name} {online_or_offline} new value: {tag_value_in}");
                    }
                    else
                    {
                        Console.WriteLine($"ERROR executing command: The data type cannot be handled. Select either DINT, BOOL, or REAL.");
                    }
                }
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine("Unable to set tag value.");
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
        private static void CallSetTagValueAsyncAndWaitOnResult(string tag_name, int tag_value_in, string online_or_offline, DataType type, string tagPath, LogixProject project)
        {
            var task = SetTagValueAsync(tag_name, tag_value_in, online_or_offline, type, tagPath, project);
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
                Console.Write(WrapText($"SUCCESS: {tag_name} online value ({online_value}) & offline value ({offline_value}) are EQUAL."));
                return 0;
            }
        }

        // Compare Two Tags Method
        private static int CompareForExpectedValue(string tag_name, string expected_value, string actual_value)
        {
            if (expected_value != actual_value)
            {
                Console.WriteLine(WrapText($"FAILURE: {tag_name} expected value ({expected_value}) & actual value ({actual_value}) NOT equal."));
                return 1;
            }
            else
            {
                Console.Write(WrapText($"SUCCESS: {tag_name} expected value ({expected_value}) & actual value ({actual_value}) EQUAL."));
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

        // Set Complex Data Type Tag Value Method
        private static void SetUDT_AllAtomicDataTypes(string tag_name, ByteString[] input_byte, TagOperationMode mode, string tagPath, LogixProject project)
        {
            tagPath = tagPath + $"[@Name='{tag_name}']";
            ByteString[] byteString_in = CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", tagPath, project);
            string[][] UDT_AllAtomicDataTypes = FormatUDT_AllAtomicDataTypes(byteString_in, false);
            string[][] new_UDT_AllAtomicDataTypes = FormatUDT_AllAtomicDataTypes(input_byte, false);
            project.SetTagValueAsync(tagPath, mode, input_byte[0].ToByteArray(), DataType.BYTE_ARRAY);

            if (mode == TagOperationMode.Online)
            {
                Console.WriteLine($"SUCCESS: {tag_name} online values: ");
                for (int i = 0; i < UDT_AllAtomicDataTypes[1].Length - 1; i++)
                    Console.WriteLine("         " + UDT_AllAtomicDataTypes[1][i] + "  -->  " + new_UDT_AllAtomicDataTypes[1][i]);
            }
            else if (mode == TagOperationMode.Offline)
            {
                Console.WriteLine($"SUCCESS: {tag_name} offline values: ");
                for (int i = 0; i < UDT_AllAtomicDataTypes[2].Length - 1; i++)
                    Console.WriteLine("         " + UDT_AllAtomicDataTypes[2][i] + "  -->  " + new_UDT_AllAtomicDataTypes[2][i]);
            }
        }

        // Set UDT Values
        private static ByteString ModifyByteString_UDT_AllAtomicDataTypesValues(string value_in, DataType type, ByteString[] byteString_UDT_AllAtomicDataTypes)
        {
            byte[] byteArray = byteString_UDT_AllAtomicDataTypes[0].ToByteArray();

            if (type == DataType.BOOL)
                byteArray[0] = Convert.ToByte(value_in, 2);

            else if (type == DataType.SINT)
            {
                string sint_string = Convert.ToString(long.Parse(value_in), 2);
                sint_string = sint_string.Substring(sint_string.Length - 8);
                byteArray[1] = Convert.ToByte(sint_string, 2);
            }

            else if (type == DataType.INT)
            {
                byte[] int_byteArray = BitConverter.GetBytes(long.Parse(value_in));
                for (int i = 0; i < int_byteArray.Length; ++i)
                    byteArray[i + 2] = int_byteArray[i];
            }

            else if (type == DataType.DINT)
            {
                byte[] dint_byteArray = BitConverter.GetBytes(long.Parse(value_in));
                for (int i = 0; i < dint_byteArray.Length; ++i)
                    byteArray[i + 4] = dint_byteArray[i];
            }

            else if (type == DataType.LINT)
            {
                byte[] lint_byteArray = BitConverter.GetBytes(long.Parse(value_in));
                for (int i = 0; i < lint_byteArray.Length; ++i)
                    byteArray[i + 8] = lint_byteArray[i];
            }

            else if (type == DataType.REAL)
            {
                byte[] real_byteArray = BitConverter.GetBytes(float.Parse(value_in));
                for (int i = 0; i < real_byteArray.Length; ++i)
                    byteArray[i + 16] = real_byteArray[i];
            }

            else if (type == DataType.STRING)
            {
                byte[] real_byteArray = new byte[value_in.Length];
                for (int i = 0; i < real_byteArray.Length; ++i)
                    byteArray[i + 24] = (byte)value_in[i];
            }

            ByteString returnBytes = ByteString.CopyFrom(byteArray);
            return returnBytes;
        }
        #endregion
    }
}