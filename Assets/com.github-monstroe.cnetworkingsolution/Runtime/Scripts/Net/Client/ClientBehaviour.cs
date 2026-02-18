using UnityEngine;

public class ClientBehaviour : MonoBehaviour
{
    protected ClientLobby lobby;

    public virtual void Init(ClientLobby lobby)
    {
        this.lobby = lobby;
    }

    protected void InstantiateOnNetwork(string originalPath, Vector3 position, Quaternion rotation)
    {
        SendObjectSpawnRequest(originalPath, position, rotation);
    }

    protected void InstantiateOnNetwork(GameObject original, Vector3 position, Quaternion rotation)
    {
        original.TryGetComponent(out ClientObject clientObject);
        if (clientObject != null)
        {
            SendObjectSpawnRequest(clientObject.PrefabPath, position, rotation);
        }
        else
        {
            Debug.LogError("ClientBehavior InstantiateOnNetwork could not find ClientObject component on given GameObject.");
        }
    }

    private void SendObjectSpawnRequest(string originalPath, Vector3 position, Quaternion rotation)
    {
        if (NetResources.Instance.GetClientPrefabKeyFromPath(originalPath) == 0)
        {
            Debug.LogError("ClientBehaviour SendObjectSpawnRequest could not find client prefab key for path: " + originalPath);
            return;
        }

        lobby.SendToServer(PacketBuilder.ObjectSpawnRequest(originalPath, position, rotation), TransportMethod.Reliable);
    }

    protected void DestroyOnNetwork(ClientObject clientObj)
    {
        if (clientObj.OwnerId == lobby.CurrentUser.PlayerId)
        {
            lobby.SendToServer(PacketBuilder.ObjectDestroyRequest(clientObj.Id), TransportMethod.Reliable);
        }
        else
        {
            Debug.LogError("ClientBehaviour DestroyOnNetwork attempted to destroy an object not owned by the current user.");
        }
    }
}
