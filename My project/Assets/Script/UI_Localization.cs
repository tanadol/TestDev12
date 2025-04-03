using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

public class UI_Localization : MonoBehaviour
{
    private Dictionary<string, string> localizationData = new Dictionary<string, string>();
    public static UI_Localization Instance { get; private set; }

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        // Check if there is already an instance of this class
        if (Instance != null && Instance != this)
        {
            // If another instance exists, destroy this one
            Destroy(gameObject);
            return;
        }

        // Set this instance as the singleton instance
        Instance = this;

        // Optional: Make this object persist between scenes
        DontDestroyOnLoad(gameObject);
        
        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // delete any potential duplicates that might be in the scene already, keeping only this one 
    }
    void Start()
    {
        //read localization at startup
        LoadLocalization("Localization/Localization-EN");
    }

    public void LoadLocalization(string address)
    {
        Addressables.LoadAssetAsync<TextAsset>(address).Completed += OnLocalizationLoaded;
    }

    private void OnLocalizationLoaded(AsyncOperationHandle<TextAsset> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            TextAsset csvFile = handle.Result;
            string[] lines = csvFile.text.Split('\n');

            foreach (string line in lines)
            {
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    continue;

                // Split by comma, assuming the format: key,EN
                string[] parts = line.Split(',');

                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    if (!localizationData.ContainsKey(key))
                    {
                        localizationData.Add(key, value);
                        Debug.Log($"Loaded: {key} -> {value}");
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Failed to load localization file.");
        }
    }

    public string GetLocalizedText(string key)
    {
        if (localizationData.TryGetValue(key, out string value))
        {
            return value;
        }

        return key;
        //return $"[Missing: {key}]";
    }
}
