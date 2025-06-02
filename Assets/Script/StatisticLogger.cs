using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Globalization;

public class StatisticLogger : MonoBehaviour
{
    [SerializeField] string logFileName = "SkripsiLog.csv";
    [SerializeField] string agentType;
    [SerializeField] string mapName;
    private int episode;
    private string filePath;

    void Awake()
    {
        filePath = Path.Combine(@"D:\Unity Projek\Crypt-See\LoggingData\", logFileName);
        Debug.Log($"Logging to {filePath}");

        if (!File.Exists(filePath))
        {
            string header = "AgentType;MapName;Episode;EpisodeDuration;PlayerCaptured;CaptureTime\n";
            File.WriteAllText(filePath, header);
        }

        episode = 1;
    }

    public void LogData(string playerCaptured, float captureTime, float episodeDuration)
    {
        string episodeDurationStr = episodeDuration.ToString("F2", CultureInfo.InvariantCulture);
        string captureTimeStr = captureTime.ToString("F2", CultureInfo.InvariantCulture);

        string line = $"{agentType};{mapName};{episode};{episodeDurationStr};{playerCaptured};{captureTimeStr}\n";
        File.AppendAllText(filePath, line);
        episode++;
        Debug.Log("Logged: " + line.Trim()); // Tampilkan di konsol juga
    }
}
