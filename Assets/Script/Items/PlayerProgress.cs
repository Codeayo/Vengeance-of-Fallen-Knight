using System;
using UnityEngine;

/// <summary>
/// Experience, character level and permanent upgrades for the current run.
/// Each level grants +8 max health and +2 attack; Heart Vessels add permanent health.
/// </summary>
public static class PlayerProgress
{
    [Serializable]
    private class Data
    {
        public int level = 1;
        public int xp;
        public int bonusHealth;
    }

    private static Data data = new Data();

    public const int MaxLevel = 20;
    public const int HealthPerLevel = 8;
    public const int AttackPerLevel = 2;

    public static event Action Changed;
    /// <summary>Raised with the new level.</summary>
    public static event Action<int> LeveledUp;

    public static int Level => data.level;
    public static int Xp => data.xp;
    public static int XpToNext => XpForLevel(data.level);
    public static int LevelHealthBonus => (data.level - 1) * HealthPerLevel + data.bonusHealth;
    public static int LevelAttackBonus => (data.level - 1) * AttackPerLevel;

    public static int XpForLevel(int level) => 60 + 45 * (level - 1);

    public static void AddXp(int amount)
    {
        if (amount <= 0 || data.level >= MaxLevel)
            return;

        data.xp += amount;
        bool leveled = false;
        while (data.level < MaxLevel && data.xp >= XpForLevel(data.level))
        {
            data.xp -= XpForLevel(data.level);
            data.level++;
            leveled = true;
            LeveledUp?.Invoke(data.level);
        }

        if (data.level >= MaxLevel)
            data.xp = 0;
        if (leveled || amount > 0)
            Changed?.Invoke();
    }

    public static void AddPermanentHealth(int amount)
    {
        data.bonusHealth += Mathf.Max(0, amount);
        Changed?.Invoke();
    }

    public static string ToJson() => JsonUtility.ToJson(data);

    public static void LoadJson(string json)
    {
        Data loaded = null;
        if (!string.IsNullOrEmpty(json))
        {
            try { loaded = JsonUtility.FromJson<Data>(json); }
            catch (Exception ex) { Debug.LogWarning("Progress save was unreadable and has been reset: " + ex.Message); }
        }

        data = loaded ?? new Data();
        data.level = Mathf.Clamp(data.level, 1, MaxLevel);
        data.xp = Mathf.Max(0, data.xp);
        data.bonusHealth = Mathf.Max(0, data.bonusHealth);
        Changed?.Invoke();
    }

    public static void ResetToNew()
    {
        data = new Data();
        Changed?.Invoke();
    }
}
