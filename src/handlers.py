# handlers.py

import telebot
from telebot import types
import functools
import db
import schedule_logic
from time_utils import get_local_today
from bot_instance import bot, safe_send_message, safe_answer_callback_query
from config import TIME_LIMIT_HOUR_LOCAL
from logger import logger

def create_main_menu():
    markup = types.ReplyKeyboardMarkup(resize_keyboard=True, row_width=2)
    btn_schedule = types.KeyboardButton('/schedule')
    btn_info = types.KeyboardButton('/info')
    btn_attend_other = types.KeyboardButton('/attend_other_group')
    btn_help = types.KeyboardButton('/help')
    markup.add(btn_schedule, btn_info, btn_attend_other, btn_help)
    return markup

def registration_required(func):
    @functools.wraps(func)
    def wrapper(message, *args, **kwargs):
        user_id = message.from_user.id
        student_info = db.get_student_info_by_telegram(user_id)
        if student_info is None:
            safe_send_message(message.chat.id, "Вы не зарегистрированы. Пожалуйста, используйте /start.", reply_markup=types.ReplyKeyboardRemove())
            return
        elif student_info.get("subgroup") is None:
             safe_send_message(message.chat.id, "Вы не выбрали подгруппу. Пожалуйста, завершите регистрацию.")
             return
        return func(message, student_info, *args, **kwargs)
    return wrapper

@bot.message_handler(commands=['start'])
def start(message):
    user_id = message.from_user.id
    student_info = db.get_student_info_by_telegram(user_id)
    if student_info and student_info.get("subgroup"):
        safe_send_message(message.chat.id, f"С возвращением, {student_info['name']}! 👋", reply_markup=create_main_menu())
        return
    elif student_info and not student_info.get("subgroup"):
         markup = types.InlineKeyboardMarkup(row_width=2)
         group1_button = types.InlineKeyboardButton("1-я подгруппа", callback_data="setgroup_subgroup1")
         group2_button = types.InlineKeyboardButton("2-я подгруппа", callback_data="setgroup_subgroup2")
         markup.add(group1_button, group2_button)
         safe_send_message(message.chat.id, f"Привет, {student_info['name']}! Выберите подгруппу:", reply_markup=markup)
         return
    students = db.get_students_list_for_start()
    if not students:
        safe_send_message(message.chat.id, "Не удалось загрузить список группы.")
        return
    group_list = '\n'.join([f"{number}. {name}" for number, name in students])
    safe_send_message(message.chat.id, f"Привет! Список группы:\n{group_list}\n\nВведите свой номер:", reply_markup=types.ReplyKeyboardRemove())

@bot.message_handler(commands=['help'])
def help_command(message):
    help_text = """
    **Справка по боту** 🤖

    Я помогу тебе отслеживать расписание и отмечаться на парах.

    **Доступные команды:**
    /start - Начало работы / приветствие.
    /schedule - Расписание на сегодня.
    /info - Ваша информация (ФИО, номер, подгруппа).
    /attend\_other\_group - Отметиться на паре другой подгруппы.
    /help - Эта справка.

    Уведомления о начале пар приходят автоматически.
    Отмечаться можно до {limit_hour}:00 по местному времени.
    """.format(limit_hour=TIME_LIMIT_HOUR_LOCAL)
    safe_send_message(message.chat.id, help_text, parse_mode='Markdown', reply_markup=create_main_menu())

@bot.message_handler(commands=['info'])
@registration_required
def info_command(message, student_info):
    info_text = f"""
    **Ваша информация:**
    👤 **ФИО:** {student_info['name']}
    🔢 **Номер в группе:** {student_info['number']}
    🎓 **Подгруппа:** {'1-я' if student_info['subgroup'] == 'subgroup1' else '2-я'}
    """
    safe_send_message(message.chat.id, info_text, parse_mode='Markdown', reply_markup=create_main_menu())

@bot.message_handler(commands=['schedule'])
@registration_required
def schedule_command(message, student_info):
    subgroup = student_info['subgroup']
    week_type, day_name, local_date = schedule_logic.get_current_week_type_and_day()
    subgroup_display = "1-й" if subgroup == "subgroup1" else "2-й"
    try:
        schedule_data = db.get_schedule_for_student(subgroup, week_type, day_name)
        schedule_text = schedule_logic.format_schedule(schedule_data)
        date_str = local_date.strftime('%d.%m.%Y')
        header = f"Расписание для **{subgroup_display} подгруппы** на сегодня ({date_str}, {day_name.capitalize()}, {week_type} неделя):\n\n"
        safe_send_message(message.chat.id, header + schedule_text, parse_mode='Markdown', reply_markup=create_main_menu())
    except Exception as e:
        logger.exception(f"Ошибка получения расписания %s, %s, %s.", subgroup, week_type, day_name, exc_info=True)
        safe_send_message(message.chat.id, "Не удалось получить расписание.")

