using System;
using System.Collections.Generic;
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

    public UserData Deserialize(NetPacket packet)
    {
        return new UserData()
        {
            GlobalGuid = Guid.Parse(packet.ReadString()),
            UserId = packet.ReadULong(),
            PlayerId = packet.ReadByte(),
            LobbyId = packet.ReadInt(),
            InGame = packet.ReadBool(),
            Settings = new UserSettings().Deserialize(packet)
        };
    }

    public void Serialize(NetPacket packet)
    {
        packet.Write(GlobalGuid.ToString());
        packet.Write(UserId);
        packet.Write(PlayerId);
        packet.Write(LobbyId);
        packet.Write(InGame);
        Settings.Serialize(packet);
    }
}

[Serializable]
public class UserSettings : INetSerializable<UserSettings>, IDeepClone<UserSettings>
{
    public string UserName { get => userName; set => userName = value; }
    public UserType UserType { get => userType; set => userType = value; }

    [SerializeField] private string userName;
    [SerializeField] private UserType userType;

    public UserSettings Clone()
    {
        return new UserSettings()
        {
            UserName = this.UserName,
            UserType = this.UserType,
        };
    }

    public UserSettings Deserialize(NetPacket packet)
    {
        string userName = packet.ReadString();
        UserType userType = (UserType)packet.ReadByte();

        return new UserSettings()
        {
            UserName = userName,
            UserType = userType
        };
    }

    public void Serialize(NetPacket packet)
    {
        packet.Write(UserName);
        packet.Write((byte)UserType);
    }
}

public enum UserType
{
    Player,
    // Additional user types can be added here
}
