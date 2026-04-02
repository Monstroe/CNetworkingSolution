
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

public enum ServiceType
{
    CONNECTION, LOBBY, GAME, OBJECT, MAP, PLAYER, FX, ENTITY, INTERACTABLE, CHAT,
}

public enum CommandType
{
    /* CONNECTION */
    CONNECTION_REQUEST, CONNECTION_RESPONSE,
    /* LOBBY */
    LOBBY_SETTINGS, LOBBY_USER_SETTINGS, LOBBY_USERS_LIST, LOBBY_USER_JOINED, LOBBY_USER_LEFT, LOBBY_USER_KICK, LOBBY_TICK,
    /* GAME */
    GAME_USER_JOINED, GAME_START,
    /* OBJECT */
    OBJECT_COMMUNICATION, OBJECTS_INIT, OBJECT_SPAWN_REQUEST, OBJECT_SPAWN, OBJECT_DESTROY_REQUEST, OBJECT_DESTROY, OBJECT_RPC, OBJECT_TRANSFORM,
    /* MAP */
    // Nothing
    /* PLAYER */
    PLAYER_SPAWN, PLAYER_DESTROY, PLAYER_ANIM,
    PLAYER_GRAB_REQUEST, PLAYER_GRAB_DENY, PLAYER_INTERACT_REQUEST, PLAYER_INTERACT_DENY, PLAYER_DROP_REQUEST, PLAYER_DROP_DENY,
    /* FX */
    // Nothing
    /* ENTITY */
    // Nothing
    /* INTERACTABLE */
    INTERACTABLE_GRAB, INTERACTABLE_DROP, INTERACTABLE_INTERACT,
    /* CHAT */
    CHAT_MESSAGE, CHAT_USER_JOINED, CHAT_USER_LEFT,
}

public static class PacketBuilder
{
    /* CONNECTION */

    /* LOBBY */
    /*public static NetPacket LobbySettings(LobbySettings settings, bool invokeEvent = false)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_SETTINGS);
        settings.Serialize(packet);
        packet.Write(invokeEvent);
        return packet;
    }*/

    /*public static NetPacket LobbyUserSettings(UserData user, UserSettings settings, bool invokeEvent = false)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_USER_SETTINGS);
        packet.Write(user.UserId);
        settings.Serialize(packet);
        packet.Write(invokeEvent);
        return packet;
    }*/

