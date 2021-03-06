﻿using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class SaveData
{

    private const string m_SettingsFileName = "UserSettings.json";
    private const string m_InventoryFileName = "UserInventory.json";

    private static string GetSaveDirectory() { return Application.persistentDataPath; }

    private static bool SaveDataExists(string fileName)
    {
        string fullFilePath = Path.Combine(GetSaveDirectory(), fileName);
        return File.Exists(fullFilePath);
    }

    public static void SaveSettings(float sfxVolume, float musicVolume, int graphicsLevel)
    {
        string fullFilePath = Path.Combine(GetSaveDirectory(), m_SettingsFileName);
        SaveDataSettings settings = new SaveDataSettings()
        {
            m_GraphicsLevel = graphicsLevel,
            m_SFXVolume = sfxVolume,
            m_MusicVolume = musicVolume
        };

        File.WriteAllText(fullFilePath, JsonUtility.ToJson(settings));
        Debug.LogFormat("Saved {0} to {1}", m_SettingsFileName, fullFilePath);
    }

    public static SaveDataSettings LoadSettings()
    {
        if (SaveDataExists(m_SettingsFileName))
        {
            string fullFilePath = Path.Combine(GetSaveDirectory(), m_SettingsFileName);
            string jsonData = File.ReadAllText(fullFilePath);
            Debug.LogFormat("Loaded Settings {0} from {1}", m_SettingsFileName, fullFilePath);
            return JsonUtility.FromJson<SaveDataSettings>(jsonData);
        }
        else
        {
            return new SaveDataSettings();
        }
    }

    public static void SaveInventory(Dictionary<Item.ITEM_TYPE, int> data)
    {
        string fullFilePath = Path.Combine(GetSaveDirectory(), m_InventoryFileName);
        
        SaveDataInventory settings = new SaveDataInventory();
        settings.PopulateData(data);
        
        File.WriteAllText(fullFilePath, JsonUtility.ToJson(settings));
        Debug.LogFormat("Saved {0} to {1}", m_InventoryFileName, fullFilePath);
    }

    public static SaveDataInventory LoadInventory()
    {
        if (SaveDataExists(m_InventoryFileName))
        {
            string fullFilePath = Path.Combine(GetSaveDirectory(), m_InventoryFileName);
            string jsonData = File.ReadAllText(fullFilePath);
            Debug.LogFormat("Loaded Inventory {0} from {1}", m_InventoryFileName, fullFilePath);
            return JsonUtility.FromJson<SaveDataInventory>(jsonData);
        }
        else
        {
            return new SaveDataInventory();
        }

    }

}

[Serializable]
public class SaveDataSettings
{
    public float m_SFXVolume = 1;
    public float m_MusicVolume = 1;
    public int m_GraphicsLevel = 2;
}

[Serializable]
public class SaveDataInventory : SerializableDictionary<Item.ITEM_TYPE, int>
{
}