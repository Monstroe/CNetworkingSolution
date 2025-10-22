using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public static class ServerLobbySpawningUtils
{
    public static T SpawnObject<T>(T obj, ServerLobby lobby) where T : Component
    {
        T instance = Object.Instantiate(obj);
        SceneManager.MoveGameObjectToScene(instance.gameObject, lobby.LobbyScene);
        return instance;
    }

    public static T SpawnObject<T>(T obj, ServerLobby lobby, Transform parent) where T : Component
    {
        T instance = Object.Instantiate(obj, parent);
        SceneManager.MoveGameObjectToScene(instance.gameObject, lobby.LobbyScene);
        return instance;
    }

    public static T SpawnObject<T>(T obj, ServerLobby lobby, Vector3 position, Quaternion rotation) where T : Component
    {
        T instance = Object.Instantiate(obj, position, rotation);
        SceneManager.MoveGameObjectToScene(instance.gameObject, lobby.LobbyScene);
        return instance;
    }

    public static async Task<T> LoadObjectAsync<T>(string objPath, ServerLobby lobby) where T : Component
    {
        T obj = await Addressables.LoadAssetAsync<T>(objPath).Task;
        return SpawnObject<T>(obj, lobby);
    }

    public static async Task<T> LoadObjectAsync<T>(string objPath, ServerLobby lobby, Transform parent) where T : Component
    {
        T obj = await Addressables.LoadAssetAsync<T>(objPath).Task;
        return SpawnObject<T>(obj, lobby, parent);
    }

    public static async Task<T> LoadObjectAsync<T>(string objPath, ServerLobby lobby, Vector3 position, Quaternion rotation) where T : Component
    {
        T obj = await Addressables.LoadAssetAsync<T>(objPath).Task;
        return SpawnObject<T>(obj, lobby, position, rotation);
    }
}
