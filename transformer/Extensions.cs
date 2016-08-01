﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;

namespace SimpleRoslynAnalysis
{
    public static class Extensions
    {
        public static string GetFullNamespace(this ISymbol symbol)
        {
            if ((symbol.ContainingNamespace == null) ||
                 (string.IsNullOrEmpty(symbol.ContainingNamespace.Name)))
            {
                return null;
            }

            // get the rest of the full namespace string
            string restOfResult = symbol.ContainingNamespace.GetFullNamespace();

            string result = symbol.ContainingNamespace.Name;

            if (restOfResult != null)
                // if restOfResult is not null, append it after a period
                result = restOfResult + '.' + result;

            return result;
        }

        public static string GetFullTypeString(this INamedTypeSymbol type)
        {
            string result = type.Name;

            if (type.TypeArguments.Count() > 0)
            {
                result += "<";

                bool isFirstIteration = true;
                foreach(INamedTypeSymbol typeArg in type.TypeArguments)
                {
                    if (isFirstIteration)
                    {
                        isFirstIteration = false;
                    }
                    else
                    {
                        result += ", ";
                    }

                    result += typeArg.GetFullTypeString();
                }

                result += ">";
            }

            return result;
        }

        public static string ConvertAccessabilityToString(this Accessibility accessability)
        {
            switch (accessability)
            {
                case Accessibility.Internal:
                    return "internal";
                case Accessibility.Private:
                    return "private";
                case Accessibility.Protected:
                    return "protected";
                case Accessibility.Public:
                    return "public";
                case Accessibility.ProtectedAndInternal:
                    return "protected internal";
                default:
                    return "private";
            }
        }

        public static string GetMethodSignature(this IMethodSymbol methodSymbol)
        {
            string result = methodSymbol.DeclaredAccessibility.ConvertAccessabilityToString();

            if (methodSymbol.IsAsync)
                result += " async";

            if (methodSymbol.IsAbstract)
                result += " abstract";

            if (methodSymbol.IsVirtual)
            {
                result += " virtual";
            }

            if (methodSymbol.IsStatic)
            {
                result += " static";
            }

            if (methodSymbol.IsOverride)
            {
                result += " override";
            }

            if (methodSymbol.ReturnsVoid)
            {
                result += " void";
            }
            else
            {
                result += " " + (methodSymbol.ReturnType as INamedTypeSymbol).GetFullTypeString();
            }

            result += " " + methodSymbol.Name + "(";

            bool isFirstParameter = true;
            foreach(IParameterSymbol parameter in methodSymbol.Parameters)
            {
                if (isFirstParameter)
                {
                    isFirstParameter = false;
                }
                else
                {
                    result += ", ";
                }

                if (parameter.RefKind == RefKind.Out)
                {
                    result += "out ";
                }
                else if (parameter.RefKind == RefKind.Ref)
                {
                    result += "ref ";
                }

                string parameterTypeString = 
                    (parameter.Type as INamedTypeSymbol).GetFullTypeString();

                result += parameterTypeString;
                    
                result += " " + parameter.Name;

                if (parameter.HasExplicitDefaultValue)
                {
                    result += " = " + parameter.ExplicitDefaultValue.ToString();
                }
            }

            result += ")";

            return result;
        }

        public static object 
            GetAttributeConstructorValueByParameterName(this AttributeData attributeData, string argName)
        {

            // Get the parameter
            IParameterSymbol parameterSymbol = attributeData.AttributeConstructor
                .Parameters
                .Where((constructorParam) => constructorParam.Name == argName).FirstOrDefault();

            // get the index of the parameter
            int parameterIdx = attributeData.AttributeConstructor.Parameters.IndexOf(parameterSymbol);

            // get the construct argument corresponding to this parameter
            TypedConstant constructorArg = attributeData.ConstructorArguments[parameterIdx];

            // return the value passed to the attribute
            return constructorArg.Value;
        }

        public static List<T> GetRandomElements<T>(this IEnumerable<T> list, int elementsCount)
        {
            return list.OrderBy(arg => Guid.NewGuid()).Take(elementsCount).ToList();
        }
    }
}
