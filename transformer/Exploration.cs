using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRoslynAnalysis
{   // holds the info parsed from pex report for each exploration (function)
    class Exploration
    {
        public String FunctionName { get; set; }

        public String NameSpace { get; set; }

        public String ClassName { get; set; }

        public String ResultValue { get; set; }

        public String variableName { get; set; }
        public String ChallangeCode { get; set; }

        public Exploration()
        {
            //this.ChallangeCodeList = new List<string>();
        }
        public String getFullName()
        {
            return this.NameSpace + "." + this.ClassName + "." + this.FunctionName;
        }

     


    }
}
