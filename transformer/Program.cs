using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SimpleRoslynAnalysis.Model;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;


using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;

namespace SimpleRoslynAnalysis
{
    class Transformer
    {
        private static Random rand = new Random();

        public static void Main(string[] args)
        {


            // .sln path could contain more than one project
            string pathToSolution = GlobalVariables.PathToSolution;
            //const string pathToSolution = @"C:\Users\IBM\Desktop\roslyn\roslyn-master\roslyn-master\src\Samples\Samples.sln";
            string projectName = GlobalVariables.ProjectName;

            // start Roslyn workspace
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            // formatting options
            var options = workspace.Options
    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true);            // open solution we want to analyze
            Solution solutionToAnalyze =
                workspace.OpenSolutionAsync(pathToSolution).Result;

            // get the project we want to analyze out
            // of the solution
            Project sampleProjectToAnalyze =
        solutionToAnalyze.Projects
                            .Where((proj) => proj.Name == projectName)
                            .FirstOrDefault();
            List<Method> methodsList = new List<Method>();

            // this prints all documents in the project
            foreach (var document in sampleProjectToAnalyze.Documents)
            {
                Console.WriteLine("documnet " + document.Name);
                SyntaxTree documentTree = document.GetSyntaxTreeAsync().Result;
                SemanticModel semanticModel = document.GetSemanticModelAsync().Result;
                //TODO: will we have more than class, if we change this we need to change the way we write the classes
                var firstClass = document.GetSyntaxRootAsync().Result.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (firstClass != null)
                {
                    // gets the namespace, like ConsoleApplication1.NestedNameSpace.Class1
                    string fullNameSpace = "";
                    if (semanticModel.GetDeclaredSymbol(firstClass) != null)
                    {
                        fullNameSpace = semanticModel.GetDeclaredSymbol(firstClass).ToString();
                    }

                    ///this will print all methods and not including the constructors, if we want to get the constructors we ask for ConstructorDeclarationSyntax instead of MethodDeclarationSyntax.

                    IEnumerable<MethodDeclarationSyntax> methods = firstClass.DescendantNodes()
                        .OfType<MethodDeclarationSyntax>();


                    // loop the methods of a class and create the model object of eacho one
                    foreach (MethodDeclarationSyntax method in methods)
                    {


                        Method methodObject = new Method();
                        methodObject.Name = method.Identifier.Text;
                        methodObject.FullName = fullNameSpace;
                        methodObject.Id = fullNameSpace + "." + method.Identifier.Text;

                        //Console.WriteLine("method " + methodObject.Id);
                        if (method.Modifiers.Any())// there are cases when visibility is not set
                            methodObject.Visibility = method.Modifiers[0].Text;
                        else
                            methodObject.Visibility = "";
                        //  set static to flase for now it not really used so far in the too
                        //method.Modifiers.Contains("static") ? true : false
                        methodObject.IsStatic = false;
                        methodObject.ReturnType = method.ReturnType.GetText().ToString();
                        //method.SyntaxTree.GetRootAsync().
                        //Console.WriteLine(method.);
                        List<Parameter> parameters = new List<Parameter>();
                        foreach (var p in method.ParameterList.Parameters)
                        {
                            Parameter paramObject = new Parameter(p.Identifier.Text, p.Type.ToString());
                            parameters.Add(paramObject);
                        }
                        // returns list of trivias before the function
                        SyntaxTriviaList trivia = method.GetLeadingTrivia();
                        methodObject.Comment = "";
                        foreach (SyntaxTrivia t in trivia)
                        {

                            bool isDocComment = SyntaxFacts.IsDocumentationCommentTrivia(t.Kind());
                            if (isDocComment)
                            {
                                var documentationComment =
                                                  (DocumentationCommentTriviaSyntax)t.GetStructure();
                                methodObject.Comment = documentationComment.GetText().ToString();
                                break;

                            }
                        }

                        methodObject.Parameters = parameters;

                        IEnumerable<IfStatementSyntax> ifStatements = method.DescendantNodes()
                        .OfType<IfStatementSyntax>();
                        IEnumerable<StatementSyntax> statements = method.DescendantNodes()
                      .OfType<StatementSyntax>();

                        IEnumerable<ReturnStatementSyntax> returnStatements = method.DescendantNodes()
                   .OfType<ReturnStatementSyntax>();
                        // do not add methods that should be ignored in transformation to the list
                        //Console.WriteLine(methodObject.Comment);

                        if (!methodObject.Comment.Contains(GlobalVariables.ignore_transform) && methodObject.Visibility.Trim().ToLower() == "public")
                        {
                            methodsList.Add(methodObject);
                        }
                        //Console.WriteLine("the number of if " + ifStatements.Count() + " the number of overall statemenets " + statements.Count());
                    }
                }// end of class if


            }// done looping the documents
            Console.WriteLine("Number of methods in the first list " + methodsList.Count());
            Responces.SetResponseSettings(GlobalVariables.ResponseCode, methodsList.Count());
            // start of phase two:
            // step 2 
            // parse the pex report 
            Parser parser = new Parser();
            List<Exploration> explorations = parser.parseReport();
            // merge results by full name

