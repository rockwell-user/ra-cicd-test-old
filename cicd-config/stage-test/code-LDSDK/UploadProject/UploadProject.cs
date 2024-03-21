// UploadProject.cpp
//
// This is an example Logix Designer SDK customer application.
// It allows the user to upload a Logix Designer .ACD project from a controller.
// The user will provide two arguments:
//    1) Full path for the Logix Designer .ACD project
//    2) Communications path for the controller targeted for the Upload
//
// Example:
// UploadProject "C:\ProjectDir\MyProject.ACD" AB_ETH-1\10.88.45.25\Backplane\0

using RockwellAutomation.LogixDesigner;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class UploadProject
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: UploadProject projectFilePath commPath");
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
            await project.UploadAsync();
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine("Unable to upload");
            Console.WriteLine(ex.Message);
            return 1;
        }

        try
        {
            await project.SaveAsync();
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine("Unable to save project");
            Console.WriteLine(ex.Message);
            return 1;
        }

        Console.WriteLine($"projectPath = {acdPath} controllerPath = {commPath} Upload DONE.");
        return 0;
    }
}