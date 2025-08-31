using System;
using UnityEngine;

public class UserData : INetSerializable<UserData>
{
    public Guid GlobalGuid { get; set; }
    public ulong UserId { get; set; }
    public byte PlayerId { get; set; }
    public int LobbyId { get; set; } = -1;
    public bool InLobby { get { return LobbyId > -1; } }
    public bool InGame { get; set; } = false;
    public UserSettings Settings { get; set; } = new UserSettings();

    public bool IsHost(LobbyData lobby)
    {
        return lobby.HostUser != null && lobby.HostUser.UserId == UserId;
    }

    public UserData Deserialize(ref NetPacket packet)
    {
        return new UserData()
        {
            GlobalGuid = Guid.Parse(packet.ReadString()),
            UserId = packet.ReadULong(),
            PlayerId = packet.ReadByte(),
            LobbyId = packet.ReadInt(),
            InGame = packet.ReadBool(),
            Settings = new UserSettings().Deserialize(ref packet)
        };
    }

    public void Serialize(ref NetPacket packet)
    {
        packet.Write(GlobalGuid.ToString());
        packet.Write(UserId);
        packet.Write(PlayerId);
        packet.Write(LobbyId);
        packet.Write(InGame);
        Settings.Serialize(ref packet);
    }
}

[Serializable]
public class UserSettings : INetSerializable<UserSettings>
{
    public string UserName { get => userName; set => userName = value; }

    [SerializeField] private string userName;

    public UserSettings Deserialize(ref NetPacket packet)
    {
        return new UserSettings()
        {
            UserName = packet.ReadString()
        };
    }

    public void Serialize(ref NetPacket packet)
    {
        packet.Write(UserName);
    }
}
