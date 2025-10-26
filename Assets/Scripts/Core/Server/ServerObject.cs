using UnityEditor;
using UnityEngine;

public abstract class ServerObject : ServerBehaviour, NetObject
{
    public ushort Id { get; private set; }

    public ServerPlayer Owner { get; set; } = null;

    public int PrefabKey => prefabKey;
    public string PrefabPath => prefabPath;

    [SerializeField, HideInInspector]
    private int prefabKey;
    [SerializeField, HideInInspector]
    private string prefabPath;

    public virtual void Init(ushort id, ServerLobby lobby)
    {
        this.Id = id;
        this.lobby = lobby;
        lobby.GameData.ServerObjects.Add(id, this);
    }

    public virtual void Remove()
    {
        lobby.GameData.ServerObjects.Remove(Id);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
        if (!string.IsNullOrEmpty(path) && prefabPath != path)
        {
            ResetPrefabKeyAndPath(path);
        }
    }

    public void ResetPrefabKeyAndPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
        }

        prefabPath = path;
        EditorUtility.SetDirty(this);
        prefabKey = NetResources.HashPathToId(prefabPath);
    }
#endif

    public abstract void ReceiveData(UserData user, NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod);
    public abstract void Tick();
    public abstract void UserJoined(UserData joinedUser);
    public abstract void UserJoinedGame(UserData joinedUser);
    public abstract void UserLeft(UserData leftUser);

    protected void SendToGameClientObject(NetPacket packet, TransportMethod transportMethod, UserData exception = null)
    {
        lobby.SendToGame(PacketBuilder.ObjectCommunication(this, packet), transportMethod, exception);
    }

    protected void SendToUserClientObject(UserData user, NetPacket packet, TransportMethod transportMethod)
    {
        lobby.SendToUser(user, PacketBuilder.ObjectCommunication(this, packet), transportMethod);
    }
}