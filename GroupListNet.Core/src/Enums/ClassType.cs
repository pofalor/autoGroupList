using System.ComponentModel;

namespace GroupListNet.Core.src.Enums
{
    public enum ClassType
    {
        [Description("Неизвестный тип занятия")]
        Unknown = 0,

        [Description("Лабораторная работа")]
        Lab = 1,

        [Description("Практическая работа")]
        Practice = 2,

        [Description("Лекция")]
        Lecture = 3
    }
}