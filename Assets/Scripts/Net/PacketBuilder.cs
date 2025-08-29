
using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public enum ServiceType
{
    CONNECTION, LOBBY, GAME, OBJECT, PLAYER, FX, MAP, CHAT, NOTIFICATION
}

public enum CommandType
{
    /* CONNECTION */
    CONNECTION_REQUEST, CONNECTION_RESPONSE,
    /* LOBBY */
    LOBBY_SETTINGS, LOBBY_USER_SETTINGS, LOBBY_USERS_LIST, LOBBY_USER_JOINED, LOBBY_USER_LEFT, LOBBY_TICK,
    /* GAME */
    GAME_USER_JOINED,
    /* OBJECT */
    OBJECT_COMMUNICATION,
    /* PLAYER */
    PLAYERS_LIST, PLAYER_SPAWN, PLAYER_DESTROY, PLAYER_STATE, PLAYER_ANIM, PLAYER_GRAB, PLAYER_DROP, PLAYER_INTERACT,
    /* FX */
    /* MAP */
    MAP_OBJECTS_INIT,
    SFX, VFX,
    /* CHAT */
    CHAT_MESSAGE, CHAT_USER_JOINED, CHAT_USER_LEFT,
    /* GAME */
    CHECK_IN_STATES, START_GAME, TICK, CREATE_CAMERA,
    /* NOTIFICATION */
    MID_SCREEN_NOTIF,
    /* EVENT */
    /* CRITTER */
    CRITTER_STATE, CRITTER_PICTURE, CRITTER_LIST, CRITTER_INTERACT, CRITTER_DESTROY, CRITTER_DROP,
    /* ITEM */
    REQUEST_ITEM_SPAWN, ITEM_STATE, ITEM_SPAWN, ITEM_INTERACT, ITEM_DESTROY, ITEM_DROP
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

    /* OBJECT */
    public static NetPacket ObjectCommunication(INetObject netObject, NetPacket packet)
    {
        packet.Insert(0, (byte)ServiceType.OBJECT);
        packet.Insert(1, (byte)CommandType.OBJECT_COMMUNICATION);
        packet.Insert(2, netObject.Id);
        return packet;
    }

    /* PLAYER */
    public static NetPacket PlayersList(List<ServerPlayer> players)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYERS_LIST);
        packet.Write((byte)players.Count);
        foreach (ServerPlayer player in players)
        {
            packet.Write((byte)player.Id);
            packet.Write(player.Position);
            packet.Write(player.Rotation);
            packet.Write(player.Forward);
            packet.Write(player.IsWalking);
            packet.Write(player.IsSprinting);
            packet.Write(player.IsCrouching);
            packet.Write(player.IsGrounded);
            packet.Write(player.Jumped);
            packet.Write(player.Grabbed);
        }
        return packet;
    }

    public static NetPacket PlayerSpawn(UserData user, Vector3 position, Quaternion rotation, Vector3 forward)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_SPAWN);
        packet.Write(user.PlayerId);
        packet.Write(position);
        packet.Write(rotation);
        packet.Write(forward);
        return packet;
    }

    public static NetPacket PlayerDestroy()
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_DESTROY);
        return packet;
    }

    public static NetPacket PlayerState(Vector3 position, Quaternion rotation, Vector3 forward)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_STATE);
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

    public static NetPacket PlayerInteract(byte objectId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_INTERACT);
        packet.Write(objectId);
        return packet;
    }

    public static NetPacket PlayerDrop(byte objectId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_DROP);
        packet.Write(objectId);
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

    /* MAP */
    public static NetPacket MapObjectsInit(ushort[] startingObjectIds)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.MAP);
        packet.Write((byte)CommandType.MAP_OBJECTS_INIT);
        packet.Write(startingObjectIds);
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
        packet.Write(user.PlayerId);
        return packet;
    }

    public static NetPacket ChatUserLeft(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.CHAT);
        packet.Write((byte)CommandType.CHAT_USER_LEFT);
        packet.Write(user.PlayerId);
        return packet;
    }
}