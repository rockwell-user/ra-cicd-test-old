// This is an example Logix Designer SDK customer application.
// It reads a controller tag of DINT type from a Logix Designer project
// and takes two arguments:
// - a full path to a .acd file
// - a tag name
// Example:
// GetTagValue "C:\Users\BRubble\Documents\Studio 5000\Projects\MyProj.ACD" my_tag

using RockwellAutomation.LogixDesigner;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class GetTagValue
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: GetTagValue projectFilePath tagName");
            return 1;
        }

        string acdPath = args[0];
        string tagName = args[1];

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

        var tagPath = $"Controller/Tags/Tag[@Name='{tagName}']";
        try
        {
            int tagValue = await project.GetTagValueDINTAsync(tagPath, LogixProject.TagOperationMode.Offline);
            Console.WriteLine($"Tag value: {tagValue}");
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine("Unable to get tag value DINT");
            Console.WriteLine(ex.Message);
            return 1;
        }

        return 0;
    }
}