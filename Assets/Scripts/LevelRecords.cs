using System.Globalization;
using UnityEngine;

public static class LevelRecords
{
    private static string ScoreKey(int level) => $"PacStudent.Level{level}.HighScore";
    private static string TimeKey(int level) => $"PacStudent.Level{level}.BestTime";

    public static int LoadScore(int level) => PlayerPrefs.GetInt(ScoreKey(level), 0);

    public static double LoadTime(int level)
    {
        return double.TryParse(PlayerPrefs.GetString(TimeKey(level), "0"), NumberStyles.Float,
            CultureInfo.InvariantCulture, out double seconds) && seconds >= 0d &&
            !double.IsNaN(seconds) && !double.IsInfinity(seconds) ? seconds : 0d;
    }

    public static bool SaveIfBetter(int level, int score, double seconds)
    {
        if (score < 0 || seconds < 0d || double.IsNaN(seconds) || double.IsInfinity(seconds)) return false;
        int previousScore = LoadScore(level);
        if (PlayerPrefs.HasKey(ScoreKey(level)) &&
            (score < previousScore || (score == previousScore && seconds >= LoadTime(level)))) return false;
        PlayerPrefs.SetInt(ScoreKey(level), score);
        PlayerPrefs.SetString(TimeKey(level), seconds.ToString("R", CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
        return true;
    }
}
