using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.S3;
using NUnit.Framework;
using Rhino.Mocks;
using WhaleES.Configuration;

namespace WhaleES.Integration.Test
{
    [TestFixture]
    public class non_standard_setup_tests
    {
        private AmazonS3 _s3;
        private IStreamOfEventsFor<AWierdAr> _eventStream;
        private Repository<AWierdAr> _repository;

        [SetUp] 
        public void Arrange()
        {
            ConfigureWhaleEs.With().UseReflection().UseActionToCallApply(
                (o,ar) =>
                ar.GetType().GetMethods().FirstOrDefault(
                    mi => mi.Name == "DoThingWithEvent" && mi.GetParameters().Any(pi => pi.ParameterType == o.GetType())).Invoke(ar,new[]{o}))
                    .UseReflection()
                .UseFuncToGetUncommitedEvents(ar => ar.GetType().GetMethods().FirstOrDefault(mi => mi.Name == "EventsToSendToDb").Invoke(ar,null) as object[]);

            _eventStream = MockRepository.GenerateMock<IStreamOfEventsFor<AWierdAr>>();
            _repository = new Repository<AWierdAr>(_eventStream);

        }

        [Test]
        public void should_replay()
        {
            var e1 = new WierdEvent {Blah = "B"};
            var e2 = new WierdEvent { Blah = "B" };
            _eventStream.Stub(es => es.GetEventStream(null)).IgnoreArguments().Return(new[] {e1, e2});

            _repository.Get("blah");

            Assert.IsTrue(AWierdAr.HandlerWasCalled);
        }
        [Test]
        public void can_persist()
        {
            var ar = new AWierdAr();
            ar.DoSomething();

            _repository.Put("someid",ar);

            _eventStream.AssertWasCalled(es => es.Persist(Arg<String>.Is.Equal("someid"),Arg<object[]>.Matches(we => ((WierdEvent)we[0]).Blah == "stuff")));
        }
    }
    public class AWierdAr
    {
        public static bool HandlerWasCalled = false;
        public List<object> _events = new List<object>();
        public object[] EventsToSendToDb()
        {
            return _events.ToArray();
        }
        public void DoThingWithEvent(WierdEvent @event)
        {
            HandlerWasCalled = true;
        }
        public void DoSomething()
        {
            _events.Add(new WierdEvent {Blah = "stuff"});
        }
    }
    public class WierdEvent{public string Blah { get; set; }}
}