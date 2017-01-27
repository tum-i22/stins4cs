using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRoslynAnalysis
{
    public static class RoslynExtensions
    {
        public static string GetVariableDeclarationName(this VariableDeclarationSyntax variableDeclarationSyntax)
        {
            return variableDeclarationSyntax.DescendantNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault().ToFullString();
        }

        public static string GetVariableDeclarationType(this VariableDeclarationSyntax variableDeclarationSyntax)
        {
            return variableDeclarationSyntax.ChildNodes().First().ToFullString().Trim();
        }

        public static bool StatementHas<T>(string codeline) where T : CSharpSyntaxNode
        {
            var statement = SyntaxFactory.ParseStatement(codeline);
            return statement.DescendantNodes().OfType<T>().Count() > 0;
        }

        public static List<T> GetSyntaxNode<T>(string codeLine) where T : CSharpSyntaxNode
        {
            return SyntaxFactory.ParseStatement(codeLine).DescendantNodes().OfType<T>().ToList();
        }

    }
}
