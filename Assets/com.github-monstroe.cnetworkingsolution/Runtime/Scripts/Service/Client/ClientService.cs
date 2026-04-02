using System.Net;
using UnityEditor;
using UnityEngine;

public abstract class ClientService : ClientBehaviour
{
    public ulong ServiceId => serviceId;

    [SerializeField, HideInInspector]
    private ulong serviceId;
    [SerializeField, HideInInspector]
    private string serviceType;

    public override void Init(ClientLobby lobby)
    {
        base.Init(lobby);
        if (lobby.RegisterService(this))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: ClientService {serviceType} registered.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {serviceType} is already registered.");
        }
    }

    public override void Remove()
    {
        if (lobby.UnregisterService(this))
        {
            Debug.Log($"<color=green><b>CNS</b></color>: ClientService {serviceType} unregistered.");
        }
        else
        {
            Debug.LogWarning($"<color=yellow><b>CNS</b></color>: ClientService {serviceType} was not registered.");
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