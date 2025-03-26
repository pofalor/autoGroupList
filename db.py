# db.py

from datetime import datetime, timezone, time as dt_time
import psycopg2
from config import DB_CONFIG
from time_utils import get_local_now, get_local_today

def get_db_connection():
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        return conn
    except psycopg2.OperationalError as e:
        print(f"Ошибка подключения к базе данных: {e}")
        raise

def get_student_id_by_telegram(telegram_id):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("select id from Students where telegram_id = %s", (telegram_id,))
            result = cur.fetchone()
            return result[0] if result else None

def get_student_by_number(number):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT id FROM Students WHERE number_in_group = %s", (number,))
            return cur.fetchone()

def is_number_taken(number):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT telegram_id FROM Students WHERE number_in_group = %s", (number,))
            result = cur.fetchone()
            return result is not None and result[0] is not None

def link_telegram_id_to_student(telegram_id,  number):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("UPDATE Students SET telegram_id = %s WHERE number_in_group = %s", (telegram_id, number))
            conn.commit()

def update_student_subgroup(telegram_id, subgroup):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("UPDATE Students SET subgroup = %s WHERE telegram_id = %s", (subgroup, telegram_id))
            conn.commit()

def get_students_with_telegram():
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT id, telegram_id, subgroup FROM Students WHERE telegram_id IS NOT NULL")
            return cur.fetchall()

def get_students_list_for_start():
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT number_in_group, name FROM Students ORDER BY number_in_group")
            return cur.fetchall()

def is_leader(user_id):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT student_id FROM Leader WHERE telegram_id = %s", (user_id,))
            return cur.fetchone() is not None

def get_leader_telegram_id():
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT telegram_id FROM Leader")
            result = cur.fetchone()
            return result[0] if result else None

def get_schedule_for_student(subgroup, week_type, day_of_week):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT subject, start_time
                FROM Schedule
                WHERE subgroup = %s AND week_type = %s AND day_of_week = %s
                ORDER BY start_time
            """, (subgroup, week_type, day_of_week))
            return cur.fetchall()

def get_student_info_by_telegram(telegram_id):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT id, name, number_in_group, subgroup
                FROM Students
                WHERE telegram_id = %s
            """, (telegram_id,))
            result = cur.fetchone()
            if result:
                return {
                    "id": result[0],
                    "name": result[1],
                    "number": result[2],
                    "subgroup": result[3]
                }
            else:
                return None

def mark_attendance(student_id, subject, date, is_present=True):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            timestamp_utc = datetime.now(timezone.utc)
            cur.execute("""
                INSERT INTO Attendance (student_id, subject, date, is_present, timestamp)
                VALUES (%s, %s, %s, %s, %s)
                ON CONFLICT (student_id, subject, date) DO UPDATE SET is_present = EXCLUDED.is_present, timestamp = EXCLUDED.timestamp
            """, (student_id, subject, date, is_present, timestamp_utc))
            conn.commit()

def get_todays_attendance():
    local_today = get_local_today()
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT s.name, a.subject, a.date
                FROM Attendance a
                JOIN Students s ON a.student_id = s.id
                WHERE a.is_present = TRUE AND a.date = %s
                ORDER BY a.subject, s.number_in_group
            """, (local_today,))
            return cur.fetchall()

def check_notification_sent(student_id, subject, date):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT id FROM Notifications
                WHERE student_id = %s AND subject = %s AND date = %s AND notification_type = %s
            """, (student_id, subject, date, 'start_class'))
            return cur.fetchone() is not None

def record_notification(student_id, subject, date, notification_type, is_sent=True):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                INSERT INTO Notifications (student_id, subject, date, notification_type, is_sent)
                VALUES (%s, %s, %s, %s, %s)
                ON CONFLICT (student_id, subject, date, notification_type) DO NOTHING
            """, (student_id, subject, date, notification_type, is_sent))
            conn.commit()

def check_daily_report_sent(date):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT id FROM Notifications
                WHERE notification_type = %s AND date = %s
            """, ('end_of_day_report', date))
            return cur.fetchone() is not None

def record_daily_report_sent(date):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                INSERT INTO Notifications (student_id, subject, date, notification_type, is_sent)
                VALUES (NULL, NULL, %s, %s, TRUE)
                ON CONFLICT (student_id, subject, date, notification_type) DO NOTHING
            """, (date, 'end_of_day_report'))
            conn.commit()