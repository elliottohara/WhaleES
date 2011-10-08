using NUnit.Framework;

namespace WhaleES.Integration.Test
{
    [TestFixture]
    public class When_an_aggrigate_root_has_instance_methods_for_start_and_stop_replay:ConfigurationTestFor<ElliottsSuperDuperRecordingAR>
    {
        
        [SetUp]
        public void configure_property()
        {
            ElliottsSuperDuperRecordingAR.Configure();
        }
        [Test] 
        public void can_record()
        {
            TestPersist();
        }
        [Test]
        public void can_replay()
        {
            WhenStreamContains(new StandardEvent());

            Repository.Get(Id);

            Assert.True(ElliottsSuperDuperRecordingAR.calledStart);
            Assert.True(ElliottsSuperDuperRecordingAR.calledEnd);
        }
    }
}