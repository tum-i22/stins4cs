using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRoslynAnalysis
{
    static class Responces
    {
        public const string RESPONSE_CODE_1 = "DO_NOTHONG";
        public const string RESPONSE_CODE_2 = "CRASH";
        public const string RESPONSE_CODE_3 = "DELAYED_CRASH";
        public const string RESPONSE_CODE_4 = "REMOTE_LOG";

        // we may have up to two response optins
        public static string ResponseFirstOption { get; set; }
        public static string ResponseSecondOption { get; set; }

        public static int ResponseSwitchIndex { get; set; }

        private static string RESPONSE_CODE_1_SOURCE = " ";
        private static string RESPONSE_CODE_2_SOURCE = " System.Environment.Exit(0); ";
        private static string RESPONSE_CODE_3_SOURCE = @" Random r = new Random();
            System.Timers.Timer aTimer = new System.Timers.Timer(r.Next(1," + GlobalVariables.DelayedCrashUpperBound + @")*1000);
            aTimer.Elapsed += (sender, e) => {
                System.Environment.Exit(0);
            }; 
            aTimer.Enabled = true;";

        private static string RESPONSE_CODE_4_SOURCE = @"log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Fatal(" + GlobalVariables.LogMessage + ");";



        ///<summary>
        /// returns the code statemenet of the response
        /// </summary>
        public static string GetResponse(int i, String methodName)
        {
            string responseType = (i == 0) ? ResponseFirstOption : ResponseSecondOption;
            string response;
            switch (responseType)
            {
                case RESPONSE_CODE_1:
                    response = RESPONSE_CODE_1_SOURCE;
                    break;
                case RESPONSE_CODE_2:
                    response = RESPONSE_CODE_2_SOURCE;
                    break;
                case RESPONSE_CODE_3:
                    response = RESPONSE_CODE_3_SOURCE;
                    break;
                case RESPONSE_CODE_4:
                    response = RESPONSE_CODE_4_SOURCE.Replace("$", methodName);
                    break;
                default:
                    response = RESPONSE_CODE_1_SOURCE;
                    break;
            }

            return response;

        }

        public static void SetResponseSettings(string responseCode, int methodsCount)
        {
            // we have DO_NOTHING CRASH DELAYED_CRASH REMOTE_LOG
            // we can combine  DO_NOTHONG 80,CRASH 20; 80,20 /100 comma seperated 
            string[] options = responseCode.Split(',');
            for (int i = 0; i < options.Count(); i++)
            {
                // now split the wieght from the action
                // no cross validation of the weight will be done (no 20+80== 100) 
                string[] optionSettings = options[i].Split(' '); // second is the weight
                if (i == 0)
                {
                    Responces.ResponseFirstOption = optionSettings[0];
                    double weight = double.Parse(optionSettings[1]);
                    ResponseSwitchIndex = (int)((weight / 100) * methodsCount);
                }
                else
                {
                    Responces.ResponseSecondOption = optionSettings[0];
                }
            }

            //Console.WriteLine(responseFirstOption + "---" + responseScndOption + "---" + responseSwitchIndex);

        }

        public static string RemoveResponsePart(string value)
        {
            // for primitive combination we need to remove the response part
            //if (!Environment.StackTrace.Contains("ConsoleApplication1.Program.isGood"))
            //{
            //    Program program;
            //    program = ClassFactory.CreateProgram();
            //    b = program.isGood();

            //   --> if (false != b) { RESPONSE }<--

            //}

            int lastSemiColonIndex = value.LastIndexOf(';');
            value = value.Substring(0, lastSemiColonIndex + 1) + "\n}";



            return value;
        }
    }
}
