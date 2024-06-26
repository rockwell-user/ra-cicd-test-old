﻿// ---------------------------------------------------------------------------------------------------------------------------------------------------------------
//
// FileName: Logix_Program.cs
// FileType: Visual C# Source file
// Author : Rockwell Automation
// Created : 2024
// Description : This script provides an example test in a CI/CD pipeline utilizing Studio 5000 Logix Designer SDK and Factory Talk Logix Echo SDK.
//
// ---------------------------------------------------------------------------------------------------------------------------------------------------------------

using Google.Protobuf;
using LogixEcho_ClassLibrary;
using OfficeOpenXml;
using RockwellAutomation.LogixDesigner;
using System.Text;
using static RockwellAutomation.LogixDesigner.LogixProject;
using DataType = RockwellAutomation.LogixDesigner.LogixProject.DataType;

namespace TestStage_CICDExample
{
    /// <summary>
    /// Class containing the CI/CD Deploy Script & Studio 5000 Logix Designer SDK methods.
    /// </summary>
    internal class CICDDeployProgram
    {
        /// <summary>
        /// Script that deploys the working code for the Logix integrated CI/CD pipeline.<br/>
        /// Uses the LDSDK to deploy to a controller. For this demonstration, the EchoSDK is used to emulate the target controller.
        /// </summary>
        /// <param name="args">
        /// The input arguments needed to execute the script in string array format.<br/>
        /// The string array should have two elements:<br/>
        /// args[0] = The file path to the local GitHub folder (example format: C:\Users\TestUser\Desktop\example-github-repo\).<br/>
        /// args[1] = The name of the acd file that is under development (example format: acd_filename.ACD).<br/>
        /// args[2] = The name of the person associated with the most recent git commit (example format: "Allen Bradley").<br/>
        /// args[3] = The email of the person associated with the most recent git commit (example format: exampleemail@rockwellautomation.com).<br/>
        /// args[4] = The message of the most recent git commit (example format: "Added XYZ functionality to #_Valve_Program").<br/>
        /// args[5] = The hash ID of the most recent git commit (example format: 85df4eda88c992a130484515fee4eec63d14913d).<br/>
        /// args[6] = The name of the Jenkins job being run (example format: Jenkins-CICD-Example).<br/>
        /// args[7] = The number of the Jenkins job being run (example format: 218).
        /// </param>
        /// <returns>A console printout of either "SUCCESS" or "FAILURE". Two test reports are also generated, one in txt format and the other in excel format.</returns>
        static async Task Main(string[] args)
        {
            // Pass the incoming executable arguments for TestScript_ConsoleApplication.exe
            #region PARSING INCOMING VARIABLES WHEN RUNNING PROJECT EXECUTABLE --------------------------------------------------------------------------------
            if (args.Length != 8)
            {
                CreateBanner("INCORRECT NUMBER OF INPUTS");
                Console.Write("Correct Command: ".PadRight(20, ' ') + WrapText(@".\DeployScript_ConsoleApplication githubPath acdFilename name_mostRecentCommit " +
                                  "email_mostRecentCommit message_mostRecentCommit hash_mostRecentCommit jenkinsJobName jenkinsBuildNumber", 20, 105));
                Console.Write("Example Format: ".PadRight(20, ' ') + WrapText(@".\DeployScript_ConsoleApplication C:\Users\TestUser\Desktop\example-github-repo\ " +
                                  "acd_filename.ACD Allen Bradley' example@gmail.com 'Most recent commit message insert here' " +
                                  "287bb2c93a2d1c99143d233fd3ed70cdb997f149 Jenkins-CICD-Example 218", 20, 105));
                CreateBanner("END");
            }
            string githubPath = args[0];                                           // 1st incoming argument = GitHub folder path
            string acdFilename = args[1];                                          // 2nd incoming argument = Logix Designer ACD filename
            string name_mostRecentCommit = args[2];                                // 3rd incoming argument = name of person assocatied with most recent git commit
            string email_mostRecentCommit = args[3];                               // 4th incoming argument = email of person associated with most recent git commit
            string message_mostRecentCommit = args[4];                             // 5th incoming argument = message provided in the most recent git commit
            string hash_mostRecentCommit = args[5];                                // 6th incoming argument = hash ID from most recent git commit
            string jenkinsJobName = args[6];                                       // 7th incoming argument = the Jenkins job name
            string jenkinsBuildNumber = args[7];                                   // 8th incoming argument = the Jenkins job build number
            string acdFilePath = githubPath + @"DEVELOPMENT-files\" + acdFilename; // file path to ACD project
            string textFileReportDirectory = githubPath + @"test-reports\textFiles\";   // folder path to text test reports
            string excelFileReportDirectory = githubPath + @"test-reports\excelFiles\"; // folder path to excel test reports
            string textFileReportPath = Path.Combine(textFileReportDirectory, DateTime.Now.ToString("yyyyMMddHHmmss") + "_testfile.txt");    // new text test report filename
            string excelFileReportPath = Path.Combine(excelFileReportDirectory, DateTime.Now.ToString("yyyyMMddHHmmss") + "_testfile.xlsx"); // new excel test report filename
            #endregion

            // Create new test report file (.txt) using the Console printout.
            #region FILE CREATION -----------------------------------------------------------------------------------------------------------------------------
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                ostrm = new FileStream(textFileReportPath, FileMode.OpenOrCreate, FileAccess.Write);
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

            // GitHub Information
            CreateBanner("GITHUB INFORMATION");
            Console.WriteLine("Test initiated by: ".PadRight(40, ' ') + name_mostRecentCommit);
            Console.WriteLine("Tester contact information: ".PadRight(40, ' ') + email_mostRecentCommit);
            Console.WriteLine("Git commit hash to be verified: ".PadRight(40, ' ') + hash_mostRecentCommit);
            Console.Write("Git commit message to be verified: ".PadRight(40, ' ') + WrapText(message_mostRecentCommit, 40, 85));

            // Print out relevant test information.
            CreateBanner("TEST DEPENDENCIES");
            Console.WriteLine("Jenkins job being executed: ".PadRight(40, ' ') + jenkinsJobName);
            Console.WriteLine("Jenkins job build number: ".PadRight(40, ' ') + jenkinsBuildNumber);
            Console.WriteLine("ACD file path specified: ".PadRight(40, ' ') + acdFilePath);
            Console.WriteLine("Common Language Runtime version: ".PadRight(40, ' ') + typeof(string).Assembly.ImageRuntimeVersion);
            Console.WriteLine("LDSDK .NET Framework version: ".PadRight(40, ' ') + "8.0");
            Console.WriteLine("Echo SDK .NET Framework version: ".PadRight(40, ' ') + "6.0");
            #endregion
            #region STEP: Staging Test (folder cleanup -> Logix Echo emulation -> open ACD -> to program mode -> download ACD -> to run mode)
            CreateBanner("STAGING TEST");

            // Create an excel test report to be filled out durring testing.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START setting up excel test report workbook...");
            CreateFormattedExcelFile(excelFileReportPath, acdFilePath, name_mostRecentCommit, email_mostRecentCommit,
                jenkinsBuildNumber, jenkinsJobName, hash_mostRecentCommit, message_mostRecentCommit);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE setting up excel test report workbook...\n---");

            // Check the test-reports folder and if over the specified file number limit, delete the oldest test files.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START checking test-reports folder...");
            CleanTestReportsFolder(textFileReportDirectory, 5);
            CleanTestReportsFolder(excelFileReportDirectory, 5);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE checking test-reports folder...\n---");

            // Set up emulated controller (based on the specified ACD file path) if one does not yet exist. If not, continue.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START setting up Factory Talk Logix Echo emulated controller...");
            string commPath = SetUpEmulatedController_Sync(acdFilePath);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE setting up Factory Talk Logix Echo emulated controller\n---");

            // Open the ACD project file and store the reference as myProject.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START opening ACD file...");
            LogixProject myProject = await LogixProject.OpenLogixProjectAsync(acdFilePath);
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

            // Add the initial tag values to a new sheet within the existing test report excel workbook
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START adding initial project tag values to excel report...");
            CreateInitialTagValuesSheet(excelFileReportPath);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 2, TEST_BOOL[0], "BOOL", TEST_BOOL[1], TEST_BOOL[2]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 3, TEST_SINT[0], "SINT", TEST_SINT[1], TEST_SINT[2]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 4, TEST_INT[0], "INT", TEST_INT[1], TEST_INT[2]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 5, TEST_DINT[0], "DINT", TEST_DINT[1], TEST_DINT[2]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 6, TEST_LINT[0], "LINT", TEST_LINT[1], TEST_LINT[2]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 7, TEST_REAL[0], "REAL", TEST_REAL[1], TEST_REAL[2]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 8, TEST_STRING[0], "STRING", TEST_STRING[1], TEST_STRING[2]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 9, TEST_TOGGLE_WetBulbTempCalc[0], "BOOL", TEST_TOGGLE_WetBulbTempCalc[1], TEST_TOGGLE_WetBulbTempCalc[2]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 10, TEST_AOI_WetBulbTemp_isFahrenheit[0], "BOOL", TEST_AOI_WetBulbTemp_isFahrenheit[1], TEST_AOI_WetBulbTemp_isFahrenheit[2]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 11, TEST_AOI_WetBulbTemp_RelativeHumidity[0], "REAL", TEST_AOI_WetBulbTemp_RelativeHumidity[1], TEST_AOI_WetBulbTemp_RelativeHumidity[2]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 12, TEST_AOI_WetBulbTemp_Temperature[0], "REAL", TEST_AOI_WetBulbTemp_Temperature[1], TEST_AOI_WetBulbTemp_Temperature[2]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 13, UDT_AllAtomicDataTypes[0][0], "BOOL", UDT_AllAtomicDataTypes[1][0], UDT_AllAtomicDataTypes[2][0]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 14, UDT_AllAtomicDataTypes[0][1], "BOOL", UDT_AllAtomicDataTypes[1][1], UDT_AllAtomicDataTypes[2][1]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 15, UDT_AllAtomicDataTypes[0][2], "BOOL", UDT_AllAtomicDataTypes[1][2], UDT_AllAtomicDataTypes[2][2]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 16, UDT_AllAtomicDataTypes[0][3], "BOOL", UDT_AllAtomicDataTypes[1][3], UDT_AllAtomicDataTypes[2][3]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 17, UDT_AllAtomicDataTypes[0][4], "BOOL", UDT_AllAtomicDataTypes[1][4], UDT_AllAtomicDataTypes[2][4]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 18, UDT_AllAtomicDataTypes[0][5], "BOOL", UDT_AllAtomicDataTypes[1][5], UDT_AllAtomicDataTypes[2][5]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 19, UDT_AllAtomicDataTypes[0][6], "BOOL", UDT_AllAtomicDataTypes[1][6], UDT_AllAtomicDataTypes[2][6]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 20, UDT_AllAtomicDataTypes[0][7], "BOOL", UDT_AllAtomicDataTypes[1][7], UDT_AllAtomicDataTypes[2][7]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 21, UDT_AllAtomicDataTypes[0][8], "SINT", UDT_AllAtomicDataTypes[1][8], UDT_AllAtomicDataTypes[2][8]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 22, UDT_AllAtomicDataTypes[0][9], "INT", UDT_AllAtomicDataTypes[1][9], UDT_AllAtomicDataTypes[2][9]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 23, UDT_AllAtomicDataTypes[0][10], "DINT", UDT_AllAtomicDataTypes[1][10], UDT_AllAtomicDataTypes[2][10]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 24, UDT_AllAtomicDataTypes[0][11], "LINT", UDT_AllAtomicDataTypes[1][11], UDT_AllAtomicDataTypes[2][11]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 25, UDT_AllAtomicDataTypes[0][12], "REAL", UDT_AllAtomicDataTypes[1][12], UDT_AllAtomicDataTypes[2][12]);
            AddRowToSheet(excelFileReportPath, "InitialTagValues", 26, UDT_AllAtomicDataTypes[0][13], "STRING", UDT_AllAtomicDataTypes[1][13], UDT_AllAtomicDataTypes[2][13]);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE adding initial project tag values to excel report\n---");

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
            Set_TagValue_Sync(TEST_BOOL[0], "True", OperationMode.Online, DataType.BOOL, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_SINT[0], "24", OperationMode.Online, DataType.SINT, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_INT[0], "-20500", OperationMode.Online, DataType.INT, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_DINT[0], "2000111000", OperationMode.Online, DataType.DINT, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_LINT[0], "9000111000111000111", OperationMode.Online, DataType.LINT, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_REAL[0], "-10555.888", OperationMode.Online, DataType.REAL, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_STRING[0], "1st New String!", OperationMode.Online, DataType.STRING, filePath_ControllerScope, myProject, true);
            Set_TagValue_Sync(TEST_TOGGLE_WetBulbTempCalc[0], "True", OperationMode.Online, DataType.BOOL, filePath_MainProgram, myProject, true);
            Set_TagValue_Sync(TEST_AOI_WetBulbTemp_isFahrenheit[0], "True", OperationMode.Online, DataType.BOOL, filePath_MainProgram, myProject, true);
            Set_TagValue_Sync(TEST_AOI_WetBulbTemp_RelativeHumidity[0], "30", OperationMode.Online, DataType.REAL, filePath_MainProgram, myProject, true);
            Set_TagValue_Sync(TEST_AOI_WetBulbTemp_Temperature[0], "70", OperationMode.Online, DataType.REAL, filePath_MainProgram, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("-24", DataType.SINT, OperationMode.Online, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("20500", DataType.INT, OperationMode.Online, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("-2000111000", DataType.DINT, OperationMode.Online, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("-9000111000111000111", DataType.LINT, OperationMode.Online, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("10555.888", DataType.REAL, OperationMode.Online, myProject, true);
            Set_UDTAllAtomicDataTypes_Sync("2nd New String!", DataType.STRING, OperationMode.Online, myProject, true);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE setting tag values\n---");

            // Verify expected tag values based on the tag values that had been set.
            // Each test returns a value of 0 for a success or 1 for a failure. The integer failure conditions tracks this tests progress.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START verifying expected tag outputs...");
            // The tag TEST_AOI_WetBulbTemp_WetBulbTemp is an AOI output. This showcases a test command best.
            TEST_AOI_WetBulbTemp_WetBulbTemp = Get_TagValue_Sync(TEST_AOI_WetBulbTemp_WetBulbTemp[0], DataType.REAL, filePath_MainProgram, myProject, false);
            failureCondition += TEST_CompareForExpectedValue("TEST_AOI_WetBulbTemp_WetBulbTemp", "52.997536", TEST_AOI_WetBulbTemp_WetBulbTemp[1]);
            // The below tests are not outputs from logic created in Studio 5000 Logix Designer but are included
            // to provide an example of how each basic data type tag was successfully set.
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
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_SINT", "-24", UDT_AllAtomicDataTypes[1][8]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_INT", "20500", UDT_AllAtomicDataTypes[1][9]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_DINT", "-2000111000", UDT_AllAtomicDataTypes[1][10]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_LINT", "-9000111000111000111", UDT_AllAtomicDataTypes[1][11]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_REAL", "10555.888", UDT_AllAtomicDataTypes[1][12]);
            failureCondition += TEST_CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_STRING", "2nd New String!", UDT_AllAtomicDataTypes[1][13]);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE verifying expected tag outputs\n---");

            // Truth table test 
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START testing boolean logic with truth table generation...");
            string[] all5BitCombinations = GenerateBitCombinations(5);
            string[] results = ["False", "False", "False", "False", "False", "True", "True", "True", "False", "True", "True", "True", "False", "True", "True", "True",
                "False", "False", "False", "False", "False", "False", "False", "False", "False", "False", "False", "False", "False", "False", "False", "False"];
            string[] truthTableResults = TEST_TruthTable(all5BitCombinations, results, filePath_ControllerScope, myProject);
            failureCondition += Convert.ToInt16(truthTableResults[0]);
            CreateTruthTableSheet(excelFileReportPath, truthTableResults);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE testing boolean logic with truth table generation...\n---");

            // Show final tag values.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START showing final test tag values...");
            Get_TagValue_Sync(TEST_BOOL[0], DataType.BOOL, filePath_ControllerScope, myProject, true);
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
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE showing final test tag values\n---");

            // Add the initial tag values to a new sheet within the existing test report excel workbook
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START adding final project tag values to excel report...");
            CreateFinalTagValuesSheet(excelFileReportPath);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 2, TEST_BOOL[0], "BOOL", TEST_BOOL[1], TEST_BOOL[2]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 3, TEST_SINT[0], "SINT", TEST_SINT[1], TEST_SINT[2]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 4, TEST_INT[0], "INT", TEST_INT[1], TEST_INT[2]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 5, TEST_DINT[0], "DINT", TEST_DINT[1], TEST_DINT[2]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 6, TEST_LINT[0], "LINT", TEST_LINT[1], TEST_LINT[2]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 7, TEST_REAL[0], "REAL", TEST_REAL[1], TEST_REAL[2]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 8, TEST_STRING[0], "STRING", TEST_STRING[1], TEST_STRING[2]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 9, TEST_TOGGLE_WetBulbTempCalc[0], "BOOL", TEST_TOGGLE_WetBulbTempCalc[1], TEST_TOGGLE_WetBulbTempCalc[2]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 10, TEST_AOI_WetBulbTemp_isFahrenheit[0], "BOOL", TEST_AOI_WetBulbTemp_isFahrenheit[1], TEST_AOI_WetBulbTemp_isFahrenheit[2]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 11, TEST_AOI_WetBulbTemp_RelativeHumidity[0], "REAL", TEST_AOI_WetBulbTemp_RelativeHumidity[1], TEST_AOI_WetBulbTemp_RelativeHumidity[2]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 12, TEST_AOI_WetBulbTemp_Temperature[0], "REAL", TEST_AOI_WetBulbTemp_Temperature[1], TEST_AOI_WetBulbTemp_Temperature[2]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 13, UDT_AllAtomicDataTypes[0][0], "BOOL", UDT_AllAtomicDataTypes[1][0], UDT_AllAtomicDataTypes[2][0]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 14, UDT_AllAtomicDataTypes[0][1], "BOOL", UDT_AllAtomicDataTypes[1][1], UDT_AllAtomicDataTypes[2][1]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 15, UDT_AllAtomicDataTypes[0][2], "BOOL", UDT_AllAtomicDataTypes[1][2], UDT_AllAtomicDataTypes[2][2]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 16, UDT_AllAtomicDataTypes[0][3], "BOOL", UDT_AllAtomicDataTypes[1][3], UDT_AllAtomicDataTypes[2][3]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 17, UDT_AllAtomicDataTypes[0][4], "BOOL", UDT_AllAtomicDataTypes[1][4], UDT_AllAtomicDataTypes[2][4]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 18, UDT_AllAtomicDataTypes[0][5], "BOOL", UDT_AllAtomicDataTypes[1][5], UDT_AllAtomicDataTypes[2][5]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 19, UDT_AllAtomicDataTypes[0][6], "BOOL", UDT_AllAtomicDataTypes[1][6], UDT_AllAtomicDataTypes[2][6]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 20, UDT_AllAtomicDataTypes[0][7], "BOOL", UDT_AllAtomicDataTypes[1][7], UDT_AllAtomicDataTypes[2][7]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 21, UDT_AllAtomicDataTypes[0][8], "SINT", UDT_AllAtomicDataTypes[1][8], UDT_AllAtomicDataTypes[2][8]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 22, UDT_AllAtomicDataTypes[0][9], "INT", UDT_AllAtomicDataTypes[1][9], UDT_AllAtomicDataTypes[2][9]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 23, UDT_AllAtomicDataTypes[0][10], "DINT", UDT_AllAtomicDataTypes[1][10], UDT_AllAtomicDataTypes[2][10]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 24, UDT_AllAtomicDataTypes[0][11], "LINT", UDT_AllAtomicDataTypes[1][11], UDT_AllAtomicDataTypes[2][11]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 25, UDT_AllAtomicDataTypes[0][12], "REAL", UDT_AllAtomicDataTypes[1][12], UDT_AllAtomicDataTypes[2][12]);
            AddRowToSheet(excelFileReportPath, "FinalTagValues", 26, UDT_AllAtomicDataTypes[0][13], "STRING", UDT_AllAtomicDataTypes[1][13], UDT_AllAtomicDataTypes[2][13]);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE adding final project tag values to excel report");

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
        /// Modify the input string to wrap the text to the next line after a certain length.<br/>
        /// The input string is seperated per word and then each line is incrementally added to per word.<br/>
        /// Start a new line when the character count of a line exceeds 125.
        /// </summary>
        /// <param name="inputString">The input string to be wrapped.</param>
        /// <param name="indentLength">An integer that defines the length of the characters in the indent starting each new line.</param>
        /// <param name="lineLimit">An integer that defines the maximum number of characters per line before a new line is created.</param>
        /// <returns>A modified string that wraps every 125 characters.</returns>
        private static string WrapText(string inputString, int indentLength, int lineLimit)
        {
            string[] words = inputString.Split(' ');
            string indent = new(' ', indentLength);
            StringBuilder newSentence = new();
            string line = "";
            int numberOfNewLines = 0;
            foreach (string word in words)
            {
                word.Trim();
                if ((line + word).Length > lineLimit)
                {
                    if (numberOfNewLines == 0)
                        newSentence.AppendLine(line);
                    else
                        newSentence.AppendLine(indent + line);
                    line = "";
                    numberOfNewLines++;
                }
                line += string.Format($"{word} ");
            }
            if (line.Length > 0)
            {
                if (numberOfNewLines > 0)
                    newSentence.AppendLine(indent + line);
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
            Console.WriteLine($"STATUS:  {folderPath} set to retain {keepCount} test files");
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
            string[] tall_files = Directory.GetFiles(folderPath);
            var torderedFiles = all_files.Select(f => new FileInfo(f)).OrderBy(f => f.CreationTime).ToList();
        }
        #endregion

        #region METHODS: formatting excel file
        /// <summary>
        /// Convert the integer number of a column to the Microsoft Excel letter formatting.
        /// </summary>
        /// <param name="columnNumber">Integer input to convert to excel letter formatting.</param>
        /// <returns>A string in excel letter formatting.</returns>
        private static string ConvertToExcelColumn(int columnNumber)
        {
            string columnName = "";

            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }

            return columnName;
        }

        /// <summary>
        /// Initialize the Mircosoft Excel test report with one sheet containing test information.
        /// </summary>
        /// <param name="excelFilePath">The file path to the excel workbook containing the test results.</param>
        /// <param name="acdFilePath">The file path to the Studio 5000 Logix Designer ACD file being tested.</param>
        /// <param name="name">The name of the person starting the test.</param>
        /// <param name="email">The email of the person starting the test.</param>
        /// <param name="buildNumber">The Jenkins job build number.</param>
        /// <param name="jobName">The Jenkins job name.</param>
        /// <param name="gitHash">The git hash of the commit being tested.</param>
        /// <param name="gitMessage">The git message of the commit being tested.</param>
        private static void CreateFormattedExcelFile(string excelFilePath, string acdFilePath, string name, string email, string buildNumber, string jobName, string gitHash, string gitMessage)
        {
            ExcelPackage excelPackage = new ExcelPackage();
            var returnWorkBook = excelPackage.Workbook;

            var TestSummarySheet = returnWorkBook.Worksheets.Add("TestSummary");
            TestSummarySheet.Cells["B2:C2"].Merge = true;
            TestSummarySheet.Cells["B2"].Value = "CI/CD Test Stage Results";
            TestSummarySheet.Cells["B2"].Style.Font.Bold = true;
            TestSummarySheet.Cells["B3"].Value = "Jenkins job name:";
            TestSummarySheet.Cells["C3"].Value = jobName;
            TestSummarySheet.Cells["B4"].Value = "Jenkins job build number:";
            TestSummarySheet.Cells["C4"].Value = buildNumber;
            TestSummarySheet.Cells["B5"].Value = "Tester name:";
            TestSummarySheet.Cells["C5"].Value = name;
            TestSummarySheet.Cells["B6"].Value = "Tester contact:";
            TestSummarySheet.Cells["C6"].Value = email;
            TestSummarySheet.Cells["B7"].Value = "ACD file specified:";
            TestSummarySheet.Cells["C7"].Value = acdFilePath;
            TestSummarySheet.Cells["B8"].Value = "Git commit hash to be verified:";
            TestSummarySheet.Cells["C8"].Value = gitHash;
            TestSummarySheet.Cells["B9"].Value = "Git commit message to be verified:";
            TestSummarySheet.Cells["C9"].Value = gitMessage;

            TestSummarySheet.Column(2).Style.Font.Size = 14;
            TestSummarySheet.Column(2).AutoFit();
            TestSummarySheet.Column(2).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            TestSummarySheet.Column(2).Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
            TestSummarySheet.Cells["B2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            TestSummarySheet.Cells["B2"].Style.Font.Size = 20;

            TestSummarySheet.Column(3).Style.Font.Size = 14;
            TestSummarySheet.Column(3).Width = 95;
            TestSummarySheet.Column(3).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            TestSummarySheet.Cells["C9"].Style.WrapText = true;
            TestSummarySheet.Cells["C9"].Style.ShrinkToFit = true;

            excelPackage.SaveAs(new System.IO.FileInfo(excelFilePath));
        }

        /// <summary>
        /// Create the InitialTagValues sheet in the existing Microsoft Excel workbook.
        /// </summary>
        /// <param name="excelFilePath">The file path to the excel workbook containing the test results.</param>
        private static void CreateInitialTagValuesSheet(string excelFilePath)
        {
            FileInfo existingFile = new FileInfo(excelFilePath);
            using (ExcelPackage excelPackage = new ExcelPackage(existingFile))
            {
                var InitialTagValues = excelPackage.Workbook.Worksheets.Add("InitialTagValues");

                InitialTagValues.Cells["A1"].Value = "Tag Name";
                InitialTagValues.Cells["B1"].Value = "Tag Type";
                InitialTagValues.Cells["C1"].Value = "Online Value";
                InitialTagValues.Cells["D1"].Value = "Offline Value";
                InitialTagValues.Cells["A1:D1"].Style.Font.Bold = true;

                for (int i = 1; i <= 4; i++)
                    InitialTagValues.Column(i).AutoFit();

                InitialTagValues.View.FreezePanes(2, 1);
                excelPackage.Save();
            }
        }

        /// <summary>
        /// Create the TurthTable_Example sheet and add the results of the MainRoutine Rung 3 test.
        /// </summary>
        /// <param name="excelFilePath">The file path to the excel workbook containing the test results.</param>
        /// <param name="truthTableResults">The string array containing every boolean combination result from MainRoutine Rung 3 testing.</param>
        private static void CreateTruthTableSheet(string excelFilePath, string[] truthTableResults)
        {
            FileInfo existingFile = new FileInfo(excelFilePath);
            using (ExcelPackage excelPackage = new ExcelPackage(existingFile))
            {
                var TruthTable_ExampleSheet = excelPackage.Workbook.Worksheets.Add("TruthTable_Example");
                TruthTable_ExampleSheet.View.FreezePanes(2, 2);
                TruthTable_ExampleSheet.Cells["A1"].Value = "Rung 3 of Main Program Test Cases";
                TruthTable_ExampleSheet.Cells["A1"].Style.Font.Bold = true;
                TruthTable_ExampleSheet.Cells["A2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                TruthTable_ExampleSheet.Cells["A3"].Value = "UDT_AllAtomicDataTypes.ex_BOOL1";
                TruthTable_ExampleSheet.Cells["A4"].Value = "UDT_AllAtomicDataTypes.ex_BOOL2";
                TruthTable_ExampleSheet.Cells["A5"].Value = "UDT_AllAtomicDataTypes.ex_BOOL3";
                TruthTable_ExampleSheet.Cells["A6"].Value = "UDT_AllAtomicDataTypes.ex_BOOL4";
                TruthTable_ExampleSheet.Cells["A7"].Value = "UDT_AllAtomicDataTypes.ex_BOOL5";
                TruthTable_ExampleSheet.Cells["A8"].Value = "UDT_AllAtomicDataTypes.ex_BOOL8";
                int numberOfTests = truthTableResults.Length;
                string columnLetter = "";
                for (int i = 1; i < numberOfTests; i++)
                {
                    columnLetter = ConvertToExcelColumn(i + 1);
                    TruthTable_ExampleSheet.Cells[$"{columnLetter}1"].Value = i;

                    TruthTable_ExampleSheet.Cells[$"{columnLetter}3"].Value = Char.GetNumericValue(truthTableResults[i][7]);
                    TruthTable_ExampleSheet.Cells[$"{columnLetter}4"].Value = Char.GetNumericValue(truthTableResults[i][6]);
                    TruthTable_ExampleSheet.Cells[$"{columnLetter}5"].Value = Char.GetNumericValue(truthTableResults[i][5]);
                    TruthTable_ExampleSheet.Cells[$"{columnLetter}6"].Value = Char.GetNumericValue(truthTableResults[i][4]);
                    TruthTable_ExampleSheet.Cells[$"{columnLetter}7"].Value = Char.GetNumericValue(truthTableResults[i][3]);
                    TruthTable_ExampleSheet.Cells[$"{columnLetter}8"].Value = Char.GetNumericValue(truthTableResults[i][0]);

                    TruthTable_ExampleSheet.Column(i + 1).Width = 4;
                    TruthTable_ExampleSheet.Column(i + 1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                TruthTable_ExampleSheet.Column(1).AutoFit();
                TruthTable_ExampleSheet.Column(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                excelPackage.Save();
            }
        }

        /// <summary>
        /// Create the FinalTagValues sheet in the existing Microsoft Excel workbook.
        /// </summary>
        /// <param name="excelFilePath">The file path to the excel workbook containing the test results.</param>
        private static void CreateFinalTagValuesSheet(string excelFilePath)
        {
            FileInfo existingFile = new FileInfo(excelFilePath);
            using (ExcelPackage excelPackage = new ExcelPackage(existingFile))
            {
                var InitialTagValues = excelPackage.Workbook.Worksheets.Add("FinalTagValues");

                InitialTagValues.Cells["A1"].Value = "Tag Name";
                InitialTagValues.Cells["B1"].Value = "Tag Type";
                InitialTagValues.Cells["C1"].Value = "Online Value";
                InitialTagValues.Cells["D1"].Value = "Offline Value";
                InitialTagValues.Cells["A1:D1"].Style.Font.Bold = true;

                for (int i = 1; i <= 4; i++)
                    InitialTagValues.Column(i).AutoFit();

                InitialTagValues.View.FreezePanes(2, 1);
                excelPackage.Save();
            }
        }

        /// <summary>
        /// Add a row containing tag information to a specific worksheet in an existing excel workbook.
        /// </summary>
        /// <param name="excelFilePath">The file path to the excel workbook containing the test results.</param>
        /// <param name="sheetName">The name of the worksheet being modified.</param>
        /// <param name="rowNumber">The row number of the worksheet to be modified.</param>
        /// <param name="tagName">The name of the tag for which further information is provided.</param>
        /// <param name="dataType">The datatype of the tag.</param>
        /// <param name="onlineValue">The online value of the tag.</param>
        /// <param name="offlineValue">The offline value of the tag.</param>
        private static void AddRowToSheet(string excelFilePath, string sheetName, int rowNumber, string tagName, string dataType, string onlineValue, string offlineValue)
        {
            FileInfo existingFile = new FileInfo(excelFilePath);
            using (ExcelPackage excelPackage = new ExcelPackage(existingFile))
            {
                ExcelWorksheet modifiedSheet = excelPackage.Workbook.Worksheets[sheetName];

                modifiedSheet.Cells[$"A{rowNumber}"].Value = tagName;
                modifiedSheet.Cells[$"B{rowNumber}"].Value = dataType;
                modifiedSheet.Cells[$"C{rowNumber}"].Value = onlineValue;
                modifiedSheet.Cells[$"D{rowNumber}"].Value = offlineValue;

                for (int i = 1; i <= 4; i++)
                    modifiedSheet.Column(i).AutoFit();

                excelPackage.Save();
            }
        }
        #endregion

        #region METHODS: setting up Logix Echo emulated controller
        /// <summary>
        /// Run the Echo_Program script synchronously.<br/>
        /// Script that sets up an emulated controller for CI/CD software in the loop (SIL) testing.<br/>
        /// If no emulated controller based on the ACD file path yet exists, create one, and then return the communication path.<br/>
        /// If an emulated controller based on the ACD file path exists, only return the communication path.
        /// </summary>
        /// <param name="acdFilePath">The file path to the Studio 5000 Logix Designer ACD file being tested.</param>
        /// <returns>A string containing the communication path of the emulated controller that the ACD project file will go online with during testing.</returns>
        private static string SetUpEmulatedController_Sync(string acdFilePath)
        {
            var task = LogixEchoMethods.Main(acdFilePath);
            task.Wait();
            return task.Result;
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
        /// Asynchronously get the online and offline value of a basic data type tag.<br/>
        /// (basic data types handled: boolean, single integer, integer, double integer, long integer, real, string)
        /// </summary>
        /// <param name="tagName">The name of the tag in Studio 5000 Logix Designer whose value will be returned.</param>
        /// <param name="type">The data type of the tag whose value will be returned.</param>
        /// <param name="tagPath">
        /// The tag path specifying the tag's scope and location in the Studio 5000 Logix Designer project.<br/>
        /// The tag path is based on the XML filetype (L5X) encapsulation of elements.
        /// </param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <param name="printout">A boolean that, if True, prints the online and offline values to the console.</param>
        /// <returns>
        /// A Task that results in a string array containing tag information:<br/>
        /// return_array[0] = tag name<br/>
        /// return_array[1] = online tag value<br/>
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
                    var tagValue_online = await project.GetTagValueBOOLAsync(tagPath, OperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueBOOLAsync(tagPath, OperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";

                }
                else if (type == DataType.SINT)
                {
                    var tagValue_online = await project.GetTagValueSINTAsync(tagPath, OperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueSINTAsync(tagPath, OperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";
                }
                else if (type == DataType.INT)
                {
                    var tagValue_online = await project.GetTagValueINTAsync(tagPath, OperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueINTAsync(tagPath, OperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";
                }
                else if (type == DataType.DINT)
                {
                    var tagValue_online = await project.GetTagValueDINTAsync(tagPath, OperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueDINTAsync(tagPath, OperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";
                }
                else if (type == DataType.LINT)
                {
                    var tagValue_online = await project.GetTagValueLINTAsync(tagPath, OperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueLINTAsync(tagPath, OperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";
                }
                else if (type == DataType.REAL)
                {
                    var tagValue_online = await project.GetTagValueREALAsync(tagPath, OperationMode.Online);
                    return_array[1] = $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueREALAsync(tagPath, OperationMode.Offline);
                    return_array[2] = $"{tagValue_offline}";
                }
                else if (type == DataType.STRING)
                {
                    var tagValue_online = await project.GetTagValueSTRINGAsync(tagPath, OperationMode.Online);
                    return_array[1] = (tagValue_online == "") ? "<empty_string>" : $"{tagValue_online}";
                    var tagValue_offline = await project.GetTagValueSTRINGAsync(tagPath, OperationMode.Offline);
                    return_array[2] = (tagValue_offline == "") ? "<empty_string>" : $"{tagValue_offline}";
                }
                else
                    Console.WriteLine(WrapText($"ERROR executing command: The tag {tagName} cannot be handled. Select either DINT, BOOL, or REAL.", 9, 125));
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
        /// Run the GetTagValueAsync method synchronously.<br/>
        /// Get the online and offline value of a basic data type tag.<br/>
        /// (basic data types handled: boolean, single integer, integer, double integer, long integer, real, string)
        /// </summary>
        /// <param name="tagName">The name of the tag whose value will be returned.</param>
        /// <param name="type">The data type of the tag whose value will be returned.</param>
        /// <param name="tagPath">
        /// The tag path specifying the tag's scope and location in the Studio 5000 Logix Designer project.<br/>
        /// The tag path is based on the XML filetype (L5X) encapsulation of elements.
        /// </param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <param name="printout">A boolean that, if True, prints the online and offline values to the console.</param>
        /// <returns>
        /// A Task that results in a string array containing tag information:<br/>
        /// return_array[0] = tag name<br/>
        /// return_array[1] = online tag value<br/>
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
        private static async Task Set_TagValue_Async(string tagName, string newTagValue, OperationMode mode, DataType type, string tagPath, LogixProject project, bool printout)
        {
            tagPath = tagPath + $"[@Name='{tagName}']";
            string[] old_tag_values = await Get_TagValue_Async(tagName, type, tagPath, project, false);
            string old_tag_value = "";
            try
            {
                if (mode == OperationMode.Online)
                {

                    if (type == DataType.BOOL)
                        await project.SetTagValueBOOLAsync(tagPath, OperationMode.Online, bool.Parse(newTagValue));
                    else if (type == DataType.SINT)
                        await project.SetTagValueSINTAsync(tagPath, OperationMode.Online, sbyte.Parse(newTagValue));
                    else if (type == DataType.INT)
                        await project.SetTagValueINTAsync(tagPath, OperationMode.Online, short.Parse(newTagValue));
                    else if (type == DataType.DINT)
                        await project.SetTagValueDINTAsync(tagPath, OperationMode.Online, int.Parse(newTagValue));
                    else if (type == DataType.LINT)
                        await project.SetTagValueLINTAsync(tagPath, OperationMode.Online, long.Parse(newTagValue));
                    else if (type == DataType.REAL)
                        await project.SetTagValueREALAsync(tagPath, OperationMode.Online, float.Parse(newTagValue));
                    else if (type == DataType.STRING)
                        await project.SetTagValueSTRINGAsync(tagPath, OperationMode.Online, newTagValue);
                    else
                        Console.WriteLine($"ERROR executing command: The data type cannot be handled. Select either DINT, BOOL, or REAL.");
                    old_tag_value = old_tag_values[1];
                }
                else if (mode == OperationMode.Offline)
                {
                    if (type == DataType.BOOL)
                        await project.SetTagValueBOOLAsync(tagPath, OperationMode.Offline, bool.Parse(newTagValue));
                    else if (type == DataType.SINT)
                        await project.SetTagValueSINTAsync(tagPath, OperationMode.Offline, sbyte.Parse(newTagValue));
                    else if (type == DataType.INT)
                        await project.SetTagValueINTAsync(tagPath, OperationMode.Offline, short.Parse(newTagValue));
                    else if (type == DataType.DINT)
                        await project.SetTagValueDINTAsync(tagPath, OperationMode.Offline, int.Parse(newTagValue));
                    else if (type == DataType.LINT)
                        await project.SetTagValueLINTAsync(tagPath, OperationMode.Offline, long.Parse(newTagValue));
                    else if (type == DataType.REAL)
                        await project.SetTagValueREALAsync(tagPath, OperationMode.Offline, float.Parse(newTagValue));
                    else if (type == DataType.STRING)
                        await project.SetTagValueSTRINGAsync(tagPath, OperationMode.Offline, newTagValue);
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
        private static void Set_TagValue_Sync(string tagName, string newTagValue, OperationMode mode, DataType type, string tagPath, LogixProject project, bool printout)
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
        /// The tag path specifying the tag's scope and location in the Studio 5000 Logix Designer project.<br/>
        /// The tag path is based on the XML filetype (L5X) encapsulation of elements.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <returns>
        /// A Task that results in a ByteString array containing UDT_AllAtomicDataTypes information:<br/>
        /// returnByteStringArray[0] = online tag values<br/>
        /// returnByteStringArray[1] = offline tag values
        /// </returns>
        private static async Task<ByteString[]> Get_UDTAllAtomicDataTypes_Async(string tagPath, LogixProject project)
        {
            tagPath = tagPath + $"[@Name='UDT_AllAtomicDataTypes']";
            ByteString[] returnByteStringArray = new ByteString[2];
            returnByteStringArray[0] = await project.GetTagValueAsync(tagPath, OperationMode.Online, DataType.BYTE_ARRAY);
            returnByteStringArray[1] = await project.GetTagValueAsync(tagPath, OperationMode.Offline, DataType.BYTE_ARRAY);
            return returnByteStringArray;
        }

        /// <summary>
        /// Run the GetUDT_AllAtomicDataTypesAsync Method synchronously.<br/>
        /// Get the online and offline data in ByteString form for the complex tag UDT_AllAtomicDataTypes.
        /// </summary>
        /// <param name="tagPath">
        /// The tag path specifying the tag's scope and location in the Studio 5000 Logix Designer project.<br/>
        /// The tag path is based on the XML filetype (L5X) encapsulation of elements.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <returns>
        /// A ByteString array containing UDT_AllAtomicDataTypes information:<br/>
        /// returnByteStringArray[0] = online tag values<br/>
        /// returnByteStringArray[1] = offline tag values
        /// </returns>
        private static ByteString[] Get_UDTAllAtomicDataTypes_Sync(string tagPath, LogixProject project)
        {
            var task = Get_UDTAllAtomicDataTypes_Async(tagPath, project);
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Asynchronously set either the online or offline value of a member of the complex data type tag UDT_AllAtomicDataTypes. Each member is a basic data type.<br/>
        /// (basic data types handled: boolean, single integer, integer, double integer, long integer, real, string)
        /// </summary>
        /// <param name="newTagValue">The value of the UDT_AllAtomicDataTypes tag member that will be set.</param>
        /// <param name="type">>The data type of the UDT_AllAtomicDataTypes tag member whose value will be set.</param>
        /// <param name="mode">This specifies whether the 'Online' or 'Offline' value of the UDT_AllAtomicDataTypes tag member is the one to set.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <param name="printout">A boolean that, if True, prints the "before" and "after" values of the UDT_AllAtomicDataTypes tag member to the console.</param>
        /// <returns>A Task that will set the online or offline value of the specified UDT_AllAtomicDataTypes tag member.</returns>
        private static async Task Set_UDTAllAtomicDataTypes_Async(string newTagValue, DataType type, OperationMode mode, LogixProject project, bool printout)
        {
            string tagPath = $"Controller/Tags/Tag[@Name='UDT_AllAtomicDataTypes']";
            ByteString[] old_byteString = Get_UDTAllAtomicDataTypes_Sync(tagPath, project);
            ByteString[] new_byteString = Get_UDTAllAtomicDataTypes_Sync(tagPath, project);
            string[][] old_UDT_AllAtomicDataTypes = Format_UDTAllAtomicDataTypes(old_byteString, false);
            int on_off = 0;
            byte[] new_byteArray = new byte[new_byteString[1].Length];

            if (mode == OperationMode.Online)
                new_byteArray = new_byteString[0].ToByteArray();
            else if (mode == OperationMode.Offline)
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

            if (mode == OperationMode.Online)
            {
                new_byteString[0] = ByteString.CopyFrom(new_byteArray);
                new_byteString[1] = old_byteString[1];
                on_off = 1;
            }
            else if (mode == OperationMode.Offline)
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
        /// Run the SetUDT_AllAtomicDataTypesAsync Method synchronously.<br/>
        /// Set either the online or offline value of a member of the complex data type tag UDT_AllAtomicDataTypes. Each member is a basic data type.<br/>
        /// (basic data types handled: boolean, single integer, integer, double integer, long integer, real, string)
        /// </summary>
        /// <param name="newTagValue">The value of the UDT_AllAtomicDataTypes tag member that will be set.</param>
        /// <param name="type">>The data type of the UDT_AllAtomicDataTypes tag member whose value will be set.</param>
        /// <param name="mode">This specifies whether the 'Online' or 'Offline' value of the UDT_AllAtomicDataTypes tag member is the one to set.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <param name="printout">A boolean that, if True, prints the "before" and "after" values of the UDT_AllAtomicDataTypes tag member to the console.</param>
        private static void Set_UDTAllAtomicDataTypes_Sync(string newTagValue, DataType type, OperationMode mode, LogixProject project, bool printout)
        {
            var task = Set_UDTAllAtomicDataTypes_Async(newTagValue, type, mode, project, printout);
            task.Wait();
        }

        /// <summary>
        /// Format the complex data type tag UDT_AllAtomicDataTypes from a ByteString to a nested string array.
        /// </summary>
        /// <param name="byteStringArray">
        /// This input parameter is the Get_UDTAllAtomicDataTypes_Async method output or the Get_UDTAllAtomicDataTypes_Sync method output.<br/>
        /// The ByteString array has the following format:<br/>
        /// byteStringArray[0] = ByteString for online complex data type tag values<br/>
        /// byteStringArray[1] = ByteString for offline complex data type tag values
        /// </param>
        /// <param name="printout">A boolean that, if True, prints the "before" and "after" values of the UDT_AllAtomicDataTypes tag member to the console.</param>
        /// <returns>
        /// A nested string array with the following format:<br/>
        /// returnString[0][] = the names of the 14 members of the UDT_AllAtomicDataTypes complex data type tag in the order they were made in Studio 5000 Logix Designer<br/>
        /// returnString[1][] = the online values of the 14 members of the UDT_AllAtomicDataTypes complex data type tag in the order they were made in Studio 5000 Logix Designer<br/>
        /// returnString[2][] = the offline values of the 14 members of the UDT_AllAtomicDataTypes complex data type tag in the order they were made in Studio 5000 Logix Designer
        /// <para>
        /// Every element of the nested string array:<br/>
        /// returnString[0][0] = UDT_AllAtomicDataTypes.ex_BOOL1 name<br/>
        /// returnString[0][1] = UDT_AllAtomicDataTypes.ex_BOOL2 name<br/>
        /// returnString[0][2] = UDT_AllAtomicDataTypes.ex_BOOL3 name<br/>
        /// returnString[0][3] = UDT_AllAtomicDataTypes.ex_BOOL4 name<br/>
        /// returnString[0][4] = UDT_AllAtomicDataTypes.ex_BOOL5 name<br/>
        /// returnString[0][5] = UDT_AllAtomicDataTypes.ex_BOOL5 name<br/>
        /// returnString[0][6] = UDT_AllAtomicDataTypes.ex_BOOL6 name<br/>
        /// returnString[0][7] = UDT_AllAtomicDataTypes.ex_BOOL7 name<br/>
        /// returnString[0][8] = UDT_AllAtomicDataTypes.ex_BOOL8 name<br/>
        /// returnString[0][9] = UDT_AllAtomicDataTypes.ex_SINT name<br/>
        /// returnString[0][10] = UDT_AllAtomicDataTypes.ex_INT name<br/>
        /// returnString[0][11] = UDT_AllAtomicDataTypes.ex_DINT name<br/>
        /// returnString[0][12] = UDT_AllAtomicDataTypes.ex_LINT name<br/>
        /// returnString[0][13] = UDT_AllAtomicDataTypes.ex_REAL name<br/>
        /// returnString[0][14] = UDT_AllAtomicDataTypes.ex_STRING name<br/>
        /// returnString[1][0] = UDT_AllAtomicDataTypes.ex_BOOL1 online value<br/>
        /// returnString[1][1] = UDT_AllAtomicDataTypes.ex_BOOL2 online value<br/>
        /// returnString[1][2] = UDT_AllAtomicDataTypes.ex_BOOL3 online value<br/>
        /// returnString[1][3] = UDT_AllAtomicDataTypes.ex_BOOL4 online value<br/>
        /// returnString[1][4] = UDT_AllAtomicDataTypes.ex_BOOL5 online value<br/>
        /// returnString[1][5] = UDT_AllAtomicDataTypes.ex_BOOL5 online value<br/>
        /// returnString[1][6] = UDT_AllAtomicDataTypes.ex_BOOL6 online value<br/>
        /// returnString[1][7] = UDT_AllAtomicDataTypes.ex_BOOL7 online value<br/>
        /// returnString[1][8] = UDT_AllAtomicDataTypes.ex_BOOL8 online value<br/>
        /// returnString[1][9] = UDT_AllAtomicDataTypes.ex_SINT online value<br/>
        /// returnString[1][10] = UDT_AllAtomicDataTypes.ex_INT online value<br/>
        /// returnString[1][11] = UDT_AllAtomicDataTypes.ex_DINT online value<br/>
        /// returnString[1][12] = UDT_AllAtomicDataTypes.ex_LINT online value<br/>
        /// returnString[1][13] = UDT_AllAtomicDataTypes.ex_REAL online value<br/>
        /// returnString[1][14] = UDT_AllAtomicDataTypes.ex_STRING online value<br/>
        /// returnString[1][15] = the number of bytes in element 0 of the input ByteString array (online values)<br/>
        /// returnString[2][0] = UDT_AllAtomicDataTypes.ex_BOOL1 offline value<br/>
        /// returnString[2][1] = UDT_AllAtomicDataTypes.ex_BOOL2 offline value<br/>
        /// returnString[2][2] = UDT_AllAtomicDataTypes.ex_BOOL3 offline value<br/>
        /// returnString[2][3] = UDT_AllAtomicDataTypes.ex_BOOL4 offline value<br/>
        /// returnString[2][4] = UDT_AllAtomicDataTypes.ex_BOOL5 offline value<br/>
        /// returnString[2][5] = UDT_AllAtomicDataTypes.ex_BOOL5 offline value<br/>
        /// returnString[2][6] = UDT_AllAtomicDataTypes.ex_BOOL6 offline value<br/>
        /// returnString[2][7] = UDT_AllAtomicDataTypes.ex_BOOL7 offline value<br/>
        /// returnString[2][8] = UDT_AllAtomicDataTypes.ex_BOOL8 offline value<br/>
        /// returnString[2][9] = UDT_AllAtomicDataTypes.ex_SINT offline value<br/>
        /// returnString[2][10] = UDT_AllAtomicDataTypes.ex_INT offline value<br/>
        /// returnString[2][11] = UDT_AllAtomicDataTypes.ex_DINT offline value<br/>
        /// returnString[2][12] = UDT_AllAtomicDataTypes.ex_LINT offline value<br/>
        /// returnString[2][13] = UDT_AllAtomicDataTypes.ex_REAL offline value<br/>
        /// returnString[2][14] = UDT_AllAtomicDataTypes.ex_STRING offline value<br/>
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

        #region METHODS: different types of testing -> comparisons & truth tables
        /// <summary>
        /// A test to compare the online and offline values of a tag.
        /// </summary>
        /// <param name="tagName">The name of the tag to be tested.</param>
        /// <param name="onlineValue">The online value of the tag under test.</param>
        /// <param name="offlineValue">The offline value of the tag under test.</param>
        /// <returns>Return an integer value 1 for test failure and an integer value 0 for test success.</returns>
        /// <remarks>
        /// The integer output is added to an integer that tracks the total number of failures.<br/>
        /// At the end of all testing, the overall SUCCESS/FAILURE of this CI/CD test stage is determined whether its value is greater than 0.
        /// </remarks>
        private static int TEST_CompareOnlineOffline(string tagName, string onlineValue, string offlineValue)
        {
            if (onlineValue != offlineValue)
            {
                Console.Write(WrapText($"FAILURE: {tagName} online value ({onlineValue}) & offline value ({offlineValue}) NOT equal.", 9, 125));
                return 1;
            }
            else
            {
                Console.Write(WrapText($"SUCCESS: {tagName} online value ({onlineValue}) & offline value ({offlineValue}) are EQUAL.", 9, 125));
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
        /// The integer output is added to an integer that tracks the total number of failures.<br/>
        /// At the end of all testing, the overall SUCCESS/FAILURE of this CI/CD test stage is determined whether its value is greater than 0.
        /// </remarks>
        private static int TEST_CompareForExpectedValue(string tagName, string expectedValue, string actualValue)
        {
            if (expectedValue != actualValue)
            {
                Console.Write(WrapText($"FAILURE: {tagName} expected value ({expectedValue}) & actual value ({actualValue}) NOT equal.", 9, 125));
                return 1;
            }
            else
            {
                Console.Write(WrapText($"SUCCESS: {tagName} expected value ({expectedValue}) & actual value ({actualValue}) EQUAL.", 9, 125));
                return 0;
            }
        }

        /// <summary>
        /// A test to verify the boolean logic of a rung.
        /// </summary>
        /// <param name="everyBinaryStringCombination">A string array that contains every combination possible for a binary string of specific length.</param>
        /// <param name="truthTableResults">A string array containing the boolean value expected for each element of the everyBinaryStringCombination array.</param>
        /// <param name="tagPath">The tag path for the boolean values being manipulated.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <returns>
        /// The integer output is added to an integer that tracks the total number of failures.<br/>
        /// At the end of all testing, the overall SUCCESS/FAILURE of this CI/CD test stage is determined whether its value is greater than 0.
        /// </returns>
        private static string[] TEST_TruthTable(string[] everyBinaryStringCombination, string[] truthTableResults, string tagPath, LogixProject project)
        {
            int testResult = 0;
            int testsToRun = everyBinaryStringCombination.Length;
            string[] returnString = new string[everyBinaryStringCombination.Length + 1];
            for (int i = 0; i < testsToRun; i++)
            {
                string fullBinaryStringCombination = everyBinaryStringCombination[i].PadLeft(8, '0');
                Set_UDTAllAtomicDataTypes_Sync(fullBinaryStringCombination, DataType.BOOL, OperationMode.Online, project, false);

                ByteString[] ByteString_UDT_AllAtomicDataTypes = Get_UDTAllAtomicDataTypes_Sync(tagPath, project);
                var BOOL_ByteArray = ByteString_UDT_AllAtomicDataTypes[0].ToByteArray();
                string ex_BOOLS_binaryString = Convert.ToString(BOOL_ByteArray[0], 2).PadLeft(8, '0');
                returnString[i + 1] = ex_BOOLS_binaryString;

                string testIDnumber = $"{(i + 1).ToString().PadLeft(2, '0')}/{testsToRun}";
                string UDT_AllAtomicDataTypes_ex_BOOL8 = (ex_BOOLS_binaryString[0] == '1') ? "True" : "False";
                if (UDT_AllAtomicDataTypes_ex_BOOL8 != truthTableResults[i])
                {
                    Console.WriteLine($"FAILURE: test {testIDnumber} expected value ({truthTableResults[i]}) & actual value ({UDT_AllAtomicDataTypes_ex_BOOL8}) NOT equal.");
                    testResult++;
                }
                else
                    Console.WriteLine($"SUCCESS: test {testIDnumber} expected value ({truthTableResults[i]}) & actual value ({UDT_AllAtomicDataTypes_ex_BOOL8}) EQUAL.");

            }
            returnString[0] = testResult.ToString();
            return returnString;
        }

        /// <summary>
        /// A method to generate every combination possible for a binary string of specific length.
        /// </summary>
        /// <param name="binaryStringLength">The desired length of the binary string.</param>
        /// <returns>A string array containing every combination possible for a binary string of specific length.</returns>
        private static string[] GenerateBitCombinations(int binaryStringLength)
        {
            int numCombinations = (int)Math.Pow(2, binaryStringLength);
            string[] returnCombinations = new string[numCombinations];

            for (int i = 0; i < numCombinations; i++)
                returnCombinations[i] = Convert.ToString(i, 2).PadLeft(binaryStringLength, '0');

            return returnCombinations;
        }
        #endregion
    }
}