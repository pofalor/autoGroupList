namespace GroupListNet.Utils.src.Extensions
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Получить значение словаря по ключу, в случае отсутствия ключа вернет default
        /// </summary>
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, TValue defaultValue)
        {
            ArgumentChecker.NotNull(source, "source");

            if (source.ContainsKey(key))
            {
                return source[key];
            }

            return defaultValue;
        }
    }
}
