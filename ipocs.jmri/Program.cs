using System;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using IPOCS;

namespace ipocs.jmri
{
    class Program
    {
        IMqttClient broker;
        IMqttClientOptions options;

        Dictionary<string, Tuple<int, string>> url_to_obj;
        Dictionary<Tuple<int, string>, string> obj_to_url;
        Dictionary<int, Client> unitid_to_client;

        Program() {
            var factory = new MqttFactory();
            broker = factory.CreateMqttClient();
            options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer("localhost")
                .Build();
        }

        void LoadConfigData(String filename) {
            obj_to_url = new Dictionary<Tuple<int, string>, string>();
            url_to_obj = new Dictionary<string, Tuple<int, string>>();
            var doc = new XmlDocument();
            doc.Load(filename);
            foreach (XmlNode concentrator in doc.DocumentElement.ChildNodes) {
                String unitid = null;
                foreach (XmlNode node in concentrator.ChildNodes) {
                    if (node.Name == "UnitID") {
                        if (unitid != null) Fail("multiple UnitID");
                        unitid = node.InnerText;
                    }
                }
                if (unitid == null) Fail("no UnitID");
                foreach (XmlNode node in concentrator.ChildNodes) {
                    if (node.Name == "Objects") {
                        foreach (XmlNode node2 in node.ChildNodes) {
                            String url = null;
                            String obj = null;
                            foreach (XmlNode node3 in node2.ChildNodes) {
                                if (node3.Name == "outputPin") {
                                    if (obj != null) Fail("multiple outputPin");
                                    obj = node3.InnerText;
                                } else if (node3.Name == "mqttUrl") {
                                    if (url != null) Fail("multiple mqtttUrl");
                                    url = node3.InnerText;
                                }
                            }
                            if (obj == null) Fail("no OutputPin");
                            if (url == null) Fail("no mqttUrl");
                            int id = Int32.Parse(unitid);
                            var tup  = new Tuple<int, string>(id, obj);
                            if (url_to_obj.ContainsKey(url)) Fail("dup url " + url);
                            if (obj_to_url.ContainsKey(tup)) Fail("dup obj " + tup);
                            url_to_obj[url] = tup;
                            obj_to_url[tup] = obj;
                        }
                    }
                }
            }
        }

        void Fail(String msg) {
            Console.WriteLine(msg);
            Console.WriteLine(System.Environment.StackTrace);
            System.Environment.Exit(1);
        }

        void Subscribe() {
            broker.Connected += async (s, e) => {
                await broker.SubscribeAsync(new TopicFilterBuilder().WithTopic("#").Build());
            };
            broker.ConnectAsync(options);
            broker.ApplicationMessageReceived += (sender, e) => {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Console.WriteLine("" + topic + ": " + payload);
                string order = "unknonw";
                if (payload ==  "THROWN") {
                    order = "throw";
                } else if (payload ==  "CLOSED") {
                    order =  "close";
                }
                if (url_to_obj.ContainsKey(topic)) {
                    var obj = url_to_obj[topic];
                    string unitid = obj.Item1.ToString();
                    string output = obj.Item2.ToString();
                    Console.WriteLine("sending " + unitid + ":" + output + " order " + order);
                    /*
                    var responseMsg = new IPOCS.Protocol.Message();
                    responseMsg.RXID_OBJECT = unitid;
                    responseMsg.packets.Add(new IPOCS.Protocol.Packets.ConnectionResponse
                    {
                        RM_PROTOCOL_VERSION = pkt.RM_PROTOCOL_VERSION
                    });
                    Client.Instance.Send(responseMsg);
                    */
                } else {
                    Console.WriteLine("out of control " + topic);
                }
            };
        }

        void StartListening() {
            Networker.Instance.OnConnect += (c) => {
                Console.WriteLine(c.UnitID + " connected");
                unitid_to_client[c.UnitID] = c;
            };
            Networker.Instance.OnConnectionRequest += (c, r) => {
                Console.WriteLine(c.UnitID + " request");
                string order = "UNKNOWN";
                /*
                if (r.RL_PACKET == ???) {
                    order = "THROWN";
                } else if (r.RL_PACKET == ???) {
                    order =  "CLOSED";
                }
                */
                Console.WriteLine("RL: " + r.RL_PACKET.ToString());
                /*
                var ob = r.RL_PACKET ???
                var tup  = new Tuple<int, string>(c.UnitID, ob);
                var topic = obj_to_url[tup];
                */
                var topic = obj_to_url[new Tuple<int, string>(1, "10")];

                broker.Connected += async (s, e) => {
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(order)
                        .Build();
                    await broker.PublishAsync(message);
                };
                return true;
            };
            Networker.Instance.OnDisconnect += (c) => {
                Console.WriteLine(c.UnitID + " disconnected");
                unitid_to_client.Remove(c.UnitID);
            };
            Networker.Instance.OnListening += (isListening) => {
                Console.WriteLine("on listening " + isListening + "\n");
            };
            Networker.Instance.isListening = true;
        }

        static void Main(string[] args)
        {
            string home = Environment.GetEnvironmentVariable("HOME");
            string file =  home + "/configdata.xml";
            if (args.Length == 1)
                file = args[0];
            Program p = new Program();
            p.LoadConfigData(file);
            p.Subscribe();
            p.StartListening();
            for (;;) Thread.Sleep(1000);
        }
    }
}
