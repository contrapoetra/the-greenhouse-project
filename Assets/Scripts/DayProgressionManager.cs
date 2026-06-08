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

    // --- Eye Memory ---
    private Transform _originalCameraParent;
    private Vector3 _originalCameraLocalPos;
    private Quaternion _originalCameraLocalRot;

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

        // Initialize Day 0 Weather (Sunny)
        if (TimeWeatherManager.Instance != null)
        {
            TimeWeatherManager.Instance.ForceWeather(TimeWeatherManager.WeatherType.Sunny);
        }

        StartCoroutine(StartDayTransition(0));
        if (computerCollider != null) computerCollider.SetActive(true);
    }

    void Update()
    {
        // Debug Key G: Force progress to next day
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Debug: Forced Day Progression via G key");
            OnComputerClicked(true); // Bypass tasks
        }

        if (_currentDay == 0 && !_dayEndEnabled) CheckDay0Tasks();
        if (_currentDay >= 1 && !_dayEndEnabled) CheckDay1Tasks();

        if (_isViewingTasks)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                ExitComputerView();
            }
            else
            {
                HandleComputerViewZoom();
            }
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

        if (CurrentPlantedCount >= requiredPlants && CurrentWateredCount >= CurrentPlantedCount)
        {
            _dayEndEnabled = true;
            EnableDayEnd();
        }
    }

    private void CheckDay1Tasks()
    {
        // Day 1: Just water all existing plants
        PlantGrowth[] plants = Object.FindObjectsByType<PlantGrowth>(FindObjectsSortMode.None);
        if (plants.Length == 0) return;

        int watered = 0;
        foreach (var p in plants) if (p.waterLevel >= 1f) watered++;
        CurrentWateredCount = watered;
        CurrentPlantedCount = plants.Length;

        if (watered >= plants.Length)
        {
            _dayEndEnabled = true;
            EnableDayEnd();
        }
    }

    public void OnComputerClicked(bool bypassTasks = false)
    {
        if (_isViewingTasks) ExitComputerView();
        else if (!_dayEndEnabled && !bypassTasks) EnterComputerView();
        else StartCoroutine(EndDayTransition());
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

    private IEnumerator StartDayTransition(int dayNumber)
    {
        _currentDay = dayNumber;
        _dayEndEnabled = false;

        // --- Weather Control ---
        if (TimeWeatherManager.Instance != null)
        {
            if (_currentDay == 0) TimeWeatherManager.Instance.ForceWeather(TimeWeatherManager.WeatherType.Sunny);
            else if (_currentDay == 1) TimeWeatherManager.Instance.ForceWeather(TimeWeatherManager.WeatherType.Rainy);
            else TimeWeatherManager.Instance.AdvanceDay(0); // Regular random roll for Day 2+
        }

        if (dayTitleText != null) { dayTitleText.text = "Day " + _currentDay; dayTitleText.alpha = 1f; }
        
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
        if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 0f;
        if (dayTitleText != null) dayTitleText.alpha = 0f;
    }

    private IEnumerator EndDayTransition()
    {
        // Close task view if open
        if (_isViewingTasks) ExitComputerView();

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = elapsed / fadeDuration;
            if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = alpha;
            yield return null;
        }
        if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 1f;
        
        // 1. Respawn Player
        if (PlayerController.Instance != null && spawnPoint != null)
        {
            PlayerController.Instance.GetComponent<CharacterController>().enabled = false;
            PlayerController.Instance.transform.position = spawnPoint.position;
            PlayerController.Instance.transform.rotation = spawnPoint.rotation;
            PlayerController.Instance.GetComponent<CharacterController>().enabled = true;
            Debug.Log("Player respawned at SpawnPoint.");
        }

        // 2. Progression
        _currentDay++;
        if (PlantManager.Instance != null) PlantManager.Instance.GrowAllPlants();
        
        // 3. Start New Day
        StartCoroutine(StartDayTransition(_currentDay));
    }
}
