using IPOCS;
using IPOCS.Protocol.Packets.Orders;
using IPOCS.Protocol.Packets.Status;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Diagnostics;

namespace ipocs.jmri
{
    class Program
    {
        IMqttClient broker;
        IMqttClientOptions options;

        ConcurrentDictionary<string, Tuple<int, string>> urlToObj =
            new ConcurrentDictionary<string, Tuple<int, string>>();
        ConcurrentDictionary<string, string> objToUrl =
            new ConcurrentDictionary<string, string>();
        ConcurrentDictionary<int, IPOCS.Client> unitidToClient =
            new ConcurrentDictionary<int, IPOCS.Client>();

        ConcurrentDictionary<string, string> objState =
            new ConcurrentDictionary<string, string>();

        enum PointsStraight { Unknown, Left, Right };
        Dictionary<string, PointsStraight> points =
            new Dictionary<string, PointsStraight>() {
                { "Aa60", PointsStraight.Unknown},
                { "Aa61", PointsStraight.Unknown},
                { "Aa62", PointsStraight.Unknown},
                { "Aa63", PointsStraight.Unknown},
                { "Aa64", PointsStraight.Unknown},
                { "Aa65", PointsStraight.Unknown},
                { "Aa66", PointsStraight.Unknown},
                { "Aa68", PointsStraight.Unknown},
                { "Aa69", PointsStraight.Unknown},
                { "Aa70", PointsStraight.Unknown},
                { "Aa71", PointsStraight.Unknown},
                { "Ba100", PointsStraight.Unknown},
                { "Ba101", PointsStraight.Unknown},
                { "Ba102", PointsStraight.Unknown},
                { "Ba103", PointsStraight.Unknown},
                { "Ba104", PointsStraight.Unknown},
                { "Ba105", PointsStraight.Unknown},
                { "Ba106", PointsStraight.Unknown},
                { "Ba107", PointsStraight.Unknown},
                { "Ba108", PointsStraight.Unknown},
                { "Ba110", PointsStraight.Unknown},
                { "Ba111", PointsStraight.Unknown},
                { "Ba113", PointsStraight.Unknown},
                { "Ba114", PointsStraight.Unknown},
                { "Ba115", PointsStraight.Right},
                { "Ba116", PointsStraight.Unknown},
                { "Ba117", PointsStraight.Right},
                { "Ba118", PointsStraight.Unknown},
                { "Ba119", PointsStraight.Unknown},
                { "Ba120", PointsStraight.Unknown},
                { "Ba121", PointsStraight.Unknown},
                { "Ha60", PointsStraight.Unknown},
                { "Ha61", PointsStraight.Unknown},
                { "Ha62", PointsStraight.Unknown},
                { "Ha63", PointsStraight.Unknown},
                { "Ha64", PointsStraight.Unknown},
                { "Ha65", PointsStraight.Unknown},
                { "Ha66", PointsStraight.Unknown},
                { "Ha67", PointsStraight.Unknown},
                { "Ha68", PointsStraight.Unknown},
                { "Ha69", PointsStraight.Unknown},
                { "Mk60", PointsStraight.Unknown},
                { "Mk61", PointsStraight.Unknown},
                { "Mk62", PointsStraight.Unknown},
                { "Mk63", PointsStraight.Unknown},
                { "Mk64", PointsStraight.Unknown},
                { "Mk65", PointsStraight.Unknown},
                { "Mk66", PointsStraight.Unknown},
                { "Mk67", PointsStraight.Unknown},
                { "Mk68", PointsStraight.Unknown},
                { "Mk69", PointsStraight.Unknown},
                { "Sn60", PointsStraight.Unknown},
                { "Sn61", PointsStraight.Unknown},
                { "Sn62", PointsStraight.Unknown},
                { "Sn63", PointsStraight.Unknown},
                { "Sn64", PointsStraight.Unknown},
                { "Sn65", PointsStraight.Unknown},
                { "Sn66", PointsStraight.Unknown},
                { "Sn67", PointsStraight.Unknown},
                { "Sn69", PointsStraight.Unknown},
                { "Vd60", PointsStraight.Unknown},
                { "Vd61", PointsStraight.Unknown},
                { "Vd62", PointsStraight.Unknown},
                { "Vd63", PointsStraight.Unknown},
                { "Vd64", PointsStraight.Unknown},
                { "Vd65", PointsStraight.Unknown},
                { "Vd66", PointsStraight.Unknown}
            };

