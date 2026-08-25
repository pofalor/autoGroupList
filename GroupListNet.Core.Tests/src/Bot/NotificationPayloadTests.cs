using GroupListNet.Core.src.Bot.Models;

namespace GroupListNet.Core.Tests.src.Bot
{
    public class NotificationPayloadTests
    {
        [Fact]
        public void Serialize_ThenParse_ReturnsSameTextAndButtons()
        {
            var keyboard = BotKeyboard.InlineRow(new BotButton("Я на паре", "attend_12_345"));

            var (text, parsedKeyboard) = NotificationPayload.Parse(NotificationPayload.Serialize("Пара началась", keyboard));

            Assert.Equal("Пара началась", text);
            Assert.NotNull(parsedKeyboard);
            var button = Assert.Single(Assert.Single(parsedKeyboard.Rows));
            Assert.Equal("Я на паре", button.Text);
            Assert.Equal("attend_12_345", button.Data);
        }

        [Fact]
        public void Parse_WithoutButtons_ReturnsTextOnly()
        {
            var (text, keyboard) = NotificationPayload.Parse(NotificationPayload.Serialize("Отчёт за день", null));

            Assert.Equal("Отчёт за день", text);
            Assert.Null(keyboard);
        }

        [Fact]
        public void Parse_LegacyTelegramPayload_StillReadsButton()
        {
            // Уведомления, созданные до перехода на общий формат, лежат в базе в виде клавиатуры телеграма
            var legacyJson = """
                {"Text":"Пара началась","InlineKeyboard":{"inline_keyboard":[[{"text":"Я на паре","callback_data":"attend_1_2"}]]}}
                """;

            var (text, keyboard) = NotificationPayload.Parse(legacyJson);

            Assert.Equal("Пара началась", text);
            Assert.NotNull(keyboard);
            var button = Assert.Single(Assert.Single(keyboard.Rows));
            Assert.Equal("Я на паре", button.Text);
            Assert.Equal("attend_1_2", button.Data);
        }
    }
}
