using System.Text.Json;

namespace GroupListNet.Core.src.Bot.Models
{
    /// <summary>
    /// Текст уведомления вместе с кнопками, как он лежит в базе.
    /// Формат не зависит от мессенджера: каждый клиент рисует кнопки по-своему
    /// </summary>
    public static class NotificationPayload
    {
        public static string Serialize(string text, BotKeyboard? keyboard)
        {
            var payload = new
            {
                Text = text,
                Buttons = keyboard?.Rows
                    .Select(row => row.Select(button => new { button.Text, button.Data }).ToArray())
                    .ToArray()
            };

            return JsonSerializer.Serialize(payload);
        }

        /// <summary>
        /// Разбирает уведомление из базы. Понимает и старый телеграм-формат,
        /// чтобы уведомления, созданные до обновления, всё-таки ушли адресатам
        /// </summary>
        public static (string Text, BotKeyboard? Keyboard) Parse(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var text = root.TryGetProperty("Text", out var textElement)
                ? textElement.GetString() ?? string.Empty
                : string.Empty;

            if (root.TryGetProperty("Buttons", out var buttonsElement) && buttonsElement.ValueKind == JsonValueKind.Array)
                return (text, ReadButtons(buttonsElement, "Text", "Data"));

            // Старый формат: сериализованный InlineKeyboardMarkup из библиотеки телеграма
            if (root.TryGetProperty("InlineKeyboard", out var legacyKeyboard)
                && legacyKeyboard.TryGetProperty("inline_keyboard", out var legacyRows)
                && legacyRows.ValueKind == JsonValueKind.Array)
                return (text, ReadButtons(legacyRows, "text", "callback_data"));

            return (text, null);
        }

        private static BotKeyboard? ReadButtons(JsonElement rowsElement, string textProperty, string dataProperty)
        {
            var rows = new List<BotButton[]>();

            foreach (var rowElement in rowsElement.EnumerateArray())
            {
                if (rowElement.ValueKind != JsonValueKind.Array)
                    continue;

                var row = new List<BotButton>();
                foreach (var buttonElement in rowElement.EnumerateArray())
                {
                    if (!buttonElement.TryGetProperty(textProperty, out var buttonText))
                        continue;

                    var data = buttonElement.TryGetProperty(dataProperty, out var buttonData)
                        ? buttonData.GetString()
                        : null;

                    row.Add(new BotButton(buttonText.GetString() ?? string.Empty, data));
                }

                if (row.Count != 0)
                    rows.Add([.. row]);
            }

            return rows.Count == 0 ? null : BotKeyboard.Inline(rows);
        }
    }
}
