// This is an example Logix Designer SDK customer application.
// It sets a controller tag of DINT type in a Logix Designer project
// and takes three arguments
// - a full path to a .acd file
// - a tag name
// - an integer tag value
// Example:
// SetTagValue "C:\Users\BRubble\Documents\Studio 5000\Projects\MyProj.ACD" my_tag 123

using RockwellAutomation.LogixDesigner;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class SetTagValue
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: SetTagValue projectFilePath tagName tagValue");
            return 1;
        }

        string acdPath = args[0];
        string tagName = args[1];
        int tagValue = int.Parse(args[2]);

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

        string tagPath = $"Controller/Tags/Tag[@Name='{tagName}']";
        try
        {
            await project.SetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Offline, tagValue);
            Console.WriteLine("Tag value was set.");
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine("Unable to set tag value DINT");
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

        return 0;
    }
}