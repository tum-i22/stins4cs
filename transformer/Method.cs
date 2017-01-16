using System;
using System.Collections.Generic;

namespace SimpleRoslynAnalysis.Model
{

    class Method
    {


        public String Name { get; set; }

        public String FullName { get; set; }
        public String Id { get; set; }
        public String Visibility { get; set; }
        public Boolean IsStatic { get; set; }
        public String ReturnType { get; set; }
        public List<Parameter> Parameters { get; set; }
        public string ClassName { get; set; }

        public String Comment { get; set; }

        public List<string> Usings { get; set; }


        // the expecte value
        public String ResultValue { get; set; }
        // the name of the variable that holds the result
        public String variableName { get; set; }
        // flag to indicate this function will be treated in primitive combination or not
        public Boolean PrimitiveCombination { get; set; }

        public String CombinedReturnStatement { get; set; }

        public String ChallengeCode { get; set; }

        public Boolean HasChallengeCode { get { return ChallengeCode != null ? true : false; } }
        public Method()
        {
            //this.ChallengeCodeList = new List<string>();
        }

        public Method(string name, string fullName, string id, string visibility, Boolean isStatic, string returnType, List<Parameter> parameters, string className, string comment)
        {
            this.Name = name;
            this.FullName = fullName;
            this.Id = id;
            this.Visibility = visibility;
            this.IsStatic = isStatic;
            this.ReturnType = returnType;
            this.Parameters = parameters;
            this.ClassName = className;

            this.Comment = comment;

            //this.ChallengeCodeList = new List<string>();

        }

        public string getNameSpace()
        {
            int lastDotIndex = FullName.LastIndexOf('.');
            return this.FullName.Substring(0, lastDotIndex);
        }

        public override string ToString()
        {
            return "Method: " + Id + (( ChallengeCode != null && ChallengeCode.Length>0 )?  "(true)" : "(false)");
            //+ FullName+" "+Id+" "+ Visibility+" "+IsStatic+" "+ReturnType+" "+Parameters+" "+ClassName+" "+Comment+" "+ ChallengeCodeList.Count+"\n" ;
        }


        
    }
}
