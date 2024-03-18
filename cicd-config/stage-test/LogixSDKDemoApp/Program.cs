using RockwellAutomation.LogixDesigner;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LogixSDKDemoApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //Path to your ACD file here.
            string filePath = @"C:\Users\ASYost\Desktop\s5k_cicd_testfiles\CICD_test.ACD";
            // Open the project and store the reference as myProject.
            LogixProject myProject = await LogixProject.OpenLogixProjectAsync(filePath);
            // Do stuff.
            Process.Start($"GetTagValue {filePath} test_DINT");
        }
    }
}

