using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WhaleES.Denormalizers
{
    public class Denormalizer<TViewModel> where TViewModel : new()
    {
        public TViewModel Results { get; private set; }
        // ReSharper disable StaticFieldInGenericType
        private static readonly Dictionary<Type, MethodInfo> HandleMethods = new Dictionary<Type, MethodInfo>();
        // ReSharper restore StaticFieldInGenericType
        public virtual Denormalizer<TViewModel> Process(IEnumerable<object> eventStream)
        {
            Results = new TViewModel();
            foreach (var @event in eventStream)
            {
                var eventType = @event.GetType();
                EnsureHandleMethod(eventType);
                Handle(@event);
            }
            return this;
        }
        private void Handle(object @event)
        {
            if (HandleMethods[@event.GetType()] != null)
                HandleMethods[@event.GetType()].Invoke(this, new[] { @event });
        }
        private void EnsureHandleMethod(Type eventType)
        {
            if (HandleMethods.ContainsKey(eventType)) return;
            var method =
                GetType().GetMethods().FirstOrDefault(
                    mi =>
                    mi.Name == "Handle" &&
                    mi.GetParameters().FirstOrDefault(p => p.ParameterType == eventType) != null);
            HandleMethods[eventType] = method;

        }
    }
}