            // loop methods and add the challanges to it 
            // this maybe can be better optimized by doing this loop within other loops maybe the main loop of reading the code or the network generation loop
            foreach (Exploration exp in explorations)
            {
                //Console.WriteLine(exp.getFullName());
                Method result = methodsList.FirstOrDefault(s => s.Id == exp.FullFunctionName);
                // this can be null when the method is not in the list (ignored for instance)
                if (result != null)
                {
                    result.ChallangeCode = exp.ChallangeCode;
                    result.ResultValue = exp.ResultValue;
                    result.variableName = exp.variableName;
                }
            }

            var funtionsWithTests = methodsList.Where(s => explorations.Where(e => e.FullFunctionName == s.Id).FirstOrDefault() != null).ToList();

            var explorationsWithTests = explorations.Where(e => methodsList.Where(s => s.Id == e.FullFunctionName).FirstOrDefault() != null).ToList();

            Console.WriteLine("total Number of methods with explorations found " + explorations.Count() + " merged with methods " + funtionsWithTests.Count);

            // 2.1- shuffle the list of methods
            // can use shuffle or shuffle 2
            methodsList.Shuffle();
            // 2.2- create the checking network
            // dictonary the key is the checking function name and the value is the checked one 
            // value cannot be a void method??

            //NETWORK GENERATION!!!
            var checkingNetwork = NetworkGenerator.GenerateNetwork(methodsList);
            
            //methodsList.Print();
            Console.WriteLine("Netwrork of all methods");
            checkingNetwork.Print();
            //TODO could use this dictonary to creat a graph view xml


            // insert the checks


