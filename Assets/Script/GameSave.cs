using UnityEngine;

public static class GameSave
{
    private const string SceneKey = "SavedScene";
    private const string CoinsKey = "SavedCoins";
    private const string HealthKey = "SavedHealth";
    private const string HealingFlasksKey = "SavedHealingFlasks";
    private const string UnlockedKey = "UnlockedLevelIndex";
    private const string InventoryKey = "SavedInventory";
    private const string ProgressKey = "SavedProgress";
    private const string CheckpointKey = "SavedCheckpoint";

    /// <summary>Shrine to respawn at when a Continue loads its level; cleared once used.</summary>
    public static string PendingCheckpoint { get; private set; }

    public static string ConsumePendingCheckpoint(string sceneName)
    {
        if (string.IsNullOrEmpty(PendingCheckpoint) || SavedScene != sceneName)
            return null;
        string c = PendingCheckpoint;
        PendingCheckpoint = null;
        return c;
    }

    /// <summary>Playable levels in story order: scene name and display name.</summary>
    public static readonly (string scene, string title)[] Levels =
    {
        ("Graveyard", "I. The Forsaken Graveyard"),
        ("Woodcamp", "II. The Burned Camp"),
        ("Grove", "III. The Mushroom Grove"),
        ("Taiga", "IV. The Frozen Taiga"),
        ("Castle", "V. The Fallen Castle"),
        ("ThroneRoom", "VI. The Usurper's Throne"),
    };

    public static bool HasSavedGame => PlayerPrefs.HasKey(SceneKey) &&
                                       Application.CanStreamedLevelBeLoaded(PlayerPrefs.GetString(SceneKey));

    public static string SavedScene => PlayerPrefs.GetString(SceneKey, string.Empty);

    public static int LevelIndexOf(string sceneName)
    {
        for (int i = 0; i < Levels.Length; i++)
            if (Levels[i].scene == sceneName)
                return i;
        return -1;
    }

