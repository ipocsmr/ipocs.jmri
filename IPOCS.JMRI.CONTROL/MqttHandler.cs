using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IPOCS.JMRI.CONTROL
{
  public class MqttHandler: IMqttClientConnectedHandler, IMqttApplicationMessageReceivedHandler, IMqttClientDisconnectedHandler
  {
    private Options Options { get; }
    private IMqttClient Broker { get; }
    private string ClientID { get; }

    private Dictionary<string, string> LastRecv { get; } = new Dictionary<string, string>();

    public MqttHandler(Options options)
    {
      ClientID = Guid.NewGuid().ToString();
      Options = options;
      Broker = new MqttFactory().CreateMqttClient()
        .UseApplicationMessageReceivedHandler(this)
        .UseConnectedHandler(this)
        .UseDisconnectedHandler(this);
    }

    public void Setup()
    {
      IMqttClientOptions options = new MqttClientOptionsBuilder()
          .WithClientId(ClientID)
          .WithTcpServer(Options.MqttHost)
          .Build();
      Console.WriteLine($"MQTT: Attempting to connect to server at {Options.MqttHost}...");
      Broker.ConnectAsync(options);
    }

    public void Send(string topic, string payload)
    {
      var mqttMessage = new MqttApplicationMessageBuilder()
        .WithTopic(topic)
        .WithPayload(payload)
        .Build();
      LastRecv[topic] = payload;
      Broker.PublishAsync(mqttMessage).Wait();
    }

    public async Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
    {
      Console.WriteLine("MQTT: Connected to server.");
      await Broker.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(Options.Channel + "/state/#").Build());
    }

    public delegate void OnMqttMessageDelegate(string systemName, string payload);
    public event OnMqttMessageDelegate OnMqttMessage;

    public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
      var topic = eventArgs.ApplicationMessage.Topic;
      var payload = System.Text.Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);
      if (LastRecv.GetValueOrDefault(topic, string.Empty) == payload)
      {
        Console.WriteLine($"MQTT: Received my own message, ignoring.");
        return Task.CompletedTask;
      }
      LastRecv[topic] = payload;

      Console.WriteLine($"MQTT: Received {payload} on {topic}");
      OnMqttMessage?.Invoke("MT" + topic.Split('/').Last(), payload);
      return Task.CompletedTask;
    }

    public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
    {
      IMqttClientOptions options = new MqttClientOptionsBuilder()
        .WithClientId(ClientID)
        .WithTcpServer(Options.MqttHost)
        .Build();
      Console.WriteLine($"MQTT: Attempting to connect to server at {Options.MqttHost}...");
      Broker.ConnectAsync(options);
      // TODO: Unsubscribe
      return Task.CompletedTask;
    }
  }
}
