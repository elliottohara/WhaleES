using System;

namespace WhaleES
{
    /// <summary>
    /// Interface that Serializers must implement
    /// </summary>
    public interface ISerializer
    {
        string Serialize(object t);
        object Deserialize(string value,Type targetType);
    }
}