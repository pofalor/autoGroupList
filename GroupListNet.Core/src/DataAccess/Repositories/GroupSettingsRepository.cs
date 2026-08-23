using GroupListNet.Core.src.DataAccess.IReposetories;
using GroupListNet.Core.src.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroupListNet.Core.src.DataAccess.BaseClasses
{
    public class GroupSettingsRepository(ApplicationDbContext context) : Repository<GroupSettings>(context), IGroupSettingsRepository
    {
        public async Task<GroupSettings> GetOrCreateAsync()
        {
            var settings = await _dbSet.Where(s => !s.IsDeleted)
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync();

            if (settings != null)
                return settings;

            // По умолчанию считаем, что группа делится на подгруппы — так бот работал до появления настройки
            settings = new GroupSettings { HasSubgroups = true };
            await _dbSet.AddAsync(settings);
            await _context.SaveChangesAsync();

            return settings;
        }

        public async Task<bool> HasSubgroupsAsync()
        {
            return (await GetOrCreateAsync()).HasSubgroups;
        }

        public async Task SetHasSubgroupsAsync(bool hasSubgroups)
        {
            var settings = await GetOrCreateAsync();
            if (settings.HasSubgroups == hasSubgroups)
                return;

            settings.HasSubgroups = hasSubgroups;
            await _context.SaveChangesAsync();
        }
    }
}
