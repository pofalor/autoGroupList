using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.Tests.src.Bot
{
    /// <summary>
    /// Проверяем главное требование: отмечаться можно и в телеграме, и во ВКонтакте,
    /// а бот отвечает там, откуда пришло сообщение
    /// </summary>
    public class BotUpdateHandlerTests
    {
        [Theory]
        [InlineData(MessengerType.Telegram)]
        [InlineData(MessengerType.Vk)]
        public async Task Start_UnregisteredUser_ShowsGroupListInSameMessenger(MessengerType messenger)
        {
            using var host = new BotTestHost();
            await host.AddStudentAsync(1, "Петров");

            await host.SendTextAsync(messenger, "100", "/start");

            var client = host.ClientOf(messenger);
            Assert.Contains("1. Петров", client.LastMessageText);
            // Ответ ушёл только в тот мессенджер, откуда пришло сообщение
            Assert.Empty(host.ClientOf(Other(messenger)).SentMessages);
        }

        [Theory]
        [InlineData(MessengerType.Telegram)]
        [InlineData(MessengerType.Vk)]
        public async Task NumberInput_LinksAccountOfThatMessenger(MessengerType messenger)
        {
            using var host = new BotTestHost();
            var student = await host.AddStudentAsync(7);

            await host.SendTextAsync(messenger, "555", "7");

            var saved = await host.ReloadStudentAsync(student.Id);
            Assert.NotNull(saved);
            Assert.Equal("555", saved.GetMessengerId(messenger));
            Assert.Null(saved.GetMessengerId(Other(messenger)));
            Assert.Contains("Регистрация завершена", host.ClientOf(messenger).LastMessageText);
        }

        [Fact]
        public async Task NumberInput_SameStudentInBothMessengers_LinksBothAccounts()
        {
            using var host = new BotTestHost();
            var student = await host.AddStudentAsync(7);

            await host.SendTextAsync(MessengerType.Telegram, "tg-1", "7");
            // Тот же студент заходит из ВКонтакте и вводит тот же номер
            await host.SendTextAsync(MessengerType.Vk, "vk-1", "7");

            var saved = await host.ReloadStudentAsync(student.Id);
            Assert.NotNull(saved);
            Assert.Equal("tg-1", saved.TelegramId);
            Assert.Equal("vk-1", saved.VkId);
        }

        [Fact]
        public async Task Start_RegisteredInTelegram_StillOffersRegistrationInVk()
        {
            using var host = new BotTestHost();
            await host.AddStudentAsync(3, "Сидоров");
            await host.SendTextAsync(MessengerType.Telegram, "tg-1", "3");

            await host.SendTextAsync(MessengerType.Vk, "vk-1", "/start");

            // Во ВКонтакте студент ещё не зарегистрирован, поэтому видит список группы со своим номером
            Assert.Contains("3. Сидоров", host.Vk.LastMessageText);
        }

        [Fact]
        public async Task NumberInput_NumberTakenInSameMessenger_Refuses()
        {
            using var host = new BotTestHost();
            await host.AddStudentAsync(4);

            await host.SendTextAsync(MessengerType.Telegram, "tg-1", "4");
            await host.SendTextAsync(MessengerType.Telegram, "tg-2", "4");

            Assert.Contains("уже занят", host.Telegram.LastMessageText);
        }

        [Theory]
        [InlineData(MessengerType.Telegram)]
        [InlineData(MessengerType.Vk)]
        public async Task AttendButton_MarksAttendance(MessengerType messenger)
        {
            using var host = new BotTestHost();
            var student = await host.AddStudentAsync(1);
            var schedule = await host.AddTodayScheduleAsync();
            await host.SendTextAsync(messenger, "user-1", "1");

            await host.PressButtonAsync(messenger, "user-1", $"attend_{student.Id}_{schedule.Id}");

            Assert.Equal(1, await host.CountAttendanceAsync(student.Id, schedule.Id));
            Assert.Equal("Вы отмечены!", host.ClientOf(messenger).LastAnswerText);
        }

        [Fact]
        public async Task AttendButton_PressedInBothMessengers_CountsOnce()
        {
            using var host = new BotTestHost();
            var student = await host.AddStudentAsync(1);
            var schedule = await host.AddTodayScheduleAsync();
            await host.SendTextAsync(MessengerType.Telegram, "tg-1", "1");
            await host.SendTextAsync(MessengerType.Vk, "vk-1", "1");

            // Уведомление приходит в оба мессенджера, студент может нажать кнопку дважды
            await host.PressButtonAsync(MessengerType.Vk, "vk-1", $"attend_{student.Id}_{schedule.Id}");
            await host.PressButtonAsync(MessengerType.Telegram, "tg-1", $"attend_{student.Id}_{schedule.Id}");

            Assert.Equal(1, await host.CountAttendanceAsync(student.Id, schedule.Id));
            Assert.Equal("Вы отмечены!", host.Telegram.LastAnswerText);
        }

        [Fact]
        public async Task AttendButton_OfAnotherStudent_Refused()
        {
            using var host = new BotTestHost();
            var student = await host.AddStudentAsync(1);
            var otherStudent = await host.AddStudentAsync(2, "Петров");
            var schedule = await host.AddTodayScheduleAsync();
            await host.SendTextAsync(MessengerType.Vk, "vk-1", "1");

            await host.PressButtonAsync(MessengerType.Vk, "vk-1", $"attend_{otherStudent.Id}_{schedule.Id}");

            Assert.Equal(0, await host.CountAttendanceAsync(otherStudent.Id, schedule.Id));
            Assert.Equal("Это кнопка для другого студента.", host.Vk.LastAnswerText);
            Assert.Equal(0, await host.CountAttendanceAsync(student.Id, schedule.Id));
        }

        [Fact]
        public async Task AttendButton_AfterDeadline_DoesNotMark()
        {
            // Дедлайн в полночь: на момент нажатия время отметки уже вышло
            using var host = new BotTestHost(deadlineTime: "00:00");
            var student = await host.AddStudentAsync(1);
            var schedule = await host.AddTodayScheduleAsync();
            await host.SendTextAsync(MessengerType.Vk, "vk-1", "1");

            await host.PressButtonAsync(MessengerType.Vk, "vk-1", $"attend_{student.Id}_{schedule.Id}");

            Assert.Equal(0, await host.CountAttendanceAsync(student.Id, schedule.Id));
            Assert.Contains("уже вышло", host.Vk.LastAnswerText);
        }

        [Fact]
        public async Task LeaderCommand_MainLeaderFromVkConfig_HasLeaderRights()
        {
            using var host = new BotTestHost(leaderVkIds: "vk-boss");
            await host.AddStudentAsync(1);

            await host.SendTextAsync(MessengerType.Vk, "vk-boss", "/list");

            // Староста получает отчёт, а не подсказку про меню
            Assert.DoesNotContain("Используйте кнопки меню", host.Vk.LastMessageText);
        }

        [Fact]
        public async Task LeaderCommand_OrdinaryStudent_HasNoLeaderRights()
        {
            using var host = new BotTestHost(leaderVkIds: "vk-boss");
            await host.AddStudentAsync(1);
            await host.SendTextAsync(MessengerType.Vk, "vk-1", "1");

            await host.SendTextAsync(MessengerType.Vk, "vk-1", "/list");

            Assert.Contains("Используйте кнопки меню", host.Vk.LastMessageText);
        }

        [Fact]
        public async Task Info_UnknownUser_AsksToRegister()
        {
            using var host = new BotTestHost();

            await host.SendTextAsync(MessengerType.Vk, "vk-1", "/info");

            Assert.Contains("Вы не зарегистрированы", host.Vk.LastMessageText);
        }

        private static MessengerType Other(MessengerType messenger)
        {
            return messenger == MessengerType.Telegram ? MessengerType.Vk : MessengerType.Telegram;
        }
    }
}
