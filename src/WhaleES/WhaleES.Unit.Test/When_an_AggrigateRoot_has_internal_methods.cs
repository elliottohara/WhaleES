using NUnit.Framework;

namespace WhaleES.Integration.Test
{
    [TestFixture]
    public class When_an_AggrigateRoot_has_internal_methods:ConfigurationTestFor<StandardARWithInternalApplyMethod>
    {
        [Test] 
        public void can_replay()
        {
            TestReplay();
        }

        [Test]
        public void can_record()
        {
            TestPersist();
        }

    }
}