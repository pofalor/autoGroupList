using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.Extensions
{
    public static class ClassTypeParser
    {
        public static ClassType GetClassType(string classType)
        {
            
            return classType switch
            {
                "лек" => ClassType.Lecture,
                "пр" => ClassType.Practice,
                "л.р." => ClassType.Lab,
                _ => ClassType.Unknown,
            };
        }

        public static string GetClassTypeStr(ClassType classType)
        {

            return classType switch
            {
                ClassType.Lecture => "лек",
                ClassType.Practice => "пр",
                ClassType.Lab => "л.р.",
                _ => string.Empty,
            };
        }
    }
}
