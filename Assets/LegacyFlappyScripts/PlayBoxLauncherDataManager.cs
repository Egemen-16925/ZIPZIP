namespace Legacy2D {
using DG.Tweening.Plugins.Core.PathCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class PlayBoxLauncherDataManager : MonoBehaviour
{
    public static LauncherData _gameData = new LauncherData();
    public static float GetSound => _gameData.soundValue;
    public static float SetSound(float value) => _gameData.soundValue = value;
    public static string GetLanguage => _gameData.language;

    private void Awake()
    {
        _gameData = new LauncherData();
        CreateGame();

        UnityPipeCommunication.ClearLogFile();
        UnityPipeCommunication.SendMessageToElectron($"SCORE:{4}:{0}:{DateTime.Now}:{PlayBoxLauncherDataManager.GetSound}");
    }

    private void OnApplicationQuit()
    {

        SaveSound();
    }

    private static void CreateGame()
    {
        var commandLine = System.Environment.GetCommandLineArgs();

        if (commandLine.Length <= 1)
        {
            DeveloperFunc();
            return;
        }

        if (!string.IsNullOrEmpty(commandLine[2])) // sistem ses seviyesi
        {
            _gameData.soundValue = (float)(int.TryParse(commandLine[2], out var soundValue) ? soundValue : 100) / 100;
        }

        if (!string.IsNullOrEmpty(commandLine[3])) // Dil (TR,EN vs)
        {
            _gameData.language = commandLine[3];
        }
    }

    private static void DeveloperFunc()
    {
        print("dev");
        _gameData.soundValue = 0.25f;
    }

    public static float RoundToNearestVolume()
    {
        var divide = Mathf.Round(GetSound / 0.1f);
        return divide * 0.1f;
    }

    public static void SaveSound()
    {
        List<string> datas = new List<string>();
        datas.AddRange(UnityPipeCommunication.LastMessages);

        for (int i = 0; i < datas.Count; i++)
        {
            int lastColonIndex = datas[i].LastIndexOf(':');
            if (lastColonIndex != -1)
            {
                string newFloat = RoundToNearestVolume().ToString(); // Yeni float değerin
                print("Yeni : " + newFloat);
                datas[i] = datas[i].Substring(0, lastColonIndex + 1) + newFloat;
            }
        }
        UnityPipeCommunication.ClearLogFile();

        for (int i = 0; i < datas.Count; i++)
        {
            UnityPipeCommunication.SendMessageToElectron(datas[i]);
        }
    }
}

public class LauncherData
{
    public float soundValue;
    public string language;
}
}
