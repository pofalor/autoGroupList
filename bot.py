import telebot
from telebot import types
from datetime import datetime, timedelta
from data import students, schedule_1, schedule_2  # Импорт расписаний
import time
import threading

TOKEN = '7825034644:AAHVoxPs_CThj7aUTT0wyehBPMID1PZrNr8'
bot = telebot.TeleBot(TOKEN)

# map {number in the group: telegram id}
student_data = {}
attendance = {}  # Словарь для отслеживания присутствующих на занятии

# Номер старосты
LEADER_NUMBER = 12

# Функция для проверки, является ли пользователь старостой
def is_leader(user_id):
    if user_id in student_data:
        return student_data[user_id]['number'] == LEADER_NUMBER
    return False

# command /start
@bot.message_handler(commands=['start'])
def start(message):
    group_list = '\n'.join([f"{i + 1}. {name}" for i, name in enumerate(students)])
    bot.send_message(
        message.chat.id,
        f"Привет! Список студентов твоей группы:\n\n{group_list}\nВведи свой номер в группе:"
    )

# Processing the students number
@bot.message_handler(func=lambda message: message.text.isdigit())
def handle_number(message):
    number = int(message.text)
    if 1 <= number <= len(students):
        user_id = message.from_user.id
        if user_id not in student_data:
            student_data[user_id] = {}
        student_data[user_id]['number'] = number

        # После ввода номера предлагаем выбрать подгруппу
        markup = types.InlineKeyboardMarkup()
        group1_button = types.InlineKeyboardButton("1-я подгруппа", callback_data="group_1")
        group2_button = types.InlineKeyboardButton("2-я подгруппа", callback_data="group_2")
        markup.add(group1_button, group2_button)
        bot.send_message(
            message.chat.id,
            f"Вы выбрали номер {number} - {students[number - 1]}. Теперь выберите свою подгруппу:",
            reply_markup=markup
        )
    else:
        bot.send_message(message.chat.id, "Пожалуйста, введите корректный номер из списка.")

@bot.callback_query_handler(func=lambda call: call.data.startswith("group_"))
def handle_group_choice(call):
    user_id = call.from_user.id
    if call.data == "group_1":
        sub_group = "subgroup1"
    elif call.data == "group_2":
        sub_group = "subgroup2"

    # Сохраняем выбор подгруппы
    if user_id in student_data:
        student_data[user_id]['sub_group'] = sub_group
        bot.send_message(
            call.message.chat.id,
            f"Вы выбрали {sub_group}. Ваши данные сохранены."
        )
    else:
        bot.send_message(call.message.chat.id, "Сначала введите номер в группе с помощью команды /start.")

@bot.message_handler(commands=['info'])
def info(message):
    user_id = message.from_user.id
    if user_id in student_data:
        student_info = student_data[user_id]
        number = student_info.get('number', 'Не задан')
        sub_group = student_info.get('sub_group', 'Не выбрана')
        student_name = students[number - 1] if isinstance(number, int) else 'Неизвестно'

        bot.send_message(
            message.chat.id,
            f"Информация о вас:\n"
            f"Имя: {student_name}\n"
            f"Номер в группе: {number}\n"
            f"Подгруппа: {sub_group}"
        )
    else:
        bot.send_message(message.chat.id,
                         "Информация о вас не найдена. Пожалуйста, введите номер и выберите подгруппу с помощью команды /start.")

# Функция для отправки уведомлений о начале занятия
def send_class_notification(user_id, subject, start_time):
    markup = types.InlineKeyboardMarkup()
    attend_button = types.InlineKeyboardButton("Я на паре", callback_data=f"attend_{user_id}_{subject}")
    markup.add(attend_button)
    bot.send_message(
        user_id,
        f"Уведомление: Занятие '{subject}' начинается в {start_time}!\nНажмите кнопку, чтобы подтвердить присутствие.",
        reply_markup=markup
    )

# Функция для обработки нажатия кнопки "Я на паре"
@bot.callback_query_handler(func=lambda call: call.data.startswith("attend_"))
def handle_attendance(call):
    _, user_id, subject = call.data.split('_')
    user_id = int(user_id)

    # Проверяем, если пользователь еще не добавлен в список присутствующих
    if subject not in attendance:
        attendance[subject] = []

    if user_id not in attendance[subject]:
        attendance[subject].append(user_id)
        bot.answer_callback_query(call.id, "Вы отметили присутствие на занятии.")
    else:
        bot.answer_callback_query(call.id, "Вы уже отметили присутствие на этом занятии.")

# Функция для завершения занятия и отправки уведомления преподавателю
def end_class_notification(subject):
    # Найдем идентификатор старосты среди зарегистрированных студентов
    teacher_id = None
    for user_id, data in student_data.items():
        if data['number'] == LEADER_NUMBER:
            teacher_id = user_id
            break

    # Проверяем, найден ли преподаватель
    if teacher_id is None:
        print(f"Староста с номером {LEADER_NUMBER} не найден в student_data.")
        return

    if subject in attendance:
        present_students = attendance[subject]
        count_present = len(present_students)

        if count_present > 0:
            student_names = [students[student_data[user]['number'] - 1] for user in present_students]
            student_list = '\n'.join(student_names)
        else:
            student_list = "Нет присутствующих."

        bot.send_message(
            teacher_id,
            f"Занятие '{subject}' завершено.\n"
            f"Количество присутствующих: {count_present}\n"
            f"Список присутствующих:\n{student_list}"
        )
        del attendance[subject]

