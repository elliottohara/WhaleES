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

        [Test]
        public void polymorphism_actually_works_for_an_abstract()
        {
            WhenStreamContains(new UnknownIdEvent());
            Repository.Get(Id);

            Assert.True(ElliottsSuperDuperRecordingAR.CalledAbstract);
            Assert.False(ElliottsSuperDuperRecordingAR.CalledApplyForConcrete);
        }
        [Test]
        public void concrete_type_methods_are_preferred()
        {
            WhenStreamContains(new SpecialEvent());
            Repository.Get(Id);

            Assert.False(ElliottsSuperDuperRecordingAR.CalledAbstract);
            Assert.True(ElliottsSuperDuperRecordingAR.CalledApplyForConcrete);
        }
        
        
        public class UnknownIdEvent:IHaveAnId{
            public string Id { get; set; }
        }
    }
}