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
        public static Dictionary<String, Method> GenerateCheckingNetwork(List<Method> methodsList)
        {
            if (!GlobalVariables.UsePrimitiveCombination)
            {
                return NEW_GenerateCyclicCheckingNetwork(methodsList);
            }
            else if (!GlobalVariables.NonCyclicNetworks)
            {
                return GenerateNonCyclicCheckingNetworks(methodsList);
            }
            else
            {
                return GenerateCyclicCheckingNetworks(methodsList);
            }

        }

        private static Dictionary<String, Method> NEW_GenerateCyclicCheckingNetwork(List<Method> methodsList)
        {

            Dictionary<String, Method> checkingNetwork = new Dictionary<String, Method>();
            // used to register what was assigned to prevent double assignments
            // in this loop we create netwroks not only one, but they are all stored in here
            // also we complete missing code snippets        

            int methodsWithChallengeCodeCount = methodsList.Where(m => m.HasChallengeCode).Count();
            int fullNetworks = methodsWithChallengeCodeCount / GlobalVariables.NODES_NETWORK;
            int nodesInPartialNetworks = (methodsList.Count - methodsWithChallengeCodeCount) % GlobalVariables.NODES_NETWORK;

            int currentStartIndex = 0;
            int currentEndIndex = GlobalVariables.NODES_NETWORK;

            //handle full cycles
            for(int i = 0; i < fullNetworks; i++)
            {
                checkingNetwork = CreateCyclicNetwork(methodsList, currentStartIndex, currentEndIndex, checkingNetwork);
                currentStartIndex += GlobalVariables.NODES_NETWORK;
                currentEndIndex   += GlobalVariables.NODES_NETWORK;
            }

            //handle 0 < left over nodes < Nodes_Network
            if (nodesInPartialNetworks != 0)
            {
                currentEndIndex = methodsWithChallengeCodeCount - 1;
                checkingNetwork = CreateCyclicNetwork(methodsList, currentStartIndex, currentEndIndex, checkingNetwork);
            }

            return checkingNetwork;
        }

        private static Dictionary<String, Method> GenerateCyclicCheckingNetworks(List<Method> methodsList)
        {
            var methodsWithChallengeCode = methodsList.Where(m => m.HasChallengeCode);

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
                    string value = checkedMethod.ChallengeCode;
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


                        checkedMethod.ChallengeCode = value;
                    }
                    //Console.WriteLine("adding id "+checkingMethod.Id);
                    //todo there are cases of same function id in overloading
                    checkingNetwork[checkingMethod.Id] = checkedMethod;
                }
            }

            return checkingNetwork;
        }

        private static Dictionary<String, Method> GenerateNonCyclicCheckingNetworks(List<Method> methodsList)
        {
            Dictionary<String, Method> checkingNetwork = new Dictionary<String, Method>();

            //network consists of:
            // 1 - nodes 1 -> (n-1) = have challenge code
            // 2 - node n = does not have a challenge code
            int methodsWithChallengeCodeCount = methodsList.Where(m => m.HasChallengeCode).Count();
            int fullNetworks = methodsWithChallengeCodeCount / (GlobalVariables.NODES_NETWORK - 1);
            int nodesInPartialNetworks = (methodsList.Count - methodsWithChallengeCodeCount) % (GlobalVariables.NODES_NETWORK - 1);

            int currentStartIndex = 0;
            int currentEndIndex = GlobalVariables.NODES_NETWORK - 1;

            //handle full cycles
            for (int i = 0; i < fullNetworks; i++)
            {
                checkingNetwork = CreateNonCyclicNetwork(methodsList, currentStartIndex, currentEndIndex, checkingNetwork);
                currentStartIndex += GlobalVariables.NODES_NETWORK - 1;
                currentEndIndex += GlobalVariables.NODES_NETWORK - 1;
            }


            if (nodesInPartialNetworks != 0)
            {
                currentEndIndex = fullNetworks * GlobalVariables.NODES_NETWORK + nodesInPartialNetworks;
                checkingNetwork = CreateNonCyclicNetwork(methodsList, currentStartIndex, currentEndIndex, checkingNetwork);
            }

            return checkingNetwork;
        }

        private static Dictionary<string, Method> CreateCyclicNetwork(List<Method> methodsList, int startIndex, int endIndex, Dictionary<string, Method> checkingNetwork)
        {
            var methodsWithChallengeCode = methodsList.Where(m => m.HasChallengeCode).ToList();
            var methodWithoutChallengeCode = methodsList.Where(m => !m.HasChallengeCode).ToList();

            Method method1;
            Method method2;

            //case where only 1 node left
            if (startIndex == endIndex)
            {
                method1 = methodsWithChallengeCode[startIndex];
                method2 = methodsWithChallengeCode[endIndex];
                checkingNetwork[method1.Id] = method2;
            }
            //case where more than 1 node but less than NODE_NETWORK
            // or full network
            else
            {
                for (int i = startIndex; i < endIndex; i++)
                {

                    if (i == endIndex - 1)
                    {
                        method1 = methodsWithChallengeCode[i];
                        method2 = methodsWithChallengeCode[startIndex];
                    }
                    else
                    {
                        method1 = methodsWithChallengeCode[i];
                        method2 = methodsWithChallengeCode[i + 1];
                    }

                    checkingNetwork[method1.Id] = method2;

                }
            }

            return checkingNetwork;
        }

        private static Dictionary<string, Method> CreateNonCyclicNetwork(List<Method> methodsList, int startIndex, int endIndex, Dictionary<string, Method> checkingNetwork)
        {
            var methodsWithChallengeCode = methodsList.Where(m => m.HasChallengeCode).ToList();
            var methodWithoutChallengeCode = methodsList.Where(m => !m.HasChallengeCode).ToList();

            Method method1;
            Method method2;

            //case where only 1 node left
            if (startIndex == endIndex)
            {
                method1 = methodsWithChallengeCode[startIndex];
                method2 = methodWithoutChallengeCode.GetRandom();
                checkingNetwork[method1.Id] = method2;
            }
            //case where more than 1 node but less than NODE_NETWORK
            // or full network
            else
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (i == endIndex - 1)
                    {
                        method1 = methodsWithChallengeCode[i];
                        method2 = methodWithoutChallengeCode.GetRandom();
                    }
                    else
                    {
                        method1 = methodsWithChallengeCode[i];
                        method2 = methodsWithChallengeCode[i + 1];
                    }

                    checkingNetwork[method1.Id] = method2;
                }
            }
            return checkingNetwork;
        }

        /*private static string ReplaceResponce(string value, int i, string methodName, bool usePrimitiveCombination)
        {
            if(usePrimitiveCombination)
            {
                checkedMethod.PrimitiveCombination = true;
                value = Responces.RemoveResponsePart(value);
                checkedMethod = Transformer.CreateReturnStatement(checkedMethod, checkingMethod);
            }
            else
            {
                return value.Replace("RESPONSE", Responces.GetResponse(i, methodName));
            }
        }*/
    }
}