    /// <summary>Highest level index the player may start from in Level Select (0 = first level only).</summary>
    public static int HighestUnlockedLevel => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 0), 0, Levels.Length - 1);

    public static bool IsLevelUnlocked(int index) => index >= 0 && index <= HighestUnlockedLevel;

    public static void UnlockLevel(string sceneName)
    {
        int index = LevelIndexOf(sceneName);
        if (index > HighestUnlockedLevel)
        {
            PlayerPrefs.SetInt(UnlockedKey, index);
            PlayerPrefs.Save();
        }
    }

    public static string GetSavedSceneOr(string fallbackScene)
    {
        string savedScene = PlayerPrefs.GetString(SceneKey, string.Empty);
        if (string.IsNullOrWhiteSpace(savedScene))
            return fallbackScene;

        if (!Application.CanStreamedLevelBeLoaded(savedScene))
        {
            Debug.LogWarning($"Saved scene '{savedScene}' is not in Build Settings. Clearing the invalid save.");
            ClearProgress();
            return fallbackScene;
        }

        return savedScene;
    }

    public static int LoadCoins()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 0));
    }

    public static int LoadHealingFlasks(int defaultCount = 0)
    {
        return PlayerPrefs.HasKey(HealingFlasksKey)
            ? Mathf.Max(0, PlayerPrefs.GetInt(HealingFlasksKey))
            : Mathf.Max(0, defaultCount);
    }

    public static void SaveHealingFlasks(int healingFlasks)
    {
        PlayerPrefs.SetInt(HealingFlasksKey, Mathf.Max(0, healingFlasks));
        PlayerPrefs.Save();
    }

    public static int LoadHealth(int maxHealth)
    {
        int maximum = Mathf.Max(1, maxHealth);
        return PlayerPrefs.HasKey(HealthKey)
            ? Mathf.Clamp(PlayerPrefs.GetInt(HealthKey), 1, maximum)
            : maximum;
    }

    public static void SaveProgress(string sceneName, int coins, int health)
    {
        SaveProgress(sceneName, coins, health, LoadHealingFlasks());
    }

    public static void SaveProgress(string sceneName, int coins, int health, int healingFlasks)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Cannot save progress without a scene name.");
            return;
        }

        if (PlayerPrefs.GetString(SceneKey, "") != sceneName)
            PlayerPrefs.DeleteKey(CheckpointKey); // reaching a new level starts at its beginning
        PlayerPrefs.SetString(SceneKey, sceneName);
        PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, coins));
        PlayerPrefs.SetInt(HealthKey, Mathf.Max(1, health));
        PlayerPrefs.SetInt(HealingFlasksKey, Mathf.Max(0, healingFlasks));
        PlayerPrefs.SetString(InventoryKey, Inventory.ToJson());
        PlayerPrefs.SetString(ProgressKey, PlayerProgress.ToJson());
        UnlockLevel(sceneName);
        PlayerPrefs.Save();
    }

    /// <summary>Saves the run at a shrine so Continue resumes there.</summary>
    public static void SaveCheckpoint(string sceneName, string checkpointName, int coins, int health)
    {
        SaveProgress(sceneName, coins, health, Inventory.Count("crimson_flask"));
        PlayerPrefs.SetString(CheckpointKey, checkpointName);
        PlayerPrefs.Save();
    }

    /// <summary>Restores inventory, progression and the checkpoint from the save (Continue).</summary>
    public static void LoadRun()
    {
        Inventory.LoadJson(PlayerPrefs.GetString(InventoryKey, ""));
        PlayerProgress.LoadJson(PlayerPrefs.GetString(ProgressKey, ""));
        PendingCheckpoint = PlayerPrefs.GetString(CheckpointKey, "");
    }

    /// <summary>True once per New Game: the opening resurrection cinematic plays, then it is cleared.</summary>
    public static bool IntroPending { get; set; }

    public static bool ConsumeIntro()
    {
        bool v = IntroPending;
        IntroPending = false;
        return v;
    }

    /// <summary>Fresh run (New Game).</summary>
    public static void NewRun()
    {
        IntroPending = true;
        ClearProgress();
        Inventory.ResetToNew();
        PlayerProgress.ResetToNew();
        PendingCheckpoint = null;
    }

    /// <summary>
    /// Fresh run starting at a later chapter from Level Select: the knight gets the level and gear
    /// a player would normally have earned by then, so later chapters stay fair.
    /// </summary>
    public static void NewRunAtChapter(int chapter)
    {
        NewRun();
        string[] weapons = { null, "gravewarden_cleaver", "assassins_fang", "sporebane", "frostbrand", "frostbrand" };
        string[] armors = { null, "boneplate_cuirass", "ranger_leathers", "ranger_leathers", "frostguard_mail", "royal_guard_plate" };
        chapter = Mathf.Clamp(chapter, 0, Levels.Length - 1);
        if (chapter == 0)
            return;
        IntroPending = false;
        PlayerProgress.AddXp(0);
        for (int i = 0; i < chapter * 2; i++)
            PlayerProgress.AddXp(PlayerProgress.XpToNext);
        if (weapons[chapter] != null) Inventory.Add(weapons[chapter], 1, null, false);
        if (armors[chapter] != null) Inventory.Add(armors[chapter], 1, null, false);
        Inventory.Add("crimson_flask", chapter, null, false);
    }

    /// <summary>Clears the run in progress. Unlocked levels stay unlocked.</summary>
    public static void ClearProgress()
    {
        PlayerPrefs.DeleteKey(InventoryKey);
        PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.DeleteKey(CheckpointKey);
        PlayerPrefs.DeleteKey(SceneKey);
        PlayerPrefs.DeleteKey(CoinsKey);
        PlayerPrefs.DeleteKey(HealthKey);
        PlayerPrefs.DeleteKey(HealingFlasksKey);
        PlayerPrefs.Save();
    }
}
