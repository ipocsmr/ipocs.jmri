using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using IPOCS;

namespace ipocs.jmri
{
    class Program
    { 
        //MqttClient broker;
        static void Main(string[] args)
        {
            var factory = new MqttFactory();
            var client = factory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer("localhost")
                .Build();

            client.Connected += async (s, e) => {
                await client.SubscribeAsync(new TopicFilterBuilder().WithTopic("#").Build());
            };
            client.ConnectAsync(options);
            client.ApplicationMessageReceived += (sender, e) => {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                //Console.WriteLine("" + topic + ": " + payload);
                switch (payload) {
                    case "THROWN": break;
                    case "CLOSED": break;
                    default: break;
                }
            };

            client.Connected += async (s, e) => {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("/trains/track/turnout/17")
                    .WithPayload("CLOSED")
                    .Build();
                await client.PublishAsync(message);
            };
            Networker.Instance.OnConnect += (c) => {
                Console.Write("on conn");
            };
            IPOCS.Networker.Instance.OnConnectionRequest += (c, r) => {
                Console.Write("on conn req");
                return true;
            };
            IPOCS.Networker.Instance.OnDisconnect += (c) => {
                Console.Write("on disc");
            };
            IPOCS.Networker.Instance.OnListening += (isListening) => {
                Console.Write("on listening " + isListening + "\n");
            };

            for (;;) Thread.Sleep(1000);
        }
    }
}
