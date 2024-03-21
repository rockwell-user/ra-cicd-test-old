// UploadToNewProject.cpp
//
// This is an example Logix Designer SDK customer application.
// It allows the user to upload a Logix Designer .ACD project from a controller.
// The user will provide two arguments:
//    1) Full path for the Logix Designer .ACD project that will be created.
//    2) Communications path for the controller targeted for the Upload.
//
// Example:
// UploadToNewProject "C:\ProjectDir\NotYetExistingMyProject.ACD" AB_ETH-1\10.88.45.25\Backplane\0


using RockwellAutomation.LogixDesigner;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class UploadToNewProject
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: UploadToNewProject newProjectPath commPath");
            return 1;
        }

        string newProjectPath = args[0];
        string commPath = args[1];

        LogixProject project;

        try
        {
            await LogixProject.UploadToNewProjectAsync(newProjectPath, commPath);
        }
        catch (LogixSdkException ex) 
        {
            Console.WriteLine("Unable to upload to new project");
            Console.WriteLine(ex.Message);
            return 1;
        }

        Console.WriteLine($"newProjectPath = {newProjectPath} controllerPath = {commPath} UploadToNewProject DONE.");
        return 0;
    }
}