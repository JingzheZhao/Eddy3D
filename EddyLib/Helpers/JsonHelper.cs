using Newtonsoft.Json;

namespace EddyLib.Helpers
{
    /// <summary>
    /// Provides common JSON serialization/deserialization utilities.
    /// </summary>
    public static class JsonHelper
    {
        private static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// Serializes an object to JSON string.
        /// </summary>
        public static string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, DefaultSettings);
        }

        /// <summary>
        /// Deserializes JSON string to an object.
        /// Returns default(T) if JSON is invalid.
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            json = json.Trim();

            if ((json.StartsWith("{") && json.EndsWith("}")) ||
                (json.StartsWith("[") && json.EndsWith("]")))
            {
                return JsonConvert.DeserializeObject<T>(json, DefaultSettings);
            }

            return default;
        }

        /// <summary>
        /// Attempts to deserialize JSON, returning success state.
        /// </summary>
        public static bool TryDeserialize<T>(string json, out T result)
        {
            result = default;

            try
            {
                result = Deserialize<T>(json);
                return result != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
