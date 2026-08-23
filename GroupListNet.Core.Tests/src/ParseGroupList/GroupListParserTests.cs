using GroupListNet.Core.src.DataResult;
using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.ParseGroupList;
using Microsoft.Extensions.Logging.Abstractions;

namespace GroupListNet.Core.Tests.src.ParseGroupList
{
    public class GroupListParserTests
    {
        #region Нумерация

        [Theory]
        [InlineData("1. Иванов Иван Иванович")]
        [InlineData("1) Иванов Иван Иванович")]
        [InlineData("1 - Иванов Иван Иванович")]
        [InlineData("1: Иванов Иван Иванович")]
        [InlineData("1 Иванов Иван Иванович")]
        [InlineData("1.Иванов Иван Иванович")]
        public void ParseGroupListText_NumberSeparatorVariants_ParsesStudent(string line)
        {
            var students = ParseSuccessfully(line);

            Assert.Equal(["1|Иванов|Иван|Иванович"], students);
        }

        [Fact]
        public void ParseGroupListText_UnorderedNumbers_SortsByNumber()
        {
            var students = ParseSuccessfully("""
                3. Петров Пётр Петрович
                1. Иванов Иван Иванович
                """);

            Assert.Equal(["1|Иванов|Иван|Иванович", "3|Петров|Пётр|Петрович"], students);
        }

        [Fact]
        public void ParseGroupListText_ListWithoutNumbers_NumbersLinesSequentially()
        {
            var students = ParseSuccessfully("""
                Петров Пётр Петрович
                Иванов Иван Иванович
                """);

            Assert.Equal(["1|Петров|Пётр|Петрович", "2|Иванов|Иван|Иванович"], students);
        }

        [Fact]
        public void ParseGroupListText_PartiallyNumberedList_ReturnsError()
        {
            var errors = ParseWithErrors("""
                1. Иванов Иван Иванович
                Петров Пётр Петрович
                """);

            Assert.Contains("Пронумеруйте все строки", Assert.Single(errors));
        }

        [Fact]
        public void ParseGroupListText_DuplicatedNumber_ReturnsError()
        {
            var errors = ParseWithErrors("""
                1. Иванов Иван Иванович
                1. Петров Пётр Петрович
                """);

            Assert.Contains("Номер 1 повторяется", Assert.Single(errors));
        }

        [Fact]
        public void ParseGroupListText_ZeroNumber_ReturnsError()
        {
            var errors = ParseWithErrors("0. Иванов Иван Иванович");

            Assert.Contains("Неверный номер студента", Assert.Single(errors));
        }

        #endregion

        #region ФИО

        [Fact]
        public void ParseGroupListText_NameWithoutFatherName_LeavesFatherNameEmpty()
        {
            var students = ParseSuccessfully("""
                1. Иванов Иван
                2. Ли Вэй
                """);

            Assert.Equal(["1|Иванов|Иван|", "2|Ли|Вэй|"], students);
        }

        [Theory]
        [InlineData("1. Иванов - Петров Иван Иванович")]
        [InlineData("1. Иванов-Петров Иван Иванович")]
        public void ParseGroupListText_DoubleSurname_JoinsPartsWithHyphen(string line)
        {
            var students = ParseSuccessfully(line);

            Assert.Equal(["1|Иванов-Петров|Иван|Иванович"], students);
        }

        [Theory]
        [InlineData("1. Иванов И.И.")]
        [InlineData("1. Иванов И. И.")]
        public void ParseGroupListText_Initials_SplitsThemIntoNameAndFatherName(string line)
        {
            var students = ParseSuccessfully(line);

            Assert.Equal(["1|Иванов|И.|И."], students);
        }

        [Fact]
        public void ParseGroupListText_CompoundFatherNameAndApostrophe_ParsesStudents()
        {
            var students = ParseSuccessfully("""
                1. Гасанов Гасан Гасан оглы
                2. О'Нил Джон Джонович
                """);

            Assert.Equal(["1|Гасанов|Гасан|Гасан оглы", "2|О'Нил|Джон|Джонович"], students);
        }

