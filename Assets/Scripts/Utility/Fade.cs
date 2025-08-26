using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Fade : MonoBehaviour, IDisplay
{
    class RendererData
    {
        public Renderer ObjRenderer { get; set; }
        public float HighestAlpha { get; set; }
        public float StartingAlpha { get; set; }
        public float FinalAlpha { get; set; }
        public float CurrentAlpha { get; set; }
    }

    public bool FadeChildren { get => fadeChildren; set => fadeChildren = value; }
    public float FadeTime { get => fadeTime; set => fadeTime = value; }

    [SerializeField] private bool startVisible = false;
    [SerializeField] private bool fadeChildren = true;
    [SerializeField] private float fadeTime = 0.25f;

    private UnityAction currentCallback;
    private List<RendererData> renderers;

    private float timer = 0f;
    private bool fade = false;

    void Awake()
    {
        renderers = new List<RendererData>();
        Initialize();
    }

    public void Initialize()
    {
        renderers.Clear();
        if (fadeChildren)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(false))
            {
                SetupRenderer(r);
            }
        }
        else
        {
            SetupRenderer(GetComponent<Renderer>());
        }
    }

    private void SetupRenderer(Renderer renderer)
    {
        if (renderer != null)
        {
            RendererData d = new RendererData();
            d.ObjRenderer = renderer;
            d.HighestAlpha = renderer.material.color.a * 255f;

            if (startVisible)
            {
                d.StartingAlpha = d.HighestAlpha / 255f;
                d.FinalAlpha = 0f;
            }
            else
            {
                d.StartingAlpha = 0f;
                d.FinalAlpha = d.HighestAlpha / 255f;
            }

            renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, d.StartingAlpha);
            renderers.Add(d);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (fade)
        {
            FadeLerp();
        }
    }

    private void FadeLerp()
    {
        timer += Time.deltaTime;

        foreach (RendererData d in renderers)
        {
            d.CurrentAlpha = Mathf.Lerp(d.StartingAlpha, d.FinalAlpha, timer / fadeTime);
            d.ObjRenderer.material.color = new Color(d.ObjRenderer.material.color.r, d.ObjRenderer.material.color.g, d.ObjRenderer.material.color.b, d.CurrentAlpha);
        }

        if (timer >= fadeTime)
        {
            currentCallback?.Invoke();
            currentCallback = null;

            timer = 0f;
            fade = false;
        }
    }

    public void Display(bool display, UnityAction callback = null)
    {
        if (fade)
        {
            throw new Exception("Object is already fading.");
        }

        if (display)
            Activate();
        else
            Deactivate();

        currentCallback = callback;
        fade = true;
    }

    private void Activate()
    {
        foreach (RendererData d in renderers)
        {
            d.StartingAlpha = 0f;
            d.FinalAlpha = d.HighestAlpha / 255f;
        }
    }

    private void Deactivate()
    {
        foreach (RendererData d in renderers)
        {
            d.StartingAlpha = d.HighestAlpha / 255f;
            d.FinalAlpha = 0f;
        }
    }

    public void Reset()
    {
        Initialize();
    }
}
