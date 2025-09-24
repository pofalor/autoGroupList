# config.py

# Токен телеграм-бота
TELEGRAM_BOT_TOKEN = '7825034644:AAHVoxPs_CThj7aUTT0wyehBPMID1PZrNr8'

# Настройки подключения к PostgreSQL
DB_CONFIG = {
    'dbname': 'telegram_bot',
    'user': 'postgres',
    'password': 'admin',
    'host': 'localhost',
    'port': 5434
}

TELEGRAM_ADMIN_ID = 569209035

#Во сколько заканчивается сбор данных о посещениях
TIME_LIMIT_HOUR_LOCAL = 20

LOCAL_TIMEZONE_NAME = 'Europe/Moscow'