# Функция для проверки расписания и отправки уведомлений
def check_schedule():
    current_time = datetime.now()
    current_week_type = 'even' if current_time.isocalendar()[1] % 2 == 0 else 'odd'

    for user_id, data in student_data.items():
        subgroup = data['sub_group']
        schedule = schedule_1 if subgroup == 'subgroup1' else schedule_2

        # Проверяем расписание на текущий день недели
        today = current_time.strftime('%A').lower()  # Получаем название дня недели (например, 'monday')
        lessons_today = schedule[current_week_type][today]

        for lesson in lessons_today:
            lesson_start_time = datetime.strptime(lesson['start'], '%H:%M').replace(
                year=current_time.year, month=current_time.month, day=current_time.day
            )
            lesson_end_time = datetime.strptime(lesson['end'], '%H:%M').replace(
                year=current_time.year, month=current_time.month, day=current_time.day
            )

            # Проверяем, если занятие начинается в данный момент
            if lesson_start_time == current_time.replace(second=0, microsecond=0):
                send_class_notification(user_id, lesson['subject'], lesson['start'])

                # Вызов функции завершения занятия в момент окончания
                threading.Timer((lesson_end_time - current_time).total_seconds(), end_class_notification,
                                args=[lesson['subject']]).start()

# Функция для вывода расписания на сегодня
@bot.message_handler(commands=['schedule'])
def show_schedule(message):
    user_id = message.from_user.id
    if user_id in student_data:
        subgroup = student_data[user_id]['sub_group']
        current_week_number = datetime.now().isocalendar()[1]
        current_week_type = 'even' if current_week_number % 2 == 0 else 'odd'
        schedule = schedule_1 if subgroup == 'subgroup1' else schedule_2

        # Получаем сегодняшний день недели
        today = datetime.now().strftime('%A').lower()  # Например, 'monday'
        lessons_today = schedule[current_week_type][today]

        # Формируем текст с указанием четности недели
        schedule_text = f"Расписание на сегодня ({today.capitalize()} - {current_week_type.capitalize()} неделя):\n"
        if not lessons_today:
            schedule_text += "Занятий нет.\n"
        else:
            for lesson in lessons_today:
                schedule_text += f"{lesson['start']} - {lesson['subject']}\n"

        bot.send_message(message.chat.id, schedule_text)
    else:
        bot.send_message(message.chat.id, "Сначала введите номер и выберите подгруппу с помощью команды /start.")

@bot.message_handler(commands=['time'])
def show_time(message):
    current_time = datetime.now().strftime('%H:%M:%S')  # Получаем текущее время в формате ЧЧ:ММ:СС
    bot.send_message(message.chat.id, f"Текущее время: {current_time}")

@bot.message_handler(commands=['leader'])
def show_leader(message):
    leader_name = students[LEADER_NUMBER - 1]  # Получаем имя старосты по номеру
    bot.send_message(message.chat.id, f"Староста группы: {leader_name}")

@bot.message_handler(commands=['help'])
def help_message(message):
    help_text = (
        "/start - Запустить бота и начать взаимодействие.\n"
        "/info - Получить информацию о себе (имя, номер в группе, подгруппа).\n"
        "/schedule - Показать расписание на сегодня.\n"
        "/time - Показать текущее время.\n"
        "/leader - Показать старосту группы.\n"
        "/list - Показать список отметившихся студентов (только для старосты).\n"
        "/help - Показать это сообщение с описанием команд."
    )
    bot.send_message(message.chat.id, help_text)

# Команда /list для отправки списка отметившихся студентов
@bot.message_handler(commands=['list'])
def send_attendance_list(message):
    user_id = message.from_user.id

    # Проверяем, является ли пользователь старостой
    if not is_leader(user_id):
        bot.send_message(user_id, "У вас нет доступа к этой команде.")
        return

    # Формируем список отметившихся студентов
    if not attendance:
        bot.send_message(user_id, "Список присутствующих пуст.")
        return

    attendance_list = []
    for subject, user_ids in attendance.items():
        student_names = [students[student_data[user]['number'] - 1] for user in user_ids]
        attendance_list.append(f"Предмет: {subject}\nПрисутствовали: {', '.join(student_names)}\n")

    # Отправляем список старосте
    bot.send_message(user_id, "Список отметившихся студентов:\n\n" + "\n".join(attendance_list))

def notify_loop():
    while True:
        check_schedule()
        time.sleep(60)  # Проверка каждую минуту

# Запускаем поток для уведомлений
threading.Thread(target=notify_loop, daemon=True).start()

# Начинаем polling
bot.polling(none_stop=True)