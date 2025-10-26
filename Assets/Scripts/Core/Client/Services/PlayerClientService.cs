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
                        Player.Instance.Owner = Player.Instance;
                        Player.Instance.User = ClientManager.Instance.CurrentUser;
                        Player.Instance.Init(ClientManager.Instance.CurrentUser.PlayerId);
                    }
                    else
                    {
                        UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                        if (!ClientManager.Instance.CurrentLobby.GameData.ClientPlayers.ContainsKey(user) && !ClientManager.Instance.CurrentLobby.GameData.ClientObjects.ContainsKey(user.PlayerId))
                        {
                            OtherPlayer op = Instantiate(NetResources.Instance.ClientPlayerPrefab.gameObject).GetComponent<OtherPlayer>();
                            op.Owner = op;
                            op.User = user;
                            op.Init(user.PlayerId);
                        }
                        else
                        {
                            Debug.LogWarning($"Player with Id {playerId} already exists. Spawn request ignored.");
                        }
                    }
                    break;
                }
            case CommandType.PLAYER_DESTROY:
                {
                    byte playerId = packet.ReadByte();
                    if (ClientManager.Instance.CurrentUser.PlayerId == playerId)
                    {
                        Debug.LogWarning("Received PLAYER_DESTROY for the local player. Ignoring.");
                        // Can add logic here to handle local player destruction if needed
                        break;
                    }
                    else
                    {
                        UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                        if (ClientManager.Instance.CurrentLobby.GameData.ClientPlayers.TryGetValue(user, out ClientPlayer player) && ClientManager.Instance.CurrentLobby.GameData.ClientObjects.ContainsKey(user.PlayerId))
                        {
                            player.Remove();
                            Destroy(player.gameObject);
                        }
                        else
                        {
                            Debug.LogWarning($"No player with Id {playerId} found. Destroy request ignored.");
                        }
                    }
                    break;
                }
        }
    }
}
