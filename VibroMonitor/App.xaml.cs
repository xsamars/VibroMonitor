using System;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using VibroMonitor.Services;
using VibroMonitor.Views;
using VibroMonitor.ViewModels;
using VibroMonitor.Data;
using Microsoft.EntityFrameworkCore;

namespace VibroMonitor
{
    public partial class App : Application
    {
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<AppDbContext>(opts =>
                        opts.UseNpgsql(context.Configuration.GetConnectionString("Default")));

                    services.AddSingleton<MqttService>();

                    services.AddTransient<MainWindow>();
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<EquipmentDetailsViewModel>();
                })
                .Build();

            _host.Start();

            var mw = _host.Services.GetRequiredService<MainWindow>();
            mw.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }
    }
}
