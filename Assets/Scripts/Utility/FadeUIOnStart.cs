using System.Collections;
using UnityEngine;

public class FadeUIOnStart : FadeUI
{
    public float FadeInDelay { get => fadeInDelay; set => fadeInDelay = value; }
    public float FadeOutDelay { get => fadeOutDelay; set => fadeOutDelay = value; }
    [SerializeField] private float fadeInDelay = 0f;
    [Tooltip("Set to negative value if you don't want this object to automatically fade out.")]
    [SerializeField] private float fadeOutDelay = -1f;
    private bool resetFade = false;

    void OnEnable()
    {
        if (resetFade)
        {
            Reset();
        }
        StartCoroutine(FadeInCoroutine());
        Debug.Log("FadeOnStart enabled");
    }

    private IEnumerator FadeInCoroutine()
    {
        resetFade = fadeOutDelay < 0; // Only reset if fadeOutDelay is negative
        yield return new WaitForSeconds(fadeInDelay);
        Display(true, () =>
        {
            if (fadeOutDelay > 0)
            {
                StartCoroutine(FadeOutCoroutine());
            }
        });
    }

    private IEnumerator FadeOutCoroutine()
    {
        yield return new WaitForSeconds(fadeOutDelay);
        Display(false, () =>
        {
            gameObject.SetActive(false);
        });
    }
}
