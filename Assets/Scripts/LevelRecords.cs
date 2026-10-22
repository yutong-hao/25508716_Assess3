using System.Globalization;
using UnityEngine;

public static class LevelRecords
{
    private static string ScoreKey(int level)
    {
        return "PacStudent.Level" + level + ".HighScore";
    }

    private static string TimeKey(int level)
    {
        return "PacStudent.Level" + level + ".BestTime";
    }

    public static int LoadScore(int level)
    {
        return PlayerPrefs.GetInt(ScoreKey(level), 0);
    }

    public static double LoadTime(int level)
    {
        string savedTime = PlayerPrefs.GetString(TimeKey(level), "0");
        double seconds;

        bool validTime = double.TryParse(
            savedTime,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out seconds
        );

        if (!validTime)
        {
            return 0d;
        }

        if (seconds < 0d || double.IsNaN(seconds) || double.IsInfinity(seconds))
        {
            return 0d;
        }

        return seconds;
    }

    public static bool SaveIfBetter(int level, int score, double seconds)
    {
        if (score < 0 || seconds < 0d)
        {
            return false;
        }

        if (double.IsNaN(seconds) || double.IsInfinity(seconds))
        {
            return false;
        }

        int previousScore = LoadScore(level);

        if (PlayerPrefs.HasKey(ScoreKey(level)))
        {
            if (score < previousScore)
            {
                return false;
            }

            if (score == previousScore)
            {
                double previousTime = LoadTime(level);

                if (seconds >= previousTime)
                {
                    return false;
                }
            }
        }

        PlayerPrefs.SetInt(ScoreKey(level), score);

        string timeText = seconds.ToString("R", CultureInfo.InvariantCulture);
        PlayerPrefs.SetString(TimeKey(level), timeText);

        PlayerPrefs.Save();

        return true;
    }
}