    public static NetPacket LobbyUsersList(List<UserData> users)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_USERS_LIST);
        packet.Write((byte)users.Count);
        foreach (UserData user in users)
        {
            user.Serialize(packet);
        }
        return packet;
    }

    public static NetPacket LobbyUserJoined(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_USER_JOINED);
        user.Serialize(packet);
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

    public static NetPacket LobbyUserKick(UserData user, string reason)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_USER_KICK);
        packet.Write(user.UserId);
        packet.Write(reason);
        return packet;
    }

    public static NetPacket LobbyTick(ulong tick, bool invokeEvent = false)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_TICK);
        packet.Write(tick);
        packet.Write(invokeEvent);
        return packet;
    }

    /* GAME */
    public static NetPacket GameStart()
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.GAME);
        packet.Write((byte)CommandType.GAME_START);
        return packet;
    }

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

    public static NetPacket ObjectsInit(ushort[] startingObjectIds)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.OBJECT);
        packet.Write((byte)CommandType.OBJECTS_INIT);
        packet.Write(startingObjectIds);
        return packet;
    }

    public static NetPacket ObjectSpawnRequest(string clientPrefabPath, Vector3 pos, Quaternion rot)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.OBJECT);
        packet.Write((byte)CommandType.OBJECT_SPAWN_REQUEST);
        int key = NetResources.Instance.GetClientPrefabKeyFromPath(clientPrefabPath);
        if (key == 0)
        {
            Debug.LogError("PacketBuilder ObjectSpawnRequest could not find client prefab key for path: " + clientPrefabPath);
            return null;
        }
        packet.Write(key);
        packet.Write(pos);
        packet.Write(rot);
        return packet;
    }

    public static NetPacket ObjectSpawn(ushort objectId, int clientPrefabKey, Vector3 pos, Quaternion rot, bool isPlayer, byte? ownerId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.OBJECT);
        packet.Write((byte)CommandType.OBJECT_SPAWN);
        packet.Write(objectId);
        packet.Write(clientPrefabKey);
        packet.Write(pos);
        packet.Write(rot);
        packet.Write(isPlayer);
        if (ownerId != null)
            packet.Write(ownerId.Value);
        return packet;
    }

    public static NetPacket ObjectDestroyRequest(ushort objectId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.OBJECT);
        packet.Write((byte)CommandType.OBJECT_DESTROY_REQUEST);
        packet.Write(objectId);
        return packet;
    }

    public static NetPacket ObjectDestroy(ushort objectId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.OBJECT);
        packet.Write((byte)CommandType.OBJECT_DESTROY);
        packet.Write(objectId);
        return packet;
    }

    public static NetPacket ObjectTransform(Vector3 position, Quaternion rotation)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.OBJECT);
        packet.Write((byte)CommandType.OBJECT_TRANSFORM);
        packet.Write(position);
        packet.Write(rotation);
        return packet;
    }

    public static NetPacket ObjectRpc(uint methodId, MethodInfo method, params object[] args)
    {
        var parameters = method.GetParameters();
        if (args.Length != parameters.Length)
        {
            throw new ArgumentException("RPC argument count mismatch");
        }

        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.OBJECT);
        packet.Write((byte)CommandType.OBJECT_RPC);
        packet.Write(methodId);

        for (int i = 0; i < args.Length; i++)
        {
            packet.Write(args[i], parameters[i].ParameterType);
        }
        return packet;
    }

    /* MAP */
    // Nothing

    /* PLAYER */
    public static NetPacket PlayerSpawn(UserData user, Vector3 pos, Quaternion rot, bool walking, bool sprinting, bool crouching, bool grounded, bool jumped, bool grabbed)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_SPAWN);
        packet.Write(user.PlayerId);
        packet.Write(pos);
        packet.Write(rot);
        packet.Write(walking);
        packet.Write(sprinting);
        packet.Write(crouching);
        packet.Write(grounded);
        packet.Write(jumped);
        packet.Write(grabbed);
        return packet;
    }

    public static NetPacket PlayerDestroy(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_DESTROY);
        packet.Write(user.PlayerId);
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

    public static NetPacket PlayerGrabRequest(ushort interactableId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_GRAB_REQUEST);
        packet.Write(interactableId);
        return packet;
    }

    public static NetPacket PlayerGrabDeny()
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_GRAB_DENY);
        return packet;
    }

    public static NetPacket PlayerInteractRequest()
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_INTERACT_REQUEST);
        return packet;
    }

    public static NetPacket PlayerInteractDeny()
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_INTERACT_DENY);
        return packet;
    }

    public static NetPacket PlayerDropRequest()
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_DROP_REQUEST);
        return packet;
    }

    public static NetPacket PlayerDropDeny()
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_DROP_DENY);
        return packet;
    }

    /* FX */
    // Nothing

    /* INTERACTABLE */
    public static NetPacket InteractableGrab(byte playerId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.INTERACTABLE);
        packet.Write((byte)CommandType.INTERACTABLE_GRAB);
        packet.Write(playerId);
        return packet;
    }

    public static NetPacket InteractableInteract(byte playerId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.INTERACTABLE);
        packet.Write((byte)CommandType.INTERACTABLE_INTERACT);
        packet.Write(playerId);
        return packet;
    }

    public static NetPacket InteractableDrop(byte playerId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.INTERACTABLE);
        packet.Write((byte)CommandType.INTERACTABLE_DROP);
        packet.Write(playerId);
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
}