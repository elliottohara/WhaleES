using System.Collections.Generic;
using NUnit.Framework;
using Rhino.Mocks;
using WhaleES.Configuration;

namespace WhaleES.Integration.Test
{
    [TestFixture]
    public class when_using_a_repository
    {
        private IStreamOfEventsFor<StandardARWithPublicMethods> _stream;
        [SetUp]
        public void arrange()
        {
            ConfigureWhaleEs.With();
            _stream = MockRepository.GenerateMock<IStreamOfEventsFor<StandardARWithPublicMethods>>();
        }
        [Test]
        public void no_events_return_null_ar()
        {
            var id = "hhh";
            _stream.Stub(s => s.GetEventStream(id)).Return(new List<object>());

            var repo = new Repository<StandardARWithPublicMethods>(_stream);
            Assert.Null(repo.Get(id));
        }
    }
}