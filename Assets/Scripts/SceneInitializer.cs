using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneInitializer : MonoBehaviour
{
    public static SceneInitializer Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("There are multiple SceneInitializer instances in the scene, destroying one.");
            Destroy(gameObject);
            return;
        }

        if (SceneManager.GetActiveScene().name == NetResources.Instance.GameSceneName)
        {
            SceneManager.LoadSceneAsync(NetResources.Instance.MenuSceneName);
        }
    }
}