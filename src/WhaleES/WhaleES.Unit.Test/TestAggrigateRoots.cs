using System.Collections.Generic;
using System.Linq;
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
                .UseActionToCallApply((@event, ar) => ar
                    .GetType().GetMethods()
                    .First(mi => mi.Name == "DoStuffWith").Invoke(ar, new[] { @event, true }))
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
}