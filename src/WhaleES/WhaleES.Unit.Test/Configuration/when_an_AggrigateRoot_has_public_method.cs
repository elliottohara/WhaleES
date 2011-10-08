using NUnit.Framework;
using Rhino.Mocks;

namespace WhaleES.Integration.Test
{
    [TestFixture]
    public class when_an_AggrigateRoot_has_public_method:ConfigurationTestFor<StandardARWithPublicMethods>
    {
        [Test]
        public void can_replay_events()
        {
            TestReplay();
        }

        [Test]
        public void can_record_events()
        {
            TestPersist();
        }
    }
}