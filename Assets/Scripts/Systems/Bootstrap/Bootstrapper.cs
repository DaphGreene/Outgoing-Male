using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [SerializeField] private string firstScene = "MainMenu";
    private static bool hasBootstrapped;
    private GameObject bootstrapRoot;

    private void Awake()
    {
        bootstrapRoot = transform.root.gameObject;

        if (hasBootstrapped)
        {
            Destroy(bootstrapRoot);
            return;
        }

        hasBootstrapped = true;
        DontDestroyOnLoad(bootstrapRoot);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ApplySavedAudioSettings();
        SceneManager.LoadScene(firstScene);
    }

    private void OnDestroy()
    {
        if (bootstrapRoot == gameObject || bootstrapRoot == transform.root.gameObject)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySavedAudioSettings();
    }

    private static void ApplySavedAudioSettings()
    {
        if (SoundMixerManager.Instance != null)
            SoundMixerManager.Instance.ApplySavedVolumes();
    }
}
