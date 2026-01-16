using System.Net;
using UnityEditor;
using UnityEngine;

public abstract class ClientObject : ClientBehaviour, INetObject
{
    public ushort Id { get; private set; }

    public ClientPlayer Owner { get; set; } = null;

    public int PrefabKey => prefabKey;
    public string PrefabPath => prefabPath;

    [SerializeField, HideInInspector]
    private int prefabKey;
    [SerializeField, HideInInspector]
    private string prefabPath;

    public ServerObject ServerPrefab => serverPrefab;

    [Header("Server Prefab")]
    [Tooltip("Reference to the corresponding server prefab for this client object.")]
    [SerializeField] private ServerObject serverPrefab;

    public virtual void Init(ushort id, ClientLobby lobby)
    {
        Id = id;
        this.lobby = lobby;
        lobby.GetService<ObjectClientService>().ClientObjects.Add(id, this);
    }

    public virtual void Remove()
    {
        lobby.GetService<ObjectClientService>().ClientObjects.Remove(Id);
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

    protected virtual void AwakeOnOwner() { }
    protected virtual void AwakeOnNonOwner() { }
    protected virtual void Awake()
    {
        if (Owner == Player.Instance)
        {
            AwakeOnOwner();
        }
        else
        {
            AwakeOnNonOwner();
        }
    }

    protected virtual void StartOnOwner() { }
    protected virtual void StartOnNonOwner() { }
    protected virtual void Start()
    {
        if (Owner == Player.Instance)
        {
            StartOnOwner();
        }
        else
        {
            StartOnNonOwner();
        }
    }

    protected virtual void UpdateOnOwner() { }
    protected virtual void UpdateOnNonOwner() { }
    protected virtual void Update()
    {
        if (Owner == Player.Instance)
        {
            UpdateOnOwner();
        }
        else
        {
            UpdateOnNonOwner();
        }
    }

    protected virtual void FixedUpdateOnOwner() { }
    protected virtual void FixedUpdateOnNonOwner() { }
    protected virtual void FixedUpdate()
    {
        if (Owner == Player.Instance)
        {
            FixedUpdateOnOwner();
        }
        else
        {
            FixedUpdateOnNonOwner();
        }
    }

    public abstract void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod);
    public abstract void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, ServiceType serviceType, CommandType commandType);

    public void SendToServerObject(NetPacket packet, TransportMethod transportMethod)
    {
        lobby.SendToServer(PacketBuilder.ObjectCommunication(this, packet), transportMethod);
    }
}
