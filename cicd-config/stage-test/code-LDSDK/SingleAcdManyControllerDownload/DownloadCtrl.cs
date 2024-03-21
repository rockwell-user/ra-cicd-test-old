using RockwellAutomation.LogixDesigner;

namespace SingleAcdManyControllerDownload
{
    /// <summary>
    /// This class shows how to create a static class which could be referenced by a program.
    /// 
    /// This class associates multiple controller comm paths with a single ACD file,
    /// then exposes a method which will open the projects and initiate downloads in series.
    /// 
    /// It then saves those projects into a dictionary, using the comm path as the key, and a handle on the <see cref="LogixProject"/>
    /// 
    /// 
    /// If you instead wanted to download in parallel, see the ManyAcdManyControllerDownload example.
    /// 
    /// If you want to see how to reference a class in your main program,
    /// see the examples CreateDeploymentSdCard or ProvisionAndValidate.
    /// </summary>
    public class DownloadCtrl
    {
        /// <summary>
        /// This is a hard coded list of controller comm paths.
        /// This information could come from anywhere. You could expand this hard-coded list,
        /// or load the information from an XML or CSV file if you wish, or passed as variables from a CI system.
        /// </summary>
        private const string CTRL1 = @"AB_ETH-1\10.116.38.143\Backplane\8";
        private const string CTRL2 = @"AB_ETH-1\10.116.38.144\Backplane\8";
        private const string CTRL3 = @"AB_ETH-1\10.116.38.145\Backplane\8";
        private const string CTRL4 = @"AB_ETH-1\10.116.38.146\Backplane\8";
        private const string CTRL5 = @"AB_ETH-1\10.116.38.146\Backplane\8";

        /// <summary>
        /// The file path to the ACD.
        /// </summary>
        private static string _filePath = @"C:\Path\To\Project.acd";

        /// <summary>
        /// The controller comm path list.
        /// </summary>
        private static List<string> _controllerCommPaths = new List<string>()
        {
            CTRL1,
            CTRL2,
            CTRL3,
            CTRL4,
            CTRL5,
        };

        /// <summary>
        /// This dictionary will be populated so that multiple instances of <see cref="LogixProject"/>
        /// can be identified and retrieved by their comm path for use elsewhere in a program.
        /// </summary>
        private static Dictionary<string, LogixProject> logixSdkProjects = new Dictionary<string, LogixProject>();
        
        public static async Task DownloadToMultipleControllers()
        {
            var programMode = LogixProject.RequestedControllerMode.Program;
            var runMode = LogixProject.RequestedControllerMode.Run;
            /*! Download ACD file to all controllers and set to run mode. !*/
            foreach (var controller in _controllerCommPaths)
            {
                LogixProject logixProject = await LogixProject.OpenLogixProjectAsync(_filePath);
                await logixProject.SetCommunicationsPathAsync(controller);
                await logixProject.ChangeControllerModeAsync(programMode);
                await logixProject.DownloadAsync();
                await logixProject.ChangeControllerModeAsync(runMode);

                /*! hold project reference for later use !*/
                logixSdkProjects.Add(controller, logixProject);
            }

            // you could read from the controller later.
            var tagPath = "Controller/Tags/Tag[@Name='someBool']";
            var ctrl1Project = logixSdkProjects.Single((x) => x.Key == CTRL1).Value; /*! A reference to the LogixProject that matches the same commPath as CTRL1 !*/
            
            //if SOME CONDITION
            if(await ctrl1Project.GetTagValueBOOLAsync(tagPath, LogixProject.TagOperationMode.Online))
            {
                //Do something
            }
        }
    }
}