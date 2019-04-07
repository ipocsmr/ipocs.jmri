﻿using IPOCS;
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
                /*
                if (payload != "THROWN" && payload != "CLOSED") {
                    Console.WriteLine("MQTT: Skip unexpected message " + payload);
                    return;
                }
                */
                if (!world.IsSop(topic)) {
                    Console.WriteLine("MQTT: Skip unknown SoP " + topic);
                    return;
                }
                Sop sop = world.GetSop(topic);
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

                pkg.RQ_POINTS_COMMAND = sop.straight == Sop.Straight.Left ?
                    RQ_POINTS_COMMAND.DIVERT_LEFT : RQ_POINTS_COMMAND.DIVERT_RIGHT;

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
                Console.WriteLine("OnConnect: " + c.UnitID);

                if (unitidToClient.ContainsKey("" + c.UnitID))
                    unitidToClient["" + c.UnitID]?.Disconnect();
                unitidToClient["" + c.UnitID] = c;

                c.OnMessage += async (m) => {
                    Console.WriteLine("onMessage: " + c.UnitID);
                    if (!world.IsSop(m.RXID_OBJECT)) { 
                        Console.WriteLine("onMessage: Unknown object " + m.RXID_OBJECT);
                        return;
                    }
                    Console.WriteLine("onMessage: Known object " + m.RXID_OBJECT);
                    Sop sop = world.GetSop(m.RXID_OBJECT);
                    foreach (var pkg in m.packets) {
                        if (!(pkg is IPOCS.Protocol.Packets.Status.Points)) {
                            Console.WriteLine("onMessage: Unexpected package type");
                            continue;
                        }
                        var sPkg = pkg as IPOCS.Protocol.Packets.Status.Points;
                        if (sPkg.RQ_POINTS_STATE == RQ_POINTS_STATE.LEFT)
                            sop.SetLeft();
                        else if (sPkg.RQ_POINTS_STATE == RQ_POINTS_STATE.RIGHT)
                            sop.SetRight();
                        string order;
                        if (sop.straight == Sop.Straight.Left)
                            order = "CLOSED";
                        else if (sop.straight == Sop.Straight.Right)
                            order = "THROWN";
                        else
                            order = "UNKNOWN";
                        Console.WriteLine("onMessage: " + m.RXID_OBJECT + " interpreted " + sPkg.RQ_POINTS_STATE + " as " + order + " on " + sop.url);
                        if (!sop.IsChanged()) {
                            Console.WriteLine("onMessage: nothing to do");
                            return;
                        }
                        var message = new MqttApplicationMessageBuilder()
                            .WithTopic(sop.url)
                            .WithPayload(order)
                            .Build();
                        sop.ClearChange();
                        await broker.PublishAsync(message);
                    }
                };
            };

            Networker.Instance.OnConnectionRequest += (c, r) => {
                Console.WriteLine("OnConnectionRequest: OCS(" + c.UnitID + ")");
                return true;
            };

            Networker.Instance.OnDisconnect += (c) => {
                Console.WriteLine("onDisconnect: OCS(" + c.UnitID + ")");
                world.GetOcs("" + c.UnitID)?.Lost();
                if (c == unitidToClient["" + c.UnitID])
                    unitidToClient["" + c.UnitID] = null;
            };

            Networker.Instance.OnListening += (isListening) => {
                Console.WriteLine("isListening: " + isListening);
            };

            Networker.Instance.isListening = true;
        }

        void Tick() {

        }

        static void Main(string[] args) {
            World world = new World();
            world.LoadFile(args.Length == 1 ? args[0] :
                Environment.GetEnvironmentVariable("HOME") + "/configdata.xml");
            var prog = new Program(world);
            for (;;) {
                prog.Tick();
                Thread.Sleep(1000);
            }
        }
    }
}
