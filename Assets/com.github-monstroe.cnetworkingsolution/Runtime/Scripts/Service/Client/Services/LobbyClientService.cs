using System.Linq;
using UnityEngine;

[ServiceId("LobbyService")]
public class LobbyClientService : ClientService
{
    public delegate void LobbyInitializedEventHandler(ulong tick, LobbyData lobbyData);
    public event LobbyInitializedEventHandler OnLobbyInitialized;

    public delegate void LobbyUserJoinedEventHandler(UserData user);
    public event LobbyUserJoinedEventHandler OnLobbyUserJoined;

    public delegate void LobbyUserLeftEventHandler(UserData user);
    public event LobbyUserLeftEventHandler OnLobbyUserLeft;

    public override void ReceiveData(NetPacket packet, ushort commandType, TransportMethod? transportMethod)
    {
        base.ReceiveData(packet, commandType, transportMethod);
        switch ((LobbyCommandType)commandType)
        {
            case LobbyCommandType.LOBBY_INIT:
                {
                    ulong tick = packet.ReadULong();
                    lobby.ClientTick = tick; // Update the client tick
                    int userCount = packet.ReadByte();
                    for (int i = 0; i < userCount; i++)
                    {
                        UserData user = new UserData().Deserialize(packet);
                        lobby.LobbyData.AddUser(user);
                    }
                    lobby.CurrentUser = lobby.LobbyData.LobbyUsers[lobby.LobbyData.LobbyUsers.Count - 1]; // Set the local user data
                    OnLobbyInitialized?.Invoke(tick, lobby.LobbyData);
                    break;
                }
            case LobbyCommandType.LOBBY_USER_JOINED:
                {
                    UserData user = new UserData().Deserialize(packet);
                    lobby.LobbyData.AddUser(user);
                    OnLobbyUserJoined?.Invoke(user);
                    break;
                }
            case LobbyCommandType.LOBBY_USER_LEFT:
                {
                    ulong userId = packet.ReadULong();
                    UserData user = lobby.LobbyData.LobbyUsers.FirstOrDefault(u => u.UserId == userId);
                    if (user == null)
                    {
                        Debug.LogWarning($"Received LOBBY_USER_LEFT for user ID {userId}, but no such user was found in the lobby.");
                        return;
                    }

                    lobby.LobbyData.RemoveUser(user);
                    OnLobbyUserLeft?.Invoke(user);
                    break;
                }
        }
    }
}
