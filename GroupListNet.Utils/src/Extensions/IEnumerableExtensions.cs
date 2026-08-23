namespace GroupListNet.Utils.src.Extensions
{
    public static class IEnumerableExtensions
    {
        public static void Foreach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            ArgumentChecker.NotNull(enumerable, "enumerable");
            ArgumentChecker.NotNull(action, "action");

            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        public static int SafeSum(this IEnumerable<int> source)
        {
            ArgumentChecker.NotNull(source, "source");

            return source.Select(x => (int?)x).Sum() ?? default(int);
        }

        public static long SafeSum(this IEnumerable<long> source)
        {
            ArgumentChecker.NotNull(source, "source");

            return source.Select(x => (long?)x).Sum() ?? default(long);
        }

        public static decimal SafeSum(this IEnumerable<decimal> source)
        {
            ArgumentChecker.NotNull(source, "source");

            return source.Select(x => (decimal?)x).Sum() ?? default(decimal);
        }

        public static double SafeSum(this IEnumerable<double> source)
        {
            ArgumentChecker.NotNull(source, "source");

            return source.Select(x => (double?)x).Sum() ?? default(double);
        }

        public static float SafeSum(this IEnumerable<float> source)
        {
            ArgumentChecker.NotNull(source, "source");

            return source.Select(x => (float?)x).Sum() ?? default(float);
        }

        public static int SafeSum<T>(this IEnumerable<T> source, Func<T, int> selector)
        {
            ArgumentChecker.NotNull(source, "source");
            ArgumentChecker.NotNull(selector, "selector");

            return source.Select(selector).SafeSum();
        }

        public static long SafeSum<T>(this IEnumerable<T> source, Func<T, long> selector)
        {
            ArgumentChecker.NotNull(source, "source");
            ArgumentChecker.NotNull(selector, "selector");

            return source.Select(selector).SafeSum();
        }

        public static decimal SafeSum<T>(this IEnumerable<T> source, Func<T, decimal> selector)
        {
            ArgumentChecker.NotNull(source, "source");
            ArgumentChecker.NotNull(selector, "selector");

            return source.Select(selector).SafeSum();
        }

        public static double SafeSum<T>(this IEnumerable<T> source, Func<T, double> selector)
        {
            ArgumentChecker.NotNull(source, "source");
            ArgumentChecker.NotNull(selector, "selector");

            return source.Select(selector).SafeSum();
        }

        public static float SafeSum<T>(this IEnumerable<T> source, Func<T, float> selector)
        {
            ArgumentChecker.NotNull(source, "source");
            ArgumentChecker.NotNull(selector, "selector");

            return source.Select(selector).SafeSum();
        }
    }
}
