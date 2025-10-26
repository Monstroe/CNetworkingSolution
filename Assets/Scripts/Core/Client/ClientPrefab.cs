using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ClientObject), true)]
public class ClientPrefab : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ClientObject obj = (ClientObject)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This component automatically detects the prefab’s path and generates a unique key based on that path. Use the button below to manually update the path and key if needed.",
            MessageType.Info
        );
        EditorGUILayout.Space();

        if (GUILayout.Button("Get Prefab Path"))
        {
            string path = AssetDatabase.GetAssetPath(obj.gameObject);
            obj.ResetPrefabKeyAndPath(path);
        }

        EditorGUILayout.LabelField("Prefab Key", obj.PrefabKey.ToString());
        EditorGUILayout.LabelField("Prefab Path", obj.PrefabPath ?? "<Not a prefab>");

        /*if (GUILayout.Button("Copy Prefab Key to Clipboard"))
        {
            EditorGUIUtility.systemCopyBuffer = obj.PrefabKey.ToString();
        }

        if (GUILayout.Button("Copy Prefab Path to Clipboard"))
        {
            EditorGUIUtility.systemCopyBuffer = obj.PrefabPath;
        }*/
    }
}
