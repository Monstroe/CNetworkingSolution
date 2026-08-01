using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

[InitializeOnLoad]
public static class CNSAddressablesInstaller
{
    private const string Key = "CNS_AddressablesConfigured";

    private const string SettingsFolder = "Assets/AddressableAssetsData";
    private const string SettingsName = "AddressableAssetSettings";

    private static readonly string[] defaultGroups = new string[]
    {
        "CNS_ServerObjects",
        "CNS_ClientObjects"
    };

    private static readonly string[] defaultLabels = new string[]
    {
        "CNS_SFX",
        "CNS_VFX",
        "CNS_ClientPrefabs",
        "CNS_ServerPrefabs"
    };

    static CNSAddressablesInstaller()
    {
        if (EditorPrefs.GetBool(Key))
            return;

        EditorApplication.delayCall += () =>
        {
            Configure();
            EditorPrefs.SetBool(Key, true);
        };
    }

    [MenuItem("Tools/CNetworkingSolution/Configure Addressables")]
    private static void ConfigureMenu()
    {
        Configure();
    }

    private static void Configure()
    {
        var settings = GetSettings();

        foreach (var groupName in defaultGroups)
        {
            EnsureGroup(settings, groupName);
        }

        foreach (var label in defaultLabels)
        {
            settings.AddLabel(label);
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    private static AddressableAssetSettings GetSettings()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings != null)
            return settings;

        settings = AddressableAssetSettings.Create(SettingsFolder, SettingsName, true, true);

        AddressableAssetSettingsDefaultObject.Settings = settings;

        return settings;
    }

    private static void EnsureGroup(AddressableAssetSettings settings, string groupName)
    {
        if (settings.FindGroup(groupName) != null)
            return;

        var defaultGroup = settings.DefaultGroup;
        settings.CreateGroup(groupName, false, false, true, defaultGroup.Schemas);
    }
}