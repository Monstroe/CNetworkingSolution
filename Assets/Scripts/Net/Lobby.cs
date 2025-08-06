using System.Collections.Generic;
using UnityEngine;

public abstract class Lobby : MonoBehaviour
{
    public LobbyData LobbyData { get; protected set; } = new LobbyData();
    protected NetTransport transport;

    public virtual void Init(int lobbyId, NetTransport transport)
    {
        LobbyData.LobbyId = lobbyId;
        this.transport = transport;
    }

    public virtual void Tick() { }
}

public enum LobbyVisibility
{
    PUBLIC,
    PRIVATE
}
