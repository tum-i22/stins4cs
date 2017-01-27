using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace SimpleRoslynAnalysis
{
    class GlobalVariables
    {
        internal static Random Random = new Random();
        internal static string ResponseCode { get { return (string)Properties.Settings.Default["Response_Code"]; } }
        internal static string FactoryName { get { return (string)Properties.Settings.Default["Factory_Class_Name"]; } }
        internal static bool UsePrimitiveCombination { get { return (bool)Properties.Settings.Default["UseprimitiveCombination"]; } }
        //"REMOTE_LOG 50, DELAYED_CRASH 50";
        //"CRASH 100,DO_NOTHONG 40";
        //, DELAYED_CRASH 0,REMOTE_LOG 0";
        internal static int NODES_NETWORK { get { return (int)Properties.Settings.Default["NodeS_Network"]; } }
        internal static int BoolCastMode { get { return (int)Properties.Settings.Default["BoolCastMode"]; } }
        internal static string ignore_transform { get { return (string)Properties.Settings.Default["Ignore_Annotation"]; } }

        internal static string PexReportPath { get { return (string)Properties.Settings.Default["Pex_Report_Path"]; } }

        internal static bool UseStackInspection { get { return (bool)Properties.Settings.Default["UseStackInspection"]; } }

        internal static string FactoryClassName { get { return (string)Properties.Settings.Default["Factory_Class_Name"]; } }

        internal static string OutputDirectory { get { return (string)Properties.Settings.Default["Output_Dir"]; } }

        // .sln path could contain more than one project
        internal static string PathToSolution { get {return (string)Properties.Settings.Default["Path_To_Solution"]; } }
        //const string pathToSolution = @"C:\Users\IBM\Desktop\roslyn\roslyn-master\roslyn-master\src\Samples\Samples.sln";
        internal static string ProjectName { get { return (string)Properties.Settings.Default["Project_Name"]; } }

        internal static int DelayedCrashUpperBound { get { return (int)Properties.Settings.Default["Delayed_Crash_Upper_Bound_Sec"]; } }
        internal static string LogMessage { get { return "\"" + (string)Properties.Settings.Default["Log_Message"] + "\""; } }

        internal static bool NonCyclicNetworks {  get { return (bool)Properties.Settings.Default["NonCyclicNetwork"]; } }

        internal static bool HighlightChecksForAnalysis { get { return (bool)Properties.Settings.Default["HighlightChecksForAnalysis"]; } }

    }
}
