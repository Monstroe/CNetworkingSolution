
using UnityEngine;
using System.Collections.Generic;

public enum ServiceType
{
    CONNECTION, LOBBY, PLAYER, GAME, FX, EVENT, INTERACTABLE, CHAT, OBJECT
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
    PLAYER_SPAWN, PLAYER_DESTROY, PLAYER_TRANSFORM, PLAYER_ANIM,
    PLAYER_GRAB_REQUEST, PLAYER_GRAB_DENY, PLAYER_INTERACT_REQUEST, PLAYER_INTERACT_DENY, PLAYER_DROP_REQUEST, PLAYER_DROP_DENY,
    /* FX */
    SFX_REQUEST, SFX, VFX_REQUEST, VFX,
    /* EVENT */
    EVENT_GROUND_HIT,
    /* INTERACTABLE */
    INTERACTABLE_GRAB, INTERACTABLE_DROP, INTERACTABLE_INTERACT, INTERACTABLE_TRANSFORM,
    /* CHAT */
    CHAT_MESSAGE, CHAT_USER_JOINED, CHAT_USER_LEFT,
    /* OBJECT */
    OBJECT_COMMUNICATION, OBJECTS_INIT, OBJECT_SPAWN_REQUEST, OBJECT_SPAWN, OBJECT_DESTROY_REQUEST, OBJECT_DESTROY
}

public static class PacketBuilder
{
    /* CONNECTION */
#if CNS_SERVER_MULTIPLE
    public static NetPacket ConnectionRequest(string token)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.CONNECTION);
        packet.Write((byte)CommandType.CONNECTION_REQUEST);
        packet.Write(token);
        return packet;
    }
#endif
    public static NetPacket ConnectionRequest(ConnectionData connectionData)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.CONNECTION);
        packet.Write((byte)CommandType.CONNECTION_REQUEST);
        connectionData.Serialize(packet);
        return packet;
    }


    public static NetPacket ConnectionResponse(bool accepted, int lobbyId, LobbyRejectionType? errorType = null)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.CONNECTION);
        packet.Write((byte)CommandType.CONNECTION_RESPONSE);
        packet.Write(accepted);
        packet.Write(lobbyId);
        if (errorType != null)
            packet.Write((byte)errorType);
        return packet;
    }

    /* LOBBY */
    public static NetPacket LobbySettings(LobbySettings settings)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_SETTINGS);
        settings.Serialize(packet);
        return packet;
    }

    public static NetPacket LobbyUserSettings(UserData user, UserSettings settings)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.LOBBY);
        packet.Write((byte)CommandType.LOBBY_USER_SETTINGS);
        packet.Write(user.UserId);
        settings.Serialize(packet);
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

    public static NetPacket PlayerDestroy(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.PLAYER);
        packet.Write((byte)CommandType.PLAYER_DESTROY);
        packet.Write(user.PlayerId);
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
    public static NetPacket PlaySFXRequest(string path, float volume, Vector3? pos)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.FX);
        packet.Write((byte)CommandType.SFX_REQUEST);
        int key = NetResources.Instance.GetSFXKeyFromPath(path);
        if (key == 0)
        {
            Debug.LogError("PacketBuilder PlaySFXRequest could not find SFX key for path: " + path);
            return null;
        }
        packet.Write(key);
        packet.Write(volume);
        if (pos != null)
            packet.Write(pos.Value);
        return packet;
    }

    public static NetPacket PlaySFX(int key, float volume, Vector3? pos)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.FX);
        packet.Write((byte)CommandType.SFX);
        packet.Write(key);
        packet.Write(volume);
        if (pos != null)
            packet.Write(pos.Value);
        return packet;
    }

    public static NetPacket PlayVFXRequest(string path, Vector3 pos, float scale)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.FX);
        packet.Write((byte)CommandType.VFX_REQUEST);
        int key = NetResources.Instance.GetVFXKeyFromPath(path);
        if (key == 0)
        {
            Debug.LogError("PacketBuilder PlayVFXRequest could not find VFX key for path: " + path);
            return null;
        }
        packet.Write(key);
        packet.Write(pos);
        packet.Write(scale);
        return packet;
    }

    public static NetPacket PlayVFX(int key, Vector3 pos, float scale)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.FX);
        packet.Write((byte)CommandType.VFX);
        packet.Write(key);
        packet.Write(pos);
        packet.Write(scale);
        return packet;
    }

    /* EVENT */
    public static NetPacket EventGroundHit(byte playerId, GroundHitArgs args)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.EVENT);
        packet.Write((byte)CommandType.EVENT_GROUND_HIT);
        packet.Write(playerId);
        args.Serialize(packet);
        return packet;
    }

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

    public static NetPacket InteractableTransform(Vector3 position, Quaternion rotation, Vector3 forward)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.INTERACTABLE);
        packet.Write((byte)CommandType.INTERACTABLE_TRANSFORM);
        packet.Write(position);
        packet.Write(rotation);
        packet.Write(forward);
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
    public static NetPacket ObjectCommunication(NetObject netObject, NetPacket packet)
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

    public static NetPacket ObjectSpawnRequest(string clientPrefabPath, Vector3 pos, Quaternion rot, bool setThisPlayerAsOwner = true)
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
        packet.Write(setThisPlayerAsOwner);
        return packet;
    }

    public static NetPacket ObjectSpawn(ushort objectId, int clientPrefabKey, Vector3 pos, Quaternion rot, byte? ownerId)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.OBJECT);
        packet.Write((byte)CommandType.OBJECT_SPAWN);
        packet.Write(objectId);
        packet.Write(clientPrefabKey);
        packet.Write(pos);
        packet.Write(rot);
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
}