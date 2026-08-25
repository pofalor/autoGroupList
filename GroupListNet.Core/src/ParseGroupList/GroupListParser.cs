using GroupListNet.Core.src.DataResult;
using GroupListNet.Core.src.Entities;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace GroupListNet.Core.src.ParseGroupList
{
    public static partial class GroupListParser
    {
        /// <summary>
        /// Максимальное количество ошибок разбора, которое показываем старосте за один раз
        /// </summary>
        private const int MaxReportedErrors = 5;

        /// <summary>
        /// Максимальное количество слов в ФИО (фамилия, имя и составное отчество)
        /// </summary>
        private const int MaxFullNameParts = 5;

        public static IDataResult<List<Student>> ParseGroupListText(string text, ILogger logger)
        {
            var result = new DataResult<List<Student>>();

            if (string.IsNullOrWhiteSpace(text))
                return result.WithError("Список группы пуст.");

            var errors = new List<string>();
            var parsedLines = new List<ParsedStudentLine>();

            foreach (var sourceLine in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var line = NormalizeLine(sourceLine);

                // Пустые строки и разделители студентами не считаем
                if (line.Length == 0)
                    continue;

                int? number = null;
                var fullName = line;

                var numberMatch = NumberedLineRegex().Match(line);
                if (numberMatch.Success)
                {
                    if (!int.TryParse(numberMatch.Groups["number"].Value, out var parsedNumber) || parsedNumber <= 0)
                    {
                        errors.Add($"Неверный номер студента в строке: \"{sourceLine}\".");
                        continue;
                    }

                    number = parsedNumber;
                    fullName = numberMatch.Groups["name"].Value;
                }

                var fullNameParts = SplitFullName(fullName);
                if (fullNameParts.Length < 2 || fullNameParts.Length > MaxFullNameParts || !fullNameParts.All(IsNamePart))
                {
                    // Заголовок вида "Список группы 4212" пропускаем молча, но только если он не разобрался как ФИО
                    if (HeaderLineRegex().IsMatch(line))
                    {
                        logger.LogDebug("Строка списка группы пропущена как служебная: {Line}", sourceLine);
                        continue;
                    }

                    logger.LogWarning("Не удалось распарсить строку списка группы: {Line}", sourceLine);
                    errors.Add($"Строка не похожа на ФИО: \"{sourceLine}\".");
                    continue;
                }

                parsedLines.Add(new ParsedStudentLine(number, fullNameParts));
            }

            if (errors.Count != 0)
                return AddErrors(result, errors);

            if (parsedLines.Count == 0)
                return result.WithError("В сообщении не нашлось ни одной строки со студентом.");

            // Нумерация либо есть у всех строк, либо её нет ни у кого — иначе непонятно, что делать с номерами
            var numberedLinesCount = parsedLines.Count(parsedLine => parsedLine.Number.HasValue);
            if (numberedLinesCount != 0 && numberedLinesCount != parsedLines.Count)
                return result.WithError("Часть строк пронумерована, а часть — нет. Пронумеруйте все строки или уберите нумерацию полностью.");

            var students = new List<Student>();
            var usedNumbers = new HashSet<int>();
            var autoNumber = 0;

            foreach (var parsedLine in parsedLines)
            {
                // Если список прислали без нумерации, проставляем номера по порядку строк
                var number = parsedLine.Number ?? ++autoNumber;
                if (!usedNumbers.Add(number))
                {
                    errors.Add($"Номер {number} повторяется в списке группы.");
                    continue;
                }

                students.Add(new Student
                {
                    NumberInGroup = number,
                    SurName = parsedLine.FullNameParts[0],
                    Name = parsedLine.FullNameParts[1],
                    FatherName = string.Join(' ', parsedLine.FullNameParts.Skip(2)),
                    TelegramId = null
                });
            }

            if (errors.Count != 0)
                return AddErrors(result, errors);

            return result.WithData(students.OrderBy(student => student.NumberInGroup).ToList());
        }

        /// <summary>
        /// Приводит строку к единому виду: убирает невидимые символы, маркеры списка и лишние пробелы
        /// </summary>
        private static string NormalizeLine(string line)
        {
            var normalized = InvisibleCharsRegex().Replace(line, string.Empty);
            normalized = WhitespaceRegex().Replace(normalized, " ").Trim();

            return BulletPrefixRegex().Replace(normalized, string.Empty).Trim();
        }

        /// <summary>
        /// Разбивает ФИО на части, склеивая двойные фамилии и разделяя слитно написанные инициалы
        /// </summary>
        private static string[] SplitFullName(string fullName)
        {
            var normalized = HyphenInsideNameRegex().Replace(fullName, "-");
            normalized = InitialsRegex().Replace(normalized, ". ");

            return normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(TrimTrailingDot)
                .ToArray();
        }

        /// <summary>
        /// У инициала точка значимая ("И."), у полного слова — лишняя ("Иванович.")
        /// </summary>
        private static string TrimTrailingDot(string namePart)
        {
            return namePart.Length > 2 && namePart.EndsWith('.')
                ? namePart.TrimEnd('.')
                : namePart;
        }

        private static bool IsNamePart(string namePart)
        {
            return NamePartRegex().IsMatch(namePart);
        }

        private static IDataResult<List<Student>> AddErrors(DataResult<List<Student>> result, List<string> errors)
        {
            foreach (var error in errors.Take(MaxReportedErrors))
            {
                result.WithError(error);
            }

            if (errors.Count > MaxReportedErrors)
                result.WithError($"Всего строк с ошибками: {errors.Count}.");

            return result;
        }

        /// <summary>
        /// Строка студента: номер (если староста его проставил) и разобранное на части ФИО
        /// </summary>
        private sealed record ParsedStudentLine(int? Number, string[] FullNameParts);

        /// <summary>
        /// Номер студента с разделителем ("1.", "1)", "1 -", "1:") или просто через пробел ("1 Иванов")
        /// </summary>
        [GeneratedRegex(@"^(?<number>\d{1,4})(?:\s*[.)\-:–—]\s*|\s+)(?<name>.+)$")]
        private static partial Regex NumberedLineRegex();

        /// <summary>
        /// Служебные строки, которые староста мог скопировать вместе со списком
        /// </summary>
        [GeneratedRegex(@"^(список|группа|группы|состав|студенты|фио)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex HeaderLineRegex();

        /// <summary>
        /// Часть ФИО: слово из букв, возможно с дефисом или апострофом, либо инициал ("И.")
        /// </summary>
        [GeneratedRegex(@"^\p{L}[\p{L}'’\-]*\.?$")]
        private static partial Regex NamePartRegex();

        /// <summary>
        /// Невидимые символы (нулевой ширины, метки направления текста, BOM), приезжающие при копировании
        /// </summary>
        [GeneratedRegex(@"[\u200B-\u200F\u202A-\u202E\uFEFF]")]
        private static partial Regex InvisibleCharsRegex();

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        /// <summary>
        /// Маркеры списка в начале строки
        /// </summary>
        [GeneratedRegex(@"^[•·*‣▪\-–—]+\s*")]
        private static partial Regex BulletPrefixRegex();

        /// <summary>
        /// Дефис двойной фамилии, вокруг которого поставили пробелы ("Иванов - Петров")
        /// </summary>
        [GeneratedRegex(@"(?<=\p{L})\s*[\-–—]\s*(?=\p{L})")]
        private static partial Regex HyphenInsideNameRegex();

        /// <summary>
        /// Точка между слитно написанными инициалами ("И.И.")
        /// </summary>
        [GeneratedRegex(@"(?<=\p{L})\.(?=\p{L})")]
        private static partial Regex InitialsRegex();
    }
}
