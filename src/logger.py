import logging
from src.utils import time_utils


def setup_logger():
    # Формируем имя файла с текущей датой
    log_filename = f"app_{time_utils.get_local_today().strftime('%Y-%m-%d')}.log"

    # Настраиваем логгер
    logger = logging.getLogger('daily_logger')
    logger.setLevel(logging.INFO)  # Уровень логирования

    # Создаем FileHandler для записи в файл
    file_handler = logging.FileHandler(log_filename, encoding='utf-8')

    # Формат логов: время, уровень, сообщение
    formatter = logging.Formatter('%(asctime)s %(levelname)s %(message)s')
    file_handler.setFormatter(formatter)

    # Добавляем обработчик к логгеру
    logger.addHandler(file_handler)

    return logger

# Инициализация логгера
logger = setup_logger()