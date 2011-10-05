using System;
using System.Runtime.Serialization;

namespace WhaleES
{
    [DataContract][Serializable]
    public class EventEnvelope
    {
        [DataMember(Order = 1)] public String EventType { get; set; }
        [DataMember(Order = 2)] public DateTime CommittedAt { get; private set; }
        [DataMember(Order = 3)] public string Payload { get; set; }
        
        public static EventEnvelope New(string serializedEvent, Type eventType)
        {
            var envelope = new EventEnvelope(serializedEvent) { EventType = eventType.AssemblyQualifiedName };
            return envelope;
        }
        private EventEnvelope(string serializedEvent)
        {
            CommittedAt = DateTime.Now;
            Payload = serializedEvent;
        }
        [Obsolete("Only here for deserilization, don't use this! Use EventEnvelope.New")]
        public EventEnvelope(){}
    }
    
}