            // looping again the docs in the project to produce the output
            foreach (var document in sampleProjectToAnalyze.Documents)
            {


                //Console.WriteLine(sampleProjectToAnalyze.Name + "\t\t\t" + document.Name);
                SyntaxTree documentTree = document.GetSyntaxTreeAsync().Result;
                SemanticModel semanticModel = document.GetSemanticModelAsync().Result;
                var root = documentTree.GetRoot();


                // used for the bulk changes update of nodes
                var dict = new Dictionary<SyntaxNode, SyntaxNode>();
                // will we have more than class ???
                var firstClass = document.GetSyntaxRootAsync().Result.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (firstClass != null)
                {
                    // gets the namespace, like ConsoleApplication1.NestedNameSpace.Class1
                    string fullNameSpace = "";
                    if (semanticModel.GetDeclaredSymbol(firstClass) != null)
                    {
                        fullNameSpace = semanticModel.GetDeclaredSymbol(firstClass).ToString();
                    }


                    IEnumerable<MethodDeclarationSyntax> methods = firstClass.DescendantNodes()
                           .OfType<MethodDeclarationSyntax>();


                    // a set of "using" decalrtives that should be added to classes for the newly function calls
                    HashSet<string> usingsToAdd = new HashSet<string>();
                    // loop the methods of a class and create the model object of eacho one
                    foreach (MethodDeclarationSyntax method in methods)
                    {
                        String id = fullNameSpace + "." + method.Identifier.Text;
                        // get the checked method from dictnoary
                        // cases when no connections
                        if (checkingNetwork.ContainsKey(id))
                        {
                            Method checkedMethod = checkingNetwork[id];
                            // this dic will be used later to update all methods in the class
                            if (method.Body != null)
                            {
                                dict.Add(method, InsertCheck(method, checkedMethod));
                            }

                            usingsToAdd.Add(checkedMethod.getNameSpace());
                        }
                    }



                    UsingDirectiveSyntax[] items = new UsingDirectiveSyntax[usingsToAdd.Count()];
                    int i = 0;
                    // loop the usings to be added
                    foreach (string usingToAdd in usingsToAdd)
                    {

                        // add cases to remove same class using, check if this is taken care of by the compiler
                        var name = (SyntaxFactory.IdentifierName(usingToAdd));
                        items[i] = SyntaxFactory.UsingDirective(name).NormalizeWhitespace();
                        i++;
                    }


                    if (dict.Any())
                    {

                        // replace the methods and write the checks
                        root = root.ReplaceNodes(dict.Keys, (n1, n2) => dict[n1]).NormalizeWhitespace();
                        var compilationUnitSyntax = (CompilationUnitSyntax)root;
                        var newCompilationUnitSyntax = compilationUnitSyntax.AddUsings(items);

                        root = root.ReplaceNode(compilationUnitSyntax, newCompilationUnitSyntax).NormalizeWhitespace();

                        documentTree = documentTree.WithRootAndOptions(root, documentTree.Options);

                    }



                }
                //Formatter.
                // Format the document.
                root = Formatter.Format(root, workspace, options);
                string fileContent = UsingList.AddUsingListToOfFile(disablePragmaWarningCS0618(root.ToFullString()));
                // sampleProjectToAnalyze.Documents.Where( doc => doc.)
                string augmentedDocSaveDir = String.Format("{0}{1}", GlobalVariables.OutputDirectory, GenerateDocumentFolderPath(document.Folders));
                string augmentedDocSavePath = String.Format("{0}{1}", augmentedDocSaveDir, document.Name);
                Directory.CreateDirectory(augmentedDocSaveDir);
                File.WriteAllText(augmentedDocSavePath, fileContent);
            }// end of documents loop

            // add the factory class
            CompilationUnitSyntax factoryClass = InsertFactoryClass(Parser.classesForFactory);
            File.WriteAllText(GlobalVariables.OutputDirectory + GlobalVariables.FactoryName + ".cs", factoryClass.ToFullString());

        }

        private static object GenerateDocumentFolderPath(IReadOnlyList<string> folders)
        {
            string path = "";
            foreach(var folder in folders)
            {
                path += folder + @"\"; 
            }

            return path;
        }

        public static Method CreateReturnStatement(Method child, Method parent)
        { // this method will work based on the 16 cases of combination that are in the design

            string parentReturntype = parent.ReturnType;
            string childReturnType = child.ReturnType;
            string operand = "";
            string returnStatement = "";
            if (Types.IsNumeric(parentReturntype) || parentReturntype.Trim().ToLower() == "char")
            { // cases 1-4 and 9-12
                operand = " + ";
                if (Types.IsNumeric(childReturnType) || childReturnType.Trim().ToLower() == "char")
                { // case 1, 3 return res + (expected - actual)
                    returnStatement = createNumericExpression(child.variableName, child.ResultValue);
                }
                else if (childReturnType.Trim().ToLower() == "bool")
                { // case 2 res+ (int) (exp xor actual)
                    returnStatement = createXorExpression(child.variableName, child.ResultValue, true);
                }
                else if (childReturnType.Trim().ToLower() == "string")
                { // case 4 res+ (int) (exp != actual)
                    returnStatement = createNagationLogicalExpression(child.variableName, child.ResultValue, true);
                }


                returnStatement = operand + returnStatement;
            }// end of numeric parent
            else if (parentReturntype.Trim() == "bool")
            { // cases 5-8
                operand = " ^ ";
                if (Types.IsNumeric(childReturnType) || childReturnType.Trim().ToLower() == "bool" || childReturnType.Trim().ToLower() == "char" || childReturnType.Trim().ToLower() == "string")
                { // case 5  6 7 8 return res && (expected == actual)
                    returnStatement = createNagationLogicalExpression(child.variableName, child.ResultValue, false);
                }
                returnStatement = operand + returnStatement;
            }// end of bool parent
            if (parentReturntype.Trim().ToLower() == "string")
            { // cases 13-16
                operand = ".Substring($)";
                if (Types.IsNumeric(childReturnType) || childReturnType.Trim().ToLower() == "char")
                { // case 14, 16 return res.Substring(act-exp)
                    returnStatement = operand.Replace("$", createNumericExpression(child.variableName, child.ResultValue));

                }
                else if (childReturnType.Trim().ToLower() == "bool")
                { // case 15 return res.Substring(act-exp)
                    returnStatement = operand.Replace("$", createXorExpression(child.variableName, child.ResultValue, true));
                }
                else if (childReturnType.Trim().ToLower() == "string")
                { // case 16 return res.Substring(act-exp)
                    returnStatement = operand.Replace("$", createNagationLogicalExpression(child.variableName, child.ResultValue, true));
                }

            }


            //Console.WriteLine(child.variableName + "==" + parent.ReturnType + "--" + child.ReturnType + "---" + returnStatement);


            child.CombinedReturnStatement = returnStatement;
            return child;
        }

