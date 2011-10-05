using System.Collections.Generic;
using NUnit.Framework;
using Rhino.Mocks;
using WhaleES.Configuration;

namespace WhaleES.Integration.Test
{
    [TestFixture]
    public class standard_setup_tests
    {
        private IStreamOfEventsFor<StandardAr> _eventStream;
        private Repository<StandardAr> _repository;

        [SetUp]
        public void arrange()
        {
            ConfigureWhaleEs.With();
            _eventStream = MockRepository.GenerateMock<IStreamOfEventsFor<StandardAr>>();
            _repository = new Repository<StandardAr>(_eventStream);
        }
        [Test]
        public void can_replay()
        {
            _eventStream.Stub(es => es.GetEventStream(null)).IgnoreArguments().Return(new List<object>
                                                                                          {new StandardEvent()});

            _repository.Get("blah");

            Assert.IsTrue(StandardAr.EventWasHandled);
            Assert.IsTrue(StandardAr.WasInReplay.Value);
        }
        [Test]
        public void will_save_events()
        {
            var ar = new StandardAr();
            ar.UncommittedEvents.Add(new StandardEvent());

            _repository.Put("someId",ar);

            _eventStream.AssertWasCalled(es => es.Persist(Arg<string>.Is.Equal("someId"),Arg<object[]>.Matches(events => events[0].GetType() == typeof(StandardEvent))));
        }
    }
    public class StandardAr
    {
        public static bool EventWasHandled = false;
        public static bool? WasInReplay = null;
        public StandardAr()
        {
            UncommittedEvents = new List<object>();
        }
        public void Apply(StandardEvent @event,bool isReplaying = false)
        {
            EventWasHandled = true;
            WasInReplay = isReplaying;
        }

        public List<object> UncommittedEvents { get; private set; }
    }

    public class StandardEvent
    {
    }
}