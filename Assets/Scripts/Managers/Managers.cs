using UnityEngine;

public class Managers : MonoBehaviour
{

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}