        private static string createNumericExpression(string variableName, string resultValue)
        {
            // this method will create the expressions like (expected - actual) 

            return " (int)(" + variableName + " - " + resultValue + ")";
        }





        private static string createNagationLogicalExpression(string variableName, string resultValue, bool castToInt)
        {
            // this method will create the expressions like (expected==actual)
            
            if (castToInt)
            {      //  depending on the setting BoolCastMode:
                // 1: inline if
                // 2: Convert.ToInt32
                // other: choose randomly 

                if (GlobalVariables.BoolCastMode == 1)
                {
                    return "((" + variableName + " != " + resultValue + ")" + "? 1 : 0)";
                }
                else if (GlobalVariables.BoolCastMode == 2)
                {
                    return " Convert.ToInt32(" + variableName + " != " + resultValue + ")";
                }
                else
                {
                    if ((rand.Next(1, 1000) % 2) == 0)
                        return " Convert.ToInt32(" + variableName + " != " + resultValue + ")";
                    else
                        return "((" + variableName + " != " + resultValue + ")" + "? 1 : 0)";
                }
            }
            return "(" + variableName + " != " + resultValue + ")";
        }

        private static string createXorExpression(string variableName, string resultValue, bool castToInt)
        {
            // this method will create the expressions like int (expected^actual)
            if (castToInt)
            {
                // choose randomly 
                //if ((rand.Next(1, 1000) % 2) == 0)
                    return " Convert.ToInt32(" + variableName + " ^ " + resultValue + ")";
               // else
               //     return "((" + variableName + " ^ " + resultValue + ")" + "? 1 : 0)";
            }

            return "(" + variableName + " ^ " + resultValue + ")";
        }


        public static MethodDeclarationSyntax InsertCheck(MethodDeclarationSyntax currMethod, Method checkedMethod)
        {

            //TODO decide in case of empty
            //Console.WriteLine(checkedMethod.ChallangeCode);
            if (checkedMethod.ChallangeCode == null || (checkedMethod.ChallangeCode.Length == 0))
            {
                return currMethod;
                //checkedMethod.ChallangeCode = "// testing empty";
            }

            var checkText = checkedMethod.ChallangeCode;
            string aLine = null;
            // create and fill statement list 
            List<StatementSyntax> statements = new List<StatementSyntax>();
            StringReader strReader = new StringReader(checkText);
            // added as a set of statements, otherwise it will be formatted as one statement
            while (true)
            {
                aLine = strReader.ReadLine();
                if (aLine != null)
                {

                    var checkStatement = SyntaxFactory.ParseStatement(aLine);
                    statements.Add(checkStatement);
                }
                else
                {
                    break;
                }
            }

            var bodyStatementsWithCheck = currMethod.Body.Statements.InsertRange(0, statements);

            var newBody = currMethod.Body.Update(SyntaxFactory.Token(SyntaxKind.OpenBraceToken), bodyStatementsWithCheck,
                                    SyntaxFactory.Token(SyntaxKind.CloseBraceToken));
            var newMethod = currMethod.ReplaceNode(currMethod.Body, newBody);

            IEnumerable<ReturnStatementSyntax> returnStatements = newMethod.DescendantNodes()
                  .OfType<ReturnStatementSyntax>();


            if (checkedMethod.PrimitiveCombination)
            {
                var dict = new Dictionary<SyntaxNode, SyntaxNode>();

                foreach (ReturnStatementSyntax returnStm in returnStatements)
                {
                    string originalReturn = returnStm.GetText().ToString().Trim().Trim(new char[] { ';' });
                    string newReturnStr = originalReturn + checkedMethod.CombinedReturnStatement;
                    if (currMethod.ReturnType.GetText().ToString().Trim() == "char")
                    {
                        // if the parent is char we have to add a cast here, cannot be done else where 
                        string[] parts = newReturnStr.Trim().Split(' ');
                        string result = "";
                        for (int i = 0; i < parts.Count(); i++)
                        {

                            if (parts[i] == "return")
                            {
                                parts[i] += " (char)(";
                            }

                            if (i == parts.Count() - 1)
                            {
                                parts[i] += ")";
                            }
                            result += " " + parts[i];
                        }
                        newReturnStr = result;
                    }

                    newReturnStr += ";";
                    var newReturn = SyntaxFactory.ParseStatement(newReturnStr);
                    dict.Add(returnStm, newReturn);
                    //newMethod = newMethod.ReplaceNode(returnStm, newReturn);
                }
                if (dict.Any())
                {
                    // replace the methods and write the checks
                    newMethod = newMethod.ReplaceNodes(dict.Keys, (n1, n2) => dict[n1]).NormalizeWhitespace();
                }
            }


            return newMethod;


        }

