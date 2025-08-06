
using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public enum ServiceType
{
    NONE, CONNECTION, LOBBY, PLAYER, CHAT, FX, GAME, NOTIFICATION, TURN, EVENT, CRITTER, ITEM
}

public enum CommandType
{
    /* LOBBY */
    LOBBY_SETTINGS, LOBBY_USERS, LOBBY_USER_JOINED, LOBBY_USER_LEFT, LOBBY_PLAYER_KICK, LOBBY_CHECK_IN, LOBBY_PLAYER_LIST, LOBBY_STATE, LOBBY_START_GAME,
    /* PLAYER */
    PLAYER_STATE, PLAYER_ANIM, PLAYER_HOST, DYNAMIC_PLAYER_ANIM, SERVER_OBJECT_COMMUNICATION, CLIENT_OBJECT_COMMUNICATION,
    PLAYER_SPRINTING, PLAYER_GROUNDED, OVERRIDE_STATE, PLAYER_DIED, PLAYER_LOADOUT, PLAYER_INTERACT, PLAYER_HOTBAR, PLAYER_MONEY,
    /* FX */
    SFX, VFX, PARTICLE_SYSTEM,
    /* GAME */
    CHECK_IN_STATES, START_GAME, TICK, CREATE_CAMERA, CHAT_MESSAGE,
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
    /* LOBBY */
    public static NetPacket LobbySettings(LobbySettings settings)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_SETTINGS);
        packet.Write(settings.MaxUsers);
        packet.Write((byte)settings.LobbyVisibility);
        packet.Write(settings.LobbyName);
        return packet;
    }

    public static NetPacket LobbyUsers(List<UserData> users)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_USERS);
        packet.Write((byte)users.Count);
        foreach (var user in users)
        {
            packet.Write(user.GlobalGuid.ToString());
            packet.Write(user.UserId);
            packet.Write(user.PlayerId);
            packet.Write(user.Settings.UserName);
        }
        return packet;
    }

    public static NetPacket LobbyUserJoined(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_USER_JOINED);
        packet.Write(user.GlobalGuid.ToString());
        packet.Write(user.UserId);
        packet.Write(user.PlayerId);
        packet.Write(user.Settings.UserName);
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
}