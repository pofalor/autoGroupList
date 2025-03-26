# main.py

import threading
import time
import psycopg2
import telebot
import schedule_logic
from bot_instance import bot
import handlers
import db
from config import TELEGRAM_ADMIN_ID
from bot_instance import safe_send_message

def run_bot_polling():
    print("Запуск бота...")
    while True:
        try:
            print("Проверка соединения с БД...")
            connection = db.get_db_connection()
            connection.close()
            print("Соединение с БД успешно.")
            safe_send_message(TELEGRAM_ADMIN_ID, "Бот успешно запущен/перезапущен.")
            bot.polling(none_stop=True, interval=0, timeout=20)

        except psycopg2.OperationalError as db_err:
            print(f"Критическая ошибка подключения к БД при запуске polling: {db_err}")
            print("Бот остановлен. Проверьте настройки и доступность БД.")
            safe_send_message(TELEGRAM_ADMIN_ID, f"Критическая ошибка БД, бот остановлен: {db_err}")
            break
        except telebot.apihelper.ApiException as api_err:
            print(f"Ошибка Telegram API: {api_err}")
            print("Попытка перезапуска через 30 секунд...")
            safe_send_message(TELEGRAM_ADMIN_ID, f"Ошибка API Telegram: {api_err}. Перезапуск через 30 сек.")
            time.sleep(30)
        except Exception as e:
            print(f"Непредвиденная ошибка в главном цикле polling: {e}")
            print("Перезапуск через 15 секунд...")
            safe_send_message(TELEGRAM_ADMIN_ID, f"Непредвиденная ошибка: {e}. Перезапуск через 15 сек.")
            import traceback
            traceback.print_exc()
            time.sleep(15)

if __name__ == "__main__":
    notification_thread = threading.Thread(target=schedule_logic.notify_loop, daemon=True)
    notification_thread.start()
    run_bot_polling()