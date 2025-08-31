
using System;
using UnityEngine;
using System.Collections.Generic;

public enum ServiceType
{
    CONNECTION, LOBBY, GAME, PLAYER, FX, ITEM, CHAT, OBJECT
}

public enum CommandType
{
    /* CONNECTION */
    CONNECTION_REQUEST, CONNECTION_RESPONSE,
    /* LOBBY */
    LOBBY_SETTINGS, LOBBY_USER_SETTINGS, LOBBY_USERS_LIST, LOBBY_USER_JOINED, LOBBY_USER_LEFT, LOBBY_TICK,
    /* GAME */
    GAME_USER_JOINED,
    /* PLAYER */
    PLAYER_SPAWN, PLAYER_DESTROY, PLAYER_TRANSFORM, PLAYER_ANIM, PLAYER_GRAB, PLAYER_DROP, PLAYER_INTERACT,
    /* FX */
    SFX, VFX,
    /* ITEM */
    STARTING_ITEMS_INIT, ITEM_SPAWN, ITEM_DESTROY,
    /* CHAT */
    CHAT_MESSAGE, CHAT_USER_JOINED, CHAT_USER_LEFT,
    /* OBJECT */
    OBJECT_COMMUNICATION,
}

public static class PacketBuilder
{
    /* CONNECTION */
#if CNS_DEDICATED_SERVER_MULTI_LOBBY_AUTH
    public static NetPacket ConnectionRequest(string token)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.CONNECTION);
        packet.Write((byte)CommandType.CONNECTION_REQUEST);
        packet.Write(token);
        return packet;
    }
#elif CNS_DEDICATED_SERVER_SINGLE_LOBBY_AUTH || CNS_HOST_AUTH
    public static NetPacket ConnectionRequest(ConnectionData connectionData)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.CONNECTION);
        packet.Write((byte)CommandType.CONNECTION_REQUEST);
        connectionData.Serialize(ref packet);
        return packet;
    }
#endif

    public static NetPacket ConnectionResponse(bool accepted)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.CONNECTION);
        packet.Write((byte)CommandType.CONNECTION_RESPONSE);
        packet.Write(accepted);
        return packet;
    }

    /* LOBBY */
    public static NetPacket LobbySettings(LobbySettings settings)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_SETTINGS);
        settings.Serialize(ref packet);
        return packet;
    }

    public static NetPacket LobbyUserSettings(UserData user, UserSettings settings)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_USER_SETTINGS);
        packet.Write(user.UserId);
        settings.Serialize(ref packet);
        return packet;
    }

    public static NetPacket LobbyUsersList(List<UserData> users)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_USERS_LIST);
        packet.Write((byte)users.Count);
        foreach (UserData user in users)
        {
            user.Serialize(ref packet);
        }
        return packet;
    }

    public static NetPacket LobbyUserJoined(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_USER_JOINED);
        user.Serialize(ref packet);
        return packet;
    }

    public static NetPacket LobbyUserLeft(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_USER_LEFT);
        packet.Write(user.UserId);
        return packet;
    }

    public static NetPacket LobbyTick(ulong tick)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_TICK);
        packet.Write(tick);
        return packet;
    }

    /* GAME */
    public static NetPacket GameUserJoined(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.GAME);
        packet.Write((byte)CommandType.GAME_USER_JOINED);
        packet.Write(user.PlayerId);
        return packet;
    }

    /* PLAYER */
    public static NetPacket PlayerSpawn(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_SPAWN);
        packet.Write(user.PlayerId);
        return packet;
    }

    public static NetPacket PlayerDestroy()
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_DESTROY);
        return packet;
    }

    public static NetPacket PlayerTransform(Vector3 position, Quaternion rotation, Vector3 forward)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_TRANSFORM);
        packet.Write(position);
        packet.Write(rotation);
        packet.Write(forward);
        return packet;
    }

    public static NetPacket PlayerAnim(bool walking, bool sprinting, bool crouching, bool grounded, bool jumped, bool grabbed)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_ANIM);
        packet.Write(walking);
        packet.Write(sprinting);
        packet.Write(crouching);
        packet.Write(grounded);
        packet.Write(jumped);
        packet.Write(grabbed);
        return packet;
    }

    public static NetPacket PlayerGrab(byte playerId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_GRAB);
        packet.Write(playerId);
        return packet;
    }

    public static NetPacket PlayerInteract(byte playerId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_INTERACT);
        packet.Write(playerId);
        return packet;
    }

    public static NetPacket PlayerDrop(byte playerId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_DROP);
        packet.Write(playerId);
        return packet;
    }

    /* FX */
    public static NetPacket PlaySFX(int sfxId, float volume, Vector3? pos)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.FX);
        packet.Write((byte)CommandType.SFX);
        packet.Write(sfxId);
        packet.Write(volume);
        if (pos != null)
            packet.Write(pos.Value);
        return packet;
    }

    public static NetPacket PlayVFX(int vfxId, Vector3 pos, float scale)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.FX);
        packet.Write((byte)CommandType.VFX);
        packet.Write(vfxId);
        packet.Write(pos);
        packet.Write(scale);
        return packet;
    }

    /* ITEM */
    public static NetPacket StartingItemsInit(ushort[] startingItemIds)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.ITEM);
        packet.Write((byte)CommandType.STARTING_ITEMS_INIT);
        packet.Write(startingItemIds);
        return packet;
    }

    public static NetPacket ItemSpawn(ushort itemId, ItemType itemType)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.ITEM);
        packet.Write((byte)CommandType.ITEM_SPAWN);
        packet.Write(itemId);
        packet.Write((byte)itemType);
        return packet;
    }

    public static NetPacket ItemDestroy()
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.ITEM);
        packet.Write((byte)CommandType.ITEM_DESTROY);
        return packet;
    }

    /* CHAT */
    public static NetPacket ChatMessage(UserData user, string message)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.CHAT);
        packet.Write((byte)CommandType.CHAT_MESSAGE);
        packet.Write(user.PlayerId);
        packet.Write(message);
        return packet;
    }

    public static NetPacket ChatUserJoined(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.CHAT);
        packet.Write((byte)CommandType.CHAT_USER_JOINED);
        packet.Write(user.Settings.UserName);
        return packet;
    }

    public static NetPacket ChatUserLeft(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.CHAT);
        packet.Write((byte)CommandType.CHAT_USER_LEFT);
        packet.Write(user.Settings.UserName);
        return packet;
    }

    /* OBJECT */
    public static NetPacket ObjectCommunication(INetObject netObject, NetPacket packet)
    {
        packet.Insert(0, (byte)ServiceType.OBJECT);
        packet.Insert(1, (byte)CommandType.OBJECT_COMMUNICATION);
        packet.Insert(2, netObject.Id);
        return packet;
    }
}