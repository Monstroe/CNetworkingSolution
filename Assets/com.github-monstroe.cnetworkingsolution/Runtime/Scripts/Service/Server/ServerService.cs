using System;
using UnityEditor;
using UnityEngine;

public abstract class ServerService : ServerBehaviour
{
    public int ExecutionOrder => executionOrder;
    public ulong ServiceId => serviceId;

    [SerializeField, HideInInspector]
    private ulong serviceId;

    [Header("Server Service Settings")]
    [SerializeField] protected int executionOrder = 0;

    public override void Init(ServerLobby lobby)
    {
        base.Init(lobby);
        if (lobby.RegisterService(this, out serviceId))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: ServerService {GetType().Name} registered.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {GetType().Name} is already registered.");
        }
    }

    public override void Remove()
    {
        if (lobby.UnregisterService(this))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: ServerService {GetType().Name} unregistered.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ServerService {GetType().Name} was not registered.");
        }
        base.Remove();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
            return;
        ResetServiceId(GetType());
    }

    internal void ResetServiceId(Type type)
    {
        EditorUtility.SetDirty(this);
        serviceId = ServiceBus.GetServiceId(type);
    }
#endif
}