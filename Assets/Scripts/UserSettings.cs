using System;
using UnityEngine;

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