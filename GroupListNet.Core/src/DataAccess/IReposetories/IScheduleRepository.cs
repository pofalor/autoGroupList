using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;

namespace GroupListNet.Core.src.DataAccess.IReposetories
{
    public interface IScheduleRepository : IRepository<Schedule>
    {
        Task<IEnumerable<Schedule>> GetScheduleForStudentAsync(Subgroup? subgroup, WeekType? weekType, DayOfWeek dayOfWeek);
        Task<IEnumerable<Schedule>> GetTodayScheduleAsync(Subgroup? subgroup, WeekType weekType, DayOfWeek dayOfWeek);

        Task<IEnumerable<Schedule>> GetStartedClassesAsync(WeekType weekType, DayOfWeek dayOfWeek, TimeSpan startTime);

        Task<IEnumerable<int>> DeleteAllAsync();

        Task<Schedule?> GetWithSubjectAsync(int id);
    }
}
