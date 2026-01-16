using System.Net;
using UnityEngine;

public abstract class ClientTransform : ClientObject
{
    [Header("Movement")]
    [SerializeField] protected float lerpSpeed = 15;

    // Movement
    protected Vector3 receivedPosition;
    protected Quaternion receivedRotation;
    protected Vector3 receivedForward;

    protected bool firstTransformReceived = false;

    public override void Init(ushort id, ClientLobby lobby)
    {
        base.Init(id, lobby);
        receivedPosition = transform.position;
        receivedRotation = transform.rotation;
        receivedForward = transform.forward;
    }

    public override void Remove()
    {
        base.Remove();
    }

    protected override void UpdateOnNonOwner()
    {
        base.UpdateOnNonOwner();
        transform.position = Vector3.Lerp(transform.position, receivedPosition, lerpSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, receivedRotation, lerpSpeed * Time.deltaTime);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.OBJECT_TRANSFORM:
                {
                    receivedPosition = packet.ReadVector3();
                    receivedRotation = packet.ReadQuaternion();
                    receivedForward = packet.ReadVector3();

                    if (!firstTransformReceived)
                    {
                        firstTransformReceived = true;
                        transform.position = receivedPosition;
                        transform.rotation = receivedRotation;
                    }
                    break;
                }
        }
    }
}
