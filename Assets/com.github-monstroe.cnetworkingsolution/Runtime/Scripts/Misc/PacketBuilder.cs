using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CNetworkingSolution
{
    internal static class ConnectionPacketBuilder
    {
        public static NetPacket ConnectionRequest(ConnectionData connectionData)
        {
            NetPacket packet = new NetPacket();
            packet.Write(ConnectionCommandType.CONNECTION_REQUEST);
            connectionData.Serialize(packet);
            return packet;
        }

        public static NetPacket ConnectionResponse(bool accepted, NetPacket dataPkt = null)
        {
            NetPacket packet = new NetPacket();
            packet.Write(ConnectionCommandType.CONNECTION_RESPONSE);
            packet.Write(accepted);
            if (dataPkt != null && dataPkt.Length > 0)
                packet.Write(dataPkt.ByteArray);
            return packet;
        }

        public static NetPacket ConnectionData(int lobbyId, NetPacket packet)
        {
            packet.Insert(0, lobbyId.ToString());
            return packet;
        }
    }

    internal static class ReservedPacketBuilder
    {
        public static NetPacket Rpc(ulong methodId, MethodInfo method, params object[] args)
        {
            ParameterInfo[] parameters = method.GetParameters().Where(p => p.GetCustomAttribute<RpcSenderAttribute>() == null && p.GetCustomAttribute<RpcIgnoreAttribute>() == null).ToArray();
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
}