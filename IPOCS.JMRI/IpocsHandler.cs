using IPOCS.Protocol;
using IPOCS_Programmer.ObjectTypes;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IPOCS.JMRI
{
  public class IpocsHandler
  {
    public static ConcurrentDictionary<int, Client> Clients { get; } = new ConcurrentDictionary<int, Client>();

    private List<Concentrator> Concentrators { get; }

    public IpocsHandler(List<Concentrator> ipocsConfig)
    {
      Concentrators = ipocsConfig;
    }

    protected void OnClientDisconnected(Client client)
    {
      Log.Information("IPOCS: Unit {@UnitID} disconnected", client.UnitID);
      client.OnMessage -= OnClientMessage;
      if (Clients.TryGetValue(client.UnitID, out var storedClient) && storedClient == client)
      {
        Clients.Remove(client.UnitID, out _);
      }
    }

    public event Client.OnMessageDelegate OnMessage;

    protected void OnClientMessage(Message msg)
    {
      this.OnMessage?.Invoke(msg);
    }

    protected void OnClientConnected(Client client)
    {
      var concentrator = Concentrators.FirstOrDefault((c) => c.UnitID == client.UnitID);
      if (concentrator == null)
      {
        Log.Warning("Unknown IPOCS unit ({@UnitID}) tried to connect. Rejecting connection...", client.UnitID);
        client.Disconnect();
      }
      if (Clients.ContainsKey(client.UnitID))
      {
        Clients[client.UnitID].Disconnect();
      }
      Log.Information("IPOCS: Unit {@UnitID} connected", client.UnitID);
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
        Log.Warning("IPOCS: Unit {@UnitID} attempted to connect, failed due to site data mismatch. Sening current configuration...", client.UnitID);
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
      Log.Information("IPOCS: {@Listening} for connections...", (isListening ? "L" : "Stopped l") + "istening");
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
          Log.Warning("IPOCS: Attempting to send order to {@RXID_OBJECT}, but that unit ({@UnitID}) is not connected ", RXID_OBJECT, c.UnitID);
          return false;
        }
        Log.Information("IPOCS: Sending order to {@RXID_OBJECT} on unit {@UnitID}", RXID_OBJECT, c.UnitID);
        Clients[c.UnitID].Send(msg);
      }
      else
      {
        Log.Error("IPOCS: Intended receiver not found {@RXID_OBJECT}", RXID_OBJECT);
        return false;
      }
      return true;
    }
  }
}
