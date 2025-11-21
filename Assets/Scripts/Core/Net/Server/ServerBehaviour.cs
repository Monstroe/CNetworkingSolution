using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class ServerBehaviour : MonoBehaviour
{
    protected ServerLobby lobby;

    protected ServerObject InstantiateOnServer(string originalPath, bool initAndSendToUsers = true, ServerPlayer owner = null)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
        ServerObject instance = InstantiateOnServer(handle, initAndSendToUsers, owner);
        Addressables.Release(handle);
        return instance;
    }

    protected ServerObject InstantiateOnServer(string originalPath, Vector3 position, Quaternion rotation, bool initAndSendToUsers = true, ServerPlayer owner = null)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
        ServerObject instance = InstantiateOnServer(handle, position, rotation, initAndSendToUsers, owner);
        Addressables.Release(handle);
        return instance;
    }

    protected ServerObject InstantiateOnServer(string originalPath, Vector3 position, Quaternion rotation, Transform parent, bool initAndSendToUsers = true, ServerPlayer owner = null)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
        ServerObject instance = InstantiateOnServer(handle, position, rotation, parent, initAndSendToUsers, owner);
        Addressables.Release(handle);
        return instance;
    }

    protected ServerObject InstantiateOnServer(string originalPath, Transform parent, bool initAndSendToUsers = true, ServerPlayer owner = null)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(originalPath).WaitForCompletion();
        ServerObject instance = InstantiateOnServer(handle, parent, initAndSendToUsers, owner);
        Addressables.Release(handle);
        return instance;
    }

    protected ServerObject InstantiateOnServer(GameObject original, bool initAndSendToUsers = true, ServerPlayer owner = null)
    {
        Instantiate(original).TryGetComponent(out ServerObject instance);
        InitInstance(instance, lobby.transform, initAndSendToUsers, owner);
        return instance;
    }

    protected ServerObject InstantiateOnServer(GameObject original, Vector3 position, Quaternion rotation, bool initAndSendToUsers = true, ServerPlayer owner = null)
    {
        Instantiate(original, position, rotation).TryGetComponent(out ServerObject instance);
        InitInstance(instance, lobby.transform, initAndSendToUsers, owner);
        return instance;
    }

    protected ServerObject InstantiateOnServer(GameObject original, Vector3 position, Quaternion rotation, Transform parent, bool initAndSendToUsers = true, ServerPlayer owner = null)
    {
        Instantiate(original, position, rotation).TryGetComponent(out ServerObject instance);
        InitInstance(instance, parent, initAndSendToUsers, owner);
        return instance;
    }

    protected ServerObject InstantiateOnServer(GameObject original, Transform parent, bool initAndSendToUsers = true, ServerPlayer owner = null)
    {
        Instantiate(original).TryGetComponent(out ServerObject instance);
        InitInstance(instance, parent, initAndSendToUsers, owner);
        return instance;
    }

    private void InitInstance(ServerObject instance, Transform parent, bool initAndSendToUsers, ServerPlayer owner)
    {
        if (instance != null)
        {
            SceneManager.MoveGameObjectToScene(instance.gameObject, lobby.LobbyScene);
            instance.transform.SetParent(parent);
            instance.Owner = owner;
            if (initAndSendToUsers)
            {
                instance.Init(lobby.GenerateObjectId(), lobby);
                Tuple<int, string> clientPrefabInfo = NetResources.Instance.GetClientPrefabFromServerKey(instance.PrefabKey);
                if (clientPrefabInfo != null)
                {
                    lobby.SendToGame(PacketBuilder.ObjectSpawn(instance.Id, clientPrefabInfo.Item1, instance.transform.position, instance.transform.rotation, instance.Owner ? (byte?)instance.Owner.Id : null), TransportMethod.Reliable);
                }
            }
        }
    }

    protected void DestroyOnServer(ServerObject serverObj, bool initAndSendToUsers = true)
    {
        serverObj.Remove();
        Destroy(serverObj.gameObject);
        if (initAndSendToUsers)
        {
            lobby.SendToGame(PacketBuilder.ObjectDestroy(serverObj.Id), TransportMethod.Reliable);
        }
    }
}
