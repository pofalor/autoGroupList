# schedule_logic.py

from datetime import datetime, timezone, time as dt_time
import time as py_time
import psycopg2
from telebot import types
import db
from bot_instance import safe_send_message
from config import TELEGRAM_ADMIN_ID, TIME_LIMIT_HOUR_LOCAL
from src.logger import logger
from time_utils import LOCAL_TIMEZONE, get_local_now, get_local_today

def get_current_week_type_and_day():
    now_local = get_local_now()
    week_number = now_local.isocalendar()[1]
    week_type = 'even' if week_number % 2 == 0 else 'odd'
    day_name = now_local.strftime('%A').lower()
    local_date = now_local.date()
    return week_type, day_name, local_date

def can_mark_attendance():
    now_local = get_local_now()
    return now_local.time() < dt_time(hour=TIME_LIMIT_HOUR_LOCAL)

def format_schedule(schedule_data):
    if not schedule_data:
        return "Сегодня пар нет."
    schedule_list = [f"• {start_time} - {subject}" for subject_id, subject, start_time in schedule_data]
    return "\n".join(schedule_list)

def send_attendance_list_to_leader():
    local_today = get_local_today()
    if db.check_daily_report_sent(local_today):
        return
    leader_id = db.get_leader_telegram_id()
    if not leader_id:
        return
    attendance_data = db.get_todays_attendance()
    if not attendance_data:
        message_to_leader = f"За {local_today.strftime('%d.%m.%Y')} никто не отметился."
    else:
        attendance_list = f"Список отметившихся студентов за {local_today.strftime('%d.%m.%Y')}:\n\n"
        current_subject = None
        for name, subject, _ in attendance_data:
            if subject != current_subject:
                if current_subject is not None:
                    attendance_list += "\n"
                attendance_list += f"--- {subject} ---\n"
                current_subject = subject
            attendance_list += f"- {name}\n"
        message_to_leader = attendance_list
    if safe_send_message(leader_id, message_to_leader):
        db.record_daily_report_sent(local_today)
    else:
        admin_message = f"Не удалось отправить ежедневный отчет старосте (ID: {leader_id})."
        safe_send_message(TELEGRAM_ADMIN_ID, admin_message)

def check_schedule_and_notify():
    current_time_local = get_local_now()
    current_local_date = current_time_local.date()
    week_type, day_name, _ = get_current_week_type_and_day()

    time_limit_passed = not can_mark_attendance()

    if time_limit_passed:
        send_attendance_list_to_leader()

    students = db.get_students_with_telegram()
    if not students:
        return

    for student_db_id, telegram_id, subgroup in students:
        if not subgroup:
            continue

        lessons_today = db.get_schedule_for_student(subgroup, week_type, day_name)

        for subject_id, subject, start_time_str in lessons_today:
            try:
                start_hour, start_minute = map(int, start_time_str.split(':'))
                lesson_start_time_local_aware = datetime.combine(
                   current_local_date,
                   dt_time(hour=start_hour, minute=start_minute),
                   tzinfo=LOCAL_TIMEZONE
                )

                if lesson_start_time_local_aware <= current_time_local:
                    notification_already_sent = db.check_notification_sent(student_db_id, subject, current_local_date)
                    if not notification_already_sent:
                        markup = None
                        if not time_limit_passed:
                             markup = types.InlineKeyboardMarkup()
                             attend_button = types.InlineKeyboardButton(
                                 "Я на паре",
                                 callback_data=f"attend_{student_db_id}_{subject_id}"
                             )
                             markup.add(attend_button)

                        message_text = f"🔔 Напоминание: Занятие '{subject}' начинается в {start_time_str} (по местному времени)."
                        if time_limit_passed:
                             message_text += f"\n\n(Время для отметки (до {TIME_LIMIT_HOUR_LOCAL}:00 по местному времени) уже вышло)"
                        elif markup:
                             message_text += "\nНажмите кнопку, чтобы подтвердить присутствие."

                        if safe_send_message(telegram_id, message_text, reply_markup=markup):
                            db.record_notification(student_db_id, subject, current_local_date, 'start_class')

            except ValueError:
                logger.exception("[ERROR] Неверный формат времени '%s' для предмета '%s' в расписании.", start_time_str, subject)
            except Exception as e:
                logger.exception(f"[ERROR] Ошибка обработки расписания для student %s, subject %s", telegram_id, subject)

def notify_loop():
    logger.info("Запуск цикла уведомлений...")
    while True:
        try:
            check_schedule_and_notify()
            py_time.sleep(60)
        except psycopg2.OperationalError as db_err:
             logger.exception(f"[ERROR] Ошибка БД в цикле уведомлений. Повтор через 60 сек.")
             py_time.sleep(60)
        except Exception as e:
            logger.exception(f"[CRITICAL] Непредвиденная ошибка в цикле уведомлений. Перезапуск через 60 сек.")
            py_time.sleep(60)