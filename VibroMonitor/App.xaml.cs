using System;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using VibroMonitor.Services;
using VibroMonitor.Views;
using VibroMonitor.ViewModels;
using VibroMonitor.Data;
using Microsoft.EntityFrameworkCore;

namespace VibroMonitor
{
    public partial class App : System.Windows.Application
    {
        private IHost? _host;
        public static IServiceProvider? Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("Logs/vibromonitor.log", 
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                })
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<AppDbContext>(opts =>
                        opts.UseNpgsql(hostContext.Configuration.GetConnectionString("Default")));

                    services.AddSingleton(hostContext.Configuration);
                    services.AddSingleton<MqttService>();
                    services.AddSingleton<VibroMonitor.Services.AdminService>();

                    services.AddTransient<MainWindow>();
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<EquipmentDetailsViewModel>();
                    services.AddTransient<Views.PasswordPromptWindow>();
                    services.AddTransient<Views.ChangePasswordWindow>();
                    services.AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.ClearProviders();
                        loggingBuilder.AddSerilog(Log.Logger);
                    });
                })
                .Build();

            _host.Start();
            Services = _host.Services;
            var mw = Services.GetRequiredService<MainWindow>();
            mw.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
