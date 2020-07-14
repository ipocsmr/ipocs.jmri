using IPOCS.Protocol;
using IPOCS.Protocol.Packets.Orders;
using IPOCS.Protocol.Packets.Status;
using JMRI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IPOCS.JMRI.Translators
{
  public class Points : BaseTranslator
  {
    internal override Packet GetPacketFromPayload(string payload)
    {
      var pkt = new ThrowPoints();
      switch (payload)
      {
        case "CLOSED": pkt.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_LEFT; break;
        case "THROWN": pkt.RQ_POINTS_COMMAND = RQ_POINTS_COMMAND.DIVERT_RIGHT; break;
        default:
          // TODO: Send status back to JMRI
          Console.WriteLine($"JMRI sent an unknown state: {payload}");
          return null;
      }
      return pkt;
    }

    internal override string GetPayloadFromPacket(Message message, Packet basePkt)
    {
      var sPkg = basePkt as Protocol.Packets.Status.Points;

      var state = sPkg.RQ_POINTS_STATE switch
      {
        RQ_POINTS_STATE.LEFT => "CLOSED",
        RQ_POINTS_STATE.RIGHT => "THROWN",
        RQ_POINTS_STATE.MOVING => "MOVING",
        RQ_POINTS_STATE.OUT_OF_CONTROL => "UNKNOWN",
        _ => "UNKNOWN"
      };
      Console.WriteLine($"onMessage: {message.RXID_OBJECT} interpreted {sPkg.RQ_POINTS_STATE} as {state}");
      return state;
      throw new NotImplementedException();
    }

    internal override Dictionary<string, string> LoadNameMappings(Layout_Config jmriConfig)
    {
      var turnoutManager = jmriConfig.Turnouts.First(tmt => tmt.Class == "jmri.jmrix.mqtt.configurexml.MqttTurnoutManagerXml");
      var turnoutMapping = new Dictionary<string, string>();
      foreach (var turnout in turnoutManager.Turnout)
      {
        Console.WriteLine(turnout.SystemName + " = " + turnout.UserName);
        turnoutMapping.Add(turnout.UserName, turnout.SystemName);
      }
      return turnoutMapping;
    }

    internal override Type[] SupportedPackets()
    {
      return new[] { typeof(Protocol.Packets.Status.Points) };
    }

    internal override string[] SupportedTopics()
    {
      return new[] { "turnout" };
    }
  }
}
