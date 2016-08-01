using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRoslynAnalysis
{
    class Class
    {
        public String Name { get; set; }

        public String FullName { get; set; }

        public Class() { }

        public Class(string name, string fullname)
        {
            Name = name;
            FullName = fullname;
        }

        public override string ToString()
        {
            return this.FullName+"."+this.Name;
        }

    }

}
