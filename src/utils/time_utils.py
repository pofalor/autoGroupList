# time_utils.py

from datetime import datetime, timezone
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError
from src.config.config import LOCAL_TIMEZONE_NAME

try:
    LOCAL_TIMEZONE = ZoneInfo(LOCAL_TIMEZONE_NAME)
    print(f"INFO (time_utils): Локальный часовой пояс: {LOCAL_TIMEZONE_NAME} ({LOCAL_TIMEZONE})")
except ZoneInfoNotFoundError:
    print(f"ERROR (time_utils): Не найден пояс '{LOCAL_TIMEZONE_NAME}'. Используется UTC.")
    LOCAL_TIMEZONE = timezone.utc

def get_local_now():
    return datetime.now(LOCAL_TIMEZONE)

def get_local_today():
    return datetime.now(LOCAL_TIMEZONE).date()