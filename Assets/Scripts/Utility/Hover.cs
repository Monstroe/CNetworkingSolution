using UnityEngine;

public class Hover : MonoBehaviour
{
    public bool IsHovering { get => hover; set => hover = value; }
    public float Amplitude { get => amplitude; set => amplitude = value; }
    public float Frequency { get => frequency; set => frequency = value; }

    [SerializeField] private bool hover = true;
    [SerializeField] private float amplitude, frequency;

    private Vector3 initialPosition;

    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (hover)
        {
            transform.localPosition = initialPosition + new Vector3(0, amplitude * Mathf.Sin(Time.time * frequency), 0);
        }
    }
}