        [Fact]
        public void ParseGroupListText_TrailingDotInFullName_RemovesDot()
        {
            var students = ParseSuccessfully("1. Иванов Иван Иванович.");

            Assert.Equal(["1|Иванов|Иван|Иванович"], students);
        }

        [Fact]
        public void ParseGroupListText_TooManyNameParts_ReturnsError()
        {
            var errors = ParseWithErrors("1. Иванов Иван Иванович Иванов Иван Иванович");

            Assert.Contains("не похожа на ФИО", Assert.Single(errors));
        }

        #endregion

        #region Мусор при копировании

        [Fact]
        public void ParseGroupListText_TabsAndInvisibleCharacters_ParsesStudents()
        {
            // Табы, неразрывный пробел, двойные пробелы и символ нулевой ширины
            var students = ParseSuccessfully("1.\tИванов Иван  Иванович\u200B\n2.  Петров\tПётр\tПетрович");

            Assert.Equal(["1|Иванов|Иван|Иванович", "2|Петров|Пётр|Петрович"], students);
        }

        [Fact]
        public void ParseGroupListText_BulletsAndSeparatorLines_ParsesStudents()
        {
            var students = ParseSuccessfully("""
                • Иванов Иван Иванович
                ---
                - Петров Пётр Петрович
                """);

            Assert.Equal(["1|Иванов|Иван|Иванович", "2|Петров|Пётр|Петрович"], students);
        }

        [Fact]
        public void ParseGroupListText_HeaderLine_SkipsIt()
        {
            var students = ParseSuccessfully("""
                Список группы 4212
                1. Иванов Иван Иванович
                2. Петров Пётр Петрович
                """);

            Assert.Equal(["1|Иванов|Иван|Иванович", "2|Петров|Пётр|Петрович"], students);
        }

        [Fact]
        public void ParseGroupListText_SurnameLookingLikeHeader_KeepsStudent()
        {
            var students = ParseSuccessfully("""
                1. Списков Иван Иванович
                2. Группа Пётр Петрович
                """);

            Assert.Equal(["1|Списков|Иван|Иванович", "2|Группа|Пётр|Петрович"], students);
        }

        #endregion

        #region Явный мусор

        [Theory]
        [InlineData("1. Иванов Иван Иванович\n2. пара 404 ауд")]
        [InlineData("1. Иванов")]
        public void ParseGroupListText_LineIsNotAFullName_ReturnsError(string text)
        {
            var errors = ParseWithErrors(text);

            Assert.Contains("не похожа на ФИО", Assert.Single(errors));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseGroupListText_EmptyText_ReturnsError(string text)
        {
            var errors = ParseWithErrors(text);

            Assert.Contains("Список группы пуст", Assert.Single(errors));
        }

        [Fact]
        public void ParseGroupListText_TextWithoutStudents_ReturnsError()
        {
            var errors = ParseWithErrors("""
                •
                ---
                """);

            Assert.Contains("не нашлось ни одной строки со студентом", Assert.Single(errors));
        }

        [Fact]
        public void ParseGroupListText_TooManyBrokenLines_ReportsFirstErrorsAndTotal()
        {
            var text = string.Join(Environment.NewLine, Enumerable.Range(1, 8).Select(number => $"{number}. строка{number} 404 ауд"));

            var errors = ParseWithErrors(text);

            Assert.Equal(6, errors.Length);
            Assert.All(errors.Take(5), error => Assert.Contains("не похожа на ФИО", error));
            Assert.Equal("Всего строк с ошибками: 8.", errors[^1]);
        }

        #endregion

        private static string[] ParseSuccessfully(string text)
        {
            var result = Parse(text);

            Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));

            return result.Data.Select(FormatStudent).ToArray();
        }

        private static string[] ParseWithErrors(string text)
        {
            var result = Parse(text);

            Assert.False(result.Success, "Ожидалась ошибка разбора, но список разобрался.");

            return result.Errors.Select(error => error.Message).ToArray();
        }

        private static IDataResult<List<Student>> Parse(string text)
        {
            return GroupListParser.ParseGroupListText(text, NullLogger.Instance);
        }

        private static string FormatStudent(Student student)
        {
            return $"{student.NumberInGroup}|{student.SurName}|{student.Name}|{student.FatherName}";
        }
    }
}
