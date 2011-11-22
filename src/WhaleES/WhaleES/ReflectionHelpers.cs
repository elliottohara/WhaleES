using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WhaleES
{
    public static class ReflectionHelpers
    {
        public static void InvokeMethod(this object thing, string methodName,params object[] arguments)
        {
            var allApplyMethods = thing.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance |
                                                          BindingFlags.Public).Where(mi => mi.Name == methodName).ToList();

            var concreteApplyMethod = allApplyMethods.FirstOrDefault(mi => ParametersMatchExactly(mi.GetParameters(),arguments));
            if (concreteApplyMethod != null) 
                concreteApplyMethod.Invoke(thing,arguments);
            
            var abstractApplyMethod = allApplyMethods.FirstOrDefault(mi=>ParametersMatch(mi.GetParameters(),arguments));
            if(abstractApplyMethod != null)
                abstractApplyMethod.Invoke(thing, arguments);

        }
        private static bool ParametersMatchExactly(ParameterInfo[] paramsOnMethod, object[] parameters)
        {
            for (var i = 0; i < parameters.Count(); i++)
            {
                if (paramsOnMethod[i].ParameterType != parameters[i].GetType())
                    return false;
            }
            return true;
        }
        private static bool ParametersMatch(ParameterInfo[] paramsOnMethod, object[] parameters)
        {
            for (var i = 0; i < parameters.Count(); i++)
            {
                if (! parameters[i].GetType().IsAssignableFrom(paramsOnMethod[i].ParameterType ))
                    return false;
            }
            return true;
        }
    }
}