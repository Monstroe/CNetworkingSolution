using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class ServerBehaviour : MonoBehaviour
{
    protected ServerLobby lobby;

    public virtual void Init(ServerLobby lobby)
    {
        this.lobby = lobby;
    }

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
                Tuple<int, string> clientPrefabInfo = NetResources.Instance.GetClientPrefabFromServerKey(instance.PrefabKey);
                if (clientPrefabInfo != null)
                {
                    lobby.SendToGame(PacketBuilder.ObjectSpawn(id, clientPrefabInfo.Item1, instance.transform.position, instance.transform.rotation, isPlayer, ownerId), TransportMethod.Reliable);
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
            lobby.SendToGame(PacketBuilder.ObjectDestroy(serverObj.Id), TransportMethod.Reliable);
        }
    }
}
