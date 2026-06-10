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

    void Start()
    {
        BackMainMenu(); // Initialize UI state
    }

    public void CheckSaveFile()
    {
        bool hasSave = SaveSystem.Instance != null && SaveSystem.Instance.SaveFileExists();
        
        if (PanelMainMenu != null) PanelMainMenu.SetActive(!hasSave);
        if (PanelMainMenuSavedGame != null) PanelMainMenuSavedGame.SetActive(hasSave);
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

