using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class UnityPipeCommunication
{
    private static string logFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "unity_messages.log");
    public static List<string> LastMessages = new List<string>();
    public static void ClearLogFile()
    {
        File.WriteAllText(logFilePath, "");
        LastMessages.Clear();
    }

    public static void SendMessageToElectron(string message)
    {
        LastMessages.Add(message);
        try
        {
            string directory = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(logFilePath, message + "\n");
            Debug.Log(message);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write to log file: {e.Message}");
        }
    }
}
