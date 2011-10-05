using System;
using System.Web.Script.Serialization;

namespace WhaleES.Serialization
{
    /// <summary>
    /// Serializer that uses Microsoft JavaScriptSerializer. It's slow, but simple.
    /// </summary>
    public class JsonSerializer : ISerializer
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer
                                                                      {MaxJsonLength = int.MaxValue};
        public string Serialize(object t)
        {
            return Serializer.Serialize(t);

        }

        public object Deserialize(string value, Type targetType)
        {
            return Serializer.Deserialize(value, targetType);
        }
    }
}