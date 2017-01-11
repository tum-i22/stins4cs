using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRoslynParsing
{
    class Program
    {
        static void Main(string[] args)
        {
            var tree = CSharpSyntaxTree.ParseText("s = this.Encode(s0, \"!\u00bf\u007f!\u007f!\");");
            var Mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var compilation = CSharpCompilation.Create("MyCompilation",
                syntaxTrees: new[] { tree }, references: new[] { Mscorlib });
            var model = compilation.GetSemanticModel(tree);

            var sNULL_arg = tree.GetRoot().DescendantNodes();
            
            //var assertStatement = SyntaxFactory.ParseStatement("s = this.Encode(s0, \"!\u00bf\u007f!\u007f!\");");
            //var arguments = assertStatement.DescendantNodes().OfType<ArgumentSyntax>().ToArray();


            //var type = model.GetTypeInfo(sNULL_arg);
        }
    }
}
