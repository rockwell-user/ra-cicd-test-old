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
            Console.WriteLine("  =========================================================================================================================  ");
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
            CleanTestReportsFolder(textFileReportDirectory, 50);

            // Set up emulated controller (based on the specified ACD file path) if one does not yet exist. If not, continue.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START setting up Factory Talk Logix Echo emulated controller...");
            var serviceClient = ClientFactory.GetServiceApiClientV2("CLIENT_TestStage_CICDExample");
            serviceClient.Culture = new CultureInfo("en-US");
            if (CallCheckCurrentChassisAsyncAndWaitOnResult("CICDtest_chassis", "CICD_test", serviceClient) == false)
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
            string[] testControllerInfo = await GetControllerInfo("CICDtest_chassis", "CICD_test", serviceClient);
            string commPath = @"EmulateEthernet\" + testControllerInfo[1];
            Console.WriteLine($"SUCCESS: project communication path specified is \"{commPath}\"");
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE setting up Factory Talk Logix Echo emulated controller\n---");

            // Open the ACD project file and store the reference as myProject.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START opening ACD file...");
            LogixProject myProject = await LogixProject.OpenLogixProjectAsync(filePath);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS opening ACD file\n---");

            // Change controller mode to program & verify.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START changing controller to PROGRAM...");
            ChangeControllerModeAsync(commPath, "Program", myProject).GetAwaiter().GetResult();
            if (ReadControllerModeAsync(commPath, myProject).GetAwaiter().GetResult() == "PROGRAM")
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS changing controller to PROGRAM\n---");
            else
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] FAILURE changing controller to PROGRAM\n---");

            // Download project.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START downloading ACD file...");
            DownloadProjectAsync(commPath, myProject).GetAwaiter().GetResult();
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] SUCCESS downloading ACD file\n---");

            // Change controller mode to run.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START Changing controller to RUN...");
            ChangeControllerModeAsync(commPath, "Run", myProject).GetAwaiter().GetResult();
            if (ReadControllerModeAsync(commPath, myProject).GetAwaiter().GetResult() == "RUN")
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
            string[] TEST_DINT_1 = CallGetTagValueAsyncAndWaitOnResult("TEST_DINT_1", DataType.DINT, filePath_ControllerScope, myProject, true);
            string[] TEST_TOGGLE_WetBulbTempCalc = CallGetTagValueAsyncAndWaitOnResult("TEST_TOGGLE_WetBulbTempCalc", DataType.BOOL, filePath_MainProgram, myProject, true);
            string[] TEST_AOI_WetBulbTemp_isFahrenheit = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_isFahrenheit", DataType.BOOL, filePath_MainProgram, myProject, true);
            string[] TEST_AOI_WetBulbTemp_RelativeHumidity = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_RelativeHumidity", DataType.REAL, filePath_MainProgram, myProject, true);
            string[] TEST_AOI_WetBulbTemp_Temperature = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_Temperature", DataType.REAL, filePath_MainProgram, myProject, true);
            string[] TEST_AOI_WetBulbTemp_WetBulbTemp = CallGetTagValueAsyncAndWaitOnResult("TEST_AOI_WetBulbTemp_WetBulbTemp", DataType.REAL, filePath_MainProgram, myProject, true);
            // The nested string array below is an example result for a complex data type tag. 
            ByteString[] ByteString_UDT_AllAtomicDataTypes = CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", filePath_ControllerScope, myProject);
            string[][] UDT_AllAtomicDataTypes = FormatUDT_AllAtomicDataTypes(ByteString_UDT_AllAtomicDataTypes, true);
            // The below values are examples of how to convert the complex data type's components (currently strings) to their original basic data type in c#.
            bool ex_BOOL1 = (UDT_AllAtomicDataTypes[1][0] == "1") ? true : false;
            bool ex_BOOL2 = (UDT_AllAtomicDataTypes[1][1] == "1") ? true : false;
            bool ex_BOOL3 = (UDT_AllAtomicDataTypes[1][2] == "1") ? true : false;
            bool ex_BOOL4 = (UDT_AllAtomicDataTypes[1][3] == "1") ? true : false;
            bool ex_BOOL5 = (UDT_AllAtomicDataTypes[1][4] == "1") ? true : false;
            bool ex_BOOL6 = (UDT_AllAtomicDataTypes[1][5] == "1") ? true : false;
            bool ex_BOOL7 = (UDT_AllAtomicDataTypes[1][6] == "1") ? true : false;
            bool ex_BOOL8 = (UDT_AllAtomicDataTypes[1][7] == "1") ? true : false;
            int ex_SINT = int.Parse(UDT_AllAtomicDataTypes[1][8]); // c# bytes have range 0 to 255 so INT accounts for negatives (Studio 5k SINT has range -128 to 127)
            int ex_INT = int.Parse(UDT_AllAtomicDataTypes[1][9]);
            double ex_DINT = double.Parse(UDT_AllAtomicDataTypes[1][10]);
            long ex_LINT = long.Parse(UDT_AllAtomicDataTypes[1][11]);
            float ex_REAL = float.Parse(UDT_AllAtomicDataTypes[1][12]);
            string ex_STRING = UDT_AllAtomicDataTypes[1][13];
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE getting initial project start-up tag values\n---");

            // Verify whether offline and online values are the same.
            // Each test returns a value of 0 for a success or 1 for a failure. The integer failure conditions tracks this tests progress.
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

            // Set tag values.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START setting tag values...");
            CallSetTagValueAsyncAndWaitOnResult(TEST_DINT_1[0], 111, TagOperationMode.Online, DataType.DINT, filePath_ControllerScope, myProject, true);
            CallSetTagValueAsyncAndWaitOnResult(TEST_TOGGLE_WetBulbTempCalc[0], 1, TagOperationMode.Online, DataType.BOOL, filePath_MainProgram, myProject, true);
            CallSetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_isFahrenheit[0], 1, TagOperationMode.Online, DataType.BOOL, filePath_MainProgram, myProject, true);
            CallSetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_RelativeHumidity[0], 30, TagOperationMode.Online, DataType.REAL, filePath_MainProgram, myProject, true);
            CallSetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_Temperature[0], 70, TagOperationMode.Online, DataType.REAL, filePath_MainProgram, myProject, true);
            CallSetUDT_AllAtomicDataTypesAndWait("10010010", DataType.BOOL, TagOperationMode.Online, myProject, true);
            CallSetUDT_AllAtomicDataTypesAndWait("-24", DataType.SINT, TagOperationMode.Online, myProject, true);
            CallSetUDT_AllAtomicDataTypesAndWait("20500", DataType.INT, TagOperationMode.Online, myProject, true);
            CallSetUDT_AllAtomicDataTypesAndWait("-2000111000", DataType.DINT, TagOperationMode.Online, myProject, true);
            CallSetUDT_AllAtomicDataTypesAndWait("-9000111000111000111", DataType.LINT, TagOperationMode.Online, myProject, true);
            CallSetUDT_AllAtomicDataTypesAndWait("10555.888", DataType.REAL, TagOperationMode.Online, myProject, true);
            CallSetUDT_AllAtomicDataTypesAndWait("New String!", DataType.STRING, TagOperationMode.Online, myProject, true);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE setting tag values\n---");

            // Verify expected tag values based on the tag values that had been set.
            // Each test returns a value of 0 for a success or 1 for a failure. The integer failure conditions tracks this tests progress.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START verifying expected tag outputs...");
            TEST_AOI_WetBulbTemp_WetBulbTemp = CallGetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_WetBulbTemp[0], DataType.REAL, filePath_MainProgram, myProject, false);
            failure_condition += CompareForExpectedValue("TEST_AOI_WetBulbTemp_WetBulbTemp", "52.997536", TEST_AOI_WetBulbTemp_WetBulbTemp[1]);
            ByteString_UDT_AllAtomicDataTypes = CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", filePath_ControllerScope, myProject);
            UDT_AllAtomicDataTypes = FormatUDT_AllAtomicDataTypes(ByteString_UDT_AllAtomicDataTypes, false);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL1", "False", UDT_AllAtomicDataTypes[1][0]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL2", "True", UDT_AllAtomicDataTypes[1][1]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL3", "False", UDT_AllAtomicDataTypes[1][2]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL4", "False", UDT_AllAtomicDataTypes[1][3]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL5", "True", UDT_AllAtomicDataTypes[1][4]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL6", "False", UDT_AllAtomicDataTypes[1][5]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL7", "False", UDT_AllAtomicDataTypes[1][6]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_BOOL8", "True", UDT_AllAtomicDataTypes[1][7]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_SINT", "-24", UDT_AllAtomicDataTypes[1][8]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_INT", "20500", UDT_AllAtomicDataTypes[1][9]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_DINT", "-2000111000", UDT_AllAtomicDataTypes[1][10]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_LINT", "-9000111000111000111", UDT_AllAtomicDataTypes[1][11]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_REAL", "10555.8876953125", UDT_AllAtomicDataTypes[1][12]);
            failure_condition += CompareForExpectedValue("UDT_AllAtomicDataTypes.ex_STRING", "New String!", UDT_AllAtomicDataTypes[1][13]);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE verifying expected tag outputs\n---");

            // Show final tag values.
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] START showing final test tag values...");
            CallGetTagValueAsyncAndWaitOnResult(TEST_DINT_1[0], DataType.DINT, filePath_ControllerScope, myProject, true);
            CallGetTagValueAsyncAndWaitOnResult(TEST_TOGGLE_WetBulbTempCalc[0], DataType.BOOL, filePath_MainProgram, myProject, true);
            CallGetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_isFahrenheit[0], DataType.BOOL, filePath_MainProgram, myProject, true);
            CallGetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_RelativeHumidity[0], DataType.REAL, filePath_MainProgram, myProject, true);
            CallGetTagValueAsyncAndWaitOnResult(TEST_AOI_WetBulbTemp_Temperature[0], DataType.REAL, filePath_MainProgram, myProject, true);
            FormatUDT_AllAtomicDataTypes(CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", filePath_ControllerScope, myProject), true);
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] DONE showing final test tag values");

            // Print out final banner based on test results.
            if (failure_condition > 0)
                CreateBanner("TEST FAILURE");
            else
                CreateBanner("TEST SUCCESS!");

            // Finish the process of sending console printouts to the test text file.
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
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
        /// Using the Factory Talk Logix Echo API, check to see if a specific controller exists in a specific chassis.
        /// </summary>
        /// <param name="chassisName">The name of the emulated chassis to check the emulated controler in.</param>
        /// <param name="controllerName">The name of the emulated controller to check.</param>
        /// <param name="serviceClient">The Factory Talk Logix Echo interface.</param>
        /// <returns>A boolean value 'True' if the emulated controller already exists and a 'False' if it does not.</returns>
        private static async Task<bool> CheckCurrentChassisAsync(string chassisName, string controllerName, IServiceApiClientV2 serviceClient)
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
        /// Using the Factory Talk Logix Echo API, check to see if a specific controller exists in a specific chassis.
        /// </summary>
        /// <param name="chassisName">The name of the emulated chassis to check the emulated controler in.</param>
        /// <param name="controllerName">The name of the emulated controller to check.</param>
        /// <param name="serviceClient">The Factory Talk Logix Echo interface.</param>
        /// <returns>A boolean value 'True' if the emulated controller already exists and a 'False' if it does not.</returns>
        private static bool CallCheckCurrentChassisAsyncAndWaitOnResult(string chassisName, string controllerName, IServiceApiClientV2 serviceClient)
        {
            var task = CheckCurrentChassisAsync(chassisName, controllerName, serviceClient);
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Get the emulated controller name, IP address, and project file path.
        /// </summary>
        /// <param name="chassisName">The emulated chassis to the emulatedcontroller information from.</param>
        /// <param name="controllerName">The emulated controller name.</param>
        /// <param name="serviceClient">The Factory Talk Logix Echo interface.</param>
        /// <returns>
        /// A string array containing controller information.
        /// return_array[0] = controller name
        /// return_array[1] = controller IP address
        /// return_array[2] = controller project file path
        /// </returns>
        private static async Task<string[]> GetControllerInfo(string chassisName, string controllerName, IServiceApiClientV2 serviceClient)
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
        /// <returns>An asynchronous Task.</returns>
        private static async Task ChangeControllerModeAsync(string commPath, string mode, LogixProject project)
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
        /// Asynchronously download to the controller.
        /// </summary>
        /// <param name="commPath">The controller communication path.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <returns>An asynchronous Task.</returns>
        private static async Task DownloadProjectAsync(string commPath, LogixProject project)
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

        /// <summary>
        /// Asynchronously get the current controller mode (FAULTED, PROGRAM, RUN, or TEST).
        /// </summary>
        /// <param name="commPath">The controller communication path.</param>
        /// <param name="project">An instance of the LogixProject class.</param>
        /// <returns>An asynchronous Task that results in a string.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the returned controller mode is not FAULTED, PROGRAM, RUN, or TEST.</exception>
        private static async Task<string> ReadControllerModeAsync(string commPath, LogixProject project)
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
        private static async Task SetTagValueAsync(string tag_name, int new_tag_value, TagOperationMode mode, DataType type, string tagPath, LogixProject project, bool printout)
        {
            tagPath = tagPath + $"[@Name='{tag_name}']";
            string[] old_tag_values = CallGetTagValueAsyncAndWaitOnResult(tag_name, type, tagPath, project, false);
            string old_tag_value = "";
            try
            {
                if (mode == TagOperationMode.Online)
                {
                    if (type == DataType.DINT)
                        await project.SetTagValueDINTAsync(tagPath, TagOperationMode.Online, new_tag_value);
                    else if (type == DataType.BOOL)
                        await project.SetTagValueBOOLAsync(tagPath, TagOperationMode.Online, Convert.ToBoolean(new_tag_value));
                    else if (type == DataType.REAL)
                        await project.SetTagValueREALAsync(tagPath, TagOperationMode.Online, new_tag_value);
                    else
                        Console.WriteLine($"ERROR executing command: The data type cannot be handled. Select either DINT, BOOL, or REAL.");
                    old_tag_value = old_tag_values[1];
                }
                else if (mode == TagOperationMode.Offline)
                {
                    if (type == DataType.DINT)
                        await project.SetTagValueDINTAsync(tagPath, TagOperationMode.Offline, new_tag_value);
                    else if (type == DataType.BOOL)
                        await project.SetTagValueBOOLAsync(tagPath, TagOperationMode.Offline, Convert.ToBoolean(new_tag_value));
                    else if (type == DataType.REAL)
                        await project.SetTagValueREALAsync(tagPath, TagOperationMode.Offline, new_tag_value);
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
                string new_tag_value_string = Convert.ToString(new_tag_value);
                if ((new_tag_value_string == "1") && (type == DataType.BOOL)) { new_tag_value_string = "True"; }
                if ((new_tag_value_string == "0") && (type == DataType.BOOL)) { new_tag_value_string = "False"; }
                Console.WriteLine("SUCCESS: " + mode + " " + old_tag_values[0].PadRight(40, ' ') + old_tag_value.PadLeft(20, ' ') + "  -->  " + new_tag_value_string);
            }
        }

        // Set Tag Value Wait On Result Method
        private static void CallSetTagValueAsyncAndWaitOnResult(string tag_name, int tag_value_in, TagOperationMode mode, DataType type, string tagPath, LogixProject project, bool printout)
        {
            var task = SetTagValueAsync(tag_name, tag_value_in, mode, type, tagPath, project, printout);
            task.Wait();
        }
        #endregion

        #region METHODS: get/set complex data type tags
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

        // Set Complex Data Type Tag Value Method
        private static async Task SetUDT_AllAtomicDataTypes(string value_in, DataType type, TagOperationMode mode, LogixProject project, bool printout)
        {
            string tagPath = $"Controller/Tags/Tag[@Name='UDT_AllAtomicDataTypes']";
            ByteString[] old_byteString = CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", tagPath, project);
            ByteString[] new_byteString = CallGetUDT_AllAtomicDataTypesAndWaitOnResult("UDT_AllAtomicDataTypes", tagPath, project);
            string[][] old_UDT_AllAtomicDataTypes = FormatUDT_AllAtomicDataTypes(old_byteString, false);
            int on_off = 0;
            byte[] new_byteArray = new byte[new_byteString[1].Length];

            if (mode == TagOperationMode.Online)
                new_byteArray = new_byteString[0].ToByteArray();
            else if (mode == TagOperationMode.Offline)
                new_byteArray = new_byteString[1].ToByteArray();

            if (type == DataType.BOOL)
                new_byteArray[0] = Convert.ToByte(value_in, 2);

            else if (type == DataType.SINT)
            {
                string sint_string = Convert.ToString(long.Parse(value_in), 2);
                sint_string = sint_string.Substring(sint_string.Length - 8);
                new_byteArray[1] = Convert.ToByte(sint_string, 2);
            }

            else if (type == DataType.INT)
            {
                byte[] int_byteArray = BitConverter.GetBytes(int.Parse(value_in));
                for (int i = 0; i < 2; ++i)
                    new_byteArray[i + 2] = int_byteArray[i];
            }

            else if (type == DataType.DINT)
            {
                byte[] dint_byteArray = BitConverter.GetBytes(long.Parse(value_in));
                for (int i = 0; i < 4; ++i)
                    new_byteArray[i + 4] = dint_byteArray[i];
            }

            else if (type == DataType.LINT)
            {
                byte[] lint_byteArray = BitConverter.GetBytes(long.Parse(value_in));
                for (int i = 0; i < 8; ++i)
                    new_byteArray[i + 8] = lint_byteArray[i];
            }

            else if (type == DataType.REAL)
            {
                byte[] real_byteArray = BitConverter.GetBytes(float.Parse(value_in));
                for (int i = 0; i < 4; ++i)
                    new_byteArray[i + 16] = real_byteArray[i];
            }

            else if (type == DataType.STRING)
            {
                byte[] real_byteArray = new byte[value_in.Length];
                for (int i = 0; i < real_byteArray.Length; ++i)
                    new_byteArray[i + 24] = (byte)value_in[i];
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

            string[][] new_UDT_AllAtomicDataTypes = FormatUDT_AllAtomicDataTypes(new_byteString, false);
            await project.SetTagValueAsync(tagPath, mode, new_byteString[on_off - 1].ToByteArray(), DataType.BYTE_ARRAY);

            if (printout)
            {
                for (int i = 0; i < old_UDT_AllAtomicDataTypes[1].Length - 1; ++i)
                    if (old_UDT_AllAtomicDataTypes[on_off][i] != new_UDT_AllAtomicDataTypes[on_off][i])
                        Console.WriteLine("SUCCESS: " + mode + " " + old_UDT_AllAtomicDataTypes[0][i].PadRight(40, ' ') + old_UDT_AllAtomicDataTypes[on_off][i].PadLeft(20, ' ') + "  -->  " + new_UDT_AllAtomicDataTypes[on_off][i]);
            }
        }

        private static void CallSetUDT_AllAtomicDataTypesAndWait(string value_in, DataType type, TagOperationMode mode, LogixProject project, bool printout)
        {
            var task = SetUDT_AllAtomicDataTypes(value_in, type, mode, project, printout);
            task.Wait();
        }

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
                    UDT_ByteArray[j] = byte_string[i][j];

                var ex_BOOLs = new byte[1];
                Array.ConstrainedCopy(UDT_ByteArray, 0, ex_BOOLs, 0, 1);
                for (int j = 0; j < 8; j++)
                    return_string[i + 1][j] = (Convert.ToString(CreateBinaryString(ex_BOOLs, "backward")[7 - j]) == "1") ? "True" : "False";

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

        // Helper method to Convert IEEE 754
        private static string ConvertIEEE754ToFloatString(byte[] inputArray)
        {
            if (inputArray.Length != 4)
                throw new ArgumentException("Byte array must be 4 bytes long.");

            byte[] flippedArray = new byte[4];
            for (int i = 0; i < 4; i++)
                flippedArray[i] = inputArray[3 - i];

            // Interpret the bytes as per IEEE 754 single-precision format.
            int sign = (flippedArray[0] & 0x80) >> 7;
            int exponent = ((flippedArray[0] & 0x7F) << 1) | ((flippedArray[1] & 0x80) >> 7);
            uint mantissa = ((uint)(flippedArray[1] & 0x7F) << 16) | ((uint)flippedArray[2] << 8) | flippedArray[3];

            // Calculate the value represented by the bits.
            double value = Math.Pow(-1, sign) * (1 + mantissa / Math.Pow(2, 23)) * Math.Pow(2, exponent - 127);

            // Convert the value to a string.
            return value.ToString();
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

        #region METHODS: testing online & offline / testing for expected value 
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
        #endregion
    }
}