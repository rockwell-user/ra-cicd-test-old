// This is an example Logix Designer SDK customer application.
// It sets the project communications path in a Logix Designer project
// and takes two arguments
// - a full path to a .acd file to update
// - the communications path to update the project to
// Example:
// SetCommPath "C:\Users\BRubble\Documents\Studio 5000\Projects\MyProj.ACD" AB_ETH-1\10.88.44.99


using RockwellAutomation.LogixDesigner;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class SetCommPath
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: SetCommPath projectFilePath commPath");
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
            await project.SaveAsync();
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine($"Unable to save project");
            Console.WriteLine(ex.Message);
            return 1;
        }

        Console.WriteLine("Communications path was set.");
        return 0;
    }
}