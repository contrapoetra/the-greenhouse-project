using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuLogic : MonoBehaviour
{
    [Header("── Menu States ──")]
    [Tooltip("Normal main menu (New Game only)")]
    public GameObject PanelMainMenu;
    [Tooltip("Saved game menu (Resume / New Game)")]
    public GameObject PanelMainMenuSavedGame;

    [Header("── Sub Panels ──")]
    public GameObject PanelSetting;
    public GameObject PanelTutorial;

    [Header("── UI Elements ──")]
    [Tooltip("Text showing which day the player will resume on")]
    public TMPro.TextMeshProUGUI resumeDayText;

    void Start()
    {
        BackMainMenu(); // Initialize UI state
    }

    public void CheckSaveFile()
    {
        bool hasSave = SaveSystem.Instance != null && SaveSystem.Instance.SaveFileExists();
        Debug.Log($"[MainMenu] Save detection: {hasSave} (Instance valid: {SaveSystem.Instance != null})");
        
        if (hasSave && resumeDayText != null)
        {
            UpdateResumeInfo();
        }

        if (PanelMainMenu != null) PanelMainMenu.SetActive(!hasSave);
        if (PanelMainMenuSavedGame != null) PanelMainMenuSavedGame.SetActive(hasSave);
    }

    private void UpdateResumeInfo()
    {
        try
        {
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, "melon_save.json");
            if (System.IO.File.Exists(savePath))
            {
                string json = System.IO.File.ReadAllText(savePath);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                
                if (data.currentDay > 6)
                {
                    resumeDayText.text = "It's been a while";
                }
                else
                {
                    string dayWord = data.currentDay switch
                    {
                        0 => "Zero",
                        1 => "One",
                        2 => "Two",
                        3 => "Three",
                        4 => "Four",
                        5 => "Five",
                        6 => "Six",
                        _ => data.currentDay.ToString()
                    };
                    resumeDayText.text = "Day " + dayWord;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MainMenu] Failed to read save info: {e.Message}");
            resumeDayText.text = "";
        }
    }

    private void HideAllMenus()
    {
        if (PanelMainMenu != null) PanelMainMenu.SetActive(false);
        if (PanelMainMenuSavedGame != null) PanelMainMenuSavedGame.SetActive(false);
        if (PanelSetting != null) PanelSetting.SetActive(false);
        if (PanelTutorial != null) PanelTutorial.SetActive(false);
    }

    public void OpenSetting()
    {
        HideAllMenus();
        PanelSetting.SetActive(true);
    }

    public void OpenTutorial()
    {
        HideAllMenus();
        PanelTutorial.SetActive(true);
    }

    public void BackMainMenu()
    {
        HideAllMenus();
        CheckSaveFile();
    }

    public void OpenGamePlay()
    {
        SceneManager.LoadScene("OutdoorsScene");
    }

    public void NewGame()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.DeleteSave();
        }
        OpenGamePlay();
    }
    
    public void ExitGame()
{
    Debug.Log("Game closed.");

#if UNITY_EDITOR
    // Jika sedang dijalankan di Unity Editor, hentikan mode Play
    UnityEditor.EditorApplication.isPlaying = false;
#else
    // Jika sudah di-build (EXE / APK), keluar dari game
    Application.Quit();
#endif
}
}
