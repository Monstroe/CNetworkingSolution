using UnityEngine;

public class Rotate : MonoBehaviour
{
    public bool IsRotating { get => rotate; set => rotate = value; }
    public Vector3 Rotation { get => rotation; set => rotation = value; }

    [SerializeField] private bool rotate = true;
    [SerializeField] private Vector3 rotation;

    // Update is called once per frame
    void Update()
    {
        if (rotate)
        {
            transform.Rotate(rotation * Time.deltaTime);
        }
    }
}
