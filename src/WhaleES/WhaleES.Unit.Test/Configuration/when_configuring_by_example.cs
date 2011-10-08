using System.Collections.Generic;
using NUnit.Framework;
using WhaleES.Configuration;

namespace WhaleES.Integration.Test
{
    [TestFixture]
    public class when_configuring_by_example
    {
        private ExampleArConfigurationBuilder<ElliottsSuperDuperRecordingAR> _exampleBuilder;
        private Configuration.Configuration _configuration;

        [SetUp]
        public void arrange()
        {
            _configuration = new Configuration.Configuration();
            _exampleBuilder = new ExampleArConfigurationBuilder<ElliottsSuperDuperRecordingAR>(_configuration);
        }
        [Test]
        public void can_give_apply_method()
        {
            _exampleBuilder.ApplyMethodIs<StandardEvent>((ar, @event) => ar.Apply(@event));
            //How the heck to test this?
            //_configuration.ApplyMethod.Method.
            _exampleBuilder.GetUnCommittedEventsBy(ar => ar.UncommittedEvents);
        }

    }
}