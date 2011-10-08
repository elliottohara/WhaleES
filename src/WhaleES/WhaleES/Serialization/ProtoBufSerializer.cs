using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf.Meta;

namespace WhaleES.Serialization
{
    /// <summary>
    /// Serializer that uses Google Protocol buffers. It's blazing fast.
    /// </summary>
    public class ProtoBufSerializer : ISerializer
    {
        private static List<Type> HaveSerializersFor = new List<Type>();
        private const int MaxFieldsAllowedInBaseClass = 1000;
        public string Serialize(object t)
        {
            DefineModelFor(t.GetType());
            var memoryStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(memoryStream, t);
            return Convert.ToBase64String(memoryStream.ToArray());
        }

        private void DefineModelFor(Type targetType)
        {
            if(HaveSerializersFor.Contains(targetType)) return;
            if (targetType.BaseType != typeof(object))
            {
                DefineModelFor(targetType.BaseType);
            }
            var allTypesThatDeriveFromTarget = targetType.Assembly.GetTypes().Where(t => t.BaseType == targetType && targetType != t);
            if (allTypesThatDeriveFromTarget.Count() == 0) return;
            var typeModel = RuntimeTypeModel.Default[targetType];
            var index = MaxFieldsAllowedInBaseClass;
            foreach (var type in allTypesThatDeriveFromTarget)
            {
                typeModel.AddSubType(index++, type);
            }
            HaveSerializersFor.Add(targetType);
        }

        public object Deserialize(string value, Type targetType)
        {
            DefineModelFor(targetType);
            var bytes = Convert.FromBase64String(value);
            var stream = new MemoryStream(bytes);
            return ProtoBuf.Serializer.NonGeneric.Deserialize(targetType, stream);
        }
    }
}