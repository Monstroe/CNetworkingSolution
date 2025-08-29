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
                    Vector3 position = packet.ReadVector3();
                    Quaternion rotation = packet.ReadQuaternion();
                    Vector3 forward = packet.ReadVector3();

                    if (ClientManager.Instance.CurrentUser.PlayerId == playerId)
                    {
                        Player.Instance.Init(ClientManager.Instance.CurrentUser.PlayerId);
                        Player.Instance.PlayerMovement.SetTransform(position, rotation, forward);
                    }
                    else
                    {
                        SpawnPlayer(playerId, position, rotation, forward);
                    }
                    break;
                }
            case CommandType.PLAYERS_LIST:
                {
                    int playerCount = packet.ReadByte();
                    for (int i = 0; i < playerCount; i++)
                    {
                        byte playerId = packet.ReadByte();
                        Vector3 position = packet.ReadVector3();
                        Quaternion rotation = packet.ReadQuaternion();
                        Vector3 forward = packet.ReadVector3();
                        bool walking = packet.ReadBool();
                        bool sprinting = packet.ReadBool();
                        bool crouching = packet.ReadBool();
                        bool grounded = packet.ReadBool();
                        bool jumped = packet.ReadBool();
                        bool grabbed = packet.ReadBool();
                        SpawnPlayer(playerId, position, rotation, forward, walking, sprinting, crouching, grounded, jumped, grabbed);
                    }

                    break;
                }
        }
    }

    private void SpawnPlayer(byte playerId, Vector3? position = null, Quaternion? rotation = null, Vector3? forward = null, bool? walking = null, bool? sprinting = null, bool? crouching = null, bool? grounded = null, bool? jumped = null, bool? grabbed = null)
    {
        UserData user = ClientManager.Instance.CurrentLobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
        if (!ClientManager.Instance.CurrentLobby.GameData.OtherPlayers.ContainsKey(user) || !ClientManager.Instance.CurrentLobby.GameData.ClientObjects.ContainsKey(user.PlayerId))
        {
            OtherPlayer op = Instantiate(Resources.Load<GameObject>("Prefabs/OtherPlayer")).GetComponent<OtherPlayer>();
            op.Init(user.PlayerId);
            op.Register(user, position, rotation, forward, walking, sprinting, crouching, grounded, jumped, grabbed);
            ClientManager.Instance.CurrentLobby.GameData.OtherPlayers.Add(user, op);
            ClientManager.Instance.CurrentLobby.GameData.ClientObjects.Add(op.Id, op);
        }
        else
        {
            Debug.LogWarning($"Player with Id {playerId} already exists. Spawn request ignored.");
        }
    }
}
