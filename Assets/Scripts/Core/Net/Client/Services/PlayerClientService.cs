using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class PlayerClientService : ClientService
{
    public Dictionary<UserData, ClientPlayer> ClientPlayers { get; private set; } = new Dictionary<UserData, ClientPlayer>();

    [SerializeField] private ClientPlayer clientPlayerPrefab;

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.PLAYER_SPAWN:
                {
                    byte playerId = packet.ReadByte();
                    Vector3 pos = packet.ReadVector3();
                    Quaternion rot = packet.ReadQuaternion();
                    bool walking = packet.ReadBool();
                    bool sprinting = packet.ReadBool();
                    bool crouching = packet.ReadBool();
                    bool grounded = packet.ReadBool();
                    bool jumped = packet.ReadBool();
                    bool grabbed = packet.ReadBool();
                    Debug.Log($"Spawning player with Id {playerId} at position {pos}.");

                    if (lobby.CurrentUser.PlayerId == playerId)
                    {
                        Player.Instance.Owner = Player.Instance;
                        Player.Instance.User = lobby.CurrentUser;
                        Player.Instance.transform.position = pos;
                        Player.Instance.transform.rotation = rot;
                        Player.Instance.Init(lobby.CurrentUser.PlayerId, lobby);
                        Debug.Log("Local player spawned.");
                    }
                    else
                    {
                        UserData user = lobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                        if (!ClientPlayers.ContainsKey(user) && !lobby.GetService<ObjectClientService>().ClientObjects.ContainsKey(user.PlayerId))
                        {
                            ClientPlayer op = Instantiate(clientPlayerPrefab.gameObject, pos, rot).GetComponent<ClientPlayer>();
                            op.Owner = op;
                            op.User = user;
                            op.Init(user.PlayerId, lobby);
                            op.IsWalking = walking;
                            op.IsSprinting = sprinting;
                            op.IsCrouching = crouching;
                            op.IsGrounded = grounded;
                            op.Jumped = jumped;
                            op.Grabbed = grabbed;
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
                    if (lobby.CurrentUser.PlayerId == playerId)
                    {
                        Debug.LogWarning("Received PLAYER_DESTROY for the local player. Ignoring.");
                        // Can add logic here to handle local player destruction if needed
                        break;
                    }
                    else
                    {
                        UserData user = lobby.LobbyData.LobbyUsers.Find(u => u.PlayerId == playerId);
                        if (ClientPlayers.TryGetValue(user, out ClientPlayer player) && lobby.GetService<ObjectClientService>().ClientObjects.ContainsKey(user.PlayerId))
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

    public override void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType)
    {
        // Nothing
    }
}
