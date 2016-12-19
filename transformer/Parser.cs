using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
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
                //case of nested classes in the cs file if does not have namespace
                if (declaringType.Attribute("namespace") == null)
                {
                    explorationObject.NameSpace = getNestedClassNamespace(declaringType);
                }
                else
                {
                    explorationObject.NameSpace = declaringType.Attribute("namespace").Value;
                }
                
                //get class name
                explorationObject.ClassName = declaringType.Attribute("name").Value;

                //check if the class is static -> used in parsing the check
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
                List<string> codeStatements = codeStr.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                string variableName = "s0"; // normally its s0 unless we are in the special case of using

                //variable under test is not always called s0.
                //find the varibale and change the name of the variable to search for
                //otherwise leave as is
                var varDeclarator = SyntaxFactory.ParseStatement(codeStatements[0]).DescendantNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
                if(varDeclarator != null)
                {
                    variableName = varDeclarator.ToFullString();
                }

                bool variableNameSet = false;

                /*
                    case: code starts "using (PexDisposableContext disposables = PexDisposableContext.Create())"
                    solution: if first line has using + PexDisposableContext
                                remove first and last lines
                */

                if(codeStatements[0].Contains("using") && codeStatements[0].Contains("PexDisposableContext"))
                {
                    //muust get PexDisposableContext var and delete any mention of it in the method
                    var usingVarDeclSyntx = SyntaxFactory.ParseStatement(codeStatements[0]).DescendantNodes().OfType<VariableDeclarationSyntax>().FirstOrDefault();

                    //assume we have only 1 delcaration in the using statement
                    var varNameIdentifiers = usingVarDeclSyntx.Variables[0].Identifier.Value.ToString();// usingVarDeclSyntx.Variables;

                    //remove "using (PexDisposableContext <varName> = PexDisposableContext.Create())"
                    codeStatements.RemoveAt(0);
                    //remove {
                    codeStatements.RemoveAt(0);
                    //remove }
                    codeStatements.RemoveAt(codeStatements.Count-1);

                    var codeLinesToDelete = codeStatements.Where(codeline => codeline.Contains(varNameIdentifiers)).ToList();
                    foreach(var line in codeLinesToDelete)
                    {
                        codeStatements.Remove(line);
                    }
                }

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

                        //handling of THIS statement
                        if (codeLine.Contains(explorationObject.FunctionName))// this is the call statement
                        {
                            var codeLineStatementCompontnets = SyntaxFactory.ParseStatement(codeLine).DescendantNodes();
                            var thisStatement = codeLineStatementCompontnets.OfType<ThisExpressionSyntax>();

                            if(thisStatement.Count() > 0)
                            {
                                var argumentLists = codeLineStatementCompontnets.OfType<ArgumentListSyntax>();
                                if(argumentLists.Count() > 0)
                                {
                                    foreach(var argumentList in argumentLists)
                                    {
                                        if (argumentList.Arguments.Count > 0)
                                        {
                                            var firstVariable = argumentList.Arguments.FirstOrDefault().ToFullString();
                                            if (firstVariable == variableName)
                                            {
                                                string funcArgs = argumentList.Arguments.ToString().Replace(firstVariable+",","");
                                                string funcName;
                                                //case when it is a function
                                                try
                                                {
                                                    funcName = SyntaxFactory.ParseStatement(codeLine).DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault().ToFullString();
                                                }
                                                //case when it is a get method
                                                catch
                                                {
                                                    var temp1 = SyntaxFactory.ParseStatement(codeLine).DescendantNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
                                                    funcName = temp1.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault().ToString();
                                                }
                                                codeLine = String.Format("{0}.{1}({2});", firstVariable,funcName,funcArgs);
                                            }
                                        }
                                    }
                                }
                            }
                            //if static class this.m() ->className.m() 
                            if (explorationObject.IsStaticClass)
                            {
                                codeLine = codeLine.Replace("this", explorationObject.FullClassName);
                            }
                        }


                        if (codeLine.Contains("Assert."))// this is the check statement
                        {
                            //if method call is several lines long, get it all together.
                            while (!codeLine.EndsWith(";"))
                            {
                                codeLine += codeStatements[j + 1];
                                codeStatements.RemoveAt(j + 1);
                            }
                            codeStatements[j] = codeLine;


                            /*{
                                 var assertStatement = SyntaxFactory.ParseStatement(codeLine);
                                 var arguments = assertStatement.DescendantNodes().OfType<ArgumentSyntax>().ToArray();

                                 codeLine = String.Format("\nif ({0} == null)", arguments[0].ToFullString());
                                 codeLine = codeLine + " { RESPONSE } ";
                             }
                             else if (codeLine.Contains("IsNull"))
                             {
                                 var assertStatement = SyntaxFactory.ParseStatement(codeLine);
                                 var arguments = assertStatement.DescendantNodes().OfType<ArgumentSyntax>().ToArray();

                                 codeLine = String.Format("\nif ({0} != null)", arguments[0].ToFullString());
                                 codeLine = codeLine + " { RESPONSE } ";
                             }*/
                            if (codeLine.Contains("AreEqual"))// this is a comparison for the type functions; 
                                                              //Assert.AreEqual<string>("01-Jan-01 12:00:00 AM", s);

                            {
                                var assertStatement = SyntaxFactory.ParseStatement(codeLine);
                                var arguments = assertStatement.DescendantNodes().OfType<ArgumentSyntax>().ToArray();

                                codeLine = String.Format("\nif ({0} != {1})",arguments[0].ToFullString(),arguments[1].ToFullString());
                                codeLine = codeLine + " { RESPONSE } ";
                            }
                            else if (codeLine.Contains("IsNotNull") || codeLine.Contains("IsNull"))// TODO for void functions ; Assert.IsNotNull((object)s0);
                            {

                                codeLine = "";
                            }

                            /*
                                if(!System.Environment.StackTrace.Contains("Sharpen.AList`1.EnsureCapacity"))
                                {
                                    AList<int> aList;
                                    int[] ints = new int[1];
                                    aList = new AList<int>((IEnumerable<int>)ints);
                                    s0.EnsureCapacity<int>(aList, 5); <- this case

                                    if (5 != ((List<int>)aList).Capacity) 
                                    { RESPONSE } 

                                    if (1 != ((List<int>)aList).Count) 
                                    { RESPONSE } 
                                    }
                                }
                            */
                            
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

                //case: s0 - not a valid variable
                //comment: if used - most likely the value needed is used as an argument in the method
                if (transformedCode.ToString().Contains("s0"))
                {
                    if (!explorationObject.IsStaticClass)
                    {
                        string randVar = RandomString(random.Next(2, 7));

                        //check if the s0 has a declaration already to avoid duplicated declaration
                        Regex reg = new Regex(@"(.)+ s0 = new (.)+\(\)\;");
                        var match = reg.Matches(transformedCode.ToString());
                        //if declaration does not exist -> declare
                        if (match.Count == 0)
                         //change existing declaration to use full class name Namespace.Class
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

        private string getNestedClassNamespace(XElement declaringType)
        {
            //if the object does has a namespace -> return fullNameSpace = namespace + class
            if (declaringType.Attributes().Where(atr => atr.Name == "namespace").Count() > 0)
            {
                string className = declaringType.Attribute("name").Value;
                string namespaceName = declaringType.Attribute("namespace").Value;
                return namespaceName + "." + className + ".";
            }
            //otherwise -> go deeper and check if it has a namespace
            else
            {
                var childNode = declaringType.Element("declaringType");
                //get namespace for the nested class host
                string className = declaringType.Attribute("name").Value;
                //the obtained result is the namespace
                return getNestedClassNamespace(childNode) + className;
            }
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
