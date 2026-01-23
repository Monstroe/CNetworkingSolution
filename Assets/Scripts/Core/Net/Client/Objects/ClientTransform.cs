using System.Net;
using UnityEngine;

public abstract class ClientTransform : ClientObject
{
    [Header("Movement")]
    [SerializeField] protected float lerpSpeed = 15;

    // Movement
    private Vector3 receivedPosition;
    private Quaternion receivedRotation;

    private bool firstTransformReceived = false;

    public override void Init(ushort id, ClientLobby lobby)
    {
        base.Init(id, lobby);
        receivedPosition = transform.position;
        receivedRotation = transform.rotation;
        lobby.GetService<ObjectClientService>().ClientTransforms.Add(id, this);
    }

    public override void Remove()
    {
        base.Remove();
    }

    protected override void UpdateOnNonOwner()
    {
        base.UpdateOnNonOwner();
        UpdatePosition(receivedPosition);
        UpdateRotation(receivedRotation);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        base.ReceiveData(packet, serviceType, commandType, transportMethod);
        switch (commandType)
        {
            case CommandType.OBJECT_TRANSFORM:
                {
                    receivedPosition = packet.ReadVector3();
                    receivedRotation = packet.ReadQuaternion();

                    if (!firstTransformReceived)
                    {
                        firstTransformReceived = true;
                        InitPosition(receivedPosition);
                        InitRotation(receivedRotation);
                    }
                    break;
                }
        }
    }

    protected virtual void InitPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    protected virtual void InitRotation(Quaternion rot)
    {
        transform.rotation = rot;
    }

    protected virtual void UpdatePosition(Vector3 pos)
    {
        transform.position = Vector3.Lerp(transform.position, pos, lerpSpeed * Time.deltaTime);
    }

    protected virtual void UpdateRotation(Quaternion rot)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, lerpSpeed * Time.deltaTime);
    }
}
