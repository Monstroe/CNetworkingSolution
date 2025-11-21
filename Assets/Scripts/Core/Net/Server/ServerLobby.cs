using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerLobby : MonoBehaviour
{
    public LobbyData LobbyData { get; private set; } = new LobbyData();
    public ServerGameData GameData { get; private set; } = new ServerGameData();
    //public EventManager EventManager { get; private set; }
    public Scene LobbyScene { get; private set; }
    //public Map Map { get; private set; }

    private Dictionary<ServiceType, ServerService> services = new Dictionary<ServiceType, ServerService>();
    private Dictionary<Type, ServiceType> serviceTypeCache = new Dictionary<Type, ServiceType>();
    private SortedDictionary<int, List<ServerService>> serviceOrderCache = new SortedDictionary<int, List<ServerService>>();

    private PhysicsScene physicsScene;

    public void Init(int lobbyId, Scene scene)
    {
        LobbyScene = scene;
        physicsScene = scene.GetPhysicsScene();
        LobbyData.LobbyId = lobbyId;

        // Init Server Services (ADD NEW SERVICES HERE)
        //services.Add(ServiceType.GAME, new GameObject("GameServerService").AddComponent<GameServerService>());
        //services.Add(ServiceType.PLAYER, new GameObject("PlayerServerService").AddComponent<PlayerServerService>());
        //services.Add(ServiceType.FX, new GameObject("FXServerService").AddComponent<FXServerService>());
        //services.Add(ServiceType.EVENT, new GameObject("EventServerService").AddComponent<EventServerService>());
        //services.Add(ServiceType.CHAT, new GameObject("ChatServerService").AddComponent<ChatServerService>());
        // The object server service is special because it handles all networked object communication
        // Server services should run first (with the exception of the lobby service), then server objects
        // Therefore THIS SERVER SERVICE SHOULD ALWAYS BE ADDED LAST, DON'T ADD ANYTHING AFTER THIS
        //services.Add(ServiceType.OBJECT, new GameObject("ObjectServerService").AddComponent<ObjectServerService>());
        // The lobby service is also special because it handles lobby and user management
        // It needs to run last because the clients shouldn't clean up their UserData until all other services have processed the user leaving
        //services.Add(ServiceType.LOBBY, new GameObject("LobbyServerService").AddComponent<LobbyServerService>());

        foreach (var service in this.GetComponentsInChildren<ServerService>())
        {
            service.Init(this);
            //service.transform.SetParent(this.transform);
        }

        // Init Event Manager
        //EventManager = new GameObject("EventManager").AddComponent<EventManager>();
        //EventManager.transform.SetParent(this.transform);

        // Init Map
        /*Map = Instantiate(NetResources.Instance.MapPrefab.gameObject, Vector3.zero, Quaternion.identity).GetComponent<Map>();
        Map.transform.SetParent(this.transform);
        foreach (Renderer r in Map.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
        foreach (ClientObject obj in Map.GetComponentsInChildren<ClientObject>(true))
        {
            obj.gameObject.TryGetComponent(out Collider objCollider);
            if (objCollider != null)
                objCollider.enabled = false;
            obj.enabled = false;
        }*/
    }

    public void SendToLobby(NetPacket packet, TransportMethod method, UserData exception = null)
    {
        ServerManager.Instance.SendToUsers(LobbyData.LobbyUsers.Where(u => u != exception).ToList(), packet, method);
    }

    public void SendToGame(NetPacket packet, TransportMethod method, UserData exception = null)
    {
        ServerManager.Instance.SendToUsers(LobbyData.GameUsers.Where(u => u != exception).ToList(), packet, method);
    }

    public void SendToUser(UserData user, NetPacket packet, TransportMethod method)
    {
        ServerManager.Instance.SendToUser(user, packet, method);
    }

    public void ReceiveData(UserData user, NetPacket packet, TransportMethod? transportMethod)
    {
        ServiceType serviceType = (ServiceType)packet.ReadByte();
        CommandType commandType = (CommandType)packet.ReadByte();

        if (services.TryGetValue(serviceType, out ServerService service))
        {
            service.ReceiveData(user, packet, serviceType, commandType, transportMethod);
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: No service found for type {serviceType}. Command {commandType} will not be processed.");
        }
    }

    public void UserJoined(UserData user)
    {
        user.PlayerId = GeneratePlayerId();

        foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
        {
            foreach (ServerService service in serviceList)
            {
                service.UserJoined(user);
            }
        }
    }

    public void UserJoinedGame(UserData user)
    {
        user.InGame = true;
        SendToLobby(PacketBuilder.GameUserJoined(user), TransportMethod.Reliable);

        foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
        {
            foreach (ServerService service in serviceList)
            {
                service.UserJoinedGame(user);
            }
        }
    }

    public void UserLeft(UserData user)
    {
        foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
        {
            foreach (ServerService service in serviceList)
            {
                service.UserLeft(user);
            }
        }
    }

    public void Tick()
    {
        physicsScene.Simulate(Time.fixedDeltaTime);

        foreach ((int order, List<ServerService> serviceList) in serviceOrderCache)
        {
            foreach (ServerService service in serviceList)
            {
                service.Tick();
            }
        }
    }

    public void RegisterService<T>(T service) where T : ServerService
    {
        ServiceType serviceType = service.ServiceType;
        int executionOrder = service.ExecutionOrder;
        if (!services.ContainsKey(serviceType))
        {
            services[serviceType] = service;
            serviceTypeCache[service.GetType()] = serviceType;

            if (!serviceOrderCache.TryGetValue(executionOrder, out List<ServerService> list))
            {
                list = new List<ServerService>();
                serviceOrderCache[executionOrder] = list;
            }
            list.Add(service);

            Debug.Log($"<color=green><b>CNS</b></color>: Registered ServerService {serviceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {serviceType} is already registered.");
        }
    }

    public void UnregisterService<T>()
    {
        ServiceType serviceType = serviceTypeCache[typeof(T)];
        if (services.ContainsKey(serviceType))
        {
            services.Remove(serviceType);
            Debug.Log($"<color=green><b>CNS</b></color>: Unregistered ServerService {serviceType}.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {serviceType} is not registered.");
        }
    }

    public T GetService<T>() where T : ServerService
    {
        if (serviceTypeCache.TryGetValue(typeof(T), out ServiceType serviceType) && services.TryGetValue(serviceType, out ServerService service))
        {
            return (T)service;
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {serviceType} not found.");
            return null;
        }
    }

    public byte GeneratePlayerId()
    {
        byte newPlayerId;
        do
        {
            newPlayerId = (byte)UnityEngine.Random.Range(0, byte.MaxValue);
        } while (LobbyData.LobbyUsers.Any(u => u.PlayerId == newPlayerId));
        return newPlayerId;
    }

    public ushort GenerateObjectId()
    {
        ushort newObjectId;
        do
        {
            newObjectId = (ushort)UnityEngine.Random.Range(byte.MaxValue, ushort.MaxValue);
        } while (GetService<ObjectServerService>().ServerObjects.ContainsKey(newObjectId));
        return newObjectId;
    }
}
