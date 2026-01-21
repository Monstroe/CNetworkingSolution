using System;
using System.Collections.Concurrent;
using UnityEngine;

public class ThreadManager : MonoBehaviour
{
    public static ThreadManager Instance { get; private set; }

    private static ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of ThreadManager detected. Destroying duplicate instance.");
            Destroy(gameObject);
        }
    }

    public static void ExecuteOnMainThread(Action action)
    {
        mainThreadActions.Enqueue(action);
    }

    void Update()
    {
        while (mainThreadActions.TryDequeue(out Action action))
        {
            action();
        }
    }
}
