#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ServerService), true)]
public class ServerServiceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ServerService obj = (ServerService)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This component automatically detects the service's type and generates a unique id based on that type. Use the button below to manually update the type and id if needed.",
            MessageType.Info
        );
        EditorGUILayout.Space();

        if (GUILayout.Button("Get Service Type"))
        {
            string type = obj.GetType().FullName;
            obj.ResetServiceId(type);
        }

        EditorGUILayout.LabelField("Service Id", obj.ServiceId.ToString());
        EditorGUILayout.LabelField("Service Type", obj.GetType().FullName);
    }
}
#endif
