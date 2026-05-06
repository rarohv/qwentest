using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class HoleData
{
    public int holeNumber;
    public string holeName;
    public string sceneName;
    public int par;
    public Vector3 teePosition;
    public Vector3 holePosition;
    public Vector3 playerStart;
    public float groundScaleX;
    public float groundScaleZ;
    public bool hasWater;
    public Vector3 waterPosition;
    public Vector3 waterScale;
    public bool hasBridge;
    public Vector3 bridgePosition;
    public Vector3 bridgeScale;
    public bool hasRamp;
    public Vector3 rampPosition;
    public Vector3 rampScale;
    public Vector3 rampRotation;
    public bool hasWall;
    public Vector3 wallPosition;
    public Vector3 wallScale;
    public bool hasSandBunker;
    public Vector3 bunkerPosition;
    public Vector3 bunkerScale;
    public int decorationSeed;
}

public class LevelManager : MonoBehaviour
{
    public const string MainMenuSceneName = "MainMenu";

    public static LevelManager Instance { get; private set; }

    [SerializeField] private HoleData[] holes;
    private int currentHoleIndex;
    private Dictionary<int, int> strokesPerHole = new Dictionary<int, int>();
    private Dictionary<int, string> scoreNames = new Dictionary<int, string>();

    public HoleData CurrentHole => holes != null && currentHoleIndex < holes.Length ? holes[currentHoleIndex] : null;
    public int CurrentHoleIndex => currentHoleIndex;
    public int CurrentHoleNumber => currentHoleIndex + 1;
    public int TotalHoles => holes != null ? holes.Length : 0;
    public bool IsLastHole => currentHoleIndex >= TotalHoles - 1;
    public int TotalStrokes { get; private set; }
    public int TotalPar { get; private set; }

    public HoleData GetHole(int index)
    {
        if (holes == null || index < 0 || index >= holes.Length) return null;
        return holes[index];
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitScoreNames();
        BuildHoleData();
        CalculateTotalPar();
        SyncCurrentHoleIndexFromActiveScene();
    }

    void InitScoreNames()
    {
        scoreNames[-3] = "Albatross";
        scoreNames[-2] = "Eagle";
        scoreNames[-1] = "Birdie";
        scoreNames[0] = "Par";
        scoreNames[1] = "Bogey";
        scoreNames[2] = "Double Bogey";
        scoreNames[3] = "Triple Bogey";
    }

