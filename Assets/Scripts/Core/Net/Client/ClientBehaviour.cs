using UnityEngine;

public class ClientBehaviour : MonoBehaviour
{
    protected ClientLobby lobby;

    protected void InstantiateOnNetwork(string originalPath, Vector3 position, Quaternion rotation, bool setThisPlayerAsOwner = true)
    {
        SendObjectSpawnRequest(originalPath, position, rotation, setThisPlayerAsOwner);
    }

    protected void InstantiateOnNetwork(GameObject original, Vector3 position, Quaternion rotation, bool setThisPlayerAsOwner = true)
    {
        original.TryGetComponent(out ClientObject clientObject);
        if (clientObject != null)
        {
            SendObjectSpawnRequest(clientObject.PrefabPath, position, rotation, setThisPlayerAsOwner);
        }
        else
        {
            Debug.LogError("ClientBehavior InstantiateOnNetwork could not find ClientObject component on given GameObject.");
        }
    }

    private void SendObjectSpawnRequest(string originalPath, Vector3 position, Quaternion rotation, bool setThisPlayerAsOwner = true)
    {
        if (NetResources.Instance.GetClientPrefabKeyFromPath(originalPath) == 0)
        {
            Debug.LogError("ClientBehaviour SendObjectSpawnRequest could not find client prefab key for path: " + originalPath);
            return;
        }

        lobby.SendToServer(PacketBuilder.ObjectSpawnRequest(originalPath, position, rotation, setThisPlayerAsOwner), TransportMethod.Reliable);
    }

    protected void DestroyOnNetwork(ClientObject clientObj)
    {
        lobby.SendToServer(PacketBuilder.ObjectDestroyRequest(clientObj.Id), TransportMethod.Reliable);
    }
}
