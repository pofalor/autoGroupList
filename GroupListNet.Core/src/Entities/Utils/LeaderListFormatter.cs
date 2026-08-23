using GroupListNet.Core.Extensions;
using System.Text;

namespace GroupListNet.Core.src.Entities.Utils
{
    public static class LeaderListFormatter
    {
        public static string FormatGroupList(IEnumerable<Attendance> attendanceData, DateOnly today)
        {
            string messageToLeader;
            if (!attendanceData.Any())
            {
                messageToLeader = $"За {today:dd.MM.yyyy} никто не отметился.";
            }
            else
            {
                var attendanceList = new StringBuilder();
                attendanceList.AppendLine($"Список отметившихся студентов за {today:dd.MM.yyyy}:{Environment.NewLine}");

                var groupedBySubject = attendanceData
                    .GroupBy(a => new { a.Schedule.Subject.Name, a.Schedule.ClassType })
                    .ToDictionary(x => x.Key, y => y.DistinctBy(x => x.StudentId).ToArray());

                foreach (var group in groupedBySubject)
                {
                    attendanceList.AppendLine($"--- {ClassTypeParser.GetClassTypeStr(group.Key.ClassType)} {group.Key.Name} ---");
                    int number = 1;
                    foreach (var attendance in group.Value)
                    {
                        attendanceList.AppendLine($"{number}. {attendance.Student.GetFullName()}");
                        number++;
                    }
                    attendanceList.AppendLine();
                }

                messageToLeader = attendanceList.ToString();
            }
            return messageToLeader;
        }
    }
}
