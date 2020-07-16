using IPOCS.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace IPOCS.JMRI.Translators
{
  public abstract class BaseTranslator
  {
    internal abstract Type[] SupportedPackets();

    internal abstract string[] SupportedTopics();

    internal abstract string GetPayloadFromPacket(Message message, Packet basePkt);

    internal abstract Packet GetPacketFromPayload(string payload);

    internal abstract Dictionary<string, string> LoadNameMappings(global::JMRI.Layout_Config jmriConfig);
  }
}
