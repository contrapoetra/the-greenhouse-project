using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("── Panels ──")]
    public GameObject panelPauseMenu;
    public GameObject panelSettings;

    [Header("── Sliders ──")]
    public Slider sliderBGM;
    public Slider sliderSFX;
    public Slider sliderSensitivity;

    [Header("── Texts ──")]
    public TextMeshProUGUI textSensitivityValue;

    [Header("── Audio ──")]
    public AudioSource bgmAudioSource;
    public AudioSource sfxAudioSource;

    [Header("── References ──")]
    public PlayerController playerController;
    public string mainMenuSceneName = "startPanel";

    private bool _isPaused = false;
    public bool IsPaused => _isPaused;

    private const string KEY_BGM         = "VolumeBGM";
    private const string KEY_SFX         = "VolumeSFX";
    private const string KEY_SENSITIVITY = "MouseSensitivity";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        panelPauseMenu?.SetActive(false);
        panelSettings?.SetActive(false);

        LoadSettings();

        Time.timeScale = 1f;
        LockCursor();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused) Resume();
            else           Pause();
        }
    }

    // ── Pause / Resume ──────────────────────────

    public void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        panelPauseMenu?.SetActive(true);
        panelSettings?.SetActive(false);
        UnlockCursor();
        if (playerController != null) playerController.enabled = false;
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        panelPauseMenu?.SetActive(false);
        panelSettings?.SetActive(false);
        LockCursor();
        if (playerController != null) playerController.enabled = true;
    }

    // ── Navigasi Panel ──────────────────────────

    public void OpenSettings()
    {
        panelPauseMenu?.SetActive(false);
        panelSettings?.SetActive(true);
    }

    public void BackToPauseMenu()
    {
        panelSettings?.SetActive(false);
        panelPauseMenu?.SetActive(true);
        SaveSettings();
    }

    // ── Quit ────────────────────────────────────

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quit");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Slider Callbacks ────────────────────────

    public void OnBGMChanged(float value)
    {
        if (bgmAudioSource != null) bgmAudioSource.volume = value;
    }

    public void OnSFXChanged(float value)
    {
        if (sfxAudioSource != null) sfxAudioSource.volume = value;
    }

    public void OnSensitivityChanged(float value)
    {
        if (textSensitivityValue != null)
            textSensitivityValue.text = value.ToString("F1");
        if (playerController != null)
            playerController.mouseSensitivity = value;
    }

    // ── Save / Load ─────────────────────────────

    void SaveSettings()
    {
        if (sliderBGM         != null) PlayerPrefs.SetFloat(KEY_BGM,         sliderBGM.value);
        if (sliderSFX         != null) PlayerPrefs.SetFloat(KEY_SFX,         sliderSFX.value);
        if (sliderSensitivity != null) PlayerPrefs.SetFloat(KEY_SENSITIVITY, sliderSensitivity.value);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        float bgm  = PlayerPrefs.GetFloat(KEY_BGM,         1f);
        float sfx  = PlayerPrefs.GetFloat(KEY_SFX,         1f);
        float sens = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 2f);

        if (sliderBGM != null)
        {
            sliderBGM.value = bgm;
            sliderBGM.onValueChanged.AddListener(OnBGMChanged);
            OnBGMChanged(bgm);
        }
        if (sliderSFX != null)
        {
            sliderSFX.value = sfx;
            sliderSFX.onValueChanged.AddListener(OnSFXChanged);
            OnSFXChanged(sfx);
        }
        if (sliderSensitivity != null)
        {
            sliderSensitivity.value = sens;
            sliderSensitivity.onValueChanged.AddListener(OnSensitivityChanged);
            OnSensitivityChanged(sens);
        }
    }

    // ── Cursor ──────────────────────────────────

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }
}