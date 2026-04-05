using UnityEngine;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    [Header("Game UI")]
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
            Debug.LogWarning("Multiple instances of GameUI detected. Destroying duplicate.");
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
