using System.Net;
using UnityEditor;
using UnityEngine;

public abstract class ServerService : ServerBehaviour
{
    public int ExecutionOrder => executionOrder;
    public ulong ServiceId => serviceId;

    [SerializeField, HideInInspector]
    private ulong serviceId;
    [SerializeField, HideInInspector]
    private string serviceType;

    [Header("Server Service Settings")]
    [SerializeField] protected int executionOrder = 0;

    public override void Init(ServerLobby lobby)
    {
        base.Init(lobby);
        if (lobby.RegisterService(this))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: ServerService {serviceType} registered.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {serviceType} is already registered.");
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        string type = GetType().FullName;
        if (!string.IsNullOrEmpty(type) && serviceType != type)
        {
            ResetServiceId(type);
        }
    }

    public void ResetServiceId(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            type = GetType().FullName;
        }

        serviceType = type;
        EditorUtility.SetDirty(this);
        serviceId = NetResources.GenerateHashKey(type);
    }
#endif

    public abstract void ReceiveData(UserData user, NetPacket packet, CommandType commandType, TransportMethod? transportMethod);
    public abstract void ReceiveDataUnconnected(IPEndPoint ipEndPoint, NetPacket packet, CommandType commandType);
    public abstract void Tick();
    public abstract void UserJoined(UserData joinedUser);
    public abstract void UserJoinedGame(UserData joinedUser);
    public abstract void UserLeft(UserData leftUser);
}