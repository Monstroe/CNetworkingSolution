using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Monstroe.CNetworkingSolution
{
    public class ServerLobby : MonoBehaviour
    {
        public LobbyData LobbyData { get; private set; } = new LobbyData();
        public ulong ServerTick { get; private set; } = 0;
        public Scene? LobbyScene { get; private set; }
        public PhysicsScene? PhysicsScene { get; private set; }

        private ITransportUtility transportUtility;
        private readonly ServerServiceUtility services = new ServerServiceUtility();
        private readonly ServerServiceUtility unconnectedServices = new ServerServiceUtility();

        internal RpcBus RpcBus { get; } = new RpcBus();
        private readonly GameEventBus gameEventBus = new GameEventBus();

        internal void Init(ITransportUtility transportUtility, Scene? scene = null)
        {
            this.transportUtility = transportUtility;

            LobbyScene = scene;
            PhysicsScene = scene?.GetPhysicsScene();

            foreach (var service in this.GetComponentsInChildren<ServerService>())
            {
                service.Init(this);
            }
        }

        internal void ReceiveData(UserData user, NetPacket packet, TransportMethod? transportMethod)
        {
            ulong serviceId = packet.ReadULong();
            ushort commandType = packet.ReadUShort();
            if (services.GetService(serviceId, out ServerService service))
            {
                service.ReceiveData(user, packet, commandType, transportMethod);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No service found for id {serviceId}.");
            }
        }

        internal void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet)
        {
            ulong serviceId = packet.ReadULong();
            ushort commandType = packet.ReadUShort();
            if (unconnectedServices.GetService(serviceId, out ServerService unconnectedService))
            {
                unconnectedService.ReceiveDataUnconnected(ipEndPoint, packet, commandType);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No unconnected service found for id {serviceId}.");
            }
        }

        internal void EarlyUserJoined(UserData user)
        {
            user.PlayerId = GeneratePlayerId();
            user.InLobby = true;
            services.EarlyUserJoined(user);
        }

        internal void UserJoined(UserData user)
        {
            services.UserJoined(user);
        }

        internal void LateUserJoined(UserData user)
        {
            services.LateUserJoined(user);
        }

        internal void EarlyUserJoinedGame(UserData user)
        {
            user.InGame = true;
            services.EarlyUserJoinedGame(user);
        }

        internal void UserJoinedGame(UserData user)
        {
            services.UserJoinedGame(user);
        }

        internal void LateUserJoinedGame(UserData user)
        {
            services.LateUserJoinedGame(user);
        }

        internal void EarlyUserLeftGame(UserData user)
        {
            user.InGame = false;
            services.EarlyUserLeftGame(user);
        }

        internal void UserLeftGame(UserData user)
        {
            services.UserLeftGame(user);
        }

        internal void LateUserLeftGame(UserData user)
        {
            services.LateUserLeftGame(user);
        }

        internal void EarlyUserLeft(UserData user)
        {
            user.InLobby = false;
            services.EarlyUserLeft(user);
        }

        internal void UserLeft(UserData user)
        {
            services.UserLeft(user);
        }

        internal void LateUserLeft(UserData user)
        {
            services.LateUserLeft(user);
        }

        internal void Tick()
        {
            if (PhysicsScene.HasValue)
            {
                PhysicsScene.Value.Simulate(Time.fixedDeltaTime);
            }
            services.Tick();
            ServerTick++;
        }

        internal void SendToLobby(ulong serviceId, NetPacket packet, TransportMethod method, UserData exception = null)
        {
            SendToUsers(LobbyData.LobbyUsers.Where(u => u != exception).ToList(), serviceId, packet, method);
        }

        internal void SendToGame(ulong serviceId, NetPacket packet, TransportMethod method, UserData exception = null)
        {
            SendToUsers(LobbyData.GameUsers.Where(u => u != exception).ToList(), serviceId, packet, method);
        }

        internal void SendToUser(UserData user, ulong serviceId, NetPacket packet, TransportMethod method)
        {
            if (packet != null)
            {
                packet.Insert(0, serviceId);
                transportUtility.SendToRemote(user.UserId, packet, method);
            }
        }

        internal void SendToUsers(List<UserData> users, ulong serviceId, NetPacket packet, TransportMethod method)
        {
            if (packet != null)
            {
                packet.Insert(0, serviceId);
                transportUtility.SendToRemotes(users.ConvertAll(user => user.UserId), packet, method);
            }
        }

        internal void SendToUnconnected(IPEndPoint iPEndPoint, ulong serviceId, NetPacket packet)
        {
            if (packet != null)
            {
                packet.Insert(0, serviceId);
                transportUtility.SendToUnconnectedRemote(iPEndPoint, packet);
            }
        }

        internal void SendToUnconnected(List<IPEndPoint> iPEndPoints, ulong serviceId, NetPacket packet)
        {
            if (packet != null)
            {
                packet.Insert(0, serviceId);
                transportUtility.SendToUnconnectedRemotes(iPEndPoints, packet);
            }
        }

        internal void BroadcastToUnconnected(ulong serviceId, NetPacket packet)
        {
            if (packet != null)
            {
                packet.Insert(0, serviceId);
                transportUtility.BroadcastToUnconnectedRemotes(packet);
            }
        }

        public void SendToLobby<T>(NetPacket packet, TransportMethod method, UserData exception = null) where T : ServerService
        {
            if (services.TryGetServiceId<T>(out ulong serviceId))
            {
                SendToLobby(serviceId, packet, method, exception);
            }
        }

        public void SendToGame<T>(NetPacket packet, TransportMethod method, UserData exception = null) where T : ServerService
        {
            if (services.TryGetServiceId<T>(out ulong serviceId))
            {
                SendToGame(serviceId, packet, method, exception);
            }
        }

        public void SendToUser<T>(UserData user, NetPacket packet, TransportMethod method) where T : ServerService
        {
            if (services.TryGetServiceId<T>(out ulong serviceId))
            {
                SendToUser(user, serviceId, packet, method);
            }
        }

        public void SendToUnconnected<T>(IPEndPoint iPEndPoint, NetPacket packet) where T : ServerService
        {
            if (unconnectedServices.TryGetServiceId<T>(out ulong serviceId))
            {
                SendToUnconnected(iPEndPoint, serviceId, packet);
            }
        }

        public void SendToUnconnected<T>(List<IPEndPoint> iPEndPoints, NetPacket packet) where T : ServerService
        {
            if (unconnectedServices.TryGetServiceId<T>(out ulong serviceId))
            {
                SendToUnconnected(iPEndPoints, serviceId, packet);
            }
        }

        public void BroadcastToUnconnected<T>(NetPacket packet) where T : ServerService
        {
            if (unconnectedServices.TryGetServiceId<T>(out ulong serviceId))
            {
                BroadcastToUnconnected(serviceId, packet);
            }
        }

        public void KickUser(UserData user)
        {
            transportUtility.KickRemote(user.UserId);
        }

        public void CloseLobby()
        {
            foreach (var user in LobbyData.LobbyUsers.ToList())
            {
                transportUtility.KickRemote(user.UserId);
            }
        }

        public bool RegisterService<T>(T service, out ulong serviceId) where T : ServerService
        {
            return services.RegisterService(service, out serviceId);
        }

        public bool UnregisterService(ServerService service)
        {
            return services.UnregisterService(service.ServiceId);
        }

        public T GetService<T>() where T : ServerService
        {
            ServerService service = services.GetService<T>(out ulong serviceId);
            if (service != null)
            {
                return (T)service;
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService with id {serviceId} not found.");
                return null;
            }
        }

        public bool RegisterUnconnectedService<T>(T service, out ulong serviceId) where T : ServerService
        {
            return unconnectedServices.RegisterService(service, out serviceId);
        }

        public bool UnregisterUnconnectedService(ServerService service)
        {
            return unconnectedServices.UnregisterService(service.ServiceId);
        }

        public T GetUnconnectedService<T>() where T : ServerService
        {
            ServerService service = unconnectedServices.GetService<T>(out ulong serviceId);
            if (service != null)
            {
                return (T)service;
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>CNS</b></color>: Unconnected ServerService with id {serviceId} not found.");
                return null;
            }
        }

        public void RegisterRpcContainer(INetRpc rpcContainer)
        {
            RpcBus.RegisterRpcContainer(rpcContainer);
        }

        public void UnregisterRpcContainer(INetRpc rpcContainer)
        {
            RpcBus.UnregisterRpcContainer(rpcContainer);
        }

        public void RegisterGameEventListener(INetEvent listener)
        {
            gameEventBus.RegisterListener(listener);
        }

        public void UnregisterGameEventListener(INetEvent listener)
        {
            gameEventBus.UnregisterListener(listener);
        }

        public async Task<GameEventResult> TriggerGameEvent(GameEvent e)
        {
            return await gameEventBus.Fire(e);
        }

        internal byte GeneratePlayerId()
        {
            byte newPlayerId;
            do
            {
                newPlayerId = (byte)UnityEngine.Random.Range(0, byte.MaxValue);
            } while (LobbyData.LobbyUsers.Any(u => u.PlayerId == newPlayerId));
            return newPlayerId;
        }

        internal ushort GenerateObjectId()
        {
            ushort newObjectId;
            do
            {
                newObjectId = (ushort)UnityEngine.Random.Range(byte.MaxValue, ushort.MaxValue);
            } while (GetService<ObjectServerService>().ServerObjects.ContainsKey(newObjectId));
            return newObjectId;
        }
    }
}