using IPOCS;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ipocs.jmri
{
    class Program
    {
        IMqttClient broker;
        IMqttClientOptions options;

        ConcurrentDictionary<string, Tuple<int, string>> url_to_obj =
            new ConcurrentDictionary<string, Tuple<int, string>>();
        ConcurrentDictionary<Tuple<int, string>, string> obj_to_url =
            new ConcurrentDictionary<Tuple<int, string>, string>();
        ConcurrentDictionary<int, IPOCS.Client> unitid_to_client =
            new ConcurrentDictionary<int, IPOCS.Client>();

        Program() {
            var factory = new MqttFactory();
            broker = factory.CreateMqttClient();
            options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer("localhost")
                .Build();
        }

        void LoadConfigData(String filename) {
            obj_to_url.Clear();
            url_to_obj.Clear();
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
                                if (node3.Name == "Name") {
                                    if (obj != null) Fail("multiple Name");
                                    obj = node3.InnerText;
                                } else if (node3.Name == "SystemName") {
                                    if (url != null) Fail("multiple SystemName");
                                    if (!node3.InnerText.StartsWith("MT"))
                                        Fail("Expect SystemName to start with MT");
                                    url = "/trains/track/turnout/" + node3.InnerText.Substring(2);
                                }
                            }
                            if (obj == null) Fail("no Name");
                            if (url == null) Fail("no SystemName");
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
            //Console.WriteLine(System.Environment.StackTrace);
            System.Environment.Exit(1);
        }

        void Subscribe() {
            //todo: callback for each topic
            broker.Connected += async (s, e) => {
                await broker.SubscribeAsync(
                    new TopicFilterBuilder().WithTopic("#").Build());
            };
            broker.ConnectAsync(options);
            broker.ApplicationMessageReceived += (sender, e) => {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                if (payload !=  "THROWN" && payload !=  "CLOSED") {
                    Console.WriteLine("SKIPPING: " + topic + ": " + payload);
                    return;
                }
                Console.WriteLine("SENDING: " + topic + ": " + payload);
                if (url_to_obj.ContainsKey(topic)) {
                    var obj = url_to_obj[topic];
                    string unitid = obj.Item1.ToString();
                    string output = obj.Item2;
                    if (!unitid_to_client.ContainsKey(obj.Item1)) {
                        Console.WriteLine("Not conenctd to " + obj.Item1);
                        return;
                    }
                    var cl = unitid_to_client[obj.Item1];
                    var msg = new IPOCS.Protocol.Message();
                    msg.RXID_OBJECT = obj.Item2;
                    var pkg = new IPOCS.Protocol.Packets.Orders.ThrowPoints();
                    if (payload ==  "THROWN") { // todo: verify left/right
                        pkg.RQ_POINTS_COMMAND = IPOCS.Protocol.Packets.Orders.RQ_POINTS_COMMAND.DIVERT_LEFT;
                    } else {
                        pkg.RQ_POINTS_COMMAND = IPOCS.Protocol.Packets.Orders.RQ_POINTS_COMMAND.DIVERT_RIGHT;
                    }
                    Console.WriteLine("sending " + unitid + ":" + output + " order");
                    msg.packets.Add(pkg);
                    cl.Send(msg);
                }
            };
        }

        void StartListening() {
            Networker.Instance.OnConnect += (c) => {
                Console.WriteLine(c.UnitID + " connected");
                if (unitid_to_client.ContainsKey(c.UnitID))
                    unitid_to_client[c.UnitID]?.Disconnect();
                unitid_to_client[c.UnitID] = c;

                c.OnMessage += (m) => {
                    //todo
                    /*
                    var topic = obj_to_url[new Tuple<int, string>(
                        c.UnitID, "AaCV66")]; //FIXME
                    var order = "CLOSED"; //FIXME

                    broker.Connected += async (s, e) => {
                        var message = new MqttApplicationMessageBuilder()
                            .WithTopic(topic)
                            .WithPayload(order)
                            .Build();
                        await broker.PublishAsync(message);
                    };
                    */
                };
            };

            Networker.Instance.OnConnectionRequest += (c, r) => {
                return true;
            };


            Networker.Instance.OnDisconnect += (c) => {
                Console.WriteLine(c.UnitID + " disconnected");
                if (c == unitid_to_client[c.UnitID])
                    unitid_to_client[c.UnitID] = null;
            };
            Networker.Instance.OnListening += (isListening) => {
                Console.WriteLine(isListening ? "listening" : "not listening");
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
