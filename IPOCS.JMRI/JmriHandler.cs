using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPOCS.JMRI
{
  public class JmriHandler: IMqttClientConnectedHandler, IMqttApplicationMessageReceivedHandler, IMqttClientDisconnectedHandler
  {
    private Options Options { get; }
    private IMqttClient Broker { get; }
    private string ClientID { get; }

    private ConcurrentDictionary<string, string> LastRecv { get; } = new ConcurrentDictionary<string, string>();

    public JmriHandler(Options options)
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
      Log.Information("MQTT: Attempting to connect to server at {@MqttHost}...", Options.MqttHost);
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
      Networker.Instance.isListening = true;
      Log.Information("MQTT: Connected to server.");
      await Broker.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(Options.Channel + "/command/#").Build());
    }

    public delegate void OnMqttMessageDelegate(string topic, string payload);
    public event OnMqttMessageDelegate OnMqttMessage;

    public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
      var topic = eventArgs.ApplicationMessage.Topic;
      var payload = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);
      if (LastRecv.GetValueOrDefault(topic, string.Empty) == payload)
      {
        Log.Warning($"MQTT: Received my own message, ignoring.");
        return Task.CompletedTask;
      }
      LastRecv[topic] = payload;

      Log.Information("MQTT: Received {@payload} on {@topic}", payload, topic);
      OnMqttMessage?.Invoke(topic, payload);
      return Task.CompletedTask;
    }

    public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
    {
      IMqttClientOptions options = new MqttClientOptionsBuilder()
          .WithClientId(ClientID)
          .WithTcpServer(Options.MqttHost)
          .Build();
      Log.Warning("MQTT: Attempting to reconnect to server at {@MqttHost}...", Options.MqttHost);
      Broker.ConnectAsync(options);
      Networker.Instance.isListening = false;
      // TODO: Unsubscribe
      return Task.CompletedTask;
    }
  }
}
