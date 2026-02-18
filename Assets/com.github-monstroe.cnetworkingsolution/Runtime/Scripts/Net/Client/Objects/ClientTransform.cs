using UnityEngine;

public abstract class ClientTransform : ClientObject
{
    [Header("CientTransform Settings")]
    [SerializeField] protected float lerpSpeed = 15;
    [Range(0f, 1f)]
    [Tooltip("Speed at which this object syncs it's position and rotation (relative to FixedUpdate)")]
    [SerializeField] private float transformSyncSpeed = 1f;

    private float timer = 0f;

    private Vector3 receivedPosition;
    private Quaternion receivedRotation;

    private Vector3 sentPosition;
    private Quaternion sentRotation;

    private bool firstTransformReceived = false;
    private bool sentCustomTransform = false;

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
        lobby.GetService<ObjectClientService>().ClientTransforms.Remove(Id);
    }

    protected override void FixedUpdateOnOwner()
    {
        base.FixedUpdateOnOwner();
        timer += transformSyncSpeed;
        if (timer >= 1f)
        {
            timer = 0f;
            if (!sentCustomTransform)
            {
                sentPosition = transform.position;
                sentRotation = transform.rotation;
            }
            SendToServerObject(PacketBuilder.ObjectTransform(sentPosition, sentRotation), TransportMethod.Unreliable);
            sentCustomTransform = false;
        }
    }

    protected override void UpdateOnNonOwner()
    {
        base.UpdateOnNonOwner();
        ReceiveTransform(receivedPosition, receivedRotation);
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
                        InitTransform(receivedPosition, receivedRotation);
                    }
                    break;
                }
        }
    }

    protected virtual void InitTransform(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, rot);
    }

    protected virtual void ReceiveTransform(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(Vector3.Lerp(transform.position, pos, lerpSpeed * Time.deltaTime), Quaternion.Slerp(transform.rotation, rot, lerpSpeed * Time.deltaTime));
    }

    public void SendTransformToServerObject(Vector3 pos, Quaternion rot)
    {
        sentPosition = pos;
        sentRotation = rot;
        sentCustomTransform = true;
    }
}
