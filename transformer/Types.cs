using SimpleRoslynAnalysis.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRoslynAnalysis
{
    class Types
    {
        // this will be the index of where not to continue using the first option
        public static List<string> primitiveTypes = new List<string>()
        {"byte","sbyte","int","uint","short","ushort","long","ulong","float","double","char","bool","string","decimal"};
        public static List<string> numericPrimitiveTypes = new List<string>()
        {"byte","sbyte","int","uint","short","ushort","long","ulong","float","double","decimal"};

        public static bool IsPrimitive(String type)
        {
            return (primitiveTypes.Contains(type.Trim().ToLower()));
        }

        public static bool IsNumeric(string parentReturntype)
        {
            return (numericPrimitiveTypes.Contains(parentReturntype.Trim().ToLower()));

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
                    returnStatement = CreateNumericExpression(child.variableName, child.ResultValue);
                }
                else if (childReturnType.Trim().ToLower() == "bool")
                { // case 2 res+ (int) (exp xor actual)
                    returnStatement = CreateXorExpression(child.variableName, child.ResultValue, true);
                }
                else if (childReturnType.Trim().ToLower() == "string")
                { // case 4 res+ (int) (exp != actual)
                    returnStatement = CreateNagationLogicalExpression(child.variableName, child.ResultValue, true);
                }


                returnStatement = operand + returnStatement;
            }// end of numeric parent
            else if (parentReturntype.Trim() == "bool")
            { // cases 5-8
                operand = " ^ ";
                if (Types.IsNumeric(childReturnType) || childReturnType.Trim().ToLower() == "bool" || childReturnType.Trim().ToLower() == "char" || childReturnType.Trim().ToLower() == "string")
                { // case 5  6 7 8 return res && (expected == actual)
                    returnStatement = CreateNagationLogicalExpression(child.variableName, child.ResultValue, false);
                }
                returnStatement = operand + returnStatement;
            }// end of bool parent
            if (parentReturntype.Trim().ToLower() == "string")
            { // cases 13-16
                operand = ".Substring($)";
                if (Types.IsNumeric(childReturnType) || childReturnType.Trim().ToLower() == "char")
                { // case 14, 16 return res.Substring(act-exp)
                    returnStatement = operand.Replace("$", CreateNumericExpression(child.variableName, child.ResultValue));

                }
                else if (childReturnType.Trim().ToLower() == "bool")
                { // case 15 return res.Substring(act-exp)
                    returnStatement = operand.Replace("$", CreateXorExpression(child.variableName, child.ResultValue, true));
                }
                else if (childReturnType.Trim().ToLower() == "string")
                { // case 16 return res.Substring(act-exp)
                    returnStatement = operand.Replace("$", CreateNagationLogicalExpression(child.variableName, child.ResultValue, true));
                }

            }

            //Console.WriteLine(child.variableName + "==" + parent.ReturnType + "--" + child.ReturnType + "---" + returnStatement);

            child.CombinedReturnStatement = returnStatement;
            return child;
        }

        private static string CreateNumericExpression(string variableName, string resultValue)
        {
            // this method will create the expressions like (expected - actual) 

            return " (int)(" + variableName + " - " + resultValue + ")";
        }

        private static string CreateNagationLogicalExpression(string variableName, string resultValue, bool castToInt)
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
                    if ((GlobalVariables.Random.Next(1, 1000) % 2) == 0)
                        return " Convert.ToInt32(" + variableName + " != " + resultValue + ")";
                    else
                        return "((" + variableName + " != " + resultValue + ")" + "? 1 : 0)";
                }
            }
            return "(" + variableName + " != " + resultValue + ")";
        }

        private static string CreateXorExpression(string variableName, string resultValue, bool castToInt)
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

    }
}
