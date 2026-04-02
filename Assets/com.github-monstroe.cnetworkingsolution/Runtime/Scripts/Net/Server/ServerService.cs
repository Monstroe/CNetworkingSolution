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

    public override void Remove()
    {
        if (lobby.UnregisterService(this))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: ServerService {serviceType} unregistered.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {serviceType} was not registered.");
        }
        base.Remove();
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
}