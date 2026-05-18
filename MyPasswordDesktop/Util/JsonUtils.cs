using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MyPasswordDesktop.Util
{
    /// <summary>
    /// Central JSON facade. Every (de)serialization goes through the
    /// <see cref="MyPasswordJsonContext"/> source generator — no runtime
    /// reflection — so JSON handling is trim- and AOT-safe.
    /// </summary>
    public static class JsonUtils
    {
        public static string ToJson(object obj)
        {
            JsonTypeInfo info = MyPasswordJsonContext.Default.GetTypeInfo(obj.GetType());
            return JsonSerializer.Serialize(obj, info);
        }

        public static T FromJson<T>(string data)
            => (T)FromJson(data, typeof(T));

        public static object FromJson(string data, Type type)
        {
            JsonTypeInfo info = MyPasswordJsonContext.Default.GetTypeInfo(type);
            return JsonSerializer.Deserialize(data, info);
        }

        public static object FromJson(byte[] data, Type type)
        {
            JsonTypeInfo info = MyPasswordJsonContext.Default.GetTypeInfo(type);
            return JsonSerializer.Deserialize(data, info);
        }

        public static T FromJson<T>(byte[] data)
            => (T)FromJson(data, typeof(T));
    }
}
