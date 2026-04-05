using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(FadeUI))]
public class FadeScreen : MonoBehaviour
{
    public static FadeScreen Instance { get; private set; }

    private FadeUI fadeUI;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of FadeScreen detected. Destroying duplicate instance.");
            Destroy(gameObject);
            return;
        }

        Image fadeImage = GetComponentInChildren<Image>();
        if (fadeImage != null)
        {
            fadeImage.enabled = true;
        }

        fadeUI = GetComponent<FadeUI>();
    }

    public void Display(bool value, float time, UnityAction callback = null)
    {
        fadeUI.FadeTime = time;
        fadeUI.Display(value, callback);
    }
}
