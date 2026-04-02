using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

internal static class ConnectionPacketBuilder
{
    internal static NetPacket ConnectionRequest(ConnectionData connectionData)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ConnectionCommandType.CONNECTION_REQUEST);
        connectionData.Serialize(packet);
        return packet;
    }

    internal static NetPacket ConnectionResponse(bool accepted, NetPacket dataPkt = null)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ConnectionCommandType.CONNECTION_RESPONSE);
        packet.Write(accepted);
        if (dataPkt != null && dataPkt.Length > 0)
            packet.Write(dataPkt.ByteArray);
        return packet;
    }

    internal static NetPacket ConnectionData(int lobbyId, NetPacket packet)
    {
        packet.Insert(0, lobbyId.ToString());
        return packet;
    }
}

internal static class ReservedPacketBuilder
{
    public static NetPacket Rpc(ulong methodId, MethodInfo method, params object[] args)
    {
        var parameters = method.GetParameters();
        if (args.Length != parameters.Length)
        {
            throw new ArgumentException("RPC argument count mismatch");
        }

        NetPacket packet = new NetPacket();
        packet.Write(ReservedCommandType.RPC);
        packet.Write(methodId);

        for (int i = 0; i < args.Length; i++)
        {
            packet.Write(args[i], parameters[i].ParameterType);
        }
        return packet;
    }
}

internal static class ObjectPacketBuilder
{
    public static NetPacket ObjectSpawnRequest(string clientPrefabPath, Vector3 pos, Quaternion rot)
    {
        NetPacket packet = new NetPacket();
        packet.Write(ObjectCommandType.OBJECT_SPAWN_REQUEST);
        ulong key = NetResources.Instance.GetClientPrefabKeyFromPath(clientPrefabPath);
        if (key == 0)
        {
            Debug.LogError("ObjectSpawnRequest could not find client prefab key for path: " + clientPrefabPath);
            return null;
        }
        packet.Write(key);
        packet.Write(pos);
        packet.Write(rot);
        return packet;
    }

    public static NetPacket ObjectDestroyRequest(ushort objectId)
    {
        NetPacket packet = new NetPacket();
        packet.Write(ObjectCommandType.OBJECT_DESTROY_REQUEST);
        packet.Write(objectId);
        return packet;
    }

    public static NetPacket ObjectCommunication(INetObject netObject, NetPacket packet)
    {
        int currLen = packet.Length;
        packet.Insert(0, ObjectCommandType.OBJECT_COMMUNICATION);
        packet.Insert(packet.Length - currLen, netObject.Id);
        return packet;
    }

    public static NetPacket ObjectSpawn(ushort objectId, ulong clientPrefabKey, Vector3 pos, Quaternion rot, bool isPlayer, byte? ownerId)
    {
        NetPacket packet = new NetPacket();
        packet.Write(ObjectCommandType.OBJECT_SPAWN);
        packet.Write(objectId);
        packet.Write(clientPrefabKey);
        packet.Write(pos);
        packet.Write(rot);
        packet.Write(isPlayer);
        if (ownerId != null)
            packet.Write(ownerId.Value);
        return packet;
    }

    public static NetPacket ObjectDestroy(ushort objectId)
    {
        NetPacket packet = new NetPacket();
        packet.Write(ObjectCommandType.OBJECT_DESTROY);
        packet.Write(objectId);
        return packet;
    }

    public static NetPacket ObjectTransform(Vector3 position, Quaternion rotation)
    {
        NetPacket packet = new NetPacket();
        packet.Write(ObjectCommandType.OBJECT_TRANSFORM);
        packet.Write(position);
        packet.Write(rotation);
        return packet;
    }

    public static NetPacket ObjectsInit(ushort[] startingObjectIds)
    {
        NetPacket packet = new NetPacket();
        packet.Write(ObjectCommandType.OBJECTS_INIT);
        packet.Write(startingObjectIds);
        return packet;
    }
}

internal static class LobbyPacketBuilder
{
    public static NetPacket LobbyUsersList(List<UserData> users)
    {
        NetPacket packet = new NetPacket();
        packet.Write(LobbyCommandType.LOBBY_USERS_LIST);
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
        packet.Write(LobbyCommandType.LOBBY_USER_JOINED);
        user.Serialize(packet);
        return packet;
    }

    public static NetPacket LobbyUserLeft(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write(LobbyCommandType.LOBBY_USER_LEFT);
        packet.Write(user.UserId);
        return packet;
    }

    public static NetPacket LobbyTick(ulong tick, bool invokeEvent = false)
    {
        NetPacket packet = new NetPacket();
        packet.Write(LobbyCommandType.LOBBY_TICK);
        packet.Write(tick);
        packet.Write(invokeEvent);
        return packet;
    }
}

internal static class GamePacketBuilder
{
    public static NetPacket GameUserJoined(UserData user)
    {
        NetPacket packet = new NetPacket();
        packet.Write(GameCommandType.GAME_USER_JOINED);
        packet.Write(user.PlayerId);
        return packet;
    }
}