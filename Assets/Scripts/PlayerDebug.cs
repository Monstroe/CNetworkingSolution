using UnityEngine;

public class PlayerDebug : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            Player.Instance.InvokeOnServerObject(nameof(Player.UpdateHealth), -25f, Player.Instance.Id);
        }
    }
}
