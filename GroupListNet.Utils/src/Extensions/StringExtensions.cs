using System.Text.RegularExpressions;

namespace GroupListNet.Utils.src.Extensions
{
    public static class StringExtensions
    {
        // <summary>
        /// Проверка строки на пустоту
        /// </summary>
        /// <param name="str">Строка, проверяемая на пустоту</param>
        /// <returns></returns>
        public static bool IsEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static bool IsEmail(this string str)
        {
            const string regex = @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";
            return Regex.IsMatch(str, regex);
        }
    }
}
