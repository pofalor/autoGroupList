using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using GroupListNet.Core.src.DataAccess;

namespace GroupListNet.Web.Api.Ef
{
    // ReSharper disable once UnusedMember.Global
    public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        /// <inheritdoc />
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // получаем строку подключения из файла appsettings.json
            string connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Port=5434;Database=autogrouplist;Username=postgres;Password=admin";

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseNpgsql(connectionString, opts => opts.CommandTimeout((int)TimeSpan.FromMinutes(10).TotalSeconds));
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
