using SimpleRoslynAnalysis.Model;
using SimpleRoslynAnalysis.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRoslynAnalysis
{
    class NetworkGenerator
    {
        public static Dictionary<String, Method> GenerateNetwork(List<Method> methodsList, bool cyclic = true)
        {
            if(cyclic)
            {
                return GenerateCyclicNetwork(methodsList);
            }
            else
            {
                return GenerateNonCyclicNetwork(methodsList);
            }
        }

        private static Dictionary<String, Method> GenerateCyclicNetwork(List<Method> methodsList)
        {
            Dictionary<String, Method> checkingNetwork = new Dictionary<String, Method>();
            // used to register what was assigned to prevent double assignments
            // in this loop we create netwroks not only one, but they are all stored in here
            // also we complete missing code snippets        
            int startIndex = 0;

            for (int i = 0; i < methodsList.Count; i++)
            {
                if (i % GlobalVariables.NODES_NETWORK == 0)
                {
                    startIndex = i;
                }
                // a round robin relation
                Method checkingMethod = methodsList[i];
                Method checkedMethod = null;
                // will leave ignoring voids for later stage

                // last item in a netwrok, or last item at all
                //  think about edge cases in here, one item left in a network or 2 ..etc
                if ((i % GlobalVariables.NODES_NETWORK) == GlobalVariables.NODES_NETWORK - 1 || (i == methodsList.Count - 1))
                {
                    // last item in smaller networ, points to first item in smaller network
                    if (startIndex != i)// cases of one item is not added
                        checkedMethod = methodsList[startIndex];
                }
                // normal round robin
                else
                {
                    // this is not the last item in smaller network
                    checkedMethod = methodsList[i + 1];
                }

                // replace the name of the checking function in the checking code now.
                // add the response mechanisim
                if (checkedMethod != null)
                {
                    string value = checkedMethod.ChallangeCode;
                    if (value != null)
                    {

                        // put the second option if there
                        if (i > Responces.ResponseSwitchIndex && Responces.ResponseSecondOption != "")
                        {
                            if (Responces.ResponseSecondOption == Responces.RESPONSE_CODE_2 && GlobalVariables.UsePrimitiveCombination)
                            { // for crash we can use the primitive combination if parent and child are primitives

                                //Console.WriteLine(checkedMethod.ReturnType + checkingMethod.ReturnType);
                                if (Types.IsPrimitive(checkedMethod.ReturnType) && Types.IsPrimitive(checkingMethod.ReturnType))
                                {// both primitve functions
                                    checkedMethod.PrimitiveCombination = true;
                                    value = Responces.RemoveResponsePart(value);
                                    checkedMethod = Transformer.CreateReturnStatement(checkedMethod, checkingMethod);

                                }
                                else
                                {
                                    value = value.Replace("RESPONSE", Responces.GetResponse(1, checkedMethod.Id));
                                }
                            }
                            else// regular scnd option
                            {
                                value = value.Replace("RESPONSE", Responces.GetResponse(1, checkedMethod.Id));
                            }


                        }
                        else // first option
                        {
                            if (Responces.ResponseFirstOption == Responces.RESPONSE_CODE_2 && GlobalVariables.UsePrimitiveCombination)
                            { // for crash we can use the primitive combination

                                if (Types.IsPrimitive(checkedMethod.ReturnType) && Types.IsPrimitive(checkingMethod.ReturnType))
                                {// both primitve functions
                                 //String returnSyntax = createReturnSyntax(checkedMethod, chec)
                                    checkedMethod.PrimitiveCombination = true;
                                    value = Responces.RemoveResponsePart(value);
                                    checkedMethod = Transformer.CreateReturnStatement(checkedMethod, checkingMethod);
                                }
                                else
                                {
                                    value = value.Replace("RESPONSE", Responces.GetResponse(0, checkedMethod.Id));
                                }
                            }
                            else
                            {
                                value = value.Replace("RESPONSE", Responces.GetResponse(0, checkedMethod.Id));
                            }
                        }


                        checkedMethod.ChallangeCode = value;
                    }
                    //Console.WriteLine("adding id "+checkingMethod.Id);
                    //todo there are cases of same function id in overloading
                    checkingNetwork[checkingMethod.Id] = checkedMethod;
                }
            }

            return checkingNetwork;
        }

        private static Dictionary<String, Method> GenerateNonCyclicNetwork(List<Method> methodsList)
        {
            Dictionary<String, Method> checkingNetwork = new Dictionary<String, Method>();


            return checkingNetwork;
        }
    }
}
