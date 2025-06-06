# bot_instance.py
import telebot
from config import TELEGRAM_BOT_TOKEN
from logger import logger

bot = telebot.TeleBot(TELEGRAM_BOT_TOKEN)

def safe_send_message(chat_id, text, reply_markup=None, parse_mode=None):
    try:
        bot.send_message(chat_id, text, reply_markup=reply_markup, parse_mode=parse_mode)
        return True
    except telebot.apihelper.ApiException as e:
        logger.exception(f"Ошибка API Telegram при отправке сообщения в чат %s.", chat_id, exc_info=True)
    except Exception as e:
        logger.exception(f"Неизвестная ошибка при отправке сообщения в чат %s.", chat_id, exc_info=True)
    return False

def safe_answer_callback_query(callback_query_id, text=None, show_alert=False):
    try:
        bot.answer_callback_query(callback_query_id, text=text, show_alert=show_alert)
    except telebot.apihelper.ApiException as e:
        logger.exception(f"Ошибка API Telegram при ответе на callback %s.", callback_query_id, exc_info=True)
    except Exception as e:
        logger.exception(f"Неизвестная ошибка при ответе на callback %s.", callback_query_id, exc_info=True)