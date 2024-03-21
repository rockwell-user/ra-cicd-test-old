using RockwellAutomation.LogixDesigner;

namespace ManyAcdManyControllerDownload
{
    /// <summary>
    /// This class shows how to create a static class which could be referenced by a program.
    /// 
    /// This class associates multiple controller comm paths with their specified ACD file,
    /// then exposes a method which will open the projects and initiate downloads in parallel.
    /// 
    /// If you instead wanted to download in series, see the SingleAcdManyControllerDownload example.
    /// 
    /// If you want to see how to reference a class in your main program,
    /// see the examples CreateDeploymentSdCard or ProvisionAndValidate.
    /// </summary>
    public class DownloadManyAcds
    {
        /// <summary>
        /// This is a hard coded list of controller comm paths and file-paths to ACD files.
        /// This information could come from anywhere. You could expand this hard-coded list,
        /// or load the information from an XML or CSV file if you wish, or passed as variables from a CI system.
        /// </summary>
        private const string CTRL1 = @"AB_ETH-1\10.116.38.143\Backplane\8";
        private const string CTRL2 = @"AB_ETH-1\10.116.38.144\Backplane\8";
        private const string CTRL3 = @"AB_ETH-1\10.116.38.145\Backplane\8";
        private const string CTRL4 = @"AB_ETH-1\10.116.38.146\Backplane\8";
        private const string CTRL5 = @"AB_ETH-1\10.116.38.147\Backplane\8";
        private const string FILE1 = @"C:\Path\To\Project.acd";
        private const string FILE2 = @"C:\Path\To\Project2.acd";
        private const string FILE3 = @"C:\Path\To\Project3.acd";
        private const string FILE4 = @"C:\Path\To\Project4.acd";
        private const string FILE5 = @"C:\Path\To\Project5.acd";

        /// <summary>
        /// This dictionary associates a comm path with an ACD file.
        /// This assigns uniquely, but multiple controllers could have the same ACD file if you chose.
        /// </summary>
        private static Dictionary<string, string> _controllers = new Dictionary<string, string>()
        {
            { CTRL1, FILE1 },
            { CTRL2, FILE2 },
            { CTRL3, FILE3 },
            { CTRL4, FILE4 },
            { CTRL5, FILE5 },
        };


        /// <summary>
        /// This function loops over each item in <see cref="_controllers"/> above queues all the necessary steps to download to a controller
        /// for each item.
        /// </summary>
        /// <returns>A task which resolves when all downloads are complete.</returns>
        public static Task Download()
        {
            var taskList = new List<Task>();
            foreach( var (commPath, acdFile) in _controllers){
                /*! Each comm path/acdFile pair has a task to run the following lambda function added to the list of tasks created above. !*/
                taskList.Add(Task.Run(async ()=>
                {
                    /*! Open project, set comm path, change mode to program, download, change mode to run. !*/
                    var logixProject = await LogixProject.OpenLogixProjectAsync(acdFile);
                    await logixProject.SetCommunicationsPathAsync(commPath);
                    await logixProject.ChangeControllerModeAsync(LogixProject.RequestedControllerMode.Program);
                    await logixProject.DownloadAsync();
                    await logixProject.ChangeControllerModeAsync(LogixProject.RequestedControllerMode.Run);
                    Console.WriteLine($"Finished downloading {acdFile} to ${commPath}");
                }));
            }
            return Task.WhenAll(taskList); /*! Return when all threads are complete !*/
        }
    }
}