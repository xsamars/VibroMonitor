using MQTTnet;
using System.Text;

namespace VibroMonitor.Services;

public class MqttService
{
    private readonly IMqttClient _client;

    public event Action<string, string>? MessageReceived;

    public MqttService()
    {
        var factory = new MqttClientFactory();

        _client = factory.CreateMqttClient();

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
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("192.168.1.96", 1883)
            .Build();

        if(!_client.IsConnected)
            await _client.ConnectAsync(options);
    }

    public async Task Subscribe(string topic)
    {
        await _client.SubscribeAsync(topic);
    }
}