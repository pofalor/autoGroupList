using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupListNet.Utils.src.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Преобразовать переданный объект в Int32
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        public static int ToInt(this object obj, int defaultValue = default(int))
        {
            if (obj == null)
            {
                return defaultValue;
            }

            if (obj is int v)
            {
                return v;
            }

            int val;

            if (int.TryParse(obj.ToString(), out val))
            {
                return val;
            }

            return defaultValue;
        }

        /// <summary>
        /// Преобразовать переданный объект в строку содержащую json
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <returns>Строка, содержащая json</returns>
        public static string ToJson(this object obj)
        {
            return JsonSerializer.Serialize(obj);
        }

        public static string ObjectToString(this object obj)
        {
            if (obj == null) return "null";

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(obj, obj.GetType(), options);
        }
    }
}
