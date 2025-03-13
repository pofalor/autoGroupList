import telebot
from telebot import types
from datetime import datetime, timezone
import time
import threading
import psycopg2
from psycopg2 import sql
from config import TELEGRAM_BOT_TOKEN, DB_CONFIG  # Импортируем конфигурацию

# Подключение к базе данных
def get_db_connection():
    return psycopg2.connect(**DB_CONFIG)

# Инициализация бота
bot = telebot.TeleBot(TELEGRAM_BOT_TOKEN)

# Функция для проверки, является ли пользователь старостой
def is_leader(user_id):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT student_id FROM Leader WHERE telegram_id = %s", (user_id,))
            return cur.fetchone() is not None

# Функция для проверки, занят ли номер в группе другим пользователем
def is_number_taken(number):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT telegram_id FROM Students WHERE number_in_group = %s", (number,))
            result = cur.fetchone()
            return result is not None and result[0] is not None

# Функция для проверки, можно ли отметиться (до 17:00 UTC)
def can_mark_attendance():
    now = datetime.now(timezone.utc)
    return now.hour < 17

# Обработчик команды /start
@bot.message_handler(commands=['start'])
def start(message):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            # Получаем список студентов с номерами
            cur.execute("SELECT number_in_group, name FROM Students ORDER BY number_in_group")
            students = cur.fetchall()
            group_list = '\n'.join([f"{number}. {name}" for number, name in students])
            bot.send_message(
                message.chat.id,
                f"Привет! Список студентов твоей группы:\n\n{group_list}\nВведи свой номер в группе:"
            )

# Обработчик ввода номера в группе
@bot.message_handler(func=lambda message: message.text.isdigit())
def handle_number(message):
    number = int(message.text)
    user_id = message.from_user.id

    with get_db_connection() as conn:
        with conn.cursor() as cur:
            # Проверяем, существует ли такой номер в группе
            cur.execute("SELECT id FROM Students WHERE number_in_group = %s", (number,))
            student = cur.fetchone()

            if not student:
                bot.send_message(message.chat.id, "Такого номера в группе нет. Пожалуйста, введите корректный номер.")
                return

            # Проверяем, занят ли номер другим пользователем
            if is_number_taken(number):
                bot.send_message(
                    message.chat.id,
                    f"Номер {number} уже занят другим пользователем. Пожалуйста, введите другой номер."
                )
                return

            # Привязываем telegram_id к номеру
            cur.execute("UPDATE Students SET telegram_id = %s WHERE number_in_group = %s", (user_id, number))
            conn.commit()

            # Предлагаем выбрать подгруппу
            markup = types.InlineKeyboardMarkup()
            group1_button = types.InlineKeyboardButton("1-я подгруппа", callback_data="group_1")
            group2_button = types.InlineKeyboardButton("2-я подгруппа", callback_data="group_2")
            markup.add(group1_button, group2_button)
            bot.send_message(
                message.chat.id,
                f"Вы выбрали номер {number}. Теперь выберите свою подгруппу:",
                reply_markup=markup
            )

# Обработчик выбора подгруппы
@bot.callback_query_handler(func=lambda call: call.data.startswith("group_"))
def handle_group_choice(call):
    user_id = call.from_user.id
    subgroup = "subgroup1" if call.data == "group_1" else "subgroup2"

    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("UPDATE Students SET subgroup = %s WHERE telegram_id = %s", (subgroup, user_id))
            conn.commit()

    bot.send_message(
        call.message.chat.id,
        f"Вы выбрали {subgroup}. Ваши данные сохранены."
    )

@bot.message_handler(commands=['info'])
def info(message):
    user_id = message.from_user.id
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT name, number_in_group, subgroup 
                FROM Students 
                WHERE telegram_id = %s
            """, (user_id,))
            student_info = cur.fetchone()

            if student_info:
                name, number, subgroup = student_info
                bot.send_message(
                    message.chat.id,
                    f"Информация о вас:\n"
                    f"Имя: {name}\n"
                    f"Номер в группе: {number}\n"
                    f"Подгруппа: {subgroup}"
                )
            else:
                bot.send_message(message.chat.id, "Информация о вас не найдена.")

@bot.message_handler(commands=['schedule'])
def show_schedule(message):
    user_id = message.from_user.id

    with get_db_connection() as conn:
        with conn.cursor() as cur:
            # Получаем подгруппу студента
            cur.execute("SELECT subgroup FROM Students WHERE telegram_id = %s", (user_id,))
            student_data = cur.fetchone()

            if not student_data:
                bot.send_message(message.chat.id, "Сначала введите номер и выберите подгруппу с помощью команды /start.")
                return

            subgroup = student_data[0]

            # Определяем тип недели (четная/нечетная)
            current_week_number = datetime.now(timezone.utc).isocalendar()[1]
            week_type = 'even' if current_week_number % 2 == 0 else 'odd'

            # Получаем сегодняшний день недели
            today = datetime.now(timezone.utc).strftime('%A').lower()

            # Получаем расписание на сегодня
            cur.execute("""
                SELECT subject, start_time, end_time 
                FROM Schedule 
                WHERE subgroup = %s AND week_type = %s AND day_of_week = %s
                ORDER BY start_time
            """, (subgroup, week_type, today))
            lessons_today = cur.fetchall()

            # Формируем текст расписания
            if not lessons_today:
                schedule_text = "Сегодня занятий нет."
            else:
                schedule_text = f"Расписание на сегодня ({today.capitalize()} - {week_type.capitalize()} неделя):\n"
                for subject, start_time, end_time in lessons_today:
                    schedule_text += f"{start_time} - {end_time}: {subject}\n"

            bot.send_message(message.chat.id, schedule_text)

@bot.message_handler(commands=['time'])
def show_time(message):
    current_time = datetime.now(timezone.utc).strftime('%H:%M:%S')
    bot.send_message(message.chat.id, f"Текущее время (UTC): {current_time}")

@bot.message_handler(commands=['leader'])
def show_leader(message):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            # Получаем старосту
            cur.execute("""
                SELECT s.name 
                FROM Students s
                JOIN Leader l ON s.id = l.student_id
            """)
            leader_data = cur.fetchone()

            if leader_data:
                leader_name = leader_data[0]
                bot.send_message(message.chat.id, f"Староста группы: {leader_name}")
            else:
                bot.send_message(message.chat.id, "Староста не назначен.")

@bot.message_handler(commands=['help'])
def help_message(message):
    help_text = (
        "/start - Запустить бота и начать взаимодействие.\n"
        "/info - Получить информацию о себе (имя, номер в группе, подгруппа).\n"
        "/schedule - Показать расписание на сегодня.\n"
        "/time - Показать текущее время.\n"
        "/leader - Показать старосту группы.\n"
        "/help - Показать это сообщение с описанием команд."
    )
    bot.send_message(message.chat.id, help_text)

@bot.message_handler(commands=['list'])
def send_attendance_list(message):
    user_id = message.from_user.id

    # Проверяем, является ли пользователь старостой
    if not is_leader(user_id):
        bot.send_message(user_id, "У вас нет доступа к этой команде.")
        return

    with get_db_connection() as conn:
        with conn.cursor() as cur:
            # Получаем список отметившихся студентов
            cur.execute("""
                SELECT s.name, a.subject, a.date 
                FROM Attendance a
                JOIN Students s ON a.student_id = s.id
                WHERE a.is_present = TRUE
            """)
            attendance_data = cur.fetchall()

            if not attendance_data:
                bot.send_message(user_id, "Список присутствующих пуст.")
                return

            # Формируем список
            attendance_list = "Список отметившихся студентов:\n\n"
            for name, subject, date in attendance_data:
                attendance_list += f"{name} - {subject} ({date})\n"

            bot.send_message(user_id, attendance_list)

@bot.callback_query_handler(func=lambda call: call.data.startswith("attend_"))
def handle_attendance(call):
    _, student_id, subject = call.data.split('_')
    student_id = int(student_id)
    user_id = call.from_user.id

    with get_db_connection() as conn:
        with conn.cursor() as cur:
            if not can_mark_attendance():
                bot.answer_callback_query(call.id, "Отметиться уже нельзя, время прошло.")
                return

            cur.execute("""
                SELECT id FROM Attendance 
                WHERE student_id = %s AND subject = %s AND date = %s
            """, (student_id, subject, datetime.now(timezone.utc).date()))
            if cur.fetchone():
                bot.answer_callback_query(call.id, "Вы уже отметили присутствие на этом занятии.")
                return

            cur.execute("""
                INSERT INTO Attendance (student_id, subject, date, is_present)
                VALUES (%s, %s, %s, %s)
            """, (student_id, subject, datetime.now(timezone.utc).date(), True))
            conn.commit()

            bot.answer_callback_query(call.id, "Вы отметили присутствие на занятии.")

# Функция для проверки расписания и отправки уведомлений
def check_schedule():
    current_time = datetime.now()
    current_week_number = current_time.isocalendar()[1]
    week_type = 'even' if current_week_number % 2 == 0 else 'odd'
    today = current_time.strftime('%A').lower()

    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT id, telegram_id, subgroup FROM Students WHERE telegram_id IS NOT NULL")
            students = cur.fetchall()

            for student_id, telegram_id, subgroup in students:
                cur.execute("""
                    SELECT subject, start_time 
                    FROM Schedule 
                    WHERE subgroup = %s AND week_type = %s AND day_of_week = %s
                    ORDER BY start_time
                """, (subgroup, week_type, today))
                lessons_today = cur.fetchall()

                for subject, start_time in lessons_today:
                    lesson_start_time = datetime.strptime(start_time, '%H:%M').replace(
                        year=current_time.year, month=current_time.month, day=current_time.day
                    )

                    if lesson_start_time <= current_time and can_mark_attendance():
                        cur.execute("""
                            SELECT id FROM Notifications 
                            WHERE student_id = %s AND subject = %s AND date = %s
                        """, (student_id, subject, current_time.date()))
                        if not cur.fetchone():
                            markup = types.InlineKeyboardMarkup()
                            attend_button = types.InlineKeyboardButton(
                                "Я на паре",
                                callback_data=f"attend_{student_id}_{subject}"
                            )
                            markup.add(attend_button)
                            bot.send_message(
                                telegram_id,
                                f"Уведомление: Занятие '{subject}' начинается в {start_time}!\n"
                                "Нажмите кнопку, чтобы подтвердить присутствие.",
                                reply_markup=markup
                            )

                            cur.execute("""
                                INSERT INTO Notifications (student_id, subject, date, notification_type, is_sent)
                                VALUES (%s, %s, %s, %s, %s)
                            """, (student_id, subject, current_time.date(), 'start_class', True))
                            conn.commit()

# Запуск потока для уведомлений
def notify_loop():
    while True:
        check_schedule()
        time.sleep(60)

threading.Thread(target=notify_loop, daemon=True).start()

# Запуск бота
if __name__ == "__main__":
    bot.polling(none_stop=True)