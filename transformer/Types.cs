using SimpleRoslynAnalysis.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRoslynAnalysis
{
    internal class PrimitiveType
    {
        internal string Type { get; set; }
        internal string FullCliType { get; set; }
        internal string CliType { get; set; }

        internal PrimitiveType(string type, string cliType)
        {
            Type = type.ToLower();
            FullCliType = cliType.ToLower();
            CliType = FullCliType.Replace("System.", "");
        }

        public static bool operator== (PrimitiveType pt, string type)
        {
            type = type.Trim().ToLower();
            return (pt.Type == type || pt.FullCliType == type || pt.CliType == type);
        }

        public static bool operator== (string type, PrimitiveType pt)
        {
            return pt == type;
        }

        public static bool operator!= (PrimitiveType pt, string type)
        {
            type = type.Trim().ToLower();
            return (pt.Type != type && pt.FullCliType != type && pt.CliType != type);
        }

        public static bool operator!= (string type, PrimitiveType pt)
        {
            return pt != type;
        }
    }

    static class PrimitiveTypes
    {
        internal static PrimitiveType Byte      = new PrimitiveType("byte"   , "System.Byte");
        internal static PrimitiveType SByte     = new PrimitiveType("sbyte"  , "System.SByte");
        internal static PrimitiveType Short     = new PrimitiveType("short"  , "System.Int16");
        internal static PrimitiveType Long      = new PrimitiveType("long"   , "System.Int64");
        internal static PrimitiveType Int       = new PrimitiveType("int"    , "System.Int32");
        internal static PrimitiveType UShort    = new PrimitiveType("ushort" , "System.UInt16");
        internal static PrimitiveType ULong     = new PrimitiveType("long"   , "System.UInt64");
        internal static PrimitiveType UInt      = new PrimitiveType("int"    , "System.UInt32");
        internal static PrimitiveType Double    = new PrimitiveType("double" , "System.Double");
        internal static PrimitiveType Float     = new PrimitiveType("float"  , "System.Single");
        internal static PrimitiveType Decimal   = new PrimitiveType("decimal", "System.Decimal");
        internal static PrimitiveType Char      = new PrimitiveType("char"   , "System.Char");
        internal static PrimitiveType String    = new PrimitiveType("string" , "System.String");
        internal static PrimitiveType Boolean   = new PrimitiveType("bool"   , "System.Boolean");

        internal static List<PrimitiveType> PrimitiveTypesList = new List<PrimitiveType>() { Byte, SByte, Short, UShort, Long, ULong, Int, UInt, Double, Float, Decimal, Char, String, Boolean };
        internal static List<PrimitiveType> PrimitiveNumericTypesList = new List<PrimitiveType>() { Byte, SByte, Short, UShort, Long, ULong, Int, UInt, Double, Float, Decimal };

        internal static bool IsPrimitive(string type)
        {
            return PrimitiveTypesList.Where(t => t == type).Count() > 0;
        }

        internal static bool IsNumeric(string type)
        {
            return PrimitiveNumericTypesList.Where(t => t == type).Count() > 0;
        }
    }

    class Types
    {        
        // this will be the index of where not to continue using the first option
        //public static List<string> primitiveTypes = new List<string>()
        //{,"sbyte","int","uint","short","ushort","long","ulong","float","double","char","bool","string","decimal"};
        //public static List<string> numericPrimitiveTypes = new List<string>()
        //{"byte","sbyte","int","uint","short","ushort","long","ulong","float","double","decimal"};

        public static bool IsPrimitive(String type)
        {
            return PrimitiveTypes.IsPrimitive(type);
        }

        public static bool IsNumeric(string type)
        {
            return PrimitiveTypes.IsNumeric(type);
        }


        public static Method CreateReturnStatement(Method child, Method parent)
        { // this method will work based on the 16 cases of combination that are in the design

            string parentReturntype = parent.ReturnType;
            string childReturnType = child.ReturnType;
            string operand = "";
            string returnStatement = "";
            if (Types.IsNumeric(parentReturntype) || parentReturntype == PrimitiveTypes.Char)
            { // cases 1-4 and 9-12
                operand = " + ";
                if (Types.IsNumeric(childReturnType) || childReturnType.Trim().ToLower() == PrimitiveTypes.Char)
                { // case 1, 3 return res + (expected - actual)
                    returnStatement = CreateNumericExpression(child.variableName, child.ResultValue);
                }
                else if (childReturnType.Trim().ToLower() == PrimitiveTypes.Boolean)
                { // case 2 res+ (int) (exp xor actual)
                    returnStatement = CreateXorExpression(child.variableName, child.ResultValue, true);
                }
                else if (childReturnType.Trim().ToLower() == PrimitiveTypes.String)
                { // case 4 res+ (int) (exp != actual)
                    returnStatement = CreateNagationLogicalExpression(child.variableName, child.ResultValue, true);
                }


                returnStatement = operand + returnStatement;
            }// end of numeric parent
            else if (parentReturntype.Trim() == PrimitiveTypes.Boolean)
            { // cases 5-8
                operand = " ^ ";
                if (Types.IsNumeric(childReturnType) || childReturnType.Trim().ToLower() == PrimitiveTypes.Boolean || childReturnType.Trim().ToLower() == PrimitiveTypes.Char || childReturnType.Trim().ToLower() == PrimitiveTypes.String)
                { // case 5  6 7 8 return res && (expected == actual)
                    returnStatement = CreateNagationLogicalExpression(child.variableName, child.ResultValue, false);
                }
                returnStatement = operand + returnStatement;
            }// end of bool parent
            if (parentReturntype.Trim().ToLower() == PrimitiveTypes.String)
            { // cases 13-16
                operand = ".Substring($)";
                if (Types.IsNumeric(childReturnType) || childReturnType.Trim().ToLower() == PrimitiveTypes.Char)
                { // case 14, 16 return res.Substring(act-exp)
                    returnStatement = operand.Replace("$", CreateNumericExpression(child.variableName, child.ResultValue));

                }
                else if (childReturnType.Trim().ToLower() == PrimitiveTypes.Boolean)
                { // case 15 return res.Substring(act-exp)
                    returnStatement = operand.Replace("$", CreateXorExpression(child.variableName, child.ResultValue, true));
                }
                else if (childReturnType.Trim().ToLower() == PrimitiveTypes.String)
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
