using UnityEngine;

public class GameContent : MonoBehaviour
{

    public static GameContent Instance { get; private set; }

    [Header("Game Content")]
    [SerializeField] private GameObject pregame;
    [SerializeField] private GameObject game;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of GameContent detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    public void ShowPregame(bool b)
    {
        pregame.SetActive(b);
    }

    public void ShowGame(bool b)
    {
        game.SetActive(b);
    }
}
