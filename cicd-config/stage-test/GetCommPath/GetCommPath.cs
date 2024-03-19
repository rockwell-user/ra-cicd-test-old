// This is an example Logix Designer SDK customer application.
// It prints the project communications path in a Logix Designer project
// and takes one argument
// - a full path to a .acd file
// Example:
// GetCommPath "C:\Users\BRubble\Documents\Studio 5000\Projects\MyProj.ACD"


using RockwellAutomation.LogixDesigner;
using System;
using System.Threading.Tasks;

public class GetCommPath
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: GetCommPath projectFilePath");
            return 1;
        }

        string acdPath = args[0];

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
            string getCommPathResult = await project.GetCommunicationsPathAsync();
            Console.WriteLine($"Project's communication path is {getCommPathResult}");
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine("Unable to get communications path");
            Console.WriteLine(ex.Message);
            return 1;
        }
        return 0;
    }
}