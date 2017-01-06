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

    }
}
