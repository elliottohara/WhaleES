using System;
using System.IO;

namespace WhaleES.Serialization
{
    /// <summary>
    /// Serializer that uses Google Protocol buffers. It's blazing fast.
    /// </summary>
    public class ProtoBufSerializer : ISerializer
    {
        public string Serialize(object t)
        {
            var memoryStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(memoryStream, t);
            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public object Deserialize(string value, Type targetType)
        {
            var bytes = Convert.FromBase64String(value);
            var stream = new MemoryStream(bytes);
            return ProtoBuf.Serializer.NonGeneric.Deserialize(targetType, stream);
        }
    }
}