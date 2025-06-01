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
from logger import logger

def run_bot_polling():
    logger.info("Запуск бота...")
    while True:
        try:
            logger.info("Проверка соединения с БД...")
            connection = db.get_db_connection()
            connection.close()
            logger.info("Соединение с БД успешно.")
            safe_send_message(TELEGRAM_ADMIN_ID, "Бот успешно запущен/перезапущен.")
            bot.polling(none_stop=True, interval=0, timeout=20)

        except psycopg2.OperationalError as db_err:
            logger.critical(f"Критическая ошибка подключения к БД при запуске polling.", exc_info=True)
            logger.info("Бот остановлен. Проверьте настройки и доступность БД.")
            safe_send_message(TELEGRAM_ADMIN_ID, f"Критическая ошибка БД, бот остановлен: {db_err}")
            break
        except telebot.apihelper.ApiException as api_err:
            logger.error(f"Ошибка Telegram API.", exc_info=True)
            logger.info("Попытка перезапуска через 30 секунд...")
            safe_send_message(TELEGRAM_ADMIN_ID, f"Ошибка API Telegram: {api_err}. Перезапуск через 30 сек.")
            time.sleep(30)
        except Exception as e:
            logger.error(f"Непредвиденная ошибка в главном цикле polling.", exc_info=True)
            logger.info("Перезапуск через 15 секунд...")
            safe_send_message(TELEGRAM_ADMIN_ID, f"Непредвиденная ошибка: {e}. Перезапуск через 15 сек.")
            time.sleep(15)

if __name__ == "__main__":
    notification_thread = threading.Thread(target=schedule_logic.notify_loop, daemon=True)
    notification_thread.start()
    run_bot_polling()