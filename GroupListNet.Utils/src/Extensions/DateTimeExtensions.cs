using System.Globalization;

namespace GroupListNet.Utils.src.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime ParseDate(this string dt)
        {
            return DateTime.ParseExact(dt, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}