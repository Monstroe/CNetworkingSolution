using UnityEngine;

public class OtherPlayer : ClientPlayer
{
    [SerializeField] private float lerpSpeed = 15;

    // Movement
    private Vector3 position;
    private Quaternion rotation;
    private Vector3 forward;

    private bool firstTransformReceived = false;

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, position, lerpSpeed * Time.deltaTime);
        //transform.rotation = Quaternion.Lerp(transform.rotation, rotation, lerpSpeed * Time.deltaTime);
        transform.forward = Vector3.Lerp(transform.forward, Vector3.ProjectOnPlane(forward, Vector3.up), lerpSpeed * Time.deltaTime);
    }

    public override void ReceiveData(NetPacket packet, ServiceType serviceType, CommandType commandType, TransportMethod? transportMethod)
    {
        switch (commandType)
        {
            case CommandType.PLAYER_TRANSFORM:
                {
                    position = packet.ReadVector3();
                    rotation = packet.ReadQuaternion();
                    forward = packet.ReadVector3();

                    if (!firstTransformReceived)
                    {
                        firstTransformReceived = true;
                        transform.position = position;
                        //transform.rotation = rotation;
                        transform.forward = Vector3.ProjectOnPlane(forward, Vector3.up);
                    }
                    break;
                }
            case CommandType.PLAYER_ANIM:
                {
                    IsWalking = packet.ReadBool();
                    IsSprinting = packet.ReadBool();
                    IsCrouching = packet.ReadBool();
                    IsGrounded = packet.ReadBool();
                    Jumped = packet.ReadBool();
                    Grabbed = packet.ReadBool();
                    break;
                }
            case CommandType.PLAYER_DESTROY:
                {
                    ClientManager.Instance.CurrentLobby.GameData.ClientPlayers.Remove(User);
                    ClientManager.Instance.CurrentLobby.GameData.ClientObjects.Remove(Id);
                    Destroy(gameObject);
                    break;
                }
        }
    }
}
