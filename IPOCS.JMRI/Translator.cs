using IPOCS.JMRI.ObjectExtensions;
using IPOCS.JMRI.Translators;
using IPOCS.Protocol;
using IPOCS_Programmer.ObjectTypes;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IPOCS.JMRI
{
  public class Translator
  {
    private Dictionary<string, string> ObjectMappings { get; } = new Dictionary<string, string>();
    public Options Options { get; }

    private IpocsHandler IpocsHandler { get; }
    private JmriHandler JmriHandler { get; }

    public Translator(IpocsHandler ipocsHandler, JmriHandler jmriHandler, Dictionary<string, string> objectMappings, Options options)
    {
      Options = options;
      IpocsHandler = ipocsHandler;
      JmriHandler = jmriHandler;
      ObjectMappings = objectMappings;
      IpocsHandler.OnMessage += Transship;
      JmriHandler.OnMqttMessage += Transship;
    }

    public void Transship(Message message)
    {
      Log.Information("IPOCS: Status received from {@RXID_OBJECT}", message.RXID_OBJECT);
      if (!ObjectMappings.ContainsKey(message.RXID_OBJECT))
      {
        Log.Warning("IPOCS: {@RXID_OBJECT} has no mapping to MQTT, ignoring", message.RXID_OBJECT);
        return;
      }

      foreach (var basePkt in message.packets)
      {
        if (basePkt.GetTranslator() is BaseTranslator translator)
        {
          var state = translator.GetPayloadFromPacket(message, basePkt);
          var topic = string.Join('/', Options.Channel, "state", "track", "turnout", ObjectMappings[message.RXID_OBJECT].Substring(2));
          JmriHandler.Send(topic, state);
          Log.Information("MQTT: Published {@payload} on {@topic}", state, topic);
        }
        else
        {
          Log.Warning("MQTT: Packet type does not have a translator {@PacketType}",basePkt.GetType().FullName);
        }
      };
    }

    public void Transship(string topic, string payload)
    {
      var parts = topic.Split('/');
      if (!ObjectMappings.ContainsValue("MT" + parts.Last()))
      {
        Log.Warning("MQTT: Unable to map topic {@topic} to IPOCS, ignoring", topic);
        return;
      }

      var msg = new Message
      {
        RXID_OBJECT = ObjectMappings.First(part => part.Value == "MT" + parts.Last()).Key
      };
      if (parts.GetTranslator(parts.SkipLast(1).Last()) is BaseTranslator translator)
      {
        if (translator.GetPacketFromPayload(payload) is Packet pkt)
        {
          msg.packets.Add(pkt);
          IpocsHandler.Send(msg.RXID_OBJECT, msg);
        }
        else
        {
          Log.Warning("IPOCS: Translator did not create a packet for {@topic} from {@payload}", topic, payload);
        }
      }
      else
      {
        Log.Warning("IPOCS: Topic does not have a translator {@topic}", topic);
      }
    }
  }
}
