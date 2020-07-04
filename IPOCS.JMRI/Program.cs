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
using CommandLine;

namespace IPOCS.JMRI
{
    class Program
    {
        ConcurrentDictionary<string, IPOCS.Client> unitidToClient = new ConcurrentDictionary<string, IPOCS.Client>();

        Program(World world) {
            var factory = new MqttFactory();
            
            IMqttClient broker = factory.CreateMqttClient();
            
            IMqttClientOptions options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer("localhost")
                .Build();

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
                
                if (!world.IsSopFromUrl(topic)) {
                    Console.WriteLine("MQTT: Skip unknown SoP " + topic);
                    return;
                }
                Sop sop = world.GetSopFromUrl(topic);
                if (!unitidToClient.ContainsKey(sop.ocs.unitid)) {
                    Console.WriteLine("MQTT: Skip unconnected OCS("+ sop.ocs.unitid + ")");
                    return;
                }

                // can we use sender instead?
                var cl = unitidToClient[sop.ocs.unitid];

                var msg = new IPOCS.Protocol.Message();
                msg.RXID_OBJECT = sop.name;
                var pkg = new IPOCS.Protocol.Packets.Orders.ThrowPoints();

                if (payload == "CLOSED") {
                    sop.SetClosed();
                } else if (payload == "THROWN") {
                    sop.SetThrown();
                } else {
                    sop.SetUnknown();
                }

                // todo remove when set unknown
                pkg.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_LEFT;

                if (sop.GetState() == Sop.State.Thrown) {
                    pkg.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_RIGHT;
                }
                else if (sop.GetState() == Sop.State.Closed) {
                    pkg.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_LEFT;
                } else {
                    // TODO set unknown?
                }

                if (sop.IsChanged()) {
                    sop.ClearChange();
                    msg.packets.Add(pkg);
                    cl.Send(msg);
                    Console.WriteLine("MQTT --> OCS(" + sop.ocs.unitid + "): SoP(" + sop.name + "): " + pkg.RQ_POINTS_COMMAND.ToString());
                } else {
                    Console.WriteLine("MQTT: skip same state");
                }
            };
        
            Networker.Instance.OnConnect += (c) => {
                Console.WriteLine("OnConnect: OCS(" + c.UnitID + ")");

                if (unitidToClient.ContainsKey("" + c.UnitID))
                    unitidToClient["" + c.UnitID]?.Disconnect();
                unitidToClient["" + c.UnitID] = c;

                c.OnMessage += async (m) => {
                    Console.WriteLine("onMessage: from OCS(" + c.UnitID + ")");
                    if (!world.IsSopFromName(m.RXID_OBJECT)) { 
                        Console.WriteLine("onMessage: Unknown object " + m.RXID_OBJECT);
                        return;
                    }
                    Console.WriteLine("onMessage: Known object " + m.RXID_OBJECT);
                    Sop sop = world.GetSopFromName(m.RXID_OBJECT);
                    foreach (var pkg in m.packets) {
                        if (!(pkg is IPOCS.Protocol.Packets.Status.Points)) {
                            Console.WriteLine("onMessage: Unexpected package type");
                            continue;
                        }
                        var sPkg = pkg as IPOCS.Protocol.Packets.Status.Points;
                        switch (sPkg.RQ_POINTS_STATE) {
                            case RQ_POINTS_STATE.LEFT: sop.SetLeft(); break; 
                            case RQ_POINTS_STATE.RIGHT: sop.SetRight(); break;
                            //case RQ_POINTS_STATE.MOVING: sop.SetMoving(); break; 
                            //case RQ_POINTS_STATE.OUT_OF_CONTROL: sop.SetOutOfControl(); break; 
                            default: sop.SetUnknown(); break;
                        }
                        string order;
                        if (sop.GetState() == Sop.State.Closed)
                            order = "CLOSED";
                        else if (sop.GetState() == Sop.State.Thrown)
                            order = "THROWN";
                        else
                            order = "UNKNOWN";

                        Console.WriteLine("onMessage: " + m.RXID_OBJECT + " interpreted " + sPkg.RQ_POINTS_STATE + " as " + order + " on " + sop.url);
                        /*
                        if (!sop.IsChanged()) {
                            Console.WriteLine("onMessage: no action");
                            return;
                        }
                        */
                        var message = new MqttApplicationMessageBuilder()
                            .WithTopic(sop.url)
                            .WithPayload(order)
                            .Build();
                        sop.ClearChange();
                        await broker.PublishAsync(message);
                        Console.WriteLine("onMessage: published");

                    }
                };
            };

            Networker.Instance.OnConnectionRequest += (c, r) => {
                Console.WriteLine("OnConnectionRequest: OCS(" + c.UnitID + ")");
                return true;
            };

            Networker.Instance.OnDisconnect += (c) => {
                var key = "" + c.UnitID; 
                Console.WriteLine("onDisconnect: OCS(" + key + ")");
                if (world.IsOcs(key))
                    world.GetOcs(key).Lost();
                if (!unitidToClient.ContainsKey(key)) {
                    Console.WriteLine("onDisconnect: unknown OCS(" + key + ")");
                    return;
                }
                if (c == unitidToClient[key])
                    unitidToClient[key] = null;
            };

            Networker.Instance.OnListening += (isListening) => {
                Console.WriteLine("isListening: " + isListening);
            };

            Networker.Instance.isListening = true;
        }

        void Tick() {

        }

        static void Main(string[] args) {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            Console.WriteLine($"{fvi.ProductName} Version {version}");

            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                        World world = new World();
                        world.LoadFile(string.IsNullOrEmpty(o.Configuration) ? "ConfigData.xml" : o.Configuration);
                        var prog = new Program(world);
                        for (;;) {
                            prog.Tick();
                            Thread.Sleep(1000);
                        }
                   });

        }
    }
}
