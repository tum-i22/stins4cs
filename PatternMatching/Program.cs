using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;


namespace PatternMatching
{
    class Program
    {
        private static readonly string FolderPath = (string)Properties.Settings.Default["code_path"];

        private static readonly string RegexStr = (string)Properties.Settings.Default["regex"];
        private static readonly string SampleName = (string)Properties.Settings.Default["sample_name"];

        private static readonly string Result_path = (string)Properties.Settings.Default["result_path"];

        private static bool wroteFirstLine = false;
        // complex data structure to hold the results
        private static readonly Dictionary<string, List<string>> Result = new Dictionary<string, List<string>>();
        static void Main(string[] args)
        {


            // looping the source file
            foreach (string file in Directory.EnumerateFiles(FolderPath, "*.cs",SearchOption.AllDirectories))
            {
                Console.WriteLine("Inspecting file " + file);
                List<string> results = new List<string>();
                string contents = File.ReadAllText(file);

                // Get a collection of matches.
                MatchCollection matches = Regex.Matches(contents, RegexStr);
                // Report the number of matches found.
                Console.WriteLine("{0} matches found in:\n   {1}",
                                  matches.Count,
                                  file);

                // Use foreach-loop.
                foreach (Match match in matches)
                {

                    int line = LineFromPos(contents, match.Index);
                    results.Add("Line: "+line.ToString()+ " Length : " + match.Length);

                }

                if (results.Any())
                {
                    Result.Add(file, results);
                }

            }

            WriteResult(Result);
        }

        private static void WriteResult(Dictionary<string, List<string>> result)
        {
            string fileName = Result_path + SampleName +"_"+ DateTime.Now.ToString("yyMMddHHmmss");
            fileName = fileName.Replace(' ', '_');
            
            File.WriteAllLines(fileName+".log",
                result.Select(x => FormatOccurances(x.Value,x.Key)).ToArray());
        }
        public static string FormatOccurances(List<string> results, string fileName )
        {
            string formattedString = "";
            if (!wroteFirstLine)
            {
                formattedString = "Result For:" + SampleName + " matched with regex: " + RegexStr + "\n";
                wroteFirstLine = true;
            }


            formattedString += results.Count + " matches found in: " + fileName + "\n";
            return results.Aggregate(formattedString, (current, result) => current + (result + "\n"));

        }
        public static int LineFromPos(string S, int Pos)
        {
            int Res = 1;
            for (int i = 0; i <= Pos - 1; i++)
                if (S[i] == '\n') Res++;
            return Res;
        }

    }

   
}

