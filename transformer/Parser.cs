using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SimpleRoslynAnalysis
{
    class Parser
    {
        private static string pex_creation_tag = "PexChooseBehavedBehavior";
        private static Random random = new Random((int)DateTime.Now.Ticks);
        public static HashSet<Class> classesForFactory = new HashSet<Class>();
        public List<Exploration> parseReport()
        {

            XElement xelement = XElement.Load((string)Properties.Settings.Default["Pex_Report_Path"]);
            bool useStackInspection = (bool)Properties.Settings.Default["UseStackInspection"];
    
            IEnumerable<XElement> explorations = from exploration in xelement.Descendants("exploration")
                                                 select exploration;

            List<Exploration> explorationList = new List<Exploration>();
            foreach (XElement exploration in explorations)
            {
                Exploration explorationObject = new Exploration();

                XElement memberUnderTest = exploration.Element("memberUnderTest");
                if (memberUnderTest == null) // then no test
                {
                    Console.WriteLine("No passing test for function " + exploration.Attribute("fullName"));
                    continue;
                }
                explorationObject.FunctionName = memberUnderTest.Attribute("name").Value;
                XElement declaringType = memberUnderTest.Element("declaringType");
                explorationObject.NameSpace = declaringType.Attribute("namespace").Value;
                explorationObject.ClassName = declaringType.Attribute("name").Value;

                if (declaringType.Parent.Attribute("static") != null)
                {
                    explorationObject.IsStaticClass = Convert.ToBoolean(declaringType.Parent.Attribute("static").Value);
                }
                
                IEnumerable<XElement> generatedTests = exploration.Elements("generatedTest");


                XElement code = null;
                // loop backward to store the last working test
                if (generatedTests.Any())
                {
                    for (int i = generatedTests.Count() - 1; i >= 0; i--)
                    {
                        var item = generatedTests.ElementAt(i);
                        if (item != null)
                        {
                            if (item.Attribute("status").Value == "normaltermination")
                            {
                                code = item;
                                break;
                            }
                        }

                    }
                }

                if (code == null)
                {
                    Console.WriteLine("No passing test for function " + explorationObject.getFullName());
                    continue;

                }

                string status = code.Attribute("status").Value;
                bool primitiveTransformation = false;
                string declarationStatement = "";
                string result = "NOT-SET";

                // ignore the failing tests
                if (status != "normaltermination")
                    continue;
                String codeStr = code.Element("code").Value;
                IEnumerable<XElement> values = code.Elements("value");

                // setting the result value from the report
                foreach (XElement value in values)
                {
                    if (value.Attribute("name").Value == "result")
                    {
                        result = value.Value;
                        break;
                    }

                }
                explorationObject.ResultValue = result;

                StringBuilder transformedCode = new StringBuilder();
                /** // transform the code: 
                     Program s0 = new Program();
                     this.tryVoid(s0, 0, (string)null);
                     Assert.IsNotNull((object)s0);
                or this 
                [using (PexChooseBehavedBehavior.Setup())
                 {
              SInterface1 sInterface1;
              Program program;  ---> targetClassDeclarationIndex
              bool b;
              sInterface1 = new SInterface1();
              program = new Program((Interface1)sInterface1); --> targetClassAssignmentIndex
              b = this.isGood(program);
              Assert.AreEqual<bool>(false, b);
              Assert.IsNotNull((object)program);
            }
                                
                **/
                Console.WriteLine("one passing exploration in report for " + explorationObject.getFullName());
                string variableName = "s0"; // normally its s0 unless we are in the special case of using
                String[] codeStatements = codeStr.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                bool variableNameSet = false;

                if (codeStr.Contains(pex_creation_tag))
                {// the cases where pex cannot create the instances
                    int targetClassDeclarationIndex = -1;
                    for (int i = 0; i < codeStatements.Count(); i++)
                    {
                        String codeLine = codeStatements[i];
                        // detect where we declare the target class( before we have declaration of the used params
                        if (codeLine.Trim().StartsWith(explorationObject.ClassName) && targetClassDeclarationIndex == -1)
                        {

                            String[] parts = codeLine.Trim().Split(' ');

                            variableName = parts[1].Substring(0, parts[1].Count() - 1);
                            variableNameSet = true;
                            targetClassDeclarationIndex = i;
                            // delete everything before until this declration 
                            for (int k = 0; k < i; k++)
                            {
                                codeStatements[k] = "";
                            }

                        }

                        if (targetClassDeclarationIndex != -1 && i > targetClassDeclarationIndex)
                        {
                            // we started with the assignments here
                            if (codeLine.Contains("="))
                            {
                                if (codeLine.Contains(explorationObject.ClassName))
                                { // this target instance creation
                                    int eqIndex = codeLine.IndexOf("=");

                                    codeLine = codeLine.Substring(0, eqIndex + 1) + " " + (string)Properties.Settings.Default["Factory_Class_Name"] + ".Create" + explorationObject.ClassName + "();";
                                    classesForFactory.Add(new Class(explorationObject.ClassName, explorationObject.NameSpace));
                                    codeStatements[i] = codeLine;
                                    break;
                                }
                                else // the instatiation of the unwanted variable
                                {
                                    codeStatements[i] = "";
                                }
                            }

                        }


                    }

                    // remove the last }
                    codeStatements[codeStatements.Count() - 1] = "";

                }


                // loop again (ordinary cases the first time) for the case of the second transformation
                for (int j = 0; j < codeStatements.Count(); j++)
                {

                    String codeLine = codeStatements[j];

                    if (codeLine.Trim().StartsWith(explorationObject.ClassName) && !variableNameSet)
                    {

                        String[] parts = codeLine.Trim().Split(' ');
                        if (parts.Count() == 2 || codeLine.Trim().Contains("="))//???
                        {
                            variableName = parts[1].Substring(0, parts[1].Count()).Trim(new Char[] { ';' });
                            variableNameSet = true;
                        }

                    }


                    if (!(codeLine.Trim().Count() == 0))
                    {

                        if (Transformer.primitveTypes.Any(str => codeLine.Trim().StartsWith(str)))// this is a primitive declaration
                        {
                            if ((Transformer.responseScndOption == Transformer.RESPONSE_CODE_2 || Transformer.responseFirstOption == Transformer.RESPONSE_CODE_2) && Transformer.UsePrimitiveCombination)
                            { // we have primitive declration, we have the one of the options to crash and the option is enabled
                              // then we will be moving this declration even if it was not used in the primitive combination

                                primitiveTransformation = true;
                                codeLine = codeLine.Trim(new char[] { ';', ' ' });
                                string[] parts = codeLine.Split(' ');
                                explorationObject.variableName = parts[1];
                                if (parts[0].Trim() == "float")
                                {
                                    result = result + "f";

                                }
                                declarationStatement = codeLine + "= " + result + ";";
                                codeLine = "";
                            }



                        }

                        if (codeLine.Contains(explorationObject.FunctionName))// this is the call statement
                        {

                            if (codeLine.Contains(variableName + ","))
                            {
                                codeLine = codeLine.Replace(variableName + ",", "");
                            }
                            else if (codeLine.Contains(variableName) && !codeLine.Trim().StartsWith(variableName)) // there are cases when they call the function directly
                            {
                                codeLine = codeLine.Replace(variableName, "");
                            }

                            //if static class this.m() ->className.m() 
                            if(explorationObject.IsStaticClass)
                            {
                                codeLine = codeLine.Replace("this", explorationObject.FullClassName);
                            }
                            else
                            {
                                codeLine = codeLine.Replace("this", variableName);
                            }

                        }


                        else if (codeLine.Contains("Assert."))// this is the check statement
                        {
                            if (!codeLine.EndsWith(";"))
                            {// cases when this goes to two lines
                                codeLine = codeLine + " " + codeStatements[j + 1];
                                codeStatements[j + 1] = "";
                            }

                            if (codeLine.Contains("AreEqual"))// this is a comparison for the type functions; 
                                                              //Assert.AreEqual<string>("01-Jan-01 12:00:00 AM", s);

                            {
                                int position = codeLine.LastIndexOf(">");
                                codeLine = codeLine.Substring(position + 1);
                                codeLine = "\nif " + codeLine;
                                codeLine = codeLine.Replace(",", " != ");
                                codeLine = codeLine.Substring(0, codeLine.Count() - 1);
                                codeLine = codeLine + " { RESPONSE } ";
                            }
                            else if (codeLine.Contains("IsNotNull") || codeLine.Contains("IsNull"))// TODO for void functions ; Assert.IsNotNull((object)s0);
                            {

                                codeLine = "";
                            }

                        }


                        if (codeLine.Any())
                            transformedCode.Append(codeLine + "\n");

                    }// end of the empty if


                }
                // generate random variable name from 1-3 chars

                if (useStackInspection)// added this setting to test the code without the if statement against pattern matching
                {
                    transformedCode.Insert(0, "if(!System.Environment.StackTrace.Contains(\"" + explorationObject.getFullName() + "\"))\n{\n");
                }
                //else {
                //    transformedCode.Insert(0, "if(!Environment.StackTrace.Contains(\"" + explorationObject.getFullName() + "\"))\n{\n");
                //}

                if (primitiveTransformation)
                {
                    transformedCode.Insert(0, declarationStatement + "\n");
                }
                if (useStackInspection)
                {
                    transformedCode.Append("\n}");
                }

                //if contains s0 - definition of variable used to test (?!)
                if (transformedCode.ToString().Contains("s0"))
                {
                    //this fixes the "undeclared variable" exception
                    //caused by change of this.method -> varName.method without varName declaration
                    //this is the actual declaration
                    if (!explorationObject.IsStaticClass)
                    {
                        string randVar = RandomString(random.Next(2, 7));

                        //check if the s0 has a declaration already to avoid duplicated declaration
                        Regex reg = new Regex(@"[a-zA-Z]+ s0 = new [a-zA-Z]+\(\);");
                        var match = reg.Matches(transformedCode.ToString());
                        //if declaration does not exist -> declare
                        if (match.Count == 0)
                        {
                            string line = String.Format("var {0} = new {1}();\n", randVar, explorationObject.FullClassName);
                            transformedCode.Replace("{", "{\n" + line);
                        }
                        else //change existing declaration to use full class name Namespace.Class
                        {
                            transformedCode.Replace(explorationObject.ClassName, explorationObject.FullClassName);
                        }
                        //else -> just rename s0
                        transformedCode = transformedCode.Replace("s0", randVar);
                    }
                    //if class is static -> this.TestMethodCall
                    else
                    {
                        transformedCode = transformedCode.Replace("s0", explorationObject.ClassName);
                    }
                }
                explorationObject.ChallangeCode = transformedCode.ToString();

                //Console.WriteLine("trans" + transformedCode.ToString());

                explorationList.Add(explorationObject);
                //}



            }

            return explorationList;

        }

        private string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString().ToLower();
        }

    }

   
}
