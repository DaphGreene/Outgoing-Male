using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ApplyButtonHouseStyle
{
    [MenuItem("Tools/Outgoing Male/Apply Button House Style To Open Scenes")]
    private static void ApplyToOpenScenes()
    {
        int updatedCount = 0;

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
                continue;

            bool sceneChanged = false;
            GameObject[] roots = scene.GetRootGameObjects();

            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Button[] buttons = roots[rootIndex].GetComponentsInChildren<Button>(true);
                for (int buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
                {
                    Button button = buttons[buttonIndex];
                    if (button == null)
                        continue;

                    Undo.RecordObject(button, "Apply Button House Style");
                    button.transition = Selectable.Transition.None;
                    EditorUtility.SetDirty(button);

                    if (button.GetComponent<StyledMenuButton>() == null)
                    {
                        Undo.AddComponent<StyledMenuButton>(button.gameObject);
                        updatedCount++;
                    }

                    sceneChanged = true;
                }
            }

            if (sceneChanged)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log($"Button house style applied. Added StyledMenuButton to {updatedCount} button(s) in loaded scenes.");
    }
}
