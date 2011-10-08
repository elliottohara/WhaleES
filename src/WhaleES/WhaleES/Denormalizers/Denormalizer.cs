using System;
using System.Collections.Generic;

namespace WhaleES.Denormalizers
{
    public class Denormalizer<TAggrigateType> where TAggrigateType : new()
    {
        private readonly StreamOfEventsFor<TAggrigateType> _eventStream;

        public Denormalizer(StreamOfEventsFor<TAggrigateType> eventStream)
        {
            _eventStream = eventStream;
        }
        
    }
}