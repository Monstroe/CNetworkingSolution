using UnityEngine;

public class PlayerClientService : ClientService
{
    void Start()
    {
        ClientManager.Instance.CurrentLobby.RegisterService(ServiceType.PLAYER, this);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.PLAYER_SPAWN:
                {
                    byte playerId = packet.ReadByte();

                    if (ClientManager.Instance.CurrentUser.PlayerId == playerId)
                    {
                        Player.Instance.Init(ClientManager.Instance.CurrentUser.PlayerId);
                        ClientManager.Instance.CurrentLobby.GameData.ClientPlayers.Add(ClientManager.Instance.CurrentUser, Player.Instance);
                        ClientManager.Instance.CurrentLobby.GameData.ClientObjects.Add(Player.Instance.Id, Player.Instance);
                    }
                    else
                    {
                        UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                        if (!ClientManager.Instance.CurrentLobby.GameData.ClientPlayers.ContainsKey(user) || !ClientManager.Instance.CurrentLobby.GameData.ClientObjects.ContainsKey(user.PlayerId))
                        {
                            OtherPlayer op = Instantiate(Resources.Load<GameObject>("Prefabs/OtherPlayer")).GetComponent<OtherPlayer>();
                            op.Init(user.PlayerId);
                            op.User = user;
                            ClientManager.Instance.CurrentLobby.GameData.ClientPlayers.Add(user, op);
                            ClientManager.Instance.CurrentLobby.GameData.ClientObjects.Add(op.Id, op);
                        }
                        else
                        {
                            Debug.LogWarning($"Player with Id {playerId} already exists. Spawn request ignored.");
                        }
                    }
                    break;
                }
        }
    }
}
