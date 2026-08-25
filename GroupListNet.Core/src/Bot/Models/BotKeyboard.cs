namespace GroupListNet.Core.src.Bot.Models
{
    /// <summary>
    /// Кнопка бота. Если <see cref="Data"/> не задан, это обычная кнопка меню,
    /// которая просто отправляет свой текст боту
    /// </summary>
    public class BotButton
    {
        public BotButton(string text, string? data = null)
        {
            Text = text;
            Data = data;
        }

        public string Text { get; }

        /// <summary>
        /// Данные, которые вернутся боту при нажатии, например attend_12_345
        /// </summary>
        public string? Data { get; }
    }

    /// <summary>
    /// Клавиатура, не зависящая от мессенджера. Каждый клиент рендерит её по правилам своего API
    /// </summary>
    public class BotKeyboard
    {
        private BotKeyboard(IReadOnlyList<IReadOnlyList<BotButton>> rows, bool isInline)
        {
            Rows = rows;
            IsInline = isInline;
        }

        public IReadOnlyList<IReadOnlyList<BotButton>> Rows { get; }

        /// <summary>
        /// true — кнопки под конкретным сообщением, false — постоянное меню под полем ввода
        /// </summary>
        public bool IsInline { get; }

        public int ButtonCount => Rows.Sum(row => row.Count);

        /// <summary>
        /// Пустая клавиатура меню. Означает «убрать меню у пользователя»
        /// </summary>
        public static BotKeyboard Remove { get; } = new([], false);

        public static BotKeyboard Inline(IEnumerable<IEnumerable<BotButton>> rows)
        {
            return new BotKeyboard(ToRows(rows), true);
        }

        /// <summary>
        /// Кнопки под сообщением по одной в строке
        /// </summary>
        public static BotKeyboard InlineColumn(IEnumerable<BotButton> buttons)
        {
            return Inline(buttons.Select(button => new[] { button }));
        }

        /// <summary>
        /// Кнопки под сообщением в одну строку
        /// </summary>
        public static BotKeyboard InlineRow(params BotButton[] buttons)
        {
            return Inline([buttons]);
        }

        public static BotKeyboard Reply(IEnumerable<IEnumerable<BotButton>> rows)
        {
            return new BotKeyboard(ToRows(rows), false);
        }

        private static IReadOnlyList<IReadOnlyList<BotButton>> ToRows(IEnumerable<IEnumerable<BotButton>> rows)
        {
            return rows.Select(row => (IReadOnlyList<BotButton>)row.ToArray())
                .Where(row => row.Count != 0)
                .ToArray();
        }
    }
}
