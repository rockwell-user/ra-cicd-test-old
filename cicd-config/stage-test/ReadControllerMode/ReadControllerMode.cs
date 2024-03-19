// ReadControllerMode.cpp
//
// This is an example Logix Designer SDK customer application.
// It allows the user to read current controller mode.
// The user will provide two or three arguments:
//    1) Full path for the Logix Designer .ACD project.
//    2) Communications path for the controller that mode is readed.
//
// Example:
// ReadControllerMode "C:\ProjectDir\MyProject.ACD" "AB_ETH-1\10.88.45.25\Backplane\0"  
// ReadControllerMode "C:\ProjectDir\MyProject.ACD" "EmulateEthernet\127.0.0.1"


using RockwellAutomation.LogixDesigner;
using System;
using System.Threading.Tasks;

public class ReadControllerMode
{
    private static string ControllerModeToString(LogixProject.ControllerMode result)
    {
        switch (result)
        {
            case LogixProject.ControllerMode.Faulted:
                return "Faulted";
            case LogixProject.ControllerMode.Program:
                return "Program";
            case LogixProject.ControllerMode.Run:
                return "Run";
            case LogixProject.ControllerMode.Test:
                return "Test";
            default:
                throw new ArgumentOutOfRangeException("Controller mode is unrecognized");
        }
    }

    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: ReadControllerMode projectFilePath commPath");
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
            LogixProject.ControllerMode result = await project.ReadControllerModeAsync();
            Console.WriteLine($"Controller mode: {ControllerModeToString(result)}");
        }
        catch (LogixSdkException ex) 
        {
            Console.WriteLine($"Unable to read controller mode");
            Console.WriteLine(ex.Message);
            return 1;
        }

        return 0;
    }
}