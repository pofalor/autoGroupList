using GroupListNet.Core.src.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GroupListNet.Core.Tests.src.Bot
{
    /// <summary>
    /// В боевой базе поле Version проставляет сам PostgreSQL, а провайдер в памяти этого не умеет
    /// и падает на обязательном поле. Заполняем его при вставке, чтобы тесты работали с настоящими сущностями
    /// </summary>
    public class RowVersionInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            FillVersions(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            FillVersions(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static void FillVersions(DbContext? context)
        {
            if (context == null)
                return;

            // Меняем только у новых записей: у изменяемых Version — токен параллельного доступа
            foreach (var entry in context.ChangeTracker.Entries<PersistentEntity>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.Version = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