    void BuildHoleData()
    {
        holes = new HoleData[]
        {
            new HoleData
            {
                holeNumber = 1, holeName = "First Swing", sceneName = "Hole01", par = 2,
                teePosition = new Vector3(0f, 0.15f, 2f),
                holePosition = new Vector3(0f, 0.08f, 35f),
                playerStart = new Vector3(0f, 1f, 0f),
                groundScaleX = 5f, groundScaleZ = 8f,
                decorationSeed = 1001,
                hasWater = false, hasBridge = false, hasRamp = false, hasWall = false, hasSandBunker = false
            },
            new HoleData
            {
                holeNumber = 2, holeName = "Over the Stream", sceneName = "Hole02", par = 3,
                teePosition = new Vector3(0f, 0.15f, 2f),
                holePosition = new Vector3(0f, 0.08f, 38f),
                playerStart = new Vector3(0f, 1f, 0f),
                groundScaleX = 6f, groundScaleZ = 8f,
                decorationSeed = 1002,
                hasWater = true, waterPosition = new Vector3(0f, 0.15f, 20f), waterScale = new Vector3(0.8f, 1f, 0.6f),
                hasBridge = true, bridgePosition = new Vector3(0f, 0.35f, 20f), bridgeScale = new Vector3(4f, 0.2f, 6f),
                hasRamp = false, hasWall = false, hasSandBunker = false
            },
            new HoleData
            {
                holeNumber = 3, holeName = "The Bunker", sceneName = "Hole03", par = 3,
                teePosition = new Vector3(0f, 0.15f, 2f),
                holePosition = new Vector3(0f, 0.08f, 40f),
                playerStart = new Vector3(0f, 1f, 0f),
                groundScaleX = 6f, groundScaleZ = 9f,
                decorationSeed = 1003,
                hasWater = false, hasBridge = false, hasRamp = false, hasWall = false,
                hasSandBunker = true, bunkerPosition = new Vector3(2f, -0.1f, 25f), bunkerScale = new Vector3(3f, 0.3f, 3f)
            },
            new HoleData
            {
                holeNumber = 4, holeName = "Ramp Shot", sceneName = "Hole04", par = 3,
                teePosition = new Vector3(0f, 0.15f, 2f),
                holePosition = new Vector3(0f, 1.2f, 38f),
                playerStart = new Vector3(0f, 1f, 0f),
                groundScaleX = 6f, groundScaleZ = 8f,
                decorationSeed = 1004,
                hasWater = false, hasBridge = false,
                hasRamp = true, rampPosition = new Vector3(0f, 0.3f, 20f), rampScale = new Vector3(4f, 0.3f, 5f), rampRotation = new Vector3(-15f, 0f, 0f),
                hasWall = false, hasSandBunker = false
            },
            new HoleData
            {
                holeNumber = 5, holeName = "Island Green", sceneName = "Hole05", par = 3,
                teePosition = new Vector3(0f, 0.15f, 2f),
                holePosition = new Vector3(0f, 0.15f, 40f),
                playerStart = new Vector3(0f, 1f, 0f),
                groundScaleX = 7f, groundScaleZ = 9f,
                decorationSeed = 1005,
                hasWater = true, waterPosition = new Vector3(0f, 0.15f, 22f), waterScale = new Vector3(1.5f, 1f, 1.2f),
                hasBridge = true, bridgePosition = new Vector3(0f, 0.35f, 22f), bridgeScale = new Vector3(2.5f, 0.2f, 6f),
                hasRamp = false, hasWall = false, hasSandBunker = false
            },
            new HoleData
            {
                holeNumber = 6, holeName = "The Wall", sceneName = "Hole06", par = 4,
                teePosition = new Vector3(0f, 0.15f, 2f),
                holePosition = new Vector3(5f, 0.08f, 35f),
                playerStart = new Vector3(0f, 1f, 0f),
                groundScaleX = 8f, groundScaleZ = 8f,
                decorationSeed = 1006,
                hasWater = false, hasBridge = false, hasRamp = false,
                hasWall = true, wallPosition = new Vector3(2f, 1f, 18f), wallScale = new Vector3(0.5f, 3f, 10f),
                hasSandBunker = true, bunkerPosition = new Vector3(5f, -0.1f, 28f), bunkerScale = new Vector3(3f, 0.3f, 3f)
            },
            new HoleData
            {
                holeNumber = 7, holeName = "Ramp & River", sceneName = "Hole07", par = 4,
                teePosition = new Vector3(0f, 0.15f, 2f),
                holePosition = new Vector3(0f, 1.5f, 42f),
                playerStart = new Vector3(0f, 1f, 0f),
                groundScaleX = 6f, groundScaleZ = 9f,
                decorationSeed = 1007,
                hasWater = true, waterPosition = new Vector3(0f, 0.15f, 25f), waterScale = new Vector3(1f, 1f, 0.8f),
                hasBridge = true, bridgePosition = new Vector3(0f, 0.35f, 25f), bridgeScale = new Vector3(3f, 0.2f, 6f),
                hasRamp = true, rampPosition = new Vector3(0f, 0.3f, 15f), rampScale = new Vector3(4f, 0.3f, 4f), rampRotation = new Vector3(-12f, 0f, 0f),
                hasWall = false, hasSandBunker = false
            },
            new HoleData
            {
                holeNumber = 8, holeName = "Narrow Path", sceneName = "Hole08", par = 3,
                teePosition = new Vector3(0f, 0.15f, 2f),
                holePosition = new Vector3(0f, 0.08f, 40f),
                playerStart = new Vector3(0f, 1f, 0f),
                groundScaleX = 4f, groundScaleZ = 9f,
                decorationSeed = 1008,
                hasWater = true, waterPosition = new Vector3(0f, 0.15f, 20f), waterScale = new Vector3(1.2f, 1f, 1f),
                hasBridge = false, hasRamp = false, hasWall = false, hasSandBunker = false
            },
            new HoleData
            {
                holeNumber = 9, holeName = "The Gauntlet", sceneName = "Hole09", par = 5,
                teePosition = new Vector3(0f, 0.15f, 2f),
                holePosition = new Vector3(0f, 1.8f, 45f),
                playerStart = new Vector3(0f, 1f, 0f),
                groundScaleX = 8f, groundScaleZ = 10f,
                decorationSeed = 1009,
                hasWater = true, waterPosition = new Vector3(0f, 0.15f, 15f), waterScale = new Vector3(1f, 1f, 0.8f),
                hasBridge = true, bridgePosition = new Vector3(0f, 0.35f, 15f), bridgeScale = new Vector3(2.5f, 0.2f, 5f),
                hasRamp = true, rampPosition = new Vector3(0f, 0.3f, 30f), rampScale = new Vector3(5f, 0.3f, 5f), rampRotation = new Vector3(-10f, 0f, 0f),
                hasWall = true, wallPosition = new Vector3(3f, 1f, 22f), wallScale = new Vector3(0.5f, 2.5f, 6f),
                hasSandBunker = true, bunkerPosition = new Vector3(-2f, -0.1f, 35f), bunkerScale = new Vector3(3f, 0.3f, 4f)
            },
            new HoleData
            {
                holeNumber = 10, holeName = "Grand Finale", sceneName = "Hole10", par = 5,
                teePosition = new Vector3(0f, 0.15f, 2f),
                holePosition = new Vector3(0f, 2f, 50f),
                playerStart = new Vector3(0f, 1f, 0f),
                groundScaleX = 10f, groundScaleZ = 12f,
                decorationSeed = 1010,
                hasWater = true, waterPosition = new Vector3(0f, 0.15f, 20f), waterScale = new Vector3(2f, 1f, 1.5f),
                hasBridge = true, bridgePosition = new Vector3(-2f, 0.35f, 20f), bridgeScale = new Vector3(2f, 0.2f, 5f),
                hasRamp = true, rampPosition = new Vector3(0f, 0.4f, 35f), rampScale = new Vector3(5f, 0.4f, 6f), rampRotation = new Vector3(-18f, 0f, 0f),
                hasWall = true, wallPosition = new Vector3(4f, 1.5f, 28f), wallScale = new Vector3(0.5f, 3f, 8f),
                hasSandBunker = true, bunkerPosition = new Vector3(-3f, -0.1f, 42f), bunkerScale = new Vector3(4f, 0.3f, 4f)
            }
        };
    }

