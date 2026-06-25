using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using System.Text;

namespace VibroMonitor.Services;

public class MqttService
{
    private readonly IMqttClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MqttService> _logger;
    private readonly string? _host;
    private readonly int? _port;

    public event Action<string, string>? MessageReceived;

    public MqttService(IConfiguration configuration, ILogger<MqttService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _host = _configuration.GetSection("Mqtt").GetValue<string>("Host");
        _port = _configuration.GetSection("Mqtt").GetValue<int>("Port");

        var factory = new MqttClientFactory();

        _client = factory.CreateMqttClient();
        _logger.LogInformation("MQTT created factory client.");
        _client.ApplicationMessageReceivedAsync += e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            MessageReceived?.Invoke(topic, payload);

            return Task.CompletedTask;
        };
    }

    public async Task Connect()
    {
        _logger.LogInformation($"MQTT connectiong to {_host}:{_port}.");

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .Build();

        if (!_client.IsConnected)
        {
            await _client.ConnectAsync(options);
            _logger.LogInformation($"MQTT connection {(_client.IsConnected ? """established""" : """fail""")}");
        }
    }

    public async Task Subscribe(string topic)
    {
        _logger.LogInformation($"MQTT subscribe to {topic}.");

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .Build();

        if (!_client.IsConnected)
        {
            _logger.LogInformation($"MQTT connectiong (on subscribe) to {_host}:{_port}.");

            await _client.ConnectAsync(options);

            _logger.LogInformation($"MQTT connection {(_client.IsConnected ? """established""" : """fail""")}");
        }

        await _client.SubscribeAsync(topic);
    }
}