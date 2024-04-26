// ---------------------------------------------------------------------------------------------------------------------------------------------------------------
//
// FileName: Program.cs
// FileType: Visual C# Source file
// Author : Rockwell Automation
// Created : 2024
// Description : This script provides an example test in a CI/CD pipeline utilizing Studio 5000 Logix Designer SDK and Factory Talk Logix Echo SDK.
//
// ---------------------------------------------------------------------------------------------------------------------------------------------------------------

using RockwellAutomation.FactoryTalkLogixEcho.Api.Client;
using RockwellAutomation.FactoryTalkLogixEcho.Api.Interfaces;
using System.Globalization;

namespace Echo_Program
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Pass the incoming executable arguments.
            if (args.Length != 2)
            {
                Console.WriteLine(@"Correct Command: .\TestStage_CICDExample github_RepositoryDirectory acd_filename");
                Console.WriteLine(@"Example Format:  .\TestStage_CICDExample C:\Users\TestUser\Desktop\example-github-repo\ acd_filename.ACD");
            }
            string githubPath = args[0];                                                                                     // 1st incoming argument = GitHub folder path
            string acdFilename = args[1];                                                                                    // 2nd incoming argument = Logix Designer ACD filename
            string filePath = githubPath + @"DEVELOPMENT-files\" + acdFilename;

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
        }

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
    }
}