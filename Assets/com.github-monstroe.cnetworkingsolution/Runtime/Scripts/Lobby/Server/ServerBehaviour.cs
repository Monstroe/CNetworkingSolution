using System;
using System.Net;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public abstract class ServerBehaviour : MonoBehaviour, INetEvent, INetRpc
{
    protected ServerLobby lobby;
    protected Type type;

    public virtual void Init(ServerLobby lobby)
    {
        this.lobby = lobby;
        this.type = GetType();
        lobby.RegisterRpcContainer(this);
        lobby.RegisterGameEventListener(this);
    }

    public virtual void Remove()
    {
        lobby.UnregisterRpcContainer(this);
        lobby.UnregisterGameEventListener(this);
    }

    public virtual void ReceiveData(UserData user, NetPacket packet, ushort commandType, TransportMethod? transportMethod)
    {
        if ((ReservedCommandType)commandType == ReservedCommandType.RPC)
        {
            ulong methodId = packet.ReadULong();
            if (lobby.RpcBus.TryGetRpcMethodByInstanceAndId(this, methodId, out MethodInfo method))
            {
                ParameterInfo[] parameters = method.GetParameters();
                object[] args = new object[parameters.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = packet.ReadObject(parameters[i].ParameterType);
                }

                method.Invoke(this, args);
            }
            else
            {
                Debug.LogError($"RPC Method with ID {methodId} not found on ServerBehavior {GetType().Name}.");
            }
            return;
        }
    }

    public virtual void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ushort commandType) { }
    public virtual void Tick() { }
    public virtual void UserJoined(UserData joinedUser) { }
    public virtual void UserJoinedGame(UserData joinedUser) { }
    public virtual void UserLeft(UserData leftUser) { }

    protected ServerObject InstantiateOnServer(string originalPath, byte? ownerId = null, bool initAndSendToUsers = true)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
        ServerObject instance = InstantiateOnServer(handle, ownerId, initAndSendToUsers);
        Addressables.Release(handle);
        return instance;
    }

    protected ServerObject InstantiateOnServer(string originalPath, Vector3 position, Quaternion rotation, byte? ownerId = null, bool initAndSendToUsers = true)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
        ServerObject instance = InstantiateOnServer(handle, position, rotation, ownerId, initAndSendToUsers);
        Addressables.Release(handle);
        return instance;
    }

    protected ServerObject InstantiateOnServer(string originalPath, Vector3 position, Quaternion rotation, Transform parent, byte? ownerId = null, bool initAndSendToUsers = true)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
        ServerObject instance = InstantiateOnServer(handle, position, rotation, parent, ownerId, initAndSendToUsers);
        Addressables.Release(handle);
        return instance;
    }

    protected ServerObject InstantiateOnServer(string originalPath, Transform parent, byte? ownerId = null, bool initAndSendToUsers = true)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
        ServerObject instance = InstantiateOnServer(handle, parent, ownerId, initAndSendToUsers);
        Addressables.Release(handle);
        return instance;
    }

    protected ServerObject InstantiateOnServer(GameObject original, byte? ownerId = null, bool initAndSendToUsers = true)
    {
        Instantiate(original).TryGetComponent(out ServerObject instance);
        InitInstance(instance, lobby.transform, ownerId, false, initAndSendToUsers);
        return instance;
    }

    protected ServerObject InstantiateOnServer(GameObject original, Vector3 position, Quaternion rotation, byte? ownerId = null, bool initAndSendToUsers = true)
    {
        Instantiate(original, position, rotation).TryGetComponent(out ServerObject instance);
        InitInstance(instance, lobby.transform, ownerId, false, initAndSendToUsers);
        return instance;
    }

    protected ServerObject InstantiateOnServer(GameObject original, Vector3 position, Quaternion rotation, Transform parent, byte? ownerId = null, bool initAndSendToUsers = true)
    {
        Instantiate(original, position, rotation).TryGetComponent(out ServerObject instance);
        InitInstance(instance, parent, ownerId, false, initAndSendToUsers);
        return instance;
    }

    protected ServerObject InstantiateOnServer(GameObject original, Transform parent, byte? ownerId = null, bool initAndSendToUsers = true)
    {
        Instantiate(original).TryGetComponent(out ServerObject instance);
        InitInstance(instance, parent, ownerId, false, initAndSendToUsers);
        return instance;
    }

    protected ServerObject InstantiateOnServerAsPlayer(string originalPath, byte ownerId, bool initAndSendToUsers = true)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
        ServerObject instance = InstantiateOnServerAsPlayer(handle, ownerId, initAndSendToUsers);
        Addressables.Release(handle);
        return instance;
    }

    protected ServerObject InstantiateOnServerAsPlayer(string originalPath, Vector3 position, Quaternion rotation, byte ownerId, bool initAndSendToUsers = true)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
        ServerObject instance = InstantiateOnServerAsPlayer(handle, position, rotation, ownerId, initAndSendToUsers);
        Addressables.Release(handle);
        return instance;
    }

    protected ServerObject InstantiateOnServerAsPlayer(string originalPath, Vector3 position, Quaternion rotation, Transform parent, byte ownerId, bool initAndSendToUsers = true)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
        ServerObject instance = InstantiateOnServerAsPlayer(handle, position, rotation, parent, ownerId, initAndSendToUsers);
        Addressables.Release(handle);
        return instance;
    }

    protected ServerObject InstantiateOnServerAsPlayer(string originalPath, Transform parent, byte ownerId, bool initAndSendToUsers = true)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
        ServerObject instance = InstantiateOnServerAsPlayer(handle, parent, ownerId, initAndSendToUsers);
        Addressables.Release(handle);
        return instance;
    }

    protected ServerObject InstantiateOnServerAsPlayer(GameObject original, byte ownerId, bool initAndSendToUsers = true)
    {
        Instantiate(original).TryGetComponent(out ServerObject instance);
        InitInstance(instance, lobby.transform, ownerId, true, initAndSendToUsers);
        return instance;
    }

    protected ServerObject InstantiateOnServerAsPlayer(GameObject original, Vector3 position, Quaternion rotation, byte ownerId, bool initAndSendToUsers = true)
    {
        Instantiate(original, position, rotation).TryGetComponent(out ServerObject instance);
        InitInstance(instance, lobby.transform, ownerId, true, initAndSendToUsers);
        return instance;
    }

    protected ServerObject InstantiateOnServerAsPlayer(GameObject original, Vector3 position, Quaternion rotation, Transform parent, byte ownerId, bool initAndSendToUsers = true)
    {
        Instantiate(original, position, rotation).TryGetComponent(out ServerObject instance);
        InitInstance(instance, parent, ownerId, true, initAndSendToUsers);
        return instance;
    }

    protected ServerObject InstantiateOnServerAsPlayer(GameObject original, Transform parent, byte ownerId, bool initAndSendToUsers = true)
    {
        Instantiate(original).TryGetComponent(out ServerObject instance);
        InitInstance(instance, parent, ownerId, true, initAndSendToUsers);
        return instance;
    }

    private void InitInstance(ServerObject instance, Transform parent, byte? ownerId, bool isPlayer, bool initAndSendToUsers)
    {
        if (instance != null)
        {
            if (lobby.LobbyScene.HasValue)
            {
                SceneManager.MoveGameObjectToScene(instance.gameObject, lobby.LobbyScene.Value);
            }
            instance.transform.SetParent(parent);

            instance.SetOwner(ownerId);
            instance.SetAsPlayer(isPlayer);

            if (initAndSendToUsers)
            {
                ushort id = lobby.GenerateObjectId();
                Tuple<ulong, string> clientPrefabInfo = NetResources.Instance.GetClientPrefabFromServerKey(instance.PrefabKey);
                if (clientPrefabInfo != null)
                {
                    lobby.SendToGame<ObjectServerService>(ObjectPacketBuilder.ObjectSpawn(id, clientPrefabInfo.Item1, instance.transform.position, instance.transform.rotation, isPlayer, ownerId), TransportMethod.Reliable);
                }
                instance.Init(id, lobby);
            }
        }
    }

    protected void DestroyOnServer(ServerObject serverObj, bool sendToUsers = true)
    {
        serverObj.Remove();
        Destroy(serverObj.gameObject);
        if (sendToUsers)
        {
            lobby.SendToGame<ObjectServerService>(ObjectPacketBuilder.ObjectDestroy(serverObj.Id), TransportMethod.Reliable);
        }
    }
}
