using GroupListNet.Core.src.BackgroundJobs;
using GroupListNet.Core.src.ConfigSectionModels;
using GroupListNet.Core.src.Constants;
using GroupListNet.Core.src.Context;
using GroupListNet.Core.src.DataAccess;
using GroupListNet.Core.src.Installers;
using GroupListNet.Core.src.Services;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using System;
using Telegram.Bot;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    var currentDir = Directory.GetCurrentDirectory();
    var configPath = Path.Combine(currentDir, "config");
    var nlog_path = Path.Combine(configPath, "nlog.config");
    NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(nlog_path);

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString(AppConfigurationConstants.DbConnectionName));
    });

    builder.Services
        .AddAutoMapper(cfg => cfg.ConfigurateAutoMapper())
        .AddCore();


    builder.Host.UseNLog();

    var app = builder.Build();

    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseStatusCodePages();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // Миграции базы данных
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        // Тот, кто разворачивает систему, задан старостой в конфиге — выдаём ему права сразу
        var leaderService = scope.ServiceProvider.GetRequiredService<ILeaderService>();
        await leaderService.SyncMainLeadersAsync();
    }

    await app.RunAsync();
}
catch(Exception ex)
{
    // NLog: catch setup errors
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}