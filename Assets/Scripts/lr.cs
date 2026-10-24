using System.Globalization;
using UnityEngine;

public static class lr
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
        string savedT = PlayerPrefs.GetString(TimeKey(level), "0");
        double secs;

        bool validTime = double.TryParse(
            savedT,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out secs
        );

        if (!validTime)
        {
            return 0d;
        }

        if (secs < 0d || double.IsNaN(secs) || double.IsInfinity(secs))
        {
            return 0d;
        }

        return secs;
    }

    public static bool SaveIfBetter(int level, int score, double secs)
    {
        if (score < 0 || secs < 0d)
        {
            return false;
        }

        if (double.IsNaN(secs) || double.IsInfinity(secs))
        {
            return false;
        }

        int prevScore = LoadScore(level);

        if (PlayerPrefs.HasKey(ScoreKey(level)))
        {
            if (score < prevScore)
            {
                return false;
            }

            if (score == prevScore)
            {
                double prevTime = LoadTime(level);

                if (secs >= prevTime)
                {
                    return false;
                }
            }
        }

        PlayerPrefs.SetInt(ScoreKey(level), score);

        string timeTxt = secs.ToString("R", CultureInfo.InvariantCulture);
        PlayerPrefs.SetString(TimeKey(level), timeTxt);

        PlayerPrefs.Save();

        return true;
    }
}