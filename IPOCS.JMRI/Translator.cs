using IPOCS.Protocol.Packets.Orders;
using IPOCS.Protocol.Packets.Status;
using IPOCS_Programmer.ObjectTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IPOCS.JMRI
{
  public class Translator
  {
    public List<Concentrator> IpocsConfig { get; set; } = new List<Concentrator>();

    public Dictionary<string, string> TurnoutMapping = new Dictionary<string, string>();
    public Dictionary<string, Protocol.Packet> LastState = new Dictionary<string, Protocol.Packet>();
    public Options Options { get; }

    public IpocsHandler IpocsHandler { get; }
    public JmriHandler JmriHandler { get; }

    public Translator(IpocsHandler ipocsHandler, JmriHandler jmriHandler, Options options)
    {
      Options = options;
      IpocsHandler = ipocsHandler;
      JmriHandler = jmriHandler;
      IpocsHandler.OnMessage += Transship;
      JmriHandler.OnMqttMessage += Transship;
    }

    public void Transship(Protocol.Message message)
    {
      Console.WriteLine("onMessage: from OCS(" + message.RXID_OBJECT + ")");
      if (!TurnoutMapping.ContainsKey(message.RXID_OBJECT))
      {
        Console.WriteLine("onMessage: Unknown object " + message.RXID_OBJECT);
        return;
      }

      foreach (var pkg in message.packets)
      {
        if (!(pkg is IPOCS.Protocol.Packets.Status.Points))
        {
          Console.WriteLine($"onMessage: Unexpected package type ({ pkg.GetType().FullName })");
          continue;
        }

        LastState[message.RXID_OBJECT] = pkg;
        var sPkg = pkg as Protocol.Packets.Status.Points;

        var order = sPkg.RQ_POINTS_STATE switch
        {
          RQ_POINTS_STATE.LEFT => "CLOSED",
          RQ_POINTS_STATE.RIGHT => "THROWN",
          RQ_POINTS_STATE.MOVING => "INCONSISTENT",
          RQ_POINTS_STATE.OUT_OF_CONTROL => "UNKNOWN",
          _ => "UNKNOWN"
        };

        var topic = string.Join('/', Options.Channel, "track", "turnout", TurnoutMapping[message.RXID_OBJECT].Substring(2));
        Console.WriteLine($"onMessage: {message.RXID_OBJECT} interpreted {sPkg.RQ_POINTS_STATE} as {order} on {topic}");
        JmriHandler.Send(topic, order);
        Console.WriteLine("onMessage: published");
      };
    }

    public void Transship(string topic, string payload)
    {
      var parts = topic.Split('/');
      if (!TurnoutMapping.ContainsValue("MT" + parts.Last()))
      {
        Console.WriteLine($"MQTT: Unknown object received: {topic}");
        return;
      }

      var msg = new Protocol.Message
      {
        RXID_OBJECT = TurnoutMapping.First(part => part.Value == "MT" + parts.Last()).Key
      };

      var pkg = new ThrowPoints();
      switch (payload)
      {
        case "CLOSED": pkg.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_LEFT; break;
        case "THROWN": pkg.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_RIGHT; break;
        default:
          // TODO: Send status back to JMRI
          Console.WriteLine($"JMRI sent an unknown state: {payload}");
          return;
      }
      msg.packets.Add(pkg);

      var query = from ic in IpocsConfig
                  where ic.Objects.Any(bo => bo.Name == msg.RXID_OBJECT)
                  select ic.UnitID;
      if (query.Count() == 0)
      {
        Console.WriteLine($"JMRI sent an order to an unknown object");
        return;
      }
      IpocsHandler.Send(query.First(), msg);
    }
  }
}
