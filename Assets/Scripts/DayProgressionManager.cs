using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages day-to-day progression, UI transitions, and task verification.
/// </summary>
public class DayProgressionManager : MonoBehaviour
{
    public static DayProgressionManager Instance { get; private set; }

    [Header("── UI References ──")]
    public TextMeshProUGUI dayTitleText;
    public CanvasGroup transitionCanvasGroup;
    public GameObject winPanel;

    [Header("── Game Objects ──")]
    public GameObject computerCollider;
    public Transform computerViewPoint;
    public Transform spawnPoint;

    [Header("── Computer Zoom Settings ──")]
    public float minFOV = 20f;
    public float maxFOV = 60f;
    public float zoomSpeed = 10f;
    private float _defaultFOV = 60f;
    private float _targetFOV = 60f;

    [Header("── Settings ──")]
    public float fadeDuration = 1.5f;
    public float titleDisplayDuration = 2.0f;

    private int _currentDay = 0;
    public int CurrentDay => _currentDay;

    public int requiredPlants = 5;
    private bool _dayEndEnabled = false;
    public bool IsDayEndEnabled => _dayEndEnabled;

    private bool _isViewingTasks = false;
    public bool IsViewingTasks => _isViewingTasks;

    public int CurrentPlantedCount { get; private set; }
    public int CurrentWateredCount { get; private set; }
    public int CurrentPollinatedCount { get; private set; }

    // --- Eye Memory ---
    private Transform _originalCameraParent;
    private Vector3 _originalCameraLocalPos;
    private Quaternion _originalCameraLocalRot;

    // --- Debug ---
    private bool _isForcedProgression = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Remember where the camera belongs
        if (Camera.main != null)
        {
            _originalCameraParent = Camera.main.transform.parent;
            _originalCameraLocalPos = Camera.main.transform.localPosition;
            _originalCameraLocalRot = Camera.main.transform.localRotation;
            _defaultFOV = Camera.main.fieldOfView;
        }

