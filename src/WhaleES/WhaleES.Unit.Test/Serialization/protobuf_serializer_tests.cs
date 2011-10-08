using System;
using System.Runtime.Serialization;
using NUnit.Framework;
using WhaleES.Serialization;

namespace WhaleES.Integration.Test.Serialization
{
    [TestFixture]
    public class protobuf_serializer_tests
    {
        private ProtoBufSerializer _serializer;

        [SetUp]
        public void setup()
        {
             _serializer = new ProtoBufSerializer();
        }
        [Test]
        public void can_deserialize_deep_graphs()
        {
            var original = new SomeMessage
                               {
                                   Base1 = Guid.NewGuid(),
                                   Base2 = Guid.NewGuid(),
                                   Base3 = Guid.NewGuid(),
                                   OnConcrete = Guid.NewGuid()
                               };
            var content = _serializer.Serialize(original);
            var deserialized = (SomeMessage)_serializer.Deserialize(content, typeof (SomeMessage));
            Assert.AreEqual(original.Base1,deserialized.Base1);
            Assert.AreEqual(original.Base2, deserialized.Base2);
            Assert.AreEqual(original.Base3, deserialized.Base3);
            Assert.AreEqual(original.OnConcrete, deserialized.OnConcrete);
        }

    }
    [DataContract]
    public class base1
    {
        [DataMember(Order = 1)] public Guid Base1 { get; set; }
    }
    [DataContract]
    public class base2:base1
    {
        [DataMember(Order = 1)] public Guid Base2 { get; set; }
    }
    [DataContract]
    public class base3 : base2
    {
        [DataMember(Order = 1)] public Guid Base3 { get; set; }
    }
    [DataContract]
    public class SomeMessage:base3
    {
        [DataMember(Order = 1)] public Guid OnConcrete { get; set; }
    }
}