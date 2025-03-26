# bot_instance.py

import telebot
from config import TELEGRAM_BOT_TOKEN

bot = telebot.TeleBot(TELEGRAM_BOT_TOKEN)

def safe_send_message(chat_id, text, reply_markup=None, parse_mode=None):
    try:
        bot.send_message(chat_id, text, reply_markup=reply_markup, parse_mode=parse_mode)
        return True
    except telebot.apihelper.ApiException as e:
        print(f"Ошибка API Telegram при отправке сообщения в чат {chat_id}: {e}")
    except Exception as e:
        print(f"Неизвестная ошибка при отправке сообщения в чат {chat_id}: {e}")
    return False

def safe_answer_callback_query(callback_query_id, text=None, show_alert=False):
    try:
        bot.answer_callback_query(callback_query_id, text=text, show_alert=show_alert)
    except telebot.apihelper.ApiException as e:
        print(f"Ошибка API Telegram при ответе на callback {callback_query_id}: {e}")
    except Exception as e:
        print(f"Неизвестная ошибка при ответе на callback {callback_query_id}: {e}")