    void CalculateTotalPar()
    {
        TotalPar = 0;
        if (holes == null) return;
        foreach (var hole in holes)
            TotalPar += hole.par;
    }

    void SyncCurrentHoleIndexFromActiveScene()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active == null || string.IsNullOrEmpty(active.name)) return;

        for (int i = 0; i < holes.Length; i++)
        {
            if (holes[i].sceneName == active.name)
            {
                currentHoleIndex = i;
                return;
            }
        }
    }

    public void RecordHoleStrokes(int strokes)
    {
        if (strokesPerHole.ContainsKey(currentHoleIndex))
        {
            TotalStrokes -= strokesPerHole[currentHoleIndex];
        }
        strokesPerHole[currentHoleIndex] = strokes;
        TotalStrokes += strokes;
    }

    public string GetScoreName(int holeIndex)
    {
        if (!strokesPerHole.ContainsKey(holeIndex)) return "";
        if (holes == null || holeIndex < 0 || holeIndex >= holes.Length) return "";
        int diff = strokesPerHole[holeIndex] - holes[holeIndex].par;
        if (diff < -3) return "Albatross";
        if (diff > 3) return "+" + diff;
        return scoreNames.ContainsKey(diff) ? scoreNames[diff] : "+" + diff;
    }

    public int GetHoleStrokes(int holeIndex)
    {
        return strokesPerHole.ContainsKey(holeIndex) ? strokesPerHole[holeIndex] : 0;
    }

    public void AdvanceToNextHole()
    {
        currentHoleIndex++;
    }

    public void ResetProgress()
    {
        currentHoleIndex = 0;
        strokesPerHole.Clear();
        TotalStrokes = 0;
    }

    public string GetFinalScoreSummary()
    {
        int diff = TotalStrokes - TotalPar;
        string relation = diff == 0 ? "Even" : (diff > 0 ? "+" + diff : diff.ToString());
        return string.Format("Total: {0} strokes ({1})", TotalStrokes, relation);
    }

    public bool HasSceneInBuild(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (sceneFileName == name) return true;
        }
        return false;
    }

    public void LoadHoleScene(int holeIndex)
    {
        if (holes == null || holeIndex < 0 || holeIndex >= holes.Length) return;
        currentHoleIndex = holeIndex;
        string sceneName = holes[holeIndex].sceneName;
        if (HasSceneInBuild(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("[LevelManager] Scene '" + sceneName + "' not in Build Settings. Use GolfGame > Build All 10 Holes and add scenes via GolfGame > Configure Build Settings.");
        }
    }

    public void LoadFirstHole()
    {
        ResetProgress();
        LoadHoleScene(0);
    }

    public void LoadMainMenu()
    {
        if (HasSceneInBuild(MainMenuSceneName))
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }
    }
}
