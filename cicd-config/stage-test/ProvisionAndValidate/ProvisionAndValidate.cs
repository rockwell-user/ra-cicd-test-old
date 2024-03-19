using RockwellAutomation.LogixDesigner;

namespace ProvisionAndValidate
{
    /// <summary>
    /// Example class to show how you might deploy a project to a controller and then validate the information.
    /// Uses the initializer pattern
    /// as the initial steps are asynchronous.
    /// </summary>
    public class ProvisionAndValidate
    {
        /// <summary>
        /// Reference to the <see cref="LogixProject"/>.
        /// </summary>
        private LogixProject _logixProject;
        private LogixProject.TagOperationMode tagModeOnline = LogixProject.TagOperationMode.Online;

        /// <summary>
        /// Instantiates an instance of the <see cref="ProvisionAndValidate"/> class.
        /// </summary>
        /// <param name="project">The <see cref="LogixProject"/>.</param>
        private ProvisionAndValidate(LogixProject project)
        {
            _logixProject = project;
        }

        /// <summary>
        /// Method to validate a given string matches a string value in a controller.
        /// </summary>
        /// <param name="tagPath">The full path to the tag.</param>
        /// <param name="expectedValue">The value we should expect to see.</param>
        /// <returns>Boolean true if the values match.</returns>
        /// <exception cref="ValueMismatchException">An exception if the values do not match the expected value.</exception>
        public async Task<bool> StringValueMatches(string tagPath, string expectedValue)
        {
            try
            {
                var actualValue = await _logixProject.GetTagValueSTRINGAsync(tagPath, tagModeOnline);
                bool valuesMatch = expectedValue == actualValue;
                if (!valuesMatch)
                {
                    throw new ValueMismatchException($"Value ${tagPath} did not match. Actual: ${actualValue} Expected: ${expectedValue}");
                }
                return valuesMatch;
            }
            catch (LogixSdkException)
            {
                Console.Write($"Something went wrong trying to get tag value ${tagPath}.");
                throw;
            }
        }

        /// <summary>
        /// Method to validate a given Dint value in a controller.
        /// </summary>
        /// <param name="tagPath">The full path to the tag.</param>
        /// <param name="expectedValue">The value we should expect to see.</param>
        /// <returns></returns>
        /// <exception cref="ValueMismatchException">An exception if the values do not match the expected value.</exception>
        public async Task<bool> DINTValueMatches(string tagPath, int expectedValue)
        {
            try
            {
                var actualValue = await _logixProject.GetTagValueDINTAsync(tagPath, tagModeOnline); /*! Get the value !*/
                bool valuesMatch = expectedValue == actualValue; /*! Compare !*/
                if (!valuesMatch)
                {
                    throw new ValueMismatchException($"Value ${tagPath} did not match. Actual: ${actualValue} Expected: ${expectedValue}");
                }
                return valuesMatch;
            }
            catch (LogixSdkException)
            {
                /*! If somethine went wrong getting the tag value, output an error message and re-throw */
                Console.Write($"Something went wrong trying to get tag value ${tagPath}.");
                throw;
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="LogixProject"/> and stores it for use later.
        /// </summary>
        /// <param name="acdPath">The ACD path.</param>
        /// <param name="commPath">The comm path to the controller.</param>
        /// <returns>An instance of the ProvisionAndValidate class.</returns>
        public static async Task<ProvisionAndValidate> Init(string acdPath, string commPath)
        {
            var proj = await LogixProject.OpenLogixProjectAsync(acdPath);
            await proj.SetCommunicationsPathAsync(commPath);
            await proj.ChangeControllerModeAsync(LogixProject.RequestedControllerMode.Program);
            await proj.DownloadAsync();
            await proj.ChangeControllerModeAsync(LogixProject.RequestedControllerMode.Run);

            return new ProvisionAndValidate(proj);
        }
    }

    /// <summary>
    /// Custom exception class for values that mismatch.
    /// </summary>
    public class ValueMismatchException : Exception
    {
        public ValueMismatchException(string message)
            : base(message)
        {
        }
    }
}