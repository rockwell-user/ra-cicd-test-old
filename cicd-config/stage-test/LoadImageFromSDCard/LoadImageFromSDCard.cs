// LoadImageFromSDCard.cpp
//
// This is an example Logix Designer SDK customer application.
// It allows the user to load stored image from SD card, save logs in file and logging to console.
// Load from SD card functionality is supported since version 34. 
// The user will provide two or three arguments:
//    1) Full path for the Logix Designer .ACD project.
//    2) Communications path for the controller that mode change is requested.
//
// Example:
// LoadImageFromSDCard "C:\ProjectDir\MyProject.ACD" AB_ETH-1\10.88.45.25\Backplane\0


using RockwellAutomation.LogixDesigner;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class LoadImageFromSDCard
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: LoadImageFromSDCard projectFilePath commPath");
            return 1;
        }

        string acdPath = args[0];
        string commPath = args[1];

        LogixProject project;
        try
        {
            project = await LogixProject.OpenLogixProjectAsync(acdPath);
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine($"Unable to open project at {acdPath}");
            Console.WriteLine(ex.Message);
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
            await project.DownloadAsync();
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine($"Unable to download");
            Console.WriteLine(ex.Message);
            return 1;
        }

        try
        {
            await project.ChangeControllerModeAsync(LogixProject.RequestedControllerMode.Program);
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine("Unable to change controller mode to Program");
            Console.WriteLine(ex.Message);
            return 1;
        }

        try
        {
            await project.LoadImageFromSDCardAsync();
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine("Unable to load image from SD card");
            Console.WriteLine(ex.Message);
            return 1;
        }
        Console.WriteLine("LoadImageFromSDCard succeed");
        Console.WriteLine("Project was successfully loaded from SD card.");
        return 0;
    }
}