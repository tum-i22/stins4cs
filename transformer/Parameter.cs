using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRoslynAnalysis.Model
{
    class Parameter
    {
        private string Name { get; set; }
        private string Type { get; set; }

        public Parameter() {
        }
        public Parameter(string name, string type){
            this.Name = name;
            this.Type = type;

            }

        public override string ToString()
        {
            return "Param: " + Name + " " + Type+ " ";
        }

    }
}
