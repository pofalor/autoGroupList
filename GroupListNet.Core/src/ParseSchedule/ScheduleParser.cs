using GroupListNet.Core.src.DataResult;
using GroupListNet.Core.src.Enums;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GroupListNet.Core.src.ParseSchedule
{
    public static class ScheduleParser
    {
        // Метод парсинга текста расписания
        public static IDataResult<List<ParsedScheduleItem>>? ParseScheduleText(string text, ILogger _logger)
        {
            var result = new DataResult<List<ParsedScheduleItem>>();
            var items = new List<ParsedScheduleItem>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            DayOfWeek? currentDay = null;
            WeekType? currentWeekType = null; // Для строк типа "чет", "неч"

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Проверка на день недели
                var dayMatch = Regex.Match(line, @"^───────(\w+)───────$");
                if (dayMatch.Success)
                {
                    var dayName = dayMatch.Groups[1].Value;
                    if (Enum.TryParse<DayOfWeek>(dayName, true, out var parsedDay))
                    {
                        currentDay = parsedDay;
                        currentWeekType = null; // Сбрасываем тип недели на новый день
                        continue;
                    }
                    else
                    {
                        // Попробуем альтернативные названия, если стандартный Parse не сработал
                        // Это нужно, если в тексте дни на русском
                        currentDay = dayName switch
                        {
                            "Понедельник" => DayOfWeek.Monday,
                            "Вторник" => DayOfWeek.Tuesday,
                            "Среда" => DayOfWeek.Wednesday,
                            "Четверг" => DayOfWeek.Thursday,
                            "Пятница" => DayOfWeek.Friday,
                            "Суббота" => DayOfWeek.Saturday,
                            "Воскресенье" => DayOfWeek.Sunday,
                            _ => (DayOfWeek?)null
                        };
                        if (currentDay.HasValue)
                        {
                            currentWeekType = null;
                            continue;
                        }
                    }
                }

                // Проверка на тип недели (чет/неч) в начале строки
                var weekTypeMatch = Regex.Match(line, @"(чет|неч)\s+");
                if (weekTypeMatch.Success)
                {
                    currentWeekType = weekTypeMatch.Groups[1].Value switch
                    {
                        "чет" => WeekType.Even,
                        "неч" => WeekType.Odd,
                        _ => (WeekType?)null
                    };
                    // Продолжаем парсинг остальной части строки
                }

                // Проверка на даты в формате ДД.ММ
                var dateMatch = Regex.Match(line, @"^\d{2}\.\d{2}");
                if (dateMatch.Success)
                {
                    var dateStr = dateMatch.Groups[0].Value.Trim();
                    var dates = new List<DateOnly>();
                    foreach (var datePart in dateStr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (DateOnly.TryParseExact(datePart, "dd.MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                        {
                            dates.Add(parsedDate);
                        }
                    }
                    // Продолжаем парсинг остальной части строки
                }

                // Основной парсинг строки предмета
                // Пример: "➤ 08.09 22.09 06.10 20.10 03.11 17.11 01.12 15.12 ⌛15:10 л.р. Введение в теорию принятия решений 335 7 зд."
                // Пример: "➤ чет ⌛09:40 пр Безопасность жизнедеятельности 217 1 зд."
                var mainMatch = Regex.Match(line, @"➤\s*(.*?)\s*[^\d]*(\d{2}:\d{2})\s*(\S+)\s+(.+?)\s+(\S+?)\s+(\d+)\s+зд\.$");
                if (mainMatch.Success)
                {
                    var prefix = mainMatch.Groups[1].Value.Trim();
                    var timeStr = mainMatch.Groups[2].Value;
                    var type = mainMatch.Groups[3].Value;
                    var subjectName = mainMatch.Groups[4].Value;
                    var room = mainMatch.Groups[5].Value;
                    var building = mainMatch.Groups[6].Value;

                    if (!TimeOnly.TryParseExact(timeStr, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
                    {
                        _logger.LogWarning("Не удалось распарсить время: {TimeStr} в строке: {Line}", timeStr, line);
                        continue;
                    }

                    var newItem = new ParsedScheduleItem
                    {
                        DayOfWeek = currentDay,
                        StartTime = startTime,
                        Type = type,
                        SubjectName = subjectName,
                        Room = room,
                        Building = string.IsNullOrEmpty(building) ? null : int.Parse(building),
                        // Определение WeekType и Subgroup из префикса
                        WeekType = currentWeekType,
                        Subgroup = null,
                        Date = null,
                        Dates = new List<DateOnly>()
                    };

                    // Парсинг префикса
                    var prefixParts = prefix.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in prefixParts)
                    {
                        if (part == "чет")
                        {
                            newItem.WeekType = WeekType.Even;
                        }
                        else if (part == "неч")
                        {
                            newItem.WeekType = WeekType.Odd;
                        }
                        else if (part == "1")
                        {
                            newItem.Subgroup = Subgroup.First;
                        }
                        else if (part == "2")
                        {
                            newItem.Subgroup = Subgroup.Second;
                        }
                        else if (DateOnly.TryParseExact(part, "dd.MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var specificDate))
                        {
                            newItem.Dates.Add(specificDate);
                        }
                    }

                    // Если есть конкретные даты, WeekType и DayOfWeek не используются
                    if (newItem.Dates.Any())
                    {
                        newItem.WeekType = null;
                        newItem.DayOfWeek = null;
                    }
                    // Если WeekType определена из префикса, DayOfWeek используется
                    // Если нет, но DayOfWeek определен, WeekType может быть null (означает обе недели)

                    items.Add(newItem);
                }
            }

            if(items.Count == 0)
                return result.WithError($"Не удалось распарсить расписание. Не нашлось ни одной строки.");

            return result.WithData(items);
        }
    }
}
