using GroupListNet.Core.src.Bot.Models;
using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.Bot
{
    /// <summary>
    /// Транспорт бота. Реализация есть у каждого поддерживаемого мессенджера,
    /// а логика команд живёт в <see cref="BotUpdateHandler"/> и работает только через этот интерфейс
    /// </summary>
    public interface IMessengerClient
    {
        MessengerType Messenger { get; }

        /// <summary>
        /// false, если мессенджер не настроен в конфиге. Такой клиент не принимает и не отправляет сообщения
        /// </summary>
        bool IsEnabled { get; }

        Task SendMessageAsync(string chatId, string text, BotKeyboard? keyboard = null, bool markdown = false);

        Task EditMessageAsync(string chatId, string messageId, string text, BotKeyboard? keyboard = null, bool markdown = false);

        /// <summary>
        /// Убирает кнопки у ранее отправленного сообщения.
        /// <paramref name="fallbackText"/> нужен мессенджерам, которые не умеют менять только клавиатуру
        /// </summary>
        Task RemoveKeyboardAsync(string chatId, string messageId, string? fallbackText = null);

        /// <summary>
        /// Отвечает на нажатие кнопки всплывающим сообщением
        /// </summary>
        Task AnswerCallbackAsync(BotCallback callback, string? text = null, bool showAlert = false);
    }
}
