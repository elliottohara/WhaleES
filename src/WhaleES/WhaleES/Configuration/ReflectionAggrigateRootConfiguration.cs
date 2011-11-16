using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;

namespace WhaleES.Configuration
{
    public class ReflectionAggrigateRootConfiguration
    {
        private readonly Configuration _configuration;

        public ReflectionAggrigateRootConfiguration(Configuration configuration)
        {
            _configuration = configuration;
        }
        public Configuration UncommitedEventsGetMethodNameIs(string methodName)
        {
            return UseFuncToGetUncommitedEvents(ar =>
            {
                var property = ar.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(pi => pi.Name == methodName);
                if (property == null)
                    throw new ConfigurationErrorsException("can not find Property with name " + methodName + "for record operation. Please check that your AggrigateRoot contains an IEnumerable<object> property with that name, or Configure WhaleES with the proper property name by calling UncommitedEventsGetMethodNameIs on ConfigureWhaleEs.With()");
                var getter = property.GetGetMethod() ?? property.GetGetMethod(true);

                return getter.Invoke(ar, null) as IEnumerable<object>;
            });

        }
        public Configuration UseFuncToGetUncommitedEvents(Func<object, IEnumerable<object>> uncommittedEventsMethodGetter)
        {
            _configuration.GetUncommitedEventsMethod = uncommittedEventsMethodGetter;
            return _configuration;
        }
        /// <summary>
        /// first param is the event, second is the event
        /// </summary>
        /// <param name="applyAction"></param>
        /// <returns></returns>
        public Configuration UseActionToCallApply(Action<object, object> applyAction)
        {
            _configuration.ApplyMethod = applyAction;
            return _configuration;
        }
        public Configuration ApplyMethodNameIs(string methodName)
        {

            return UseActionToCallApply((@event, ar) =>
            {
                var applyMethod = FindApplyMethod(@event, ar,methodName);
                if (applyMethod == null) return;
                if (_configuration.HasReplaySwith)
                    applyMethod.Invoke(ar, new[] { @event });
                else
                    applyMethod.Invoke(ar, new[] { @event, true });


            });
        }

        private static MethodInfo FindApplyMethod(object @event, object ar,string methodName)
        {
            var allApplyMethods = ar.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance |
                                                          BindingFlags.Public).Where(mi => mi.Name == methodName).ToList();

            var concreteApplyMethod = allApplyMethods.FirstOrDefault(mi => 
                                      mi.GetParameters()
                                          .Any(
                                              pi =>
                                              pi.ParameterType == @event.GetType()));
            if (concreteApplyMethod != null) return concreteApplyMethod;
            var abstractApplyMethod =
                allApplyMethods.FirstOrDefault(
                    mi => mi.GetParameters().Any(pi => pi.ParameterType.IsAssignableFrom(@event.GetType())));
            return abstractApplyMethod;

        }

        public Configuration CallMethodToStartReplay(string methodName)
        {
            _configuration.HasReplaySwith = false;
            _configuration.StartReplay =
                ar =>
                ar.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First(
                    mi => mi.Name == methodName).Invoke(ar, null);
            return _configuration;
        }
        public Configuration CallMethodToEndReplay(string methodName)
        {
            _configuration.HasReplaySwith = true;
            _configuration.EndReplay =
                ar =>
                ar.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First(
                    mi => mi.Name == methodName).Invoke(ar, null);
            return _configuration;
        }
    }
}