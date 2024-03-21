// ChangeControllerMode
//
// This is an example Logix Designer SDK customer application.
// It allows the user to change given controller mode.
// The user will provide two or three arguments:
//    1) Full path for the Logix Designer .ACD project.
//    2) Communications path for the controller that mode change is requested.
//    3) Number that represents requested mode: 0 for Program, 1 for Run, 2 for Test.
//
// Example:
// ChangeControllerMode "C:\ProjectDir\MyProject.ACD" AB_ETH-1\10.88.45.25\Backplane\0 1
// ChangeControllerMode "C:\ProjectDir\MyProject.ACD" AB_ETH-1\10.88.45.25\Backplane\0

using RockwellAutomation.LogixDesigner;
using System;
using System.Threading.Tasks;

public class ChangeControllerMode
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: ChangeControllerMode projectFilePath commPath mode\nWhere mode is: 0 for Program, 1 for Run, 2 for Test");
            return 1;
        }
        string acdPath = args[0];
        string commPath = args[1];
        uint mode = Convert.ToUInt32(args[2]);
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
            LogixProject.RequestedControllerMode requestedControllerMode = ToRequestedMode(mode);
            await project.ChangeControllerModeAsync(requestedControllerMode);
            Console.WriteLine($"Controller mode changed to: {requestedControllerMode}({mode}).");
        }
        catch (LogixSdkException ex)
        {
            Console.WriteLine($"Unable to set mode. Requested mode was {mode}");
            Console.WriteLine(ex.Message);
            return 1;
        }

        return 0;
    }

    private static LogixProject.RequestedControllerMode ToRequestedMode(uint mode)
    {
        switch (mode)
        {
            case 0:
            {
                return LogixProject.RequestedControllerMode.Program;
            }
            case 1:
            {
                return LogixProject.RequestedControllerMode.Run;
            }
            case 2:
            {
                return LogixProject.RequestedControllerMode.Test;
            }
            default:
            {
                var error = $"{mode} is not supported.";
                Console.WriteLine(error);
                throw new ArgumentException(error);
            }
        }
    }
}