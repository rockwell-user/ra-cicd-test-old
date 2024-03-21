// DownloadProject.cpp
//
// This is an example Logix Designer SDK customer application.
// It allows the user to download a Logix Designer .ACD project into a controller.
// The user will provide two arguments:
//    1) Full path for the Logix Designer .ACD project
//    2) Communications path for the controller targeted for the Download
//
// Example:
// DownloadProject "C:\ProjectDir\MyProject.ACD" AB_ETH-1\10.88.45.25\Backplane\0


using RockwellAutomation.LogixDesigner;
using System;
using System.Threading.Tasks;

public class DownloadProject
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: DownloadProject projectFilePath commPath");
            return 1;
        }

        string acdPath = args[0];
        string commPath = args[1];

        LogixProject project;
        try
        {
            project = await LogixProject.OpenLogixProjectAsync(acdPath);
        }
        catch (ProjectException ex)
        {
            Console.WriteLine($"Unable to open logix project.");
            Console.WriteLine(ex.FullMessage);
            return 1;
        }


        try
        {
            await project.SetCommunicationsPathAsync(commPath);
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine($"Unable to set commpath to {commPath}");
            Console.WriteLine(ex.Message);
            return 1;
        }

        try
        {
            LogixProject.ControllerMode controllerMode = await project.ReadControllerModeAsync();
            if (controllerMode != LogixProject.ControllerMode.Program)
            {
                Console.WriteLine($"Controller mode is {controllerMode}. Downloading is possible only if the controller is in 'Program' mode");
                return 1;
            }
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine($"Unable to read ControllerMode");
            Console.WriteLine(ex.Message);
            return 1;
        }

        try
        {
            await project.DownloadAsync();
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine($"Unable to download");
            Console.WriteLine(ex.Message);
            return 1;
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
            return 1;
        }

        Console.WriteLine("Download DONE.");
        return 0;
    }
}