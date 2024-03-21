using RockwellAutomation.LogixDesigner;

namespace CreateDeploymentSdCard
{
    /// <summary>
    /// This class shows how to make a program which can compile into an executable (.exe) file
    /// while using FactoryTalk Logix Designer SDK.
    /// 
    /// This example program will allow users to download a single ACD file to a controller, and then save
    /// the project file from the controller to SD card any number of times. Pausing between each save to allow
    /// the SD card to be swapped out.
    /// 
    /// The program could be used to quickly provision an arbitrary number of controllers via
    /// SD cards. For instance when commissioning several identical production lines.
    /// 
    /// Use this program by running the .exe like so:
    /// 
    /// SDCardFactory C:\myProjects\myProject.acd AB_ETH\10.0.0.1 6
    /// </summary>
    internal class HowToUse
    {
        /// <summary>
        /// Will be used to reference to the <see cref="SDCardFactory"/>SDCardFactory class.
        /// </summary>
        private static SDCardFactory _sdCardFactory;
        
        /// <summary>
        /// All executable files must have a public, static method called Main
        /// in order to run. This is the function that will run when SDCardFactory.exe is called
        /// from the command prompt.
        /// </summary>
        /// <param name="args">
        /// The parameters passed in from the command line are stored in an array which gets passed into
        /// the Main method. 
        /// </param>
        public static async Task Main(string[] args)
        {
            string acdPath = args[0]; /*! The first argument will be stored as ACD path !*/
            string commPath = args[1]; /*! The second argument will be stored as comm path !*/
            int howMany = Convert.ToInt32(args[2]); /*! The third argument will be converted into an int and used to determine how many SD cards to write. !*/

            // Initialize the SD Card factory and store a reference to it.
            _sdCardFactory = await SDCardFactory.Init(acdPath, commPath);

            // This loop will run until it has written as many SD cards as you like
            // or until canceled by the user.
            for (int i = 0; i < howMany; i++)
            {
                bool cancelledByUser = await HandleLoadToSdCardAndPrompt(i, howMany);
                if (cancelledByUser)
                {
                    /*! If the user canceled, break the loop !*/
                    Console.WriteLine("Operation canceled by user");
                    break;
                }
            }
            // Once the Main method ends, the program will automatically exit.
        }

        /// <summary>
        /// This function does the work of calling the methods to write to the SD card
        /// as well as handle <see cref="LogixSdkException"/> exceptions or user input.
        /// </summary>
        /// <param name="i">Which iteration of the loop you are on. Used to determine which message to show.</param>
        /// <param name="howMany">How many iterations total. Used to determine which message to show.</param>
        /// <returns>A boolean value representing if the user has elected to cancel and end the program.</returns>
        private static async Task<bool> HandleLoadToSdCardAndPrompt(int i, int howMany)
        {
            bool cancelledByUser = false; /*! This variable gets returned at the end of the method and is used to break the loop early if true !*/
            Console.WriteLine("\nWriting to SD card...");
            try { 
                await _sdCardFactory.LoadToSdCard(); /*! Call the method on the SD card factory which we stored back on line 43 !*/
                Console.WriteLine("Successfully wrote to SD card.");

                /// If loading to SD card was successful we will end up here. Otherwise we will end up in the catch block if Logix SDK throws an error.

                if (i < howMany - 1) /*! If it is not the last card prompt the user to continue or exit the program */
                {
                    Console.WriteLine("You may swap the SD Card when it is safe to do so.");
                    Console.WriteLine("\nPress any key to save project to the next SD card... (Press escape to exit)");
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.Escape)
                    {
                        cancelledByUser = true;
                    }
                }
                else
                {
                    Console.WriteLine("All done!");
                }
            }
            catch(LogixSdkException ex) /*! NOTE: We are only catching Logix Designer SDK exceptions here. Other exceptions would cause this program to end early.*/
            {
                /// If there was an error, ask the user if they want to try again.
                var tryAgain = PromptTryAgain(ex);
                if (tryAgain)
                {
                    return await HandleLoadToSdCardAndPrompt(i, howMany); /*! This function uses recursion to call itself repeatedly until it succeeds or the user elects not to try again. !*/
                }
                else
                {
                    cancelledByUser = true;
                }
            }
            return cancelledByUser;
        }

        /// <summary>
        /// Prompt to ask the user if they would like to try again.
        /// </summary>
        /// <param name="e">The <see cref="LogixSdkException"/> which was thrown.</param>
        /// <returns>Boolean true if the user pressed Y, boolean false if they pressed N.</returns>
        private static bool PromptTryAgain(LogixSdkException e)
        {
            ConsoleKey response;
            do /*! Prompt the user repeatedly until they provide a valid response (y or n) !*/
            {
                Console.WriteLine("FAILED!!!");
                Console.WriteLine("Unable to write to SD card. Message was:");
                Console.WriteLine(e.Message);
                Console.Write($"\nTry again? [y/n] ");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);

            return (response == ConsoleKey.Y);
        }
    }
}