        // --- LOAD GAME ---
        if (SaveSystem.Instance != null && SaveSystem.Instance.SaveFileExists())
        {
            string json = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.persistentDataPath, "melon_save.json"));
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            _currentDay = data.currentDay;
            SaveSystem.Instance.LoadGame();
        }
        else
        {
            ApplyNarrativeWeather(_currentDay);
        }

        StartCoroutine(StartDayTransition(_currentDay));
        if (computerCollider != null) computerCollider.SetActive(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Debug: Forced Instant Day Progression via G key");
            _isForcedProgression = true;
            OnComputerClicked(true); 
        }

        if (!_dayEndEnabled)
        {
            if (_currentDay == 0) CheckDay0Tasks();
            else if (_currentDay == 3) CheckDay3Tasks();
            else CheckGenericWateringTasks();
        }

        if (_isViewingTasks)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) ExitComputerView();
            else HandleComputerViewZoom();
        }
    }

    private void HandleComputerViewZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f && Camera.main != null)
        {
            _targetFOV -= scroll * zoomSpeed * 10f;
            _targetFOV = Mathf.Clamp(_targetFOV, minFOV, maxFOV);
        }
        if (Camera.main != null)
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, _targetFOV, Time.deltaTime * 5f);
    }

    private void CheckDay0Tasks()
    {
        PlantGrowth[] plants = Object.FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None);
        CurrentPlantedCount = plants.Length;
        int watered = 0;
        foreach (var p in plants) if (p.waterLevel >= 1f) watered++;
        CurrentWateredCount = watered;
        if (CurrentPlantedCount >= requiredPlants && CurrentWateredCount >= CurrentPlantedCount) EnableDayEnd();
    }

    private void CheckDay3Tasks()
    {
        PlantGrowth[] plants = Object.FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None);
        
        int watered = 0;
        int pollinated = 0;
        foreach (var p in plants)
        {
            if (p.waterLevel >= 1f) watered++;
            if (p.isPollinated) pollinated++;
        }
        
        CurrentWateredCount = watered;
        CurrentPollinatedCount = pollinated;
        CurrentPlantedCount = plants.Length;

        // Require at least 5 pollinated plants AND all plants watered
        if (pollinated >= 5 && watered >= plants.Length && plants.Length > 0)
        {
            EnableDayEnd();
        }
    }

    private void CheckGenericWateringTasks()
    {
        PlantGrowth[] plants = Object.FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None);
        if (plants.Length == 0) return;
        int watered = 0;
        foreach (var p in plants) if (p.waterLevel >= 1f) watered++;
        CurrentWateredCount = watered;
        CurrentPlantedCount = plants.Length;
        if (watered >= plants.Length) EnableDayEnd();
    }

    public void OnComputerClicked(bool bypassTasks = false)
    {
        if (_isViewingTasks) ExitComputerView();
        else if (!_dayEndEnabled && !bypassTasks) EnterComputerView();
        else 
        {
            if (bypassTasks) _isForcedProgression = true;
            StartCoroutine(EndDayTransition());
        }
    }

    private void EnterComputerView()
    {
        if (_isViewingTasks) return;
        _isViewingTasks = true;
        if (Camera.main != null)
        {
            _targetFOV = _defaultFOV;
            StopAllCoroutines();
            if (PlayerController.Instance != null) PlayerController.Instance.enabled = false;
            if (computerViewPoint != null)
            {
                Camera.main.transform.SetParent(computerViewPoint);
                StartCoroutine(LerpToTransform(Camera.main.transform, Vector3.zero, Quaternion.identity, 0.4f));
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ExitComputerView()
    {
        if (!_isViewingTasks) return;
        _isViewingTasks = false;
        StopAllCoroutines();
        if (Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            cam.SetParent(_originalCameraParent);
            cam.localPosition = _originalCameraLocalPos;
            cam.localRotation = _originalCameraLocalRot;
            if (PlayerController.Instance != null) PlayerController.Instance.enabled = true;
            StartCoroutine(LerpFOV(_defaultFOV, 0.3f));
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private IEnumerator LerpToTransform(Transform target, Vector3 localPos, Quaternion localRot, float duration)
    {
        Vector3 startPos = target.localPosition;
        Quaternion startRot = target.localRotation;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.localPosition = Vector3.Lerp(startPos, localPos, t);
            target.localRotation = Quaternion.Lerp(startRot, localRot, t);
            yield return null;
        }
        target.localPosition = localPos;
        target.localRotation = localRot;
    }

    private IEnumerator LerpFOV(float targetFOV, float duration)
    {
        if (Camera.main == null) yield break;
        float startFOV = Camera.main.fieldOfView;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Camera.main.fieldOfView = Mathf.Lerp(startFOV, targetFOV, elapsed / duration);
            yield return null;
        }
        Camera.main.fieldOfView = targetFOV;
    }

    public void EnableDayEnd()
    {
        _dayEndEnabled = true;
        Debug.Log("TASKS DONE");
    }

    public void TriggerWin()
    {
        StartCoroutine(WinTransition());
    }

    private IEnumerator WinTransition()
    {
        // 1. Fade to black
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = elapsed / fadeDuration;
            if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = alpha;
            yield return null;
        }
        if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 1f;

        // 2. Show Win Panel
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        // 3. Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Win Transition Complete.");
    }

    public void ResetAndExitToMainMenu()
    {
        Debug.Log("[DayProgression] Finalizing game: Deleting save and exiting...");
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.DeleteSave();
        }
        else
        {
            Debug.LogWarning("[DayProgression] SaveSystem.Instance is NULL during exit!");
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("startPanel");
    }

    private void ApplyNarrativeWeather(int day)
    {
        if (TimeWeatherManager.Instance == null) return;
        if (day == 0) TimeWeatherManager.Instance.ForceWeather(TimeWeatherManager.WeatherType.Sunny);
        else if (day == 1) TimeWeatherManager.Instance.ForceWeather(TimeWeatherManager.WeatherType.Rainy);
        else TimeWeatherManager.Instance.AdvanceDay(0); 
    }

    private IEnumerator StartDayTransition(int dayNumber, bool instant = false)
    {
        _currentDay = dayNumber;
        _dayEndEnabled = false;

        if (dayTitleText != null) 
        { 
            dayTitleText.text = "Day " + _currentDay; 
            dayTitleText.alpha = 1f; 
        }

        if (transitionCanvasGroup != null)
        {
            // Ensure panel is visible at the very start of the transition (it should be 1 if coming from EndDayTransition)
            // but we'll force it here just in case.
            if (!instant) transitionCanvasGroup.alpha = 1f;
        }
        
        if (instant)
        {
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            yield return new WaitForSeconds(titleDisplayDuration);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeDuration);
                if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = alpha;
                if (dayTitleText != null) dayTitleText.alpha = alpha;
                yield return null;
            }
        }

        // CRITICAL: Force absolute zero at the end
        if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 0f;
        if (dayTitleText != null) dayTitleText.alpha = 0f;
    }

    private IEnumerator EndDayTransition()
    {
        if (_isViewingTasks) ExitComputerView();

        bool isInstant = _isForcedProgression;

        if (!isInstant)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = elapsed / fadeDuration;
                if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = alpha;
                yield return null;
            }
            if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 1f;
        }
        
        // 1. Progression
        _currentDay++;

        // --- Environmental Effects on Plants ---
        float evaporationRate = 0.2f; // Base 20%
        if (EnvironmentManager.Instance != null)
        {
            // Lower humidity = Higher evaporation
            // High humidity (80%+) = Almost 0 evaporation
            float humidityFactor = 1f - (EnvironmentManager.Instance.Humidity / 100f);
            evaporationRate = Mathf.Lerp(0.05f, 0.5f, humidityFactor);
        }

        PlantGrowth[] allPlants = Object.FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None);
        foreach (var p in allPlants)
        {
            // Apply evaporation
            p.Evaporate(evaporationRate);
            
            // Advance growth stage
            p.ProgressStage(isInstant);
        }

        // We no longer call PlantManager.Instance.GrowAllPlants(isInstant) here
        // as we handled the individual growth steps above.

        // 2. Weather
        ApplyNarrativeWeather(_currentDay);

        // 3. Respawn
        if (PlayerController.Instance != null && spawnPoint != null)
        {
            PlayerController.Instance.GetComponent<CharacterController>().enabled = false;
            PlayerController.Instance.transform.position = spawnPoint.position;
            PlayerController.Instance.transform.rotation = spawnPoint.rotation;
            PlayerController.Instance.GetComponent<CharacterController>().enabled = true;
        }

        // 4. SAVE
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
        }
        
        // RESET FLAG
        _isForcedProgression = false;

        StartCoroutine(StartDayTransition(_currentDay, isInstant));
    }
}
