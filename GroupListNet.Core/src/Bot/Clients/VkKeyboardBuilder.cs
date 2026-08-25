using GroupListNet.Core.src.Bot.Models;
using System.Text.Json;
using VkNet.Enums.StringEnums;
using VkNet.Model;

namespace GroupListNet.Core.src.Bot.Clients
{
    /// <summary>
    /// Превращает общую клавиатуру бота в клавиатуру ВКонтакте.
    /// Ограничения там жёстче телеграмовских, поэтому длинные списки приходится перекладывать
    /// </summary>
    public static class VkKeyboardBuilder
    {
        /// <summary>
        /// Больше шести кнопок ВКонтакте в клавиатуре под сообщением не принимает
        /// </summary>
        public const int MaxInlineButtons = 6;

        /// <summary>
        /// Ограничения обычной клавиатуры: не больше десяти рядов по пять кнопок
        /// </summary>
        public const int MaxKeyboardRows = 10;
        public const int MaxButtonsInRow = 5;

        /// <summary>
        /// Подпись кнопки ВКонтакте обрезает, длинные ФИО в список не влезают
        /// </summary>
        public const int MaxButtonLabelLength = 40;

        public static MessageKeyboard? Build(BotKeyboard? keyboard, Action<int, int>? onButtonsDropped = null)
        {
            if (keyboard == null)
                return null;

            // Пустая клавиатура убирает кнопки у пользователя
            if (keyboard.Rows.Count == 0)
                return new MessageKeyboard { Inline = false, OneTime = false, Buttons = [] };

            // Кнопок под сообщением ВКонтакте разрешает мало, длинные списки показываем обычной клавиатурой
            var inline = keyboard.IsInline && keyboard.ButtonCount <= MaxInlineButtons;
            var rows = inline ? keyboard.Rows : Reflow(keyboard.Rows, onButtonsDropped);

            return new MessageKeyboard
            {
                Inline = inline,
                // Обычную клавиатуру из кнопок под сообщением делаем одноразовой: она заменяет разовый выбор
                OneTime = !inline && keyboard.IsInline,
                Buttons = rows.Select(row => row.Select(ToVkButton).ToArray()).ToArray()
            };
        }

        /// <summary>
        /// Достаёт данные кнопки из полезной нагрузки ВКонтакте
        /// </summary>
        public static string ReadCallbackData(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return string.Empty;

            try
            {
                using var document = JsonDocument.Parse(payload);
                return document.RootElement.TryGetProperty("cmd", out var command)
                    ? command.GetString() ?? string.Empty
                    : string.Empty;
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        public static string Truncate(string text, int maxLength)
        {
            return text.Length <= maxLength ? text : text[..maxLength];
        }

        private static MessageKeyboardButton ToVkButton(BotButton button)
        {
            var isCallback = !string.IsNullOrEmpty(button.Data);

            return new MessageKeyboardButton
            {
                Action = new MessageKeyboardButtonAction
                {
                    Type = isCallback ? KeyboardButtonActionType.Callback : KeyboardButtonActionType.Text,
                    Label = Truncate(button.Text, MaxButtonLabelLength),
                    // Полезная нагрузка ВКонтакте — это JSON, данные кнопки кладём в поле cmd
                    Payload = JsonSerializer.Serialize(new { cmd = button.Data ?? button.Text })
                }
            };
        }

        /// <summary>
        /// Раскладывает кнопки заново, чтобы уместиться в ограничения обычной клавиатуры ВКонтакте
        /// </summary>
        private static IReadOnlyList<IReadOnlyList<BotButton>> Reflow(IReadOnlyList<IReadOnlyList<BotButton>> rows,
            Action<int, int>? onButtonsDropped)
        {
            if (rows.Count <= MaxKeyboardRows && rows.All(row => row.Count <= MaxButtonsInRow))
                return rows;

            var buttons = rows.SelectMany(row => row).ToArray();
            var maxButtons = MaxKeyboardRows * MaxButtonsInRow;

            if (buttons.Length > maxButtons)
            {
                onButtonsDropped?.Invoke(buttons.Length, maxButtons);
                buttons = [.. buttons.Take(maxButtons)];
            }

            // Ряды растягиваем ровно настолько, чтобы весь список поместился в десять строк
            var buttonsInRow = Math.Min(MaxButtonsInRow, (int)Math.Ceiling(buttons.Length / (double)MaxKeyboardRows));
            return buttons.Chunk(Math.Max(buttonsInRow, 1)).ToArray();
        }
    }
}
