using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WhaleES.Configuration;

namespace WhaleES.Integration.Test
{
    public abstract class AR
    {
        protected AR()
        {
            TheUnCommittedEvents = new List<object>();
        }
        public static bool EventWasHandled = false;
        public static bool? WasInReplay = null;
        protected List<object> TheUnCommittedEvents { get; private set; }
        public void AddEvent(object o)
        {
            TheUnCommittedEvents.Add(o);
        }
        protected void DoApply(StandardEvent @event,bool isReplaying)
        {
            EventWasHandled = true;
            WasInReplay = isReplaying;
        }
    }
    public class StandardARWithPublicMethods : AR
    {
        
        public new void Apply(StandardEvent @event,bool isReplaying = false)
        {
            base.DoApply(@event, isReplaying);
        }
        public new IEnumerable<object>  UncommittedEvents
        {
            get { return base.TheUnCommittedEvents; }
        }
        
    }
    public class StandardARWithInternalApplyMethod : AR
    {
        internal new void Apply(StandardEvent @event,bool isReplaying = false)
        {
            base.DoApply(@event, isReplaying);
        }
        internal new IEnumerable<object> UncommittedEvents
        {
            get { return base.TheUnCommittedEvents; }
        }
    }
    public class StandardARWithPrivateMethods:AR
    {
        private new void Apply(StandardEvent @event,bool isReplaying = false)
        {
            base.DoApply(@event,isReplaying);
        }
        private IEnumerable<object>  UncommittedEvents
        {
            get { return base.TheUnCommittedEvents; }
        }
    }
    public class NonStandardArWithPublicMethods:AR
    {
        public static void Configure()
        {
            ConfigureWhaleEs.With()
                .UseExampleAggrigateRoot<NonStandardArWithPublicMethods>()
                .ApplyMethodIs<StandardEvent>((ar,@event)=>ar.DoStuffWith(@event,true))
                .And().UseReflection()
                .UseFuncToGetUncommitedEvents(
                    ar => ar.GetType().GetMethod("EventsThatNeedToBeStored").Invoke(ar, null) as object[]);
        }
        public void DoStuffWith(StandardEvent @event,bool isReplaying)
        {
            DoApply(@event,isReplaying);
        }
        public object[] EventsThatNeedToBeStored()
        {
            return TheUnCommittedEvents.ToArray();
        }
    }
    public class ElliottsSuperDuperRecordingAR:AR
    {
        public static void Configure()
        {
            ConfigureWhaleEs.With()
                .UseExampleAggrigateRoot<ElliottsSuperDuperRecordingAR>()
                .StartReplayBy(ar => ar.StartReplay())
                .StopReplayBy(ar => ar.StopReplay());

        }
        private bool _isReplaying;
        public static bool calledStart;
        public static bool calledEnd;
        private void StartReplay()
        {
            if(_isReplaying) throw new InvalidOperationException("called StartReplay when already replaying");
            _isReplaying = true;
            calledStart = true;
        }
        private void StopReplay()
        {
            if(!_isReplaying) throw new InvalidOperationException("Can't stop what you didn't start");
            _isReplaying = false;
            calledEnd = true;
        }
        internal IEnumerable<object> UncommittedEvents { get { return TheUnCommittedEvents; } }

        public void Apply(StandardEvent @event)
        {
            AddEvent(@event);
        }
    }
}