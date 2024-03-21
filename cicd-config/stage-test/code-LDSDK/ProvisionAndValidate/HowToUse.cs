using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ProvisionAndValidate
{

    /// <summary>
    /// This example shows a very basic demo of how to use LogixDesignerSDK to provision a controller and test for expected values.
    /// This could be used, for example, to retrieve an ACD file from version control
    /// and then programatically test on a physical or emulated controller.
    /// You could (and likely should) also use libraries like NUnit, XUnit, or MSTest along with the demo helper class for an even more robust solution.
    /// 
    /// This program compiles to an executable that takes one argument: The ACD path, and has a hard coded comm path.
    /// For examples on taking more than one argument, see CreateDeploymentSdCard.
    /// </summary>
    internal class HowToUse
    {
        public static async Task<int> Main(string[] args)
        {
            int failedTests = 0;
            // in this example, you could pass the string in as a command line parameter to an EXE you compile.
            string path = args[0];
            // Let's pretend we're using FatoryTalk Echo and running this on a ci device to test.
            string commPath = "EmulateEthernet\\127.0.0.7";
            var projectToTest = await ProvisionAndValidate.Init(path, commPath);

            // These helper methods are declared below. In a larger solution, you may want to move them to their own class and expand on them.
            string stringTagPath = CreateTagPathFromName("myString"); 
            string dintTagPath = CreateTagPathFromName("myDint");
            try /*! The ProvisionAndValidate class throws an error if the values do not match. !*/
            {
                await projectToTest.StringValueMatches(stringTagPath, "expectedValue");
            }
            catch { failedTests++;} /*! In this instance we only count the test as failed. You may wish to report more information. !*/

            try
            {
                await projectToTest.DINTValueMatches(dintTagPath, 42);
            }
            catch { failedTests++;}

            if(failedTests == 0)
            {
                Console.WriteLine("All tests passed!!!");
                return 0;
            }
            else
            {
                Console.WriteLine($"There were {failedTests} failed tests.");
                return 1; // Some tests failed. End program with error.
            }
        }

        private static string CreateTagPathFromName(string name)
        {
            const string tagPathPrefix = "Controller/Tags/Tag[@Name='";
            const string tagPathSuffix = "']";
            return $"{tagPathPrefix}{name}{tagPathSuffix}";
        }
    }

}
