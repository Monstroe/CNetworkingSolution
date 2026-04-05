using System;
using UnityEngine;
using UnityEngine.Events;

public class Expand : MonoBehaviour, IDisplay
{
    public float ExpandTime { get => expandTime; set => expandTime = value; }

    [SerializeField] private bool startVisible = false;
    [SerializeField] private float expandTime = 0.25f;

    private UnityAction currentCallback;

    private Vector3 highestScale;
    private Vector3 startingScale;
    private Vector3 finalScale;

    private float timer = 0f;
    private bool expand = false;

    void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        highestScale = transform.localScale;

        if (startVisible)
        {
            startingScale = highestScale;
            finalScale = Vector3.zero;
        }
        else
        {
            startingScale = Vector3.zero;
            finalScale = highestScale;
        }

        transform.localScale = startingScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (expand)
        {
            ExpandLerp();
        }
    }

    private void ExpandLerp()
    {
        timer += Time.deltaTime;

        transform.localScale = Vector3.Lerp(startingScale, finalScale, timer / expandTime);

        if (timer >= expandTime)
        {
            currentCallback?.Invoke();
            currentCallback = null;

            timer = 0f;
            expand = false;
        }
    }

    public void Display(bool display, UnityAction callback = null)
    {
        if (expand)
        {
            throw new Exception("Object is already expanding.");
        }

        if (display)
            Activate();
        else
            Deactivate();

        currentCallback = callback;
        expand = true;
    }

    private void Activate()
    {
        startingScale = Vector3.zero;
        finalScale = highestScale;
    }

    private void Deactivate()
    {
        startingScale = highestScale;
        finalScale = Vector3.zero;
    }

    public void Reset()
    {
        Initialize();
    }
}
