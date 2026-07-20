using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ButtonStyleBootstrap
{
    private static bool isInitialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized)
            return;

        isInitialized = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyToCurrentScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            ApplyToScene(SceneManager.GetSceneAt(i));
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToScene(scene);
    }

    private static void ApplyToScene(Scene scene)
    {
        if (!scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Button[] buttons = roots[i].GetComponentsInChildren<Button>(true);
            for (int j = 0; j < buttons.Length; j++)
            {
                Button button = buttons[j];
                if (button == null)
                    continue;

                button.transition = Selectable.Transition.None;

                if (button.GetComponent<StyledMenuButton>() == null)
                    button.gameObject.AddComponent<StyledMenuButton>();
            }
        }
    }
}
