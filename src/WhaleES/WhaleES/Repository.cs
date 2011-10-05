using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using WhaleES.Configuration;

namespace WhaleES
{
    /// <summary>
    /// Returns instances of <see cref="T"/>. Assumes that all objects of <see cref="T"/> have an Apply method with parameters for specific event types. Will reply events
    /// by calling those apply methods. Does not cause errors if methods don't exist, but you won't have any state on your object.
    /// Use <see cref="RepositoryFactory"/> to create an instance.
    /// </summary>
    /// <typeparam name="T">Aggregate Root type, should have Apply(SomeEventType @event) methods for the associated events.</typeparam>
    public class Repository<T> where T: new()
    {
        private readonly StreamOfEventsFor<T> _streamOfEventsFor;

        internal Repository(StreamOfEventsFor<T> streamOfEventsFor)
        {
            _streamOfEventsFor = streamOfEventsFor;
        }
        public void Put(string id,object aggrigate)
        {
            var eventsGetterName = ConfigureWhaleEs.CurrentConfig.UncommittedEventsMethodName;
            var eventGetter = typeof (T).GetProperties().FirstOrDefault(p => p.Name == eventsGetterName);
            if(eventGetter == null)
                throw new ConfigurationErrorsException("Can not find method " + eventsGetterName + " on type" + typeof(T));
            var events = eventGetter.GetValue(aggrigate, null) as List<Object> ?? new List<object>();
            _streamOfEventsFor.Persist(id,events.ToArray());

        }
        /// <summary>
        /// Returns instance of <see cref="T"/> with id.
        /// </summary>
        /// <param name="id">Unique identifier for <see cref="T"/></param>
        /// <returns></returns>
        public T Get(string id)
        {
            var stream = _streamOfEventsFor.GetEventStream(id);
            //TODO support IOC
            var ar = Activator.CreateInstance<T>();

            foreach (var @event in stream)
            {
                var eventType = @event.GetType();
                var applyMethod = ar.GetType().GetMethods()
                    .FirstOrDefault(
                        mi => mi.Name == "Apply" && mi.GetParameters().Any(pi => pi.ParameterType == eventType));
                if (applyMethod != null)
                    applyMethod.Invoke(ar, new[] { @event ,true});
            }
            return ar;
        }
    }
}