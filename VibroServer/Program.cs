using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Internal;
using MQTTnet.Server;
using System.Text;
using System.Globalization;
using System.Buffers;
using Serilog;
using VibroMonitor.Data;
using VibroMonitor.Models;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File("Logs/vibroserver.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting VibroServer...");

    int _mqttPort = new();

    var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((ctx, cfg) =>
        {
            cfg.AddJsonFile("..\\VibroMonitor\\appsettings.json", optional: true);
        })
        .UseSerilog()
        .ConfigureServices((hostContext, services) =>
        {
            var conn = hostContext.Configuration.GetConnectionString("Server");
            _mqttPort = hostContext.Configuration.GetSection("Mqtt").GetValue<int>("Port");
            services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(conn));
        })
        .Build();

    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    // Ensure database is created / migrations applied before starting server
    try
    {
        var dbInit = services.GetRequiredService<AppDbContext>();
        dbInit.Database.Migrate();
        logger.LogInformation("Database initialized / migrations applied.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize database");
        Environment.Exit(1);
    }

    var mqttServerFactory = new MqttServerFactory();
    // Start embedded MQTT broker (MQTTnet 5.0+)
    var mqttServerOptions = new MqttServerOptionsBuilder()
        .WithDefaultEndpoint()
        .WithDefaultEndpointPort(_mqttPort)
        .Build();

    using var mqttServer = mqttServerFactory.CreateMqttServer(mqttServerOptions);
    mqttServer.InterceptingPublishAsync += async args =>
    {
        try
        {
            var appMsg = args.ApplicationMessage;
            if (appMsg == null) return;

            var topic = appMsg.Topic;
            if (string.IsNullOrWhiteSpace(topic)) return;

            var payloadSeq = appMsg.Payload;
            if (payloadSeq.Length == 0) return;

            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
            if (string.IsNullOrWhiteSpace(payload)) return;

            payload = payload.Replace(".", ",");
            if (!double.TryParse(payload, out var value))
                return;

            // Create a scope per message to get a scoped DbContext
            using var msgScope = host.Services.CreateScope();
            var db = msgScope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Find equipment point with matching MQTT topic (include parent equipment)
            var point = await db.EquipmentPoints
                .Include(p => p.EquipmentItem)
                .FirstOrDefaultAsync(p => p.MqttTopic == topic);
            if (point == null)
            {
                // no matching point - nothing to do
                return;
            }

            // Update current value (optional) and evaluate level
            point.Value = value;

            db.PointHistory.Add(new PointHistory
            {
                EquipmentPointId = point.Id,
                Value = value,
                Time = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var level = point.GetAlertLevel();
            // Only act if level is not Normal (no need to create alarms for Normal)
            if (level != AlertLevel.Normal)
            {
                // Check for existing active alarms for this equipment point
                var existing = await db.AlarmItem
                    .Where(a => a.EquipmentPointId == point.Id)
                    .OrderByDescending(a => a.Created)
                    .ToListAsync();

                var active = existing.FirstOrDefault(a => a.Acked == false);

                var shouldCreate = true;

                if (active != null)
                {
                    // If there's an active non-acked alarm with same level - do not create
                    if (active.Level == level)
                    {
                        shouldCreate = false;
                    }
                    else
                    {
                        // Level changed while previous alarm still active -> create new alarm
                        shouldCreate = true;
                    }
                }
                else
                {
                    // No active alarms - create a new one
                    shouldCreate = true;
                }

                if (shouldCreate)
                {
                    var alarmText = level switch
                    {   
                        AlertLevel.HiHi => $"Превышение верхней аварийной уставки (HiHi)",
                        AlertLevel.Hi => $"Превышение верхней уставки (Hi)",
                        AlertLevel.Lo => $"Превышение нижней уставки (Lo)",
                        AlertLevel.LoLo => $"Превышение нижней аварийной (LoLo)",
                        _ => $"Авария в {point.Name}"
                    };

                    var alarm = new AlarmItem
                    {
                        Message = $"{alarmText} {point.Value} (HiHi={point.HiHi}, Hi={point.Hi}, Lo={point.Lo}, LoLo={point.LoLo})",
                        Status = "Активно",
                        Created = DateTime.UtcNow,
                        Acked = false,
                        Sensor = point.EquipmentItem != null ? $"{point.EquipmentItem.Name}.{point.Name}" : point.Name,
                        EquipmentPointId = point.Id,
                        Level = level
                    };

                    db.AlarmItem.Add(alarm);
                    // Save point value update and new alarm
                    await db.SaveChangesAsync();

                    Console.WriteLine($"Created alarm for point '{point.Name}' (topic: {topic}) value={value} level={level}");
                }
                else
                {
                    // Update timestamp maybe or ignore
                    Console.WriteLine($"Skipping alarm creation for point '{point.Name}' (topic: {topic}) value={value} level={level} - active with same level exists");
                }
            }
        }
        catch (Exception ex)
        {
        Console.WriteLine($"Error handling incoming message: {ex}");
        }

        // async lambda returns Task implicitly
    };

    // Create client to subscribe internally and process messages
    //var mqttClientOptions = new MqttClientOptionsBuilder()
    //    .WithTcpServer("localhost", 1883)
    //    .Build();

    await mqttServer.StartAsync();

    //// Load and subscribe to equipment points
    //var db_init = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //var points = db_init.EquipmentPoints.ToList();

    //await mqttClient.ConnectAsync(mqttClientOptions);
    //foreach (var pt in points)
    //{
    //    if (!string.IsNullOrWhiteSpace(pt.MqttTopic))
    //    {
    //        await mqttClient.SubscribeAsync(pt.MqttTopic);
    //        Console.WriteLine($"Subscribed to {pt.MqttTopic}");
    //    }
    //}

    Console.WriteLine($"MQTT broker started on port {_mqttPort}.");

    Console.WriteLine("VibroServer running. Press Ctrl+C to exit.");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unexpected error starting server");
    throw;
}
finally
{
    Log.Information("Stopping server");
    // Ensure to flush and close down Serilog properly on app exit
    Log.CloseAndFlush();
}
