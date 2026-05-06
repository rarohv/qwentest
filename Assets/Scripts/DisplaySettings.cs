using UnityEngine;

[DefaultExecutionOrder(-100)]
public class DisplaySettings : MonoBehaviour
{
    private static bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyOnLoad()
    {
        if (applied) return;
        applied = true;

        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        int storedIndex = PlayerPrefs.GetInt("ResolutionIndex", -1);

        Resolution[] all = Screen.resolutions;
        if (storedIndex >= 0 && storedIndex < all.Length)
        {
            Resolution r = all[storedIndex];
            Screen.SetResolution(r.width, r.height, fullscreen);
        }
        else
        {
            Screen.fullScreen = fullscreen;
        }
    }

    void Awake()
    {
        ApplyOnLoad();
    }
}
