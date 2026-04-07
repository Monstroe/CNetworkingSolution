using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(FadeUI))]
public class FadeScreen : MonoBehaviour
{
    public static FadeScreen Instance { get; private set; }

    [SerializeField] private bool activateOnStart = false;

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

    void Start()
    {
        if (activateOnStart)
        {
            Display(!fadeUI.IsVisible, 0.5f);
        }
    }

    public void Display(bool value, UnityAction callback = null)
    {
        fadeUI.Display(value, callback);
    }

    public void Display(bool value, float time, UnityAction callback = null)
    {
        fadeUI.FadeTime = time;
        fadeUI.Display(value, callback);
    }
}
