using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FadeUI : MonoBehaviour, IDisplay
{
    public bool FadeChildren { get => fadeChildren; set => fadeChildren = value; }
    public float FadeTime { get => fadeTime; set => fadeTime = value; }

    [SerializeField] private bool startVisible = false;
    [SerializeField] private bool fadeChildren = true;
    [SerializeField] private float fadeTime = 0.25f;

    private List<MaskableGraphic> graphics;

    private bool fade = false;

    protected virtual void Awake()
    {
        graphics = new List<MaskableGraphic>();
        Initialize();
    }

    public void Initialize()
    {
        graphics.Clear();
        if (fadeChildren)
        {
            foreach (MaskableGraphic graphic in GetComponentsInChildren<MaskableGraphic>(false))
            {
                SetupGraphic(graphic);
            }
        }
        else
        {
            SetupGraphic(GetComponent<MaskableGraphic>());
        }
    }

    private void SetupGraphic(MaskableGraphic graphic)
    {
        if (graphic != null)
        {
            graphics.Add(graphic);

            if (!startVisible)
            {
                graphic.CrossFadeAlpha(0f, 0f, true);
            }
        }
    }

    public void Display(bool display, UnityAction callback = null)
    {
        if (fade)
        {
            throw new Exception("Object is already fading.");
        }

        if (display)
            StartCoroutine(Activate(callback));
        else
            StartCoroutine(Deactivate(callback));

        fade = true;
    }

    private IEnumerator Fade(bool fadeIn)
    {
        foreach (MaskableGraphic graphic in graphics)
        {
            graphic.CrossFadeAlpha(fadeIn ? 1f : 0f, fadeTime, false);
        }

        yield return new WaitForSeconds(fadeTime);
        fade = false;
    }

    private IEnumerator Activate(UnityAction callback = null)
    {
        yield return Fade(true);
        callback?.Invoke();
    }

    private IEnumerator Deactivate(UnityAction callback = null)
    {
        yield return Fade(false);
        callback?.Invoke();
    }

    public void Reset()
    {
        Initialize();
    }
}
