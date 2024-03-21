using RockwellAutomation.LogixDesigner;


namespace CreateDeploymentSdCard
{
    /// <summary>
    /// Class which will opens a <see cref="LogixProject"/>, goes on-line with a given controller, downloads the program,
    /// and then exposes the <see cref="LogixProject.StoreImageOnSDCardAsync"/> to the user. Uses the initializer pattern
    /// as the initial steps are asynchronous.
    /// </summary>
    public class SDCardFactory
    {
        /// <summary>
        /// Reference to the <see cref="LogixProject"/> to be referenced later.
        /// </summary>
        private LogixProject _project;

        /// <summary>
        /// Instantiates the SDCardFactory class.
        /// </summary>
        /// <param name="project">The <see cref="LogixProject"/> to load to SD card.</param>
        private SDCardFactory(LogixProject project)
        {
            _project = project;
        }

        /// <summary>
        /// A pass through method to <see cref="LogixProject.StoreImageOnSDCardAsync"/>
        /// </summary>
        /// <returns>A task which resolves when the SD card write is complete.</returns>
        public async Task LoadToSdCard()
        {
            await _project.StoreImageOnSDCardAsync();
        }

        /// <summary>
        /// Static initializer which opens the project and does all the one time
        /// setup tasks, then returns a new instance of <see cref="SDCardFactory"/>.
        /// </summary>
        /// <param name="acdPath">The path to the ACD file.</param>
        /// <param name="commPath">The comm path to the controller.</param>
        /// <returns>An instance of <see cref="SDCardFactory"/></returns>
        public static async Task<SDCardFactory> Init(string acdPath, string commPath)
        {
            var project = await LogixProject.OpenLogixProjectAsync(acdPath); /*! Open the project !*/
            await project.SetCommunicationsPathAsync(commPath); /*! Set the comm path in the project to the one given by the user. !*/
            await project.ChangeControllerModeAsync(LogixProject.RequestedControllerMode.Program); /*! Set controller to program mode !*/
            await project.DownloadAsync(); /*! Download the program. !*/

            return new SDCardFactory(project);
        }
    }
}