using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class BootPlayModeStartScene
{
    private const string BootScenePath = "Assets/Scenes/Boot.unity";

    static BootPlayModeStartScene()
    {
        SceneAsset bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
        if (bootScene == null)
            return;

        if (EditorSceneManager.playModeStartScene == bootScene)
            return;

        EditorSceneManager.playModeStartScene = bootScene;
    }
}
