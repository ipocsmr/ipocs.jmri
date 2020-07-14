using IPOCS.Protocol;
using IPOCS_Programmer.ObjectTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IPOCS.JMRI
{
  public class IpocsHandler
  {
    public static Dictionary<int, Client> Clients { get; } = new Dictionary<int, Client>();

    private List<Concentrator> Concentrators { get; }

    public IpocsHandler(List<Concentrator> ipocsConfig)
    {
      Concentrators = ipocsConfig;
    }

    protected void OnClientDisconnected(Client client)
    {
      Console.WriteLine($"IPOCS: Unit {client.UnitID} disconnected");
      client.OnMessage -= OnClientMessage;
      if (Clients.ContainsValue(client))
      {
        Clients.Remove(client.UnitID);
      }
    }

    public event Client.OnMessageDelegate OnMessage;

    protected void OnClientMessage(Message msg)
    {
      this.OnMessage?.Invoke(msg);
    }

    protected void OnClientConnected(Client client)
    {
      if (Clients.ContainsKey(client.UnitID))
      {
        Clients[client.UnitID].Disconnect();
      }
      Console.WriteLine($"IPOCS: Unit {client.UnitID} connected");
      Clients[client.UnitID] = client;
      client.OnMessage += OnMessage;
    }

    public void Setup()
    {
      Networker.Instance.OnConnect += OnClientConnected;
      Networker.Instance.OnDisconnect += OnClientDisconnected;
      Networker.Instance.OnListening += Instance_OnListening;
      Networker.Instance.OnConnectionRequest += Instance_OnConnectionRequest;
    }

    private bool? Instance_OnConnectionRequest(Client client, Protocol.Packets.ConnectionRequest request)
    {
      Console.WriteLine($"IPOCS: Client attempting to connect");
      var concentrator = Concentrators.FirstOrDefault((c) => c.UnitID == client.UnitID);
      if (concentrator == null)
      {
        return false;
      }
      List<byte> vector;
      try
      {
        vector = concentrator.Serialize();
      }
      catch (NullReferenceException)
      {
        return false;
      }

      ushort providedChecksum = ushort.Parse(request.RXID_SITE_DATA_VERSION);
      ushort computedChecksum = IPOCS.CRC16.Calculate(vector.ToArray());
      if (providedChecksum == 0 || computedChecksum != providedChecksum)
      {
        var responseMsg = new Message();
        responseMsg.RXID_OBJECT = client.UnitID.ToString();
        responseMsg.packets.Add(new Protocol.Packets.ApplicationData
        {
          RNID_XUSER = 0x0001,
          PAYLOAD = vector.ToArray()
        });
        client.Send(responseMsg);
        return false;
      }
      return true;
    }

    private void Instance_OnListening(bool isListening)
    {
      Console.WriteLine($"IPOCS: {(isListening ? "L" : "Stopped l")}istening for connections...");
    }

    private Concentrator FindConcentratorForObject(string RXID_OBJECT)
    {
      var query = from ic in Concentrators
                  where ic.Objects.Any(bo => bo.Name == RXID_OBJECT)
                  select ic;
      return query.FirstOrDefault();
    }

    public bool Send(string RXID_OBJECT, Message msg)
    {
      if (FindConcentratorForObject(RXID_OBJECT) is Concentrator c)
      {
        if (!Clients.ContainsKey(c.UnitID))
        {
          Console.WriteLine($"OCS not connected {c.UnitID}");
          return false;
        }
        Console.WriteLine($"Sending order to OCS {c.UnitID}");
        Clients[c.UnitID].Send(msg);
      }
      else
      {
        Console.WriteLine($"Intended IPOCS receiver not found {RXID_OBJECT}");
        return false;
      }
      return true;
    }
  }
}
