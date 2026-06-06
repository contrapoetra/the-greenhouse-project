using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuLogic : MonoBehaviour
{
    public GameObject PanelMainMenu;
    public GameObject PanelSetting;
    public GameObject PanelTutorial;

    public void OpenSetting()
    {
        PanelMainMenu.SetActive(false);
        PanelSetting.SetActive(true);
    }

    public void OpenTutorial()
    {
        PanelMainMenu.SetActive(false);
        PanelTutorial.SetActive(true);
    }
    public void BackMainMenu()
    {
        PanelMainMenu.SetActive(true);
        PanelSetting.SetActive(false);
        PanelTutorial.SetActive(false);
    }




    public void OpenGamePlay()
    {
        SceneManager.LoadScene("OutdoorsScene");
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


    void Start()

    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
