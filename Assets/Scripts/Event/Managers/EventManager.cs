using UnityEngine;
using UnityEngine.Events;

public class EventManager : MonoBehaviour
{
    [HideInInspector] public UnityEvent<GroundHitArgs> OnGroundHit = new UnityEvent<GroundHitArgs>();
    // ADD MORE EVENTS HERE
}

public class GroundHitArgs : INetSerializable<GroundHitArgs>
{
    public Vector3 position;
    public Quaternion rotation;

    public GroundHitArgs Deserialize(NetPacket packet)
    {
        return new GroundHitArgs()
        {
            position = packet.ReadVector3(),
            rotation = packet.ReadQuaternion()
        };
    }

    public void Serialize(NetPacket packet)
    {
        packet.Write(position);
        packet.Write(rotation);
    }
}

// ADD MORE ARGS HERE
