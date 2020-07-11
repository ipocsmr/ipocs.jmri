using IPOCS.Protocol;
using System;
using System.Collections.Generic;

namespace IPOCS.JMRI
{
  public class IpocsHandler
  {
    public static Dictionary<int, Client> Clients { get; } = new Dictionary<int, Client>();

    public IpocsHandler() { }

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

    protected void OnClientMessage(IPOCS.Protocol.Message msg)
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
      return true;
    }

    private void Instance_OnListening(bool isListening)
    {
      Console.WriteLine($"IPOCS: {(isListening ? "L" : "Stopped l")}istening for connections...");
    }

    public void Send(int unitId, Message msg)
    {
      if (!Clients.ContainsKey(unitId))
      {
        Console.WriteLine($"OCS not connected {unitId}");
        return;
      }
      Console.WriteLine($"Sending order to OCS {unitId}");
      Clients[unitId].Send(msg);
    }
  }
}