        Program() {
            var factory = new MqttFactory();
            broker = factory.CreateMqttClient();
            options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer("localhost")
                .Build();
        }

        void LoadConfigData(String filename) {
            objToUrl.Clear();
            urlToObj.Clear();
            var doc = new XmlDocument();
            doc.Load(filename);
            foreach (XmlNode concentrator in doc.DocumentElement.ChildNodes) {
                String unitid = null;
                foreach (XmlNode node in concentrator.ChildNodes) {
                    if (node.Name == "UnitID") {
                        if (unitid != null) throw new Exception("multiple UnitID");
                        unitid = node.InnerText;
                    }
                }
                if (unitid == null) throw new Exception("no UnitID");
                foreach (XmlNode node in concentrator.ChildNodes) {
                    if (node.Name == "Objects") {
                        foreach (XmlNode node2 in node.ChildNodes) {
                            String url = null;
                            String obj = null;
                            foreach (XmlNode node3 in node2.ChildNodes) {
                                if (node3.Name == "Name") {
                                    if (obj != null) throw new Exception("multiple Name");
                                    obj = node3.InnerText;
                                } else if (node3.Name == "SystemName") {
                                    if (url != null) throw new Exception("multiple SystemName");
                                    if (!node3.InnerText.StartsWith("MT"))
                                        throw new Exception("Expect SystemName to start with MT");
                                    url = "/trains/track/turnout/" + node3.InnerText.Substring(2);
                                }
                            }
                            if (obj == null) throw new Exception("no Name");
                            if (url == null) throw new Exception("no SystemName");
                            int id = Int32.Parse(unitid);
                            var tup  = new Tuple<int, string>(id, obj);
                            if (urlToObj.ContainsKey(url)) throw new Exception("dup url " + url);
                            if (objToUrl.ContainsKey(obj)) throw new Exception("dup obj " + tup);
                            urlToObj[url] = tup;
                            objToUrl[obj] = url;
                        }
                    }
                }
            }
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
                Console.WriteLine("MQTT: Received " + payload + " on " + topic);
                if (payload != "THROWN" && payload != "CLOSED") {
                    Console.WriteLine("MQTT: Skip unexpected message " + payload);
                    return;
                }
                if (!urlToObj.ContainsKey(topic)) {
                    Console.WriteLine("MQTT: Skip unknown points " + topic);
                    return;
                }
                var obj = urlToObj[topic];
                if (!unitidToClient.ContainsKey(obj.Item1)) {
                    Console.WriteLine("MQTT: Skip unconnected OCS("+ obj.Item1 + ")");
                    return;
                }
                var cl = unitidToClient[obj.Item1];
                var msg = new IPOCS.Protocol.Message();
                msg.RXID_OBJECT = obj.Item2;
                var pkg = new IPOCS.Protocol.Packets.Orders.ThrowPoints();
                PointsStraight type = PointsStraight.Unknown;
                if (points.ContainsKey(obj.Item2)) {
                    type = points[obj.Item2];
                } else {
                    Console.WriteLine("MQTT: unknown turnout type " + obj.Item2);
                }
                if (payload == "CLOSED") {
                    if (type == PointsStraight.Left)
                        pkg.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_LEFT;
                    else if (type == PointsStraight.Right)
                        pkg.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_RIGHT;
                    else
                        pkg.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_LEFT;
                } else if (payload == "THROWN") {
                    if (type == PointsStraight.Left)
                        pkg.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_RIGHT;
                    else if (type == PointsStraight.Right)
                        pkg.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_LEFT;
                    else
                        pkg.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_RIGHT;
                } else {
                    Console.WriteLine("MQTT: Skip state " + payload);
                    return;
                }
                if (objState.ContainsKey(obj.Item2) && objState[obj.Item2] != payload) {
                    objState[obj.Item2] = payload;
                    msg.packets.Add(pkg);
                    cl.Send(msg);
                    Console.WriteLine("MQTT --> OCS(" + obj.Item1 + "): Sent " + obj.Item2 + " order " + pkg.RQ_POINTS_COMMAND.ToString());
                } else {
                    Console.WriteLine("MQTT: skip same state");
                }
            };
        }

