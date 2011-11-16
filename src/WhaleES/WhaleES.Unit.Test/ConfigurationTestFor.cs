using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rhino.Mocks;
using WhaleES.Configuration;

namespace WhaleES.Integration.Test
{
    public abstract class ConfigurationTestFor<TAggrigateRoot> where TAggrigateRoot:AR,new()
    {
        protected IStreamOfEventsFor<TAggrigateRoot> EventStream;
        protected Repository<TAggrigateRoot> Repository;
        protected TAggrigateRoot AggrigateRoot;
        protected string Id;

        [SetUp]
        public void abstract_setup()
        {
            ConfigureWhaleEs.With();
            EventStream = MockRepository.GenerateMock<IStreamOfEventsFor<TAggrigateRoot>>();
            Repository = new Repository<TAggrigateRoot>(EventStream);
            AggrigateRoot = new TAggrigateRoot();
            Id = Guid.NewGuid().ToString();
        }
        protected void WhenStreamContains(object @event)
        {
            EventStream.Stub(es => es.GetEventStream(Id)).IgnoreArguments().Return(new List<object> { @event });
        }
        protected void WhenAggrigateRootAppliedEvent(object @event)
        {
            AggrigateRoot.AddEvent(@event);
        }
        protected void TestReplay()
        {
            WhenStreamContains(new StandardEvent());

            Repository.Get(Id);

            Assert.IsTrue(AR.EventWasHandled);
            Assert.IsTrue(AR.WasInReplay.Value);
        }

        
        protected void TestPersist()
        {
            WhenAggrigateRootAppliedEvent(new StandardEvent());

            Repository.Put("someId", AggrigateRoot);

            EventStream.AssertWasCalled(es => es.Persist(Arg<string>.Is.Equal("someId"), Arg<object[]>.Matches(events => events[0].GetType() == typeof(StandardEvent))));
        }
    }
}