@bot.message_handler(commands=['attend_other_group'])
@registration_required
def attend_other_group_command(message, student_info):
    current_subgroup = student_info['subgroup']
    student_db_id = student_info['id']
    other_subgroup = "subgroup2" if current_subgroup == "subgroup1" else "subgroup1"
    other_subgroup_display = "2-й" if other_subgroup == "subgroup2" else "1-й"
    week_type, day_name, local_date = schedule_logic.get_current_week_type_and_day()

    if not schedule_logic.can_mark_attendance():
        safe_send_message(message.chat.id, f"К сожалению, время для отметки (до {TIME_LIMIT_HOUR_LOCAL}:00 по местному времени) уже вышло.", reply_markup=create_main_menu())
        return

    try:
        other_schedule_data = db.get_schedule_for_student(other_subgroup, week_type, day_name)
        date_str = local_date.strftime('%d.%m.%Y')
        if not other_schedule_data:
            safe_send_message(message.chat.id, f"У **{other_subgroup_display} подгруппы** сегодня ({date_str}, {day_name.capitalize()}, {week_type} неделя) пар нет.", reply_markup=create_main_menu())
            return

        markup = types.InlineKeyboardMarkup()
        schedule_text_list = [f"Пары **{other_subgroup_display} подгруппы** на сегодня ({date_str}, {day_name.capitalize()}, {week_type} неделя):"]
        for subject_id, subject, start_time in other_schedule_data:
            callback_data = f"markother_{student_db_id}_{subject_id}"
            button = types.InlineKeyboardButton(f"{start_time} - {subject}", callback_data=callback_data)
            markup.add(button)
            schedule_text_list.append(f"• {start_time} - {subject}")
        schedule_text = "\n".join(schedule_text_list)
        safe_send_message(message.chat.id, f"{schedule_text}\n\nВыберите пару:", reply_markup=markup, parse_mode='Markdown')
    except Exception as e:
        logger.exception(f"Ошибка получения расписания другой подгруппы (%s).", other_subgroup, exc_info=True)
        safe_send_message(message.chat.id, "Не удалось получить расписание другой подгруппы.")

@bot.message_handler(func=lambda message: message.text.isdigit())
def handle_number(message):
    user_id = message.from_user.id
    chat_id = message.chat.id
    student_info = db.get_student_info_by_telegram(user_id)
    if student_info and student_info.get("subgroup"):
        safe_send_message(chat_id, "Вы уже зарегистрированы.", reply_markup=create_main_menu())
        return
    elif student_info and not student_info.get("subgroup"):
         safe_send_message(chat_id, "Выберите подгруппу (кнопки выше).")
         return
    try:
        number = int(message.text)
    except ValueError:
        safe_send_message(chat_id, "Введите номер цифрами.")
        return
    student_exists = db.get_student_by_number(number)
    if not student_exists:
        safe_send_message(chat_id, "Такого номера нет.")
        return
    if db.is_number_taken(number):
        safe_send_message(chat_id, f"Номер {number} уже занят.")
        return
    try:
        db.link_telegram_id_to_student(user_id, number)
    except Exception as e:
        logger.exception("Ошибка привязки %s к %s", user_id, number, exc_info=True)
        safe_send_message(chat_id, "Ошибка сохранения номера.")
        return
    markup = types.InlineKeyboardMarkup(row_width=2)
    group1_button = types.InlineKeyboardButton("1-я подгруппа", callback_data="setgroup_subgroup1")
    group2_button = types.InlineKeyboardButton("2-я подгруппа", callback_data="setgroup_subgroup2")
    markup.add(group1_button, group2_button)
    safe_send_message(chat_id, f"Номер {number} сохранен! 👍\nВыберите подгруппу:", reply_markup=markup)

@bot.callback_query_handler(func=lambda call: call.data.startswith("setgroup_"))
def handle_group_choice(call):
    user_id = call.from_user.id
    chat_id = call.message.chat.id
    message_id = call.message.message_id
    subgroup = call.data.split("_")[1]
    subgroup_display_name = "1-я" if subgroup == "subgroup1" else "2-я"
    try:
        db.update_student_subgroup(user_id, subgroup)
        try:
            bot.edit_message_reply_markup(chat_id=chat_id, message_id=message_id, reply_markup=None)
        except telebot.apihelper.ApiException as e:
             logger.exception("Не удалось убрать кнопки подгруппы", exc_info=True)
        safe_send_message(chat_id, f"Вы выбрали {subgroup_display_name} подгруппу. Регистрация завершена!", reply_markup=create_main_menu())
        safe_answer_callback_query(call.id, text=f"Подгруппа {subgroup_display_name} выбрана.")
    except Exception as e:
        logger.exception("Ошибка обновления подгруппы %s", user_id, exc_info=True)
        safe_send_message(chat_id, "Ошибка сохранения подгруппы.")
        safe_answer_callback_query(call.id, text="Ошибка сохранения", show_alert=True)

@bot.callback_query_handler(func=lambda call: call.data.startswith("attend_"))
def handle_attendance(call):
    user_id = call.from_user.id
    chat_id = call.message.chat.id
    message_id = call.message.message_id
    try:
        _, student_db_id_str, subject_id = call.data.split("_", 2)
        student_db_id = int(student_db_id_str)
    except (ValueError, IndexError):
        logger.exception(f"Ошибка парсинга callback attend_: %s", call.data, exc_info=True)
        safe_answer_callback_query(call.id, text="Ошибка кнопки", show_alert=True)
        return

    current_student_db_id = db.get_student_id_by_telegram(user_id)
    if current_student_db_id != student_db_id:
        safe_answer_callback_query(call.id, text="Это кнопка для другого студента.", show_alert=True)
        return

    if not schedule_logic.can_mark_attendance():
        safe_answer_callback_query(call.id, text=f"Время для отметки (до {TIME_LIMIT_HOUR_LOCAL}:00 по местному времени) уже вышло.", show_alert=True)
        try:
            bot.edit_message_reply_markup(chat_id=chat_id, message_id=message_id, reply_markup=None)
        except Exception as e:
            logger.exception(f"Не удалось убрать кнопку attend_ после лимита.", exc_info=True)
        return

    subject = db.get_subject_name_by_id(subject_id)
    try:
        local_today = get_local_today()
        db.mark_attendance(student_db_id, subject, local_today, is_present=True)
        try:
            original_message_lines = call.message.text.split('\n')
            header = original_message_lines[0]
            new_text = f"{header}\n\n✅ Вы отмечены."
            bot.edit_message_text(chat_id=chat_id, message_id=message_id, text=new_text, reply_markup=None)
        except telebot.apihelper.ApiException as e:
            if 'message is not modified' not in str(e):
                logger.exception("Ошибка изменения сообщения attend_", exc_info=True)
                safe_send_message(chat_id, f"✅ Вы отмечены на занятии '{subject}'.")
        safe_answer_callback_query(call.id, text="Вы отмечены!")
    except Exception as e:
        logger.exception("Ошибка отметки посещаемости attend_ %s, %s", student_db_id, subject, exc_info=True)
        safe_send_message(chat_id, "Ошибка при отметке.")
        safe_answer_callback_query(call.id, text="Ошибка записи", show_alert=True)

@bot.callback_query_handler(func=lambda call: call.data.startswith("markother_"))
def handle_mark_other_attendance(call):
    user_id = call.from_user.id
    chat_id = call.message.chat.id
    message_id = call.message.message_id
    try:
        _, student_db_id_str, subject_id = call.data.split("_", 2)
        student_db_id_from_callback = int(student_db_id_str)
    except (ValueError, IndexError):
        logger.exception("Ошибка парсинга callback markother_: %s", call.data)
        safe_answer_callback_query(call.id, text="Ошибка кнопки", show_alert=True)
        return
    current_student_info = db.get_student_info_by_telegram(user_id)
    if not current_student_info or current_student_info['id'] != student_db_id_from_callback:
         safe_answer_callback_query(call.id, text="Действие не разрешено.", show_alert=True)
         return

    if not schedule_logic.can_mark_attendance():
        safe_answer_callback_query(call.id, text=f"Время для отметки (до {TIME_LIMIT_HOUR_LOCAL}:00 по местному времени) уже вышло.", show_alert=True)
        try:
            bot.edit_message_reply_markup(chat_id=chat_id, message_id=message_id, reply_markup=None)
        except Exception as e:
            logger.exception("Не удалось убрать кнопки markother_ после лимита")
        return

    subject = db.get_subject_name_by_id(subject_id)
    try:
        local_today = get_local_today()
        student_db_id = current_student_info['id']
        db.mark_attendance(student_db_id, subject, local_today, is_present=True)
        try:
            new_text = call.message.text + f"\n\n✅ Вы отмечены на занятии '{subject}'."
            bot.edit_message_text(chat_id=chat_id, message_id=message_id, text=new_text, reply_markup=None, parse_mode='Markdown')
        except telebot.apihelper.ApiException as e:
            if 'message is not modified' not in str(e):
                logger.exception("Ошибка изменения сообщения markother_", exc_info=True)
                safe_send_message(chat_id, f"✅ Вы отмечены на занятии '{subject}'.")
        safe_answer_callback_query(call.id, text=f"Вы отмечены на '{subject}'!")
    except Exception as e:
        logger.exception("Ошибка отметки посещаемости markother_ %s, %s", student_db_id, subject)
        safe_send_message(chat_id, f"Ошибка при отметке на '{subject}'.")
        safe_answer_callback_query(call.id, text="Ошибка записи", show_alert=True)

@bot.message_handler(content_types=['text'])
def handle_other_text(message):
     user_id = message.from_user.id
     student_info = db.get_student_info_by_telegram(user_id)
     if student_info and student_info.get("subgroup"):
         safe_send_message(message.chat.id, "Используйте кнопки меню или введите команду.", reply_markup=create_main_menu())
     elif student_info and not student_info.get("subgroup"):
          safe_send_message(message.chat.id, "Пожалуйста, выберите подгруппу.")
     else:
         safe_send_message(message.chat.id, "Привет! Используйте /start для регистрации.", reply_markup=types.ReplyKeyboardRemove())