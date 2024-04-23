// ---------------------------------------------------------------------------------------------------------------------------------------------------------------
//
// FileName: Program.cs
// FileType: Visual C# Source file
// Author : Rockwell Automation
// Created : 2024
// Description : This script provides an example test in a CI/CD pipeline utilizing Studio 5000 Logix Designer SDK and Factory Talk Logix Echo SDK.
//
// ---------------------------------------------------------------------------------------------------------------------------------------------------------------

using Google.Protobuf;
using RockwellAutomation.FactoryTalkLogixEcho.Api.Client;
using RockwellAutomation.FactoryTalkLogixEcho.Api.Interfaces;
using RockwellAutomation.LogixDesigner;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RockwellAutomation.LogixDesigner.LogixProject;
using DataType = RockwellAutomation.LogixDesigner.LogixProject.DataType;

namespace TestStage_CICDExample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Pass the incoming executable arguments.
            #region PARSING INCOMING VARIABLES WHEN RUNNING PROJECT EXECUTABLE --------------------------------------------------------------------------------
            if (args.Length != 2)
            {
                Console.WriteLine(@"Correct Command: .\TestStage_CICDExample github_RepositoryDirectory acd_filename");
                Console.WriteLine(@"Example Format:  .\TestStage_CICDExample C:\Users\TestUser\Desktop\example-github-repo\ acd_filename.ACD");
            }
            string githubPath = args[0];                                                                                     // 1st incoming argument = GitHub folder path
            string acdFilename = args[1];                                                                                    // 2nd incoming argument = Logix Designer ACD filename
            string filePath = githubPath + @"DEVELOPMENT-files\" + acdFilename;                                                           // file path to ACD project
            string textFileReportDirectory = Path.Combine(githubPath + @"cicd-config\stage-test\test-reports\");                          // folder path to test-reports
            string textFileReportName = Path.Combine(textFileReportDirectory, DateTime.Now.ToString("yyyyMMddHHmmss") + "_testfile.txt"); // new test report filename
            #endregion

            // Create new test report file (.txt) using the Console printout.
            #region FILE CREATION -----------------------------------------------------------------------------------------------------------------------------
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
            #endregion

            // This region contain the different steps needed to set up & execute testing.
            #region TEST STEPS --------------------------------------------------------------------------------------------------------------------------------
            #region STEP: File Title & Dependencies
            // Title Banner 
            Console.WriteLine("\n  =========================================================================================================================  ");
            Console.WriteLine("=============================================================================================================================");
            Console.WriteLine("                   CI/CD TEST STAGE | " + DateTime.Now + " " + TimeZoneInfo.Local);
            Console.WriteLine("=============================================================================================================================");
            Console.WriteLine("  =========================================================================================================================  \n\n");

            // Print out relevant test information.
            CreateBanner("TEST DEPENDENCIES");
            Console.WriteLine($"ACD file path specified:          {filePath}");
            Console.WriteLine("Common Language Runtime version:  " + typeof(string).Assembly.ImageRuntimeVersion);
            Console.WriteLine(".NET Framework version:           " + Environment.Version);
            #endregion
            #region STEP: Staging Test (folder cleanup -> Logix Echo emulation -> open ACD -> to program mode -> download ACD -> to run mode)
            CreateBanner("STAGING TEST");

            // Check the test-reports folder and if over the specified file number limit, delete the oldest test files.
            CleanTestReportsFolder(textFileReportDirectory, 5);

            // Set up emulated controller (based on the specified ACD file path) if one does not yet exist. If not, continue.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START setting up Factory Talk Logix Echo emulated controller...");
            var serviceClient = ClientFactory.GetServiceApiClientV2("CLIENT_TestStage_CICDExample");
            serviceClient.Culture = new CultureInfo("en-US");
            if (CheckCurrentChassis_Sync("CICDtest_chassis", "CICD_test", serviceClient) == false)
            {
                var chassisUpdate = new ChassisUpdate
                {
                    Name = "CICDtest_chassis",
                    Description = "Test chassis for CI/CD demonstration."
                };
                ChassisData chassisCICD = await serviceClient.CreateChassis(chassisUpdate);
                using (var fileHandle = await serviceClient.SendFile(filePath))
                {
                    var controllerUpdate = await serviceClient.GetControllerInfoFromAcd(fileHandle);
                    controllerUpdate.ChassisGuid = chassisCICD.ChassisGuid;
                    var controllerData = await serviceClient.CreateController(controllerUpdate);
                }
            }
            string[] testControllerInfo = await Get_ControllerInfo_Async("CICDtest_chassis", "CICD_test", serviceClient);
            string commPath = @"EmulateEthernet\" + testControllerInfo[1];
            Console.WriteLine($"SUCCESS: project communication path specified is \"{commPath}\"");
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE setting up Factory Talk Logix Echo emulated controller\n---");

            // Open the ACD project file and store the reference as myProject.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START opening ACD file...");
            LogixProject myProject = await LogixProject.OpenLogixProjectAsync(filePath);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS opening ACD file\n---");

            // Change controller mode to program & verify.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START changing controller to PROGRAM...");
            ChangeControllerMode_Async(commPath, "Program", myProject).GetAwaiter().GetResult();
            if (ReadControllerMode_Async(commPath, myProject).GetAwaiter().GetResult() == "PROGRAM")
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS changing controller to PROGRAM\n---");
            else
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] FAILURE changing controller to PROGRAM\n---");

            // Download project.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START downloading ACD file...");
            DownloadProject_Async(commPath, myProject).GetAwaiter().GetResult();
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS downloading ACD file\n---");

            // Change controller mode to run.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START Changing controller to RUN...");
            ChangeControllerMode_Async(commPath, "Run", myProject).GetAwaiter().GetResult();
            if (ReadControllerMode_Async(commPath, myProject).GetAwaiter().GetResult() == "RUN")
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS changing controller to RUN");
            else
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] FAILURE changing controller to RUN");
            #endregion
            #region STEP: Commencing Test (get tags -> online & offline test -> set tags -> expected values test -> get tags -> test results banner -> end test file)
            CreateBanner("COMMENCING TEST");

            // Get initial project start-up tag values.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START getting initial project start-up tag values...");
            string filePath_MainProgram = $"Controller/Programs/Program[@Name='MainProgram']/Tags/Tag";
            string filePath_ControllerScope = $"Controller/Tags/Tag";
            // The string arrays below are example results for basic data type tags. 
            string[] TEST_BOOL = Get_TagValue_Sync("TEST_BOOL", DataType.BOOL, filePath_ControllerScope, myProject, true);
            string[] TEST_SINT = Get_TagValue_Sync("TEST_SINT", DataType.SINT, filePath_ControllerScope, myProject, true);
            string[] TEST_INT = Get_TagValue_Sync("TEST_INT", DataType.INT, filePath_ControllerScope, myProject, true);
            string[] TEST_DINT = Get_TagValue_Sync("TEST_DINT", DataType.DINT, filePath_ControllerScope, myProject, true);
            string[] TEST_LINT = Get_TagValue_Sync("TEST_LINT", DataType.LINT, filePath_ControllerScope, myProject, true);
            string[] TEST_REAL = Get_TagValue_Sync("TEST_REAL", DataType.REAL, filePath_ControllerScope, myProject, true);
            string[] TEST_STRING = Get_TagValue_Sync("TEST_STRING", DataType.STRING, filePath_ControllerScope, myProject, true);
            string[] TEST_TOGGLE_WetBulbTempCalc = Get_TagValue_Sync("TEST_TOGGLE_WetBulbTempCalc", DataType.BOOL, filePath_MainProgram, myProject, true);
            string[] TEST_AOI_WetBulbTemp_isFahrenheit = Get_TagValue_Sync("TEST_AOI_WetBulbTemp_isFahrenheit", DataType.BOOL, filePath_MainProgram, myProject, true);
            string[] TEST_AOI_WetBulbTemp_RelativeHumidity = Get_TagValue_Sync("TEST_AOI_WetBulbTemp_RelativeHumidity", DataType.REAL, filePath_MainProgram, myProject, true);
            string[] TEST_AOI_WetBulbTemp_Temperature = Get_TagValue_Sync("TEST_AOI_WetBulbTemp_Temperature", DataType.REAL, filePath_MainProgram, myProject, true);
            string[] TEST_AOI_WetBulbTemp_WetBulbTemp = Get_TagValue_Sync("TEST_AOI_WetBulbTemp_WetBulbTemp", DataType.REAL, filePath_MainProgram, myProject, true);
            // The nested string array below is an example result for a complex data type tag. 
            ByteString[] ByteString_UDT_AllAtomicDataTypes = Get_UDTAllAtomicDataTypes_Sync(filePath_ControllerScope, myProject);
            string[][] UDT_AllAtomicDataTypes = Format_UDTAllAtomicDataTypes(ByteString_UDT_AllAtomicDataTypes, true);
            // The below values are examples of how to convert the complex data type's members (currently strings) to their original basic data type in c#.
            bool ex_BOOL1 = (UDT_AllAtomicDataTypes[1][0] == "1") ? true : false;
            bool ex_BOOL2 = (UDT_AllAtomicDataTypes[1][1] == "1") ? true : false;
            bool ex_BOOL3 = (UDT_AllAtomicDataTypes[1][2] == "1") ? true : false;
            bool ex_BOOL4 = (UDT_AllAtomicDataTypes[1][3] == "1") ? true : false;
            bool ex_BOOL5 = (UDT_AllAtomicDataTypes[1][4] == "1") ? true : false;
            bool ex_BOOL6 = (UDT_AllAtomicDataTypes[1][5] == "1") ? true : false;
            bool ex_BOOL7 = (UDT_AllAtomicDataTypes[1][6] == "1") ? true : false;
            bool ex_BOOL8 = (UDT_AllAtomicDataTypes[1][7] == "1") ? true : false;
            sbyte ex_SINT = sbyte.Parse(UDT_AllAtomicDataTypes[1][8]);
            int ex_INT = int.Parse(UDT_AllAtomicDataTypes[1][9]);
            double ex_DINT = double.Parse(UDT_AllAtomicDataTypes[1][10]);
            long ex_LINT = long.Parse(UDT_AllAtomicDataTypes[1][11]);
            decimal ex_REAL = decimal.Parse(UDT_AllAtomicDataTypes[1][12]);
            string ex_STRING = UDT_AllAtomicDataTypes[1][13];
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE getting initial project start-up tag values\n---");

            // Verify whether offline and online values are the same.
            // Each test returns a value of 0 for a success or 1 for a failure. The integer failure conditions tracks this tests progress.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START verifying whether offline and online values are the same...");
            int failureCondition = 0;
            failureCondition += TEST_CompareOnlineOffline(TEST_BOOL[0], TEST_BOOL[1], TEST_BOOL[2]);
            failureCondition += TEST_CompareOnlineOffline(TEST_SINT[0], TEST_SINT[1], TEST_SINT[2]);
            failureCondition += TEST_CompareOnlineOffline(TEST_INT[0], TEST_INT[1], TEST_INT[2]);
            failureCondition += TEST_CompareOnlineOffline(TEST_DINT[0], TEST_DINT[1], TEST_DINT[2]);
            failureCondition += TEST_CompareOnlineOffline(TEST_LINT[0], TEST_LINT[1], TEST_LINT[2]);
            failureCondition += TEST_CompareOnlineOffline(TEST_REAL[0], TEST_REAL[1], TEST_REAL[2]);
            failureCondition += TEST_CompareOnlineOffline(TEST_STRING[0], TEST_STRING[1], TEST_STRING[2]);
            failureCondition += TEST_CompareOnlineOffline(TEST_TOGGLE_WetBulbTempCalc[0], TEST_TOGGLE_WetBulbTempCalc[1], TEST_TOGGLE_WetBulbTempCalc[2]);
            failureCondition += TEST_CompareOnlineOffline(TEST_AOI_WetBulbTemp_isFahrenheit[0], TEST_AOI_WetBulbTemp_isFahrenheit[1], TEST_AOI_WetBulbTemp_isFahrenheit[2]);
            failureCondition += TEST_CompareOnlineOffline(TEST_AOI_WetBulbTemp_RelativeHumidity[0], TEST_AOI_WetBulbTemp_RelativeHumidity[1], TEST_AOI_WetBulbTemp_RelativeHumidity[2]);
            failureCondition += TEST_CompareOnlineOffline(TEST_AOI_WetBulbTemp_Temperature[0], TEST_AOI_WetBulbTemp_Temperature[1], TEST_AOI_WetBulbTemp_Temperature[2]);
            failureCondition += TEST_CompareOnlineOffline(TEST_AOI_WetBulbTemp_WetBulbTemp[0], TEST_AOI_WetBulbTemp_WetBulbTemp[1], TEST_AOI_WetBulbTemp_WetBulbTemp[2]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL1", UDT_AllAtomicDataTypes[1][0], UDT_AllAtomicDataTypes[2][0]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL2", UDT_AllAtomicDataTypes[1][1], UDT_AllAtomicDataTypes[2][1]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL3", UDT_AllAtomicDataTypes[1][2], UDT_AllAtomicDataTypes[2][2]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL4", UDT_AllAtomicDataTypes[1][3], UDT_AllAtomicDataTypes[2][3]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL5", UDT_AllAtomicDataTypes[1][4], UDT_AllAtomicDataTypes[2][4]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL6", UDT_AllAtomicDataTypes[1][5], UDT_AllAtomicDataTypes[2][5]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL7", UDT_AllAtomicDataTypes[1][6], UDT_AllAtomicDataTypes[2][6]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_BOOL8", UDT_AllAtomicDataTypes[1][7], UDT_AllAtomicDataTypes[2][7]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_SINT", UDT_AllAtomicDataTypes[1][8], UDT_AllAtomicDataTypes[2][8]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_INT", UDT_AllAtomicDataTypes[1][9], UDT_AllAtomicDataTypes[2][9]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_DINT", UDT_AllAtomicDataTypes[1][10], UDT_AllAtomicDataTypes[2][10]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_LINT", UDT_AllAtomicDataTypes[1][11], UDT_AllAtomicDataTypes[2][11]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_REAL", UDT_AllAtomicDataTypes[1][12], UDT_AllAtomicDataTypes[2][12]);
            failureCondition += TEST_CompareOnlineOffline("UDT_AllAtomicDataTypes.ex_STRING", UDT_AllAtomicDataTypes[1][13], UDT_AllAtomicDataTypes[2][13]);
            Console.Write($"[{DateTime.Now.ToString("T")}] DONE verifying whether offline and online values are the same\n---\n");

            // Set tag values.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START setting tag values...");
            Set_TagValue_Sync(TEST_BOOL[0], "True", TagOperationMode.Online, DataType.BOOL, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_SINT[0], "24", TagOperationMode.Online, DataType.SINT, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_INT[0], "-20500", TagOperationMode.Online, DataType.INT, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_DINT[0], "2000111000", TagOperationMode.Online, DataType.DINT, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_LINT[0], "9000111000111000111", TagOperationMode.Online, DataType.LINT, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_REAL[0], "-10555.888", TagOperationMode.Online, DataType.REAL, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_STRING[0], "1st New String!", TagOperationMode.Online, DataType.STRING, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_TOGGLE_WetBulbTempCalc[0], "True", TagOperationMode.Online, DataType.BOOL, filePath_MainProgram, myProject, true);
            Set_TagValue_Sync(TEST_AOI_WetBulbTemp_isFahrenheit[0], "True", TagOperationMode.Online, DataType.BOOL, filePath_MainProgram, myProject, true);
            Set_TagValue_Sync(TEST_AOI_WetBulbTemp_RelativeHumidity[0], "30", TagOperationMode.Online, DataType.REAL, filePath_MainProgram, myProject, true);
            Set_TagValue_Sync(TEST_AOI_WetBulbTemp_Temperature[0], "70", TagOperationMode.Online, DataType.REAL, filePath_MainProgram, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("10010010", DataType.BOOL, TagOperationMode.Online, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("-24", DataType.SINT, TagOperationMode.Online, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("20500", DataType.INT, TagOperationMode.Online, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("-2000111000", DataType.DINT, TagOperationMode.Online, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("-9000111000111000111", DataType.LINT, TagOperationMode.Online, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("10555.888", DataType.REAL, TagOperationMode.Online, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("2nd New String!", DataType.STRING, TagOperationMode.Online, myProject, true);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE setting tag values\n---");

            // Verify expected tag values based on the tag values that had been set.
            // Each test returns a value of 0 for a success or 1 for a failure. The integer failure conditions tracks this tests progress.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START verifying expected tag outputs...");
            // The tag TEST_AOI_WetBulbTemp_WetBulbTemp is an AOI output. This showcases a test command best.
            TEST_AOI_WetBulbTemp_WetBulbTemp = Get_TagValue_Sync(TEST_AOI_WetBulbTemp_WetBulbTemp[0], DataType.REAL, filePath_MainProgram, myProject, false);
            failureCondition += TEST_CompareForExpectedValue("TEST_AOI_WetBulbTemp_WetBulbTemp", "52.997536", TEST_AOI_WetBulbTemp_WetBulbTemp[1]);
            // The below tests are not outputs from logic created in Studio 5000 Logix Designer but are included
            // to provide an example of how each basic data type tag was successfully set.
            TEST_BOOL = Get_TagValue_Sync("TEST_BOOL", DataType.BOOL, filePath_ControllerScope, myProject, false);
            failureCondition += TEST_CompareForExpectedValue(TEST_BOOL[0], "True", TEST_BOOL[1]);
            TEST_SINT = Get_TagValue_Sync("TEST_SINT", DataType.SINT, filePath_ControllerScope, myProject, false);
            failureCondition += TEST_CompareForExpectedValue(TEST_SINT[0], "24", TEST_SINT[1]);
            TEST_INT = Get_TagValue_Sync("TEST_INT", DataType.INT, filePath_ControllerScope, myProject, false);
            failureCondition += TEST_CompareForExpectedValue(TEST_INT[0], "-20500", TEST_INT[1]);
            TEST_DINT = Get_TagValue_Sync("TEST_DINT", DataType.DINT, filePath_ControllerScope, myProject, false);
            failureCondition += TEST_CompareForExpectedValue(TEST_DINT[0], "2000111000", TEST_DINT[1]);
            TEST_LINT = Get_TagValue_Sync("TEST_LINT", DataType.LINT, filePath_ControllerScope, myProject, false);
            failureCondition += TEST_CompareForExpectedValue(TEST_LINT[0], "9000111000111000111", TEST_LINT[1]);
            TEST_REAL = Get_TagValue_Sync("TEST_REAL", DataType.REAL, filePath_ControllerScope, myProject, false);
            failureCondition += TEST_CompareForExpectedValue(TEST_REAL[0], "-10555.888", TEST_REAL[1]);
            TEST_STRING = Get_TagValue_Sync("TEST_STRING", DataType.STRING, filePath_ControllerScope, myProject, false);
            failureCondition += TEST_CompareForExpectedValue(TEST_STRING[0], "1st New String!", TEST_STRING[1]);
            // The below tests are not outputs from logic created in Studio 5000 Logix Designer but are included
            // to provide an example of how each basic data type in a complex tag was successfully set.
            ByteString_UDT_AllAtomicDataTypes = Get_UDTAllAtomicDataTypes_Sync(filePath_ControllerScope, myProject);
            UDT_AllAtomicDataTypes = Format_UDTAllAtomicDataTypes(ByteString_UDT_AllAtomicDataTypes, false);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL1", "False", UDT_AllAtomicDataTypes[1][0]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL2", "True", UDT_AllAtomicDataTypes[1][1]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL3", "False", UDT_AllAtomicDataTypes[1][2]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL4", "False", UDT_AllAtomicDataTypes[1][3]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL5", "True", UDT_AllAtomicDataTypes[1][4]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL6", "False", UDT_AllAtomicDataTypes[1][5]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL7", "False", UDT_AllAtomicDataTypes[1][6]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL8", "True", UDT_AllAtomicDataTypes[1][7]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_SINT", "-24", UDT_AllAtomicDataTypes[1][8]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_INT", "20500", UDT_AllAtomicDataTypes[1][9]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_DINT", "-2000111000", UDT_AllAtomicDataTypes[1][10]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_LINT", "-9000111000111000111", UDT_AllAtomicDataTypes[1][11]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_REAL", "10555.888", UDT_AllAtomicDataTypes[1][12]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_STRING", "2nd New String!", UDT_AllAtomicDataTypes[1][13]);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE verifying expected tag outputs\n---");

            // Show final tag values.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START showing final test tag values...");
            Get_TagValue_Sync(TEST_BOOL[0], DataType.BOOL, filePath_ControllerScope, myProject, true);
            Get_TagValue_Sync(TEST_DINT[0], DataType.DINT, filePath_ControllerScope, myProject, true);
            Get_TagValue_Sync(TEST_SINT[0], DataType.SINT, filePath_ControllerScope, myProject, true);
            Get_TagValue_Sync(TEST_INT[0], DataType.INT, filePath_ControllerScope, myProject, true);
            Get_TagValue_Sync(TEST_DINT[0], DataType.DINT, filePath_ControllerScope, myProject, true);
            Get_TagValue_Sync(TEST_LINT[0], DataType.LINT, filePath_ControllerScope, myProject, true);
            Get_TagValue_Sync(TEST_REAL[0], DataType.REAL, filePath_ControllerScope, myProject, true);
            Get_TagValue_Sync(TEST_STRING[0], DataType.STRING, filePath_ControllerScope, myProject, true);
            Get_TagValue_Sync(TEST_TOGGLE_WetBulbTempCalc[0], DataType.BOOL, filePath_MainProgram, myProject, true);
            Get_TagValue_Sync(TEST_AOI_WetBulbTemp_isFahrenheit[0], DataType.BOOL, filePath_MainProgram, myProject, true);
            Get_TagValue_Sync(TEST_AOI_WetBulbTemp_RelativeHumidity[0], DataType.REAL, filePath_MainProgram, myProject, true);
            Get_TagValue_Sync(TEST_AOI_WetBulbTemp_Temperature[0], DataType.REAL, filePath_MainProgram, myProject, true);
            Format_UDTAllAtomicDataTypes(Get_UDTAllAtomicDataTypes_Sync(filePath_ControllerScope, myProject), true);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE showing final test tag values");

            // Print out final banner based on test results.
            if (failureCondition > 0)
                CreateBanner("TEST FAILURE");
            else
                CreateBanner("TEST SUCCESS!");

            // Finish the process of sending console printouts to the test text file.
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();

            // Print out final banner based on test results.
            if (failureCondition > 0)
                Console.WriteLine("FAILURE");
            else
                Console.WriteLine("SUCCESS");
            #endregion
            #endregion
        }

        #region METHODS: formatting text file
        /// <summary>
        /// Modify the input string to wrap the text to the next line after a certain length. 
        /// The input string is seperated per word and then each line is incrementally added to per word.  
        /// Start a new line when the character count of a line exceeds 125.
        /// </summary>
        /// <param name="input_string">The input string to be wrapped.</param>
        /// <returns>A modified string that wraps every 125 characters.</returns>
        private static string WrapText(string input_string)
        {
            int myLimit = 125;
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

        /// <summary>
        /// Create a banner used to identify the portion of the test being executed and write it to console.
        /// </summary>
        /// <param name="bannerName">The name displayed in the console banner.</param>
        private static void CreateBanner(string bannerName)
        {
            string final_banner = "-=[" + bannerName + "]=---";
            final_banner = final_banner.PadLeft(125, '-');
            Console.WriteLine(final_banner);
        }

        /// <summary>
        /// Delete all the oldest files in a specified folder that exceeds the chosen number of files to keep.
        /// </summary>
        /// <param name="folderPath">The full path to the folder that will be cleaned.</param>
        /// <param name="keepCount">The number of files in a folder to be kept.</param>
        private static void CleanTestReportsFolder(string folderPath, int keepCount)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START checking test-reports folder...");
            Console.WriteLine($"STATUS:  test script set to retain {keepCount} test files");
            string[] all_files = Directory.GetFiles(folderPath);
            var orderedFiles = all_files.Select(f => new FileInfo(f)).OrderBy(f => f.CreationTime).ToList();
            if (orderedFiles.Count > keepCount)
            {
                for (int i = 0; i < (orderedFiles.Count - keepCount); i++)
                {
                    FileInfo deleteThisFile = orderedFiles[i];
                    deleteThisFile.Delete();
                    Console.WriteLine($"SUCCESS: deleted {deleteThisFile.FullName}");
                }
            }
            else
                Console.WriteLine($"SUCCESS: no files needed to be deleted (currently {orderedFiles.Count} test files)");

            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE checking test-reports folder...\n---");
        }
        #endregion

        #region METHODS: setting up Logix Echo emulated controller
        /// <summary>
        /// Asynchronously check to see if a specific controller exists in a specific chassis.
        /// </summary>
        /// <param name="chassisName">The name of the emulated chassis to check the emulated controler in.</param>
        /// <param name="controllerName">The name of the emulated controller to check.</param>
        /// <param name="serviceClient">The Factory Talk Logix Echo interface.</param>
        /// <returns>A Task that returns a boolean value 'True' if the emulated controller already exists and a 'False' if it does not.</returns>
        private static async Task<bool> CheckCurrentChassis_Async(string chassisName, string controllerName, IServiceApiClientV2 serviceClient)
        {
            var chassisList = (await serviceClient.ListChassis()).ToList();
            for (int i = 0; i < chassisList.Count; i++)
            {
                if (chassisList[i].Name == chassisName)
                {
                    var chassisGuid = chassisList[i].ChassisGuid;
                    var controllerList = (await serviceClient.ListControllers(chassisGuid)).ToList();
                    for (int j = 0; j < controllerList.Count; j++)
                    {
                        if (controllerList[j].ControllerName == controllerName)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Run the CheckCurrentChassisAsync method synchronously.
        /// Check to see if a specific controller exists in a specific chassis.
        /// </summary>
        /// <param name="chassisName">The name of the emulated chassis to check the emulated controler in.</param>
        /// <param name="controllerName">The name of the emulated controller to check.</param>
        /// <param name="serviceClient">The Factory Talk Logix Echo interface.</param>
        /// <returns>A boolean value 'True' if the emulated controller already exists and a 'False' if it does not.</returns>
        private static bool CheckCurrentChassis_Sync(string chassisName, string controllerName, IServiceApiClientV2 serviceClient)
        {
            var task = CheckCurrentChassis_Async(chassisName, controllerName, serviceClient);
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Asynchronously get the emulated controller name, IP address, and project file path.
        /// </summary>
        /// <param name="chassisName">The emulated chassis to the emulatedcontroller information from.</param>
        /// <param name="controllerName">The emulated controller name.</param>
        /// <param name="serviceClient">The Factory Talk Logix Echo interface.</param>
        /// <returns>
        /// A Task that returns a string array containing controller information:
        /// return_array[0] = controller name
        /// return_array[1] = controller IP address
        /// return_array[2] = controller project file path
        /// </returns>
        private static async Task<string[]> Get_ControllerInfo_Async(string chassisName, string controllerName, IServiceApiClientV2 serviceClient)
        {
            string[] return_array = new string[3];
            var chassisList = (await serviceClient.ListChassis()).ToList();
            for (int i = 0; i < chassisList.Count; i++)
            {
                if (chassisList[i].Name == chassisName)
                {
                    var chassisGuid = chassisList[i].ChassisGuid;
                    var controllerList = (await serviceClient.ListControllers(chassisGuid)).ToList();
                    for (int j = 0; j < controllerList.Count; j++)
                    {
                        if (controllerList[j].ControllerName == controllerName)
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
        #endregion

        #region METHODS: changing controller mode / download
        /// <summary>
        /// Asynchronously change the controller mode to either Program, Run, or Test mode.
        /// </summary>
        /// <param name="commPath">The controller communication path.</param>
        /// <param name="mode">The controller mode to switch to.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <returns>A Task that changes the controller mode.</returns>
        private static async Task ChangeControllerMode_Async(string commPath, string mode, LogixProject project)
        {
            var requestedControllerMode = default(LogixProject.RequestedControllerMode);
            if (mode == "Program")
                requestedControllerMode = LogixProject.RequestedControllerMode.Program;
            else if (mode == "Run")
                requestedControllerMode = LogixProject.RequestedControllerMode.Run;
            else if (mode == "Test")
                requestedControllerMode = LogixProject.RequestedControllerMode.Test;
            else
                Console.WriteLine($"ERROR: {mode} is not supported.");

            try
            {
                await project.SetCommunicationsPathAsync(commPath);
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"Unable to set commpath to {commPath}");
                Console.WriteLine(ex.Message);
            }

            try
            {
                await project.ChangeControllerModeAsync(requestedControllerMode);
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"Unable to set mode. Requested mode was {mode}");
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Asynchronously download to the specified controller.
        /// </summary>
        /// <param name="commPath">The controller communication path.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <returns>An Task that downloads to the specified controller.</returns>
        private static async Task DownloadProject_Async(string commPath, LogixProject project)
        {
            try
            {
                await project.SetCommunicationsPathAsync(commPath);
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"Unable to set commpath to {commPath}");
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
            // may not be able to succeed because project in the controller won't match opened project.
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

        /// <summary>
        /// Asynchronously get the current controller mode (FAULTED, PROGRAM, RUN, or TEST).
        /// </summary>
        /// <param name="commPath">The controller communication path.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <returns>A Task that returns a string of the current controller mode.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the returned controller mode is not FAULTED, PROGRAM, RUN, or TEST.</exception>
        private static async Task<string> ReadControllerMode_Async(string commPath, LogixProject project)
        {
            try
            {
                await project.SetCommunicationsPathAsync(commPath);
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"Unable to set commpath to {commPath}");
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
        #endregion

        #region METHODS: get/set basic data type tags
        /// <summary>
        /// Asynchronously get the online and offline value of a basic data type tag.
        /// (basic data types handled: boolean, single integer, integer, double integer, long integer, real, string)
        /// </summary>
        /// <param name="tagName">The name of the tag in Studio 5000 Logix Designer whose value will be returned.</param>
        /// <param name="type">The data type of the tag whose value will be returned.</param>
        /// <param name="tagPath">
        /// The tag path specifying the tag's scope and location in the Studio 5000 Logix Designer project.
        /// The tag path is based on the XML filetype (L5X) encapsulation of elements.
        /// </param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <param name="printout">A boolean that, if True, prints the online and offline values to the console.</param>
        /// <returns>
        /// A Task that results in a string array containing tag information:
        /// return_array[0] = tag name
        /// return_array[1] = online tag value
        /// return_array[2] = offline tag value
        /// </returns>
        private static async Task<string[]> Get_TagValue_Async(string tagName, DataType type, string tagPath, LogixProject project, bool printout)
        {
            string[] return_array = new string[3];
            tagPath = tagPath + $"[@Name='{tagName}']";
            return_array[0] = tagName;
            try
            {
                if (type == DataType.BOOL)
                {
                    var tagValue_online = await project.GetTagValueBOOLAsync(tagPath, TagOperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueBOOLAsync(tagPath, TagOperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";

                }
                else if (type == DataType.SINT)
                {
                    var tagValue_online = await project.GetTagValueSINTAsync(tagPath, TagOperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueSINTAsync(tagPath, TagOperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";
                }
                else if (type == DataType.INT)
                {
                    var tagValue_online = await project.GetTagValueINTAsync(tagPath, TagOperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueINTAsync(tagPath, TagOperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";
                }
                else if (type == DataType.DINT)
                {
                    var tagValue_online = await project.GetTagValueDINTAsync(tagPath, TagOperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueDINTAsync(tagPath, TagOperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";
                }
                else if (type == DataType.LINT)
                {
                    var tagValue_online = await project.GetTagValueLINTAsync(tagPath, TagOperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueLINTAsync(tagPath, TagOperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";
                }
                else if (type == DataType.REAL)
                {
                    var tagValue_online = await project.GetTagValueREALAsync(tagPath, TagOperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueREALAsync(tagPath, TagOperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";
                }
                else if (type == DataType.STRING)
                {
                    var tagValue_online = await project.GetTagValueSTRINGAsync(tagPath, TagOperationMode.Online);
                    return_array[1] = (tagValue_online == "") ? "<empty_string>" : $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueSTRINGAsync(tagPath, TagOperationMode.Offline);
                    return_array[2] = (tagValue_offline == "") ? "<empty_string>" : $"{tagValue_offline}";
                }
                else
                    Console.WriteLine(WrapText($"ERROR executing command: The tag {tagName} cannot be handled. Select either DINT, BOOL, or REAL."));
            }
            catch (LogixSdkException ex)
            {
                Console.WriteLine($"ERROR getting tag {tagName}");
                Console.WriteLine(ex.Message);
            }

            if (printout)
            {
                string online_message = $"online value: {return_array[1]}";
                string offline_message = $"offline value: {return_array[2]}";
                Console.WriteLine($"SUCCESS: " + tagName.PadRight(40, ' ') + online_message.PadRight(35, ' ') + offline_message.PadRight(35, ' '));
            }

            return return_array;
        }

        /// <summary>
        /// Run the GetTagValueAsync method synchronously.
        /// Get the online and offline value of a basic data type tag.
        /// (basic data types handled: boolean, single integer, integer, double integer, long integer, real, string)
        /// </summary>
        /// <param name="tagName">The name of the tag whose value will be returned.</param>
        /// <param name="type">The data type of the tag whose value will be returned.</param>
        /// <param name="tagPath">
        /// The tag path specifying the tag's scope and location in the Studio 5000 Logix Designer project.
        /// The tag path is based on the XML filetype (L5X) encapsulation of elements.
        /// </param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <param name="printout">A boolean that, if True, prints the online and offline values to the console.</param>
        /// <returns>
        /// A Task that results in a string array containing tag information:
        /// return_array[0] = tag name
        /// return_array[1] = online tag value
        /// return_array[2] = offline tag value
        /// </returns>
        private static string[] Get_TagValue_Sync(string tagName, DataType type, string tagPath, LogixProject project, bool printout)
        {
            var task = Get_TagValue_Async(tagName, type, tagPath, project, printout);
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Asynchronously set either the online or offline value of a basic data type tag.
        /// (basic data types handled: boolean, single integer, integer, double integer, long integer, real, string)
        /// </summary>
        /// <param name="tagName">The name of the tag whose value will be set.</param>
        /// <param name="newTagValue">The value of the tag that will be set.</param>
        /// <param name="mode">This specifies whether the 'Online' or 'Offline' value of the tag is the one to set.</param>
        /// <param name="type">The data type of the tag whose value will be set.</param>
        /// <param name="tagPath">
        /// The tag path specifying the tag's scope and location in the Studio 5000 Logix Designer project.
        /// The tag path is based on the XML filetype (L5X) encapsulation of elements.
        /// </param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <param name="printout">A boolean that, if True, prints the online and offline values to the console.</param>
        /// <returns>A Task that will set the online or offline value of a basic data type tag.</returns>
        private static async Task Set_TagValue_Async(string tagName, string newTagValue, TagOperationMode mode, DataType type, string tagPath, LogixProject project, bool printout)
        {
            tagPath = tagPath + $"[@Name='{tagName}']";
            string[] old_tag_values = Get_TagValue_Sync(tagName, type, tagPath, project, false);
            string old_tag_value = "";
            try
            {
                if (mode == TagOperationMode.Online)
                {

                    if (type == DataType.BOOL)
                        await project.SetTagValueBOOLAsync(tagPath, TagOperationMode.Online, bool.Parse(newTagValue));
                    else if (type == DataType.SINT)
                        await project.SetTagValueSINTAsync(tagPath, TagOperationMode.Online, sbyte.Parse(newTagValue));
                    else if (type == DataType.INT)
                        await project.SetTagValueINTAsync(tagPath, TagOperationMode.Online, short.Parse(newTagValue));
                    else if (type == DataType.DINT)
                        await project.SetTagValueDINTAsync(tagPath, TagOperationMode.Online, int.Parse(newTagValue));
                    else if (type == DataType.LINT)
                        await project.SetTagValueLINTAsync(tagPath, TagOperationMode.Online, long.Parse(newTagValue));
                    else if (type == DataType.REAL)
                        await project.SetTagValueREALAsync(tagPath, TagOperationMode.Online, float.Parse(newTagValue));
                    else if (type == DataType.STRING)
                        await project.SetTagValueSTRINGAsync(tagPath, TagOperationMode.Online, newTagValue);
                    else
                        Console.WriteLine($"ERROR executing command: The data type cannot be handled. Select either DINT, BOOL, or REAL.");
                    old_tag_value = old_tag_values[1];
                }
                else if (mode == TagOperationMode.Offline)
                {
                    if (type == DataType.BOOL)
                        await project.SetTagValueBOOLAsync(tagPath, TagOperationMode.Offline, bool.Parse(newTagValue));
                    else if (type == DataType.SINT)
                        await project.SetTagValueSINTAsync(tagPath, TagOperationMode.Offline, sbyte.Parse(newTagValue));
                    else if (type == DataType.INT)
                        await project.SetTagValueINTAsync(tagPath, TagOperationMode.Offline, short.Parse(newTagValue));
                    else if (type == DataType.DINT)
                        await project.SetTagValueDINTAsync(tagPath, TagOperationMode.Offline, int.Parse(newTagValue));
                    else if (type == DataType.LINT)
                        await project.SetTagValueLINTAsync(tagPath, TagOperationMode.Offline, long.Parse(newTagValue));
                    else if (type == DataType.REAL)
                        await project.SetTagValueREALAsync(tagPath, TagOperationMode.Offline, float.Parse(newTagValue));
                    else if (type == DataType.STRING)
                        await project.SetTagValueSTRINGAsync(tagPath, TagOperationMode.Offline, newTagValue);
                    else
                        Console.WriteLine($"ERROR executing command: The data type cannot be handled. Select either DINT, BOOL, or REAL.");
                    old_tag_value = old_tag_values[2];
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

            if (printout)
            {
                string new_tag_value_string = Convert.ToString(newTagValue);
                if ((new_tag_value_string == "1") && (type == DataType.BOOL)) { new_tag_value_string = "True"; }
                if ((new_tag_value_string == "0") && (type == DataType.BOOL)) { new_tag_value_string = "False"; }
                Console.WriteLine("SUCCESS: " + mode + " " + old_tag_values[0].PadRight(40, ' ') + old_tag_value.PadLeft(20, ' ') + "  -->  " + new_tag_value_string);
            }
        }

        /// <summary>
        /// Run the SetTagValueAsync method synchronously.
        /// Set either the online or offline value of a basic data type tag.
        /// (basic data types handled: boolean, single integer, integer, double integer, long integer, real, string)
        /// </summary>
        /// <param name="tagName">The name of the tag whose value will be set.</param>
        /// <param name="newTagValue">The value of the tag that will be set.</param>
        /// <param name="mode">This specifies whether the 'Online' or 'Offline' value of the tag is the one to set.</param>
        /// <param name="type">The data type of the tag whose value will be set.</param>
        /// <param name="tagPath">
        /// The tag path specifying the tag's scope and location in the Studio 5000 Logix Designer project.
        /// The tag path is based on the XML filetype (L5X) encapsulation of elements.
        /// </param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <param name="printout">A boolean that, if True, prints the online and offline values to the console.</param>
        private static void Set_TagValue_Sync(string tagName, string newTagValue, TagOperationMode mode, DataType type, string tagPath, LogixProject project, bool printout)
        {
            var task = Set_TagValue_Async(tagName, newTagValue, mode, type, tagPath, project, printout);
            task.Wait();
        }
        #endregion

        #region METHODS: get/set complex data type tags
        /// <summary>
        /// Asynchronously get the online and offline data in ByteString form for the complex tag UDT_AllAtomicDataTypes.
        /// </summary>
        /// <param name="tagPath">
        /// The tag path specifying the tag's scope and location in the Studio 5000 Logix Designer project.
        /// The tag path is based on the XML filetype (L5X) encapsulation of elements.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <returns>
        /// A Task that results in a ByteString array containing UDT_AllAtomicDataTypes information:
        /// returnByteStringArray[0] = online tag values
        /// returnByteStringArray[1] = offline tag values
        /// </returns>
        private static async Task<ByteString[]> Get_UDTAllAtomicDataTypes_Async(string tagPath, LogixProject project)
        {
            tagPath = tagPath + $"[@Name='UDT_AllAtomicDataTypes']";
            ByteString[] returnByteStringArray = new ByteString[2];
            returnByteStringArray[0] = await project.GetTagValueAsync(tagPath, TagOperationMode.Online, DataType.BYTE_ARRAY);
            returnByteStringArray[1] = await project.GetTagValueAsync(tagPath, TagOperationMode.Offline, DataType.BYTE_ARRAY);
            return returnByteStringArray;
        }

        /// <summary>
        /// Run the GetUDT_AllAtomicDataTypesAsync Method synchronously.
        /// Get the online and offline data in ByteString form for the complex tag UDT_AllAtomicDataTypes.
        /// </summary>
        /// <param name="tagPath">
        /// The tag path specifying the tag's scope and location in the Studio 5000 Logix Designer project.
        /// The tag path is based on the XML filetype (L5X) encapsulation of elements.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <returns>
        /// A ByteString array containing UDT_AllAtomicDataTypes information:
        /// returnByteStringArray[0] = online tag values
        /// returnByteStringArray[1] = offline tag values
        /// </returns>
        private static ByteString[] Get_UDTAllAtomicDataTypes_Sync(string tagPath, LogixProject project)
        {
            var task = Get_UDTAllAtomicDataTypes_Async(tagPath, project);
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Asynchronously set either the online or offline value of a member of the complex data type tag UDT_AllAtomicDataTypes. Each member is a basic data type.
        /// (basic data types handled: boolean, single integer, integer, double integer, long integer, real, string)
        /// </summary>
        /// <param name="newTagValue">The value of the UDT_AllAtomicDataTypes tag member that will be set.</param>
        /// <param name="type">>The data type of the UDT_AllAtomicDataTypes tag member whose value will be set.</param>
        /// <param name="mode">This specifies whether the 'Online' or 'Offline' value of the UDT_AllAtomicDataTypes tag member is the one to set.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <param name="printout">A boolean that, if True, prints the "before" and "after" values of the UDT_AllAtomicDataTypes tag member to the console.</param>
        /// <returns>A Task that will set the online or offline value of the specified UDT_AllAtomicDataTypes tag member.</returns>
        private static async Task Set_UDTAllAtomicDataTypes_Async(string newTagValue, DataType type, TagOperationMode mode, LogixProject project, bool printout)
        {
            string tagPath = $"Controller/Tags/Tag[@Name='UDT_AllAtomicDataTypes']";
            ByteString[] old_byteString = Get_UDTAllAtomicDataTypes_Sync(tagPath, project);
            ByteString[] new_byteString = Get_UDTAllAtomicDataTypes_Sync(tagPath, project);
            string[][] old_UDT_AllAtomicDataTypes = Format_UDTAllAtomicDataTypes(old_byteString, false);
            int on_off = 0;
            byte[] new_byteArray = new byte[new_byteString[1].Length];

            if (mode == TagOperationMode.Online)
                new_byteArray = new_byteString[0].ToByteArray();
            else if (mode == TagOperationMode.Offline)
                new_byteArray = new_byteString[1].ToByteArray();

            if (type == DataType.BOOL)
                new_byteArray[0] = Convert.ToByte(newTagValue, 2);

            else if (type == DataType.SINT)
            {
                string sint_string = Convert.ToString(long.Parse(newTagValue), 2);
                sint_string = sint_string.Substring(sint_string.Length - 8);
                new_byteArray[1] = Convert.ToByte(sint_string, 2);
            }

            else if (type == DataType.INT)
            {
                byte[] int_byteArray = BitConverter.GetBytes(int.Parse(newTagValue));
                for (int i = 0; i < 2; ++i)
                    new_byteArray[i + 2] = int_byteArray[i];
            }

            else if (type == DataType.DINT)
            {
                byte[] dint_byteArray = BitConverter.GetBytes(long.Parse(newTagValue));
                for (int i = 0; i < 4; ++i)
                    new_byteArray[i + 4] = dint_byteArray[i];
            }

            else if (type == DataType.LINT)
            {
                byte[] lint_byteArray = BitConverter.GetBytes(long.Parse(newTagValue));
                for (int i = 0; i < 8; ++i)
                    new_byteArray[i + 8] = lint_byteArray[i];
            }

            else if (type == DataType.REAL)
            {
                byte[] real_byteArray = BitConverter.GetBytes(float.Parse(newTagValue));
                for (int i = 0; i < 4; ++i)
                    new_byteArray[i + 16] = real_byteArray[i];
            }

            else if (type == DataType.STRING)
            {
                byte[] real_byteArray = new byte[newTagValue.Length];
                for (int i = 0; i < real_byteArray.Length; ++i)
                    new_byteArray[i + 24] = (byte)newTagValue[i];
            }

            if (mode == TagOperationMode.Online)
            {
                new_byteString[0] = ByteString.CopyFrom(new_byteArray);
                new_byteString[1] = old_byteString[1];
                on_off = 1;
            }
            else if (mode == TagOperationMode.Offline)
            {
                new_byteString[0] = old_byteString[0];
                new_byteString[1] = ByteString.CopyFrom(new_byteArray);
                on_off = 2;
            }

            string[][] new_UDT_AllAtomicDataTypes = Format_UDTAllAtomicDataTypes(new_byteString, false);
            await project.SetTagValueAsync(tagPath, mode, new_byteString[on_off - 1].ToByteArray(), DataType.BYTE_ARRAY);
            if (printout)
            {
                for (int i = 0; i < old_UDT_AllAtomicDataTypes[1].Length - 1; ++i)
                    if (old_UDT_AllAtomicDataTypes[on_off][i] != new_UDT_AllAtomicDataTypes[on_off][i])
                        Console.WriteLine("SUCCESS: " + mode + " " + old_UDT_AllAtomicDataTypes[0][i].PadRight(40, ' ') + old_UDT_AllAtomicDataTypes[on_off][i].PadLeft(20, ' ') + "  -->  " + new_UDT_AllAtomicDataTypes[on_off][i]);
            }
        }

        /// <summary>
        /// Run the SetUDT_AllAtomicDataTypesAsync Method synchronously.
        /// Set either the online or offline value of a member of the complex data type tag UDT_AllAtomicDataTypes. Each member is a basic data type.
        /// (basic data types handled: boolean, single integer, integer, double integer, long integer, real, string)
        /// </summary>
        /// <param name="newTagValue">The value of the UDT_AllAtomicDataTypes tag member that will be set.</param>
        /// <param name="type">>The data type of the UDT_AllAtomicDataTypes tag member whose value will be set.</param>
        /// <param name="mode">This specifies whether the 'Online' or 'Offline' value of the UDT_AllAtomicDataTypes tag member is the one to set.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <param name="printout">A boolean that, if True, prints the "before" and "after" values of the UDT_AllAtomicDataTypes tag member to the console.</param>
        private static void Set_UDTAllAtomicDataTypes_Sync(string newTagValue, DataType type, TagOperationMode mode, LogixProject project, bool printout)
        {
            var task = Set_UDTAllAtomicDataTypes_Async(newTagValue, type, mode, project, printout);
            task.Wait();
        }

        /// <summary>
        /// Format the complex data type tag UDT_AllAtomicDataTypes from a ByteString to a nested string array.
        /// </summary>
        /// <param name="byteStringArray">
        /// This input parameter is the Get_UDTAllAtomicDataTypes_Async method output or the Get_UDTAllAtomicDataTypes_Sync method output.
        /// The ByteString array has the following format:
        /// byteStringArray[0] = ByteString for online complex data type tag values
        /// byteStringArray[1] = ByteString for offline complex data type tag values
        /// </param>
        /// <param name="printout">A boolean that, if True, prints the "before" and "after" values of the UDT_AllAtomicDataTypes tag member to the console.</param>
        /// <returns>
        /// A nested string array with the following format:
        /// returnString[0][] = the names of the 14 members of the UDT_AllAtomicDataTypes complex data type tag in the order they were made in Studio 5000 Logix Designer
        /// returnString[1][] = the online values of the 14 members of the UDT_AllAtomicDataTypes complex data type tag in the order they were made in Studio 5000 Logix Designer
        /// returnString[2][] = the offline values of the 14 members of the UDT_AllAtomicDataTypes complex data type tag in the order they were made in Studio 5000 Logix Designer
        /// <para>
        /// Every element of the nested string array:
        /// returnString[0][0] = UDT_AllAtomicDataTypes.ex_BOOL1 name
        /// returnString[0][1] = UDT_AllAtomicDataTypes.ex_BOOL2 name
        /// returnString[0][2] = UDT_AllAtomicDataTypes.ex_BOOL3 name
        /// returnString[0][3] = UDT_AllAtomicDataTypes.ex_BOOL4 name
        /// returnString[0][4] = UDT_AllAtomicDataTypes.ex_BOOL5 name
        /// returnString[0][5] = UDT_AllAtomicDataTypes.ex_BOOL5 name
        /// returnString[0][6] = UDT_AllAtomicDataTypes.ex_BOOL6 name
        /// returnString[0][7] = UDT_AllAtomicDataTypes.ex_BOOL7 name
        /// returnString[0][8] = UDT_AllAtomicDataTypes.ex_BOOL8 name
        /// returnString[0][9] = UDT_AllAtomicDataTypes.ex_SINT name
        /// returnString[0][10] = UDT_AllAtomicDataTypes.ex_INT name
        /// returnString[0][11] = UDT_AllAtomicDataTypes.ex_DINT name
        /// returnString[0][12] = UDT_AllAtomicDataTypes.ex_LINT name
        /// returnString[0][13] = UDT_AllAtomicDataTypes.ex_REAL name
        /// returnString[0][14] = UDT_AllAtomicDataTypes.ex_STRING name
        /// returnString[1][0] = UDT_AllAtomicDataTypes.ex_BOOL1 online value
        /// returnString[1][1] = UDT_AllAtomicDataTypes.ex_BOOL2 online value
        /// returnString[1][2] = UDT_AllAtomicDataTypes.ex_BOOL3 online value
        /// returnString[1][3] = UDT_AllAtomicDataTypes.ex_BOOL4 online value
        /// returnString[1][4] = UDT_AllAtomicDataTypes.ex_BOOL5 online value
        /// returnString[1][5] = UDT_AllAtomicDataTypes.ex_BOOL5 online value
        /// returnString[1][6] = UDT_AllAtomicDataTypes.ex_BOOL6 online value
        /// returnString[1][7] = UDT_AllAtomicDataTypes.ex_BOOL7 online value
        /// returnString[1][8] = UDT_AllAtomicDataTypes.ex_BOOL8 online value
        /// returnString[1][9] = UDT_AllAtomicDataTypes.ex_SINT online value
        /// returnString[1][10] = UDT_AllAtomicDataTypes.ex_INT online value
        /// returnString[1][11] = UDT_AllAtomicDataTypes.ex_DINT online value
        /// returnString[1][12] = UDT_AllAtomicDataTypes.ex_LINT online value
        /// returnString[1][13] = UDT_AllAtomicDataTypes.ex_REAL online value
        /// returnString[1][14] = UDT_AllAtomicDataTypes.ex_STRING online value
        /// returnString[1][15] = the number of bytes in element 0 of the input ByteString array (online values)
        /// returnString[2][0] = UDT_AllAtomicDataTypes.ex_BOOL1 offline value
        /// returnString[2][1] = UDT_AllAtomicDataTypes.ex_BOOL2 offline value
        /// returnString[2][2] = UDT_AllAtomicDataTypes.ex_BOOL3 offline value
        /// returnString[2][3] = UDT_AllAtomicDataTypes.ex_BOOL4 offline value
        /// returnString[2][4] = UDT_AllAtomicDataTypes.ex_BOOL5 offline value
        /// returnString[2][5] = UDT_AllAtomicDataTypes.ex_BOOL5 offline value
        /// returnString[2][6] = UDT_AllAtomicDataTypes.ex_BOOL6 offline value
        /// returnString[2][7] = UDT_AllAtomicDataTypes.ex_BOOL7 offline value
        /// returnString[2][8] = UDT_AllAtomicDataTypes.ex_BOOL8 offline value
        /// returnString[2][9] = UDT_AllAtomicDataTypes.ex_SINT offline value
        /// returnString[2][10] = UDT_AllAtomicDataTypes.ex_INT offline value
        /// returnString[2][11] = UDT_AllAtomicDataTypes.ex_DINT offline value
        /// returnString[2][12] = UDT_AllAtomicDataTypes.ex_LINT offline value
        /// returnString[2][13] = UDT_AllAtomicDataTypes.ex_REAL offline value
        /// returnString[2][14] = UDT_AllAtomicDataTypes.ex_STRING offline value
        /// returnString[2][15] = the number of bytes in element 1 of the input ByteString array (offline values)
        /// </para>
        /// </returns>
        private static string[][] Format_UDTAllAtomicDataTypes(ByteString[] byteStringArray, bool printout)
        {

            string[][] returnString = new string[3][];
            returnString[0] = new string[] { "UDT_AllAtomicDataTypes.ex_BOOL1", "UDT_AllAtomicDataTypes.ex_BOOL2", "UDT_AllAtomicDataTypes.ex_BOOL3",
                "UDT_AllAtomicDataTypes.ex_BOOL4", "UDT_AllAtomicDataTypes.ex_BOOL5", "UDT_AllAtomicDataTypes.ex_BOOL6", "UDT_AllAtomicDataTypes.ex_BOOL7",
                "UDT_AllAtomicDataTypes.ex_BOOL8", "UDT_AllAtomicDataTypes.ex_SINT", "UDT_AllAtomicDataTypes.ex_INT", "UDT_AllAtomicDataTypes.ex_DINT",
                "UDT_AllAtomicDataTypes.ex_LINT", "UDT_AllAtomicDataTypes.ex_REAL", "UDT_AllAtomicDataTypes.ex_STRING" };
            returnString[1] = new string[15];
            returnString[2] = new string[15];

            for (int i = 0; i < 2; i++)
            {
                var UDT_ByteArray = new byte[byteStringArray[0].Length];
                for (int j = 0; j < UDT_ByteArray.Length; j++)
                    UDT_ByteArray[j] = byteStringArray[i][j];

                var ex_BOOLs = new byte[1];
                Array.ConstrainedCopy(UDT_ByteArray, 0, ex_BOOLs, 0, 1);
                string ex_BOOLS_binaryString = Convert.ToString(ex_BOOLs[0], 2).PadLeft(8, '0');
                for (int j = 0; j < 8; j++)
                    returnString[i + 1][j] = (Convert.ToString(ex_BOOLS_binaryString[7 - j]) == "1") ? "True" : "False";

                var ex_SINT = new byte[1];
                Array.ConstrainedCopy(UDT_ByteArray, 1, ex_SINT, 0, 1);
                returnString[i + 1][8] = Convert.ToString(unchecked((sbyte)ex_SINT[0]));

                var ex_INT = new byte[2];
                Array.ConstrainedCopy(UDT_ByteArray, 2, ex_INT, 0, 2);
                returnString[i + 1][9] = Convert.ToString(BitConverter.ToInt16(ex_INT));

                var ex_DINT = new byte[4];
                Array.ConstrainedCopy(UDT_ByteArray, 4, ex_DINT, 0, 4);
                returnString[i + 1][10] = Convert.ToString(BitConverter.ToInt32(ex_DINT));

                var ex_LINT = new byte[8];
                Array.ConstrainedCopy(UDT_ByteArray, 8, ex_LINT, 0, 8);
                returnString[i + 1][11] = Convert.ToString(BitConverter.ToInt64(ex_LINT));

                var ex_REAL = new byte[4];
                Array.ConstrainedCopy(UDT_ByteArray, 16, ex_REAL, 0, 4);
                returnString[i + 1][12] = Convert.ToString(BitConverter.ToSingle(ex_REAL));

                var ex_STRING = new byte[UDT_ByteArray.Length - 24];
                Array.ConstrainedCopy(UDT_ByteArray, 24, ex_STRING, 0, UDT_ByteArray.Length - 24);
                string string_result = Encoding.ASCII.GetString(ex_STRING).Replace("\0", "");
                returnString[i + 1][13] = (string_result == "") ? "<empty_string>" : $"{string_result}";

                returnString[i + 1][14] = Convert.ToString(byteStringArray[i].Length);
            }

            if (printout)
            {
                for (int i = 0; i < returnString[0].Length; i++)
                {
                    var online_message = $"online value: {returnString[1][i]}";
                    var offline_message = $"offline value: {returnString[2][i]}";
                    Console.WriteLine($"SUCCESS: " + returnString[0][i].PadRight(40, ' ') + online_message.PadRight(35, ' ') + offline_message.PadRight(35, ' '));
                }
            }

            return returnString;
        }
        #endregion

        #region METHODS: testing online & offline / testing for expected value 
        /// <summary>
        /// A test to compare the online and offline values of a tag.
        /// </summary>
        /// <param name="tagName">The name of the tag to be tested.</param>
        /// <param name="onlineValue">The online value of the tag under test.</param>
        /// <param name="offlineValue">The offline value of the tag under test.</param>
        /// <returns>Return an integer value 1 for test failure and an integer value 0 for test success.</returns>
        /// <remarks>
        /// The integer output is added to an integer that tracks the total number of failures. 
        /// At the end of all testing, the overall SUCCESS/FAILURE of this CI/CD test stage is determined whether its value is greater than 0.
        /// </remarks>
        private static int TEST_CompareOnlineOffline(string tagName, string onlineValue, string offlineValue)
        {
            if (onlineValue != offlineValue)
            {
                Console.WriteLine(WrapText($"FAILURE: {tagName} online value ({onlineValue}) & offline value ({offlineValue}) NOT equal."));
                return 1;
            }
            else
            {
                Console.Write(WrapText($"SUCCESS: {tagName} online value ({onlineValue}) & offline value ({offlineValue}) are EQUAL."));
                return 0;
            }
        }

        /// <summary>
        /// A test to compare the expected and actual values of a tag.
        /// </summary>
        /// <param name="tagName">The name of the tag to be tested.</param>
        /// <param name="expectedValue">The expected value of the tag under test.</param>
        /// <param name="actualValue">The actual value of the tag under test.</param>
        /// <returns>Return an integer value 1 for test failure and an integer value 0 for test success.</returns>
        /// <remarks>
        /// The integer output is added to an integer that tracks the total number of failures. 
        /// At the end of all testing, the overall SUCCESS/FAILURE of this CI/CD test stage is determined whether its value is greater than 0.
        /// </remarks>
        private static int TEST_CompareForExpectedValue(string tagName, string expectedValue, string actualValue)
        {
            if (expectedValue != actualValue)
            {
                Console.WriteLine(WrapText($"FAILURE: {tagName} expected value ({expectedValue}) & actual value ({actualValue}) NOT equal."));
                return 1;
            }
            else
            {
                Console.Write(WrapText($"SUCCESS: {tagName} expected value ({expectedValue}) & actual value ({actualValue}) EQUAL."));
                return 0;
            }
        }
        #endregion
    }
}