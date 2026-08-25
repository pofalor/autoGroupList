using GroupListNet.Core.src.Bot;
using GroupListNet.Core.src.Bot.Models;
using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.Tests.src.Bot
{
    /// <summary>
    /// Транспорт-заглушка: вместо отправки запоминает, что бот ответил.
    /// Позволяет проверять общую логику команд без настоящих мессенджеров
    /// </summary>
    public class RecordingMessengerClient : IMessengerClient
    {
        public RecordingMessengerClient(MessengerType messenger)
        {
            Messenger = messenger;
        }

        public MessengerType Messenger { get; }

        public bool IsEnabled => true;

        public List<(string ChatId, string Text, BotKeyboard? Keyboard)> SentMessages { get; } = [];

        public List<(string ChatId, string MessageId, string Text)> EditedMessages { get; } = [];

        public List<(string MessageId, string? FallbackText)> RemovedKeyboards { get; } = [];

        public List<(string? Text, bool ShowAlert)> Answers { get; } = [];

        public string LastMessageText => SentMessages.Count == 0 ? string.Empty : SentMessages[^1].Text;

        public string? LastAnswerText => Answers.Count == 0 ? null : Answers[^1].Text;

        public Task SendMessageAsync(string chatId, string text, BotKeyboard? keyboard = null, bool markdown = false)
        {
            SentMessages.Add((chatId, text, keyboard));
            return Task.CompletedTask;
        }

        public Task EditMessageAsync(string chatId, string messageId, string text, BotKeyboard? keyboard = null, bool markdown = false)
        {
            EditedMessages.Add((chatId, messageId, text));
            return Task.CompletedTask;
        }

        public Task RemoveKeyboardAsync(string chatId, string messageId, string? fallbackText = null)
        {
            RemovedKeyboards.Add((messageId, fallbackText));
            return Task.CompletedTask;
        }

        public Task AnswerCallbackAsync(BotCallback callback, string? text = null, bool showAlert = false)
        {
            Answers.Add((text, showAlert));
            return Task.CompletedTask;
        }
    }
}