        public static CompilationUnitSyntax InsertFactoryClass(HashSet<Class> classesForFactory)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"class " + GlobalVariables.FactoryName + @"
{
}");
            var compilationUnit = (CompilationUnitSyntax)tree.GetRoot();

            // Get ClassDeclarationSyntax corresponding to 'class C' above.
            ClassDeclarationSyntax classDeclaration = compilationUnit.ChildNodes()
                .OfType<ClassDeclarationSyntax>().Single();
            MethodDeclarationSyntax[] methods = new MethodDeclarationSyntax[classesForFactory.Count()];
            UsingDirectiveSyntax[] items = new UsingDirectiveSyntax[classesForFactory.Count()];

            int i = 0;
            foreach (Class classObject in classesForFactory)
            {

                MethodDeclarationSyntax newMethodDeclaration =
                SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("public static " + classObject.Name), "Create" + classObject.Name)
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("null")).WithLeadingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.SingleLineCommentTrivia, "//TODO Add the code needed to create " + classObject.Name))));
                methods[i] = newMethodDeclaration;
                var name = (SyntaxFactory.IdentifierName(classObject.FullName));
                items[i] = SyntaxFactory.UsingDirective(name).NormalizeWhitespace(); ;
                i++;
            }

            // Add this new MethodDeclarationSyntax to the above ClassDeclarationSyntax.
            ClassDeclarationSyntax newClassDeclaration =
                classDeclaration.AddMembers(methods);

            // Update the CompilationUnitSyntax with the new ClassDeclarationSyntax.
            CompilationUnitSyntax newCompilationUnit =
                compilationUnit.ReplaceNode(classDeclaration, newClassDeclaration);
            newCompilationUnit = newCompilationUnit.AddUsings(items);
            // normalize the whitespace
            newCompilationUnit = newCompilationUnit.NormalizeWhitespace("    ");
            return newCompilationUnit;

        }

        private static string disablePragmaWarningCS0618(string fileContent)
        {
            string warningDisabler = "\n#pragma warning disable CS0618 // Type or member is obsolete";
            return warningDisabler + "\n" + fileContent;
        }
    }

    public static class UsingList
    {
        private static List<string> _usingList = new List<string>();

        public static void Add(UsingStatementSyntax uss)
        {
            _usingList.Add(uss.ToFullString());
        }

        public static string GetUsings()
        {
            string result = "";
            foreach(var use in _usingList)
            {
                result += use + "\n";
            }

            return result;
        }

        public static string AddUsingListToOfFile(string fileContent)
        {
            string[] additionalUsings = { "using System.Collections;",
                "using System.Text;",
                "using System.Globalization;",
                "using System.Collections.Generic;" };

            foreach (var _using in additionalUsings)
            {
                if (!fileContent.Contains(_using))
                {
                    fileContent = _using + "\n" + fileContent;
                }
            }

            return fileContent;
        }
    }
}