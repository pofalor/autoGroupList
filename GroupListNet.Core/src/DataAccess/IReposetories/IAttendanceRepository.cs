using GroupListNet.Core.src.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupListNet.Core.src.DataAccess.IReposetories
{
    public interface IAttendanceRepository : IRepository<Attendance>
    {
        Task MarkAttendanceAsync(int studentId, int scheduleId, DateTime date);
        Task<IEnumerable<Attendance>> GetTodayAttendanceAsync();

        /// <summary>
        /// Отметки за конкретный день вместе с архивными. Нужен только отчёту старосте:
        /// он собирается при отправке, а к этому моменту староста мог перезалить расписание
        /// или список группы, пометив отметки удалёнными
        /// </summary>
        Task<IEnumerable<Attendance>> GetAttendanceForReportAsync(DateOnly date);
        Task<bool> HasAttendanceAsync(int studentId, int scheduleId, DateOnly date);
        Task DeleteAttendanceByScheduleIdsAsync(IEnumerable<int> scheduleIds);
        Task DeleteAttendanceByStudentIdsAsync(IEnumerable<int> studentIds);
    }

}
