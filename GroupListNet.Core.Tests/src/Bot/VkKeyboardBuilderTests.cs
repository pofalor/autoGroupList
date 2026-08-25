using GroupListNet.Core.src.Bot.Clients;
using GroupListNet.Core.src.Bot.Models;

namespace GroupListNet.Core.Tests.src.Bot
{
    /// <summary>
    /// Ограничения клавиатуры ВКонтакте жёстче телеграмовских, поэтому проверяем именно перекладку кнопок
    /// </summary>
    public class VkKeyboardBuilderTests
    {
        [Fact]
        public void Build_FewButtons_KeepsThemUnderMessage()
        {
            var keyboard = BotKeyboard.InlineColumn(MakeButtons(3));

            var vkKeyboard = VkKeyboardBuilder.Build(keyboard);

            Assert.NotNull(vkKeyboard);
            Assert.True(vkKeyboard.Inline);
            Assert.Equal(3, vkKeyboard.Buttons.Count());
        }

        [Fact]
        public void Build_ManyButtons_FallsBackToOrdinaryKeyboard()
        {
            // Двадцать студентов в списке — под сообщением ВКонтакте столько кнопок не покажет
            var keyboard = BotKeyboard.InlineColumn(MakeButtons(20));

            var vkKeyboard = VkKeyboardBuilder.Build(keyboard);

            Assert.NotNull(vkKeyboard);
            Assert.False(vkKeyboard.Inline);
            Assert.True(vkKeyboard.OneTime);

            var rows = vkKeyboard.Buttons.ToArray();
            Assert.True(rows.Length <= VkKeyboardBuilder.MaxKeyboardRows);
            Assert.All(rows, row => Assert.True(row.Count() <= VkKeyboardBuilder.MaxButtonsInRow));
            Assert.Equal(20, rows.Sum(row => row.Count()));
        }

        [Fact]
        public void Build_MoreButtonsThanKeyboardHolds_DropsExtraAndReports()
        {
            var maxButtons = VkKeyboardBuilder.MaxKeyboardRows * VkKeyboardBuilder.MaxButtonsInRow;
            var keyboard = BotKeyboard.InlineColumn(MakeButtons(maxButtons + 5));
            var reportedTotal = 0;

            var vkKeyboard = VkKeyboardBuilder.Build(keyboard, (total, _) => reportedTotal = total);

            Assert.NotNull(vkKeyboard);
            Assert.Equal(maxButtons, vkKeyboard.Buttons.Sum(row => row.Count()));
            Assert.Equal(maxButtons + 5, reportedTotal);
        }

        [Fact]
        public void Build_EmptyKeyboard_RemovesButtons()
        {
            var vkKeyboard = VkKeyboardBuilder.Build(BotKeyboard.Remove);

            Assert.NotNull(vkKeyboard);
            Assert.Empty(vkKeyboard.Buttons);
            Assert.False(vkKeyboard.Inline);
        }

        [Fact]
        public void Build_LongLabel_IsTruncated()
        {
            var longName = new string('я', 100);
            var keyboard = BotKeyboard.InlineColumn([new BotButton(longName, "addleader_1")]);

            var vkKeyboard = VkKeyboardBuilder.Build(keyboard);

            var label = vkKeyboard!.Buttons.Single().Single().Action.Label;
            Assert.Equal(VkKeyboardBuilder.MaxButtonLabelLength, label.Length);
        }

        [Fact]
        public void Build_CallbackData_TravelsThroughPayload()
        {
            var keyboard = BotKeyboard.InlineRow(new BotButton("Я на паре", "attend_12_345"));

            var vkKeyboard = VkKeyboardBuilder.Build(keyboard);
            var payload = vkKeyboard!.Buttons.Single().Single().Action.Payload;

            Assert.Equal("attend_12_345", VkKeyboardBuilder.ReadCallbackData(payload));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("не json")]
        [InlineData("{\"other\":\"value\"}")]
        public void ReadCallbackData_BrokenPayload_ReturnsEmpty(string? payload)
        {
            Assert.Equal(string.Empty, VkKeyboardBuilder.ReadCallbackData(payload));
        }

        private static BotButton[] MakeButtons(int count)
        {
            return [.. Enumerable.Range(1, count).Select(number => new BotButton($"Кнопка {number}", $"addleader_{number}"))];
        }
    }
}
