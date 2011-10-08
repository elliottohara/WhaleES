using NUnit.Framework;

namespace WhaleES.Integration.Test
{
    [TestFixture]
    public class When_an_AggrigateRoot_has_non_standard_public_methods:ConfigurationTestFor<NonStandardArWithPublicMethods>
    {
        
        [SetUp]
        public void configure_properly()
        {
            NonStandardArWithPublicMethods.Configure();
        }
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