        void StartListening() {
            Networker.Instance.OnConnect += (c) => {
                Console.WriteLine("OnConnect: " + c.UnitID);
                if (unitidToClient.ContainsKey(c.UnitID))
                    unitidToClient[c.UnitID]?.Disconnect();
                unitidToClient[c.UnitID] = c;
                c.OnMessage += async (m) => {
                    Console.WriteLine("onMessage: " + c.UnitID);
                    if (!objToUrl.ContainsKey(m.RXID_OBJECT)) {
                        Console.WriteLine("onMessage: Unknown object " + m.RXID_OBJECT);
                        return;
                    }
                    Console.WriteLine("onMessage: Known object " + m.RXID_OBJECT);
                    var topic = objToUrl[m.RXID_OBJECT];
                    foreach (var pkg in m.packets) {
                        var pkg2 = pkg as IPOCS.Protocol.Packets.Status.Points;
                        var state = pkg2.RQ_POINTS_STATE;
                        string order = "UNKNOWN";

                        PointsStraight type = PointsStraight.Unknown;
                        string unitid = m.RXID_OBJECT;
                        if (points.ContainsKey(unitid)) {
                                type = points[unitid];
                        } else {
                                Console.WriteLine("onMessage: Warning: unknown turnout type " + unitid);
                        }
                        switch (state) {
                            case RQ_POINTS_STATE.LEFT:
                                if (type == PointsStraight.Left)
                                    order = "CLOSED";
                                else if (type == PointsStraight.Right)
                                    order = "THROWN";
                                else
                                    order = "CLOSED";
                                break;
                            case RQ_POINTS_STATE.RIGHT:
                                if (type == PointsStraight.Left)
                                    order = "THROWN";
                                else if (type == PointsStraight.Right)
                                    order = "CLOSED";
                                else
                                    order = "THROWN";
                                    break;
                        }
                        Console.WriteLine("onMessage: " + m.RXID_OBJECT + " interpreted " + state + " as " + order);
                        var message = new MqttApplicationMessageBuilder()
                            .WithTopic(topic)
                            .WithPayload(order)
                            .Build();
                        Console.WriteLine("onMessage: publishing " + order + " on " + topic);
                        await broker.PublishAsync(message);
                    }
                };
            };

            Networker.Instance.OnConnectionRequest += (c, r) => {
                Console.WriteLine("OnConnectionRequest: " + c.UnitID);
                return true;
            };

            Networker.Instance.OnDisconnect += (c) => {
                Console.WriteLine("onDisconnect: " + c.UnitID);
                if (c == unitidToClient[c.UnitID])
                    unitidToClient[c.UnitID] = null;
            };

            Networker.Instance.OnListening += (isListening) => {
                Console.WriteLine("isListening: " + isListening);
            };

            Networker.Instance.isListening = true;
        }

        static void Main(string[] args) {
            var prog = new Program();
            try {
                if (args.Length == 1) {
                    prog.LoadConfigData(args[0]);
                } else {
                    prog.LoadConfigData(Environment.GetEnvironmentVariable("HOME") + "/configdata.xml");
                }
            }catch (Exception e) {
                Console.Error.WriteLine(e);
                System.Environment.Exit(1);
            }
            prog.Subscribe();
            prog.StartListening();
            for (;;) Thread.Sleep(1000);
        }
    }
}
