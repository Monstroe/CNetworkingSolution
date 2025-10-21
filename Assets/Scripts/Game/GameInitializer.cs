using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    public static GameInitializer Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("There are multiple GameInitializer instances in the scene, destroying one.");
            Destroy(gameObject);
            return;
        }

        if (SceneManager.GetActiveScene().name == GameResources.Instance.GameSceneName)
        {
            SceneManager.LoadSceneAsync(GameResources.Instance.MenuSceneName);
        }
    }
}