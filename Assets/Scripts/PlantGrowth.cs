using UnityEngine;

/// <summary>
/// Simplified Plant Growth script.
/// Uses naming conventions: children named "unkempt", "tiedup", or "fruithanging".
/// </summary>
public class PlantGrowth : MonoBehaviour
{
    [Header("Growth Settings")]
    [Tooltip("The parent objects for each stage (Stage 0, 1, 2, etc.)")]
    public GameObject[] stageParents;
    public int currentStage = 0;

    [Header("Plant State")]
    public bool isTiedUp = false;
    public bool isPollinated = false;
    public bool isFertilized = false;
    public float waterLevel = 0f;
    public bool isFruitVisible = false; // Whether fruit is actually shown

    private float _growthTracker = 0f;
    private int _pollinationCycles = 0; // Growth cycles completed since pollination

    public bool isWatered => waterLevel > 0.5f;
    public bool hasFruit => isFruitVisible && currentStage >= 6;

    // Helpers for Save System
    public float GetGrowthTracker() => _growthTracker;
    public int GetPollinationCycles() => _pollinationCycles;

    void Start()
    {
        // Add UI indicator
        if (GetComponentInChildren<PlantUI>() == null)
        {
            gameObject.AddComponent<PlantUI>();
        }

        UpdateVisuals();
    }

    public void Water(float amount)
    {
        waterLevel = Mathf.Min(waterLevel + amount, 1f);
        Debug.Log($"Plant watered. Current level: {waterLevel}");
    }

    public void ProgressStage(bool force = false)
    {
        // Require water for growth (unless forced via debug)
        if (!force && !isWatered)
        {
            Debug.Log("Growth halted: Plant needs water.");
            return;
        }

        // Track pollination progress: fruit appears 3 growth cycles after pollination
        if (isPollinated && !isFruitVisible)
        {
            _pollinationCycles++;
            if (_pollinationCycles >= 3)
            {
                isFruitVisible = true;
                Debug.Log("Fruit is now visible due to pollination progress!");
                UpdateVisuals();
            }
        }

        // Calculate growth amount
        float growthAmount = (isFertilized || force) ? 1f : 0.5f;
        _growthTracker += growthAmount;

        if (_growthTracker >= 1f)
        {
            if (currentStage < stageParents.Length - 1)
            {
                currentStage++;
                _growthTracker = 0f;
                waterLevel = 0f;
                UpdateVisuals();
                Debug.Log($"Plant grew to stage {currentStage}");
            }
        }
        else
        {
            waterLevel = 0f;
            Debug.Log("Plant is growing...");
        }
    }

    public void TieUp()
    {
        if (currentStage >= 3)
        {
            isTiedUp = true;
            UpdateVisuals();
            Debug.Log($"Plant is now TIED UP.");
        }
    }

    public void Harvest()
    {
        isFruitVisible = false;
        isPollinated = false; // Reset pollination so it can bear fruit again
        _pollinationCycles = 0;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        for (int i = 0; i < stageParents.Length; i++)
        {
            if (stageParents[i] == null) continue;

            bool isCurrent = (i == currentStage);
            stageParents[i].SetActive(isCurrent);

            if (isCurrent)
            {
                foreach (Transform child in stageParents[i].transform)
                {
                    // 1. Fruit logic - now uses isFruitVisible
                    if (child.name == "fruithanging")
                        child.gameObject.SetActive(isFruitVisible && currentStage >= 6);

                    // 2. Regular models (Pollinated or early stages)
                    else if (child.name == "tiedup")
                        child.gameObject.SetActive(isTiedUp && (isFruitVisible || currentStage < 5));
                    else if (child.name == "unkempt")
                        child.gameObject.SetActive(!isTiedUp && (isFruitVisible || currentStage < 5));

                    // 3. Fruitless models (Stages 5 and 6 only)
                    else if (child.name == "tiedup-fruitless")
                        child.gameObject.SetActive(isTiedUp && !isFruitVisible && currentStage >= 5);
                    else if (child.name == "unkempt-fruitless")
                        child.gameObject.SetActive(!isTiedUp && !isFruitVisible && currentStage >= 5);
                }
            }
        }
    }

    public void LoadFromData(PlantSaveData data)
    {
        this.currentStage = data.currentStage;
        this.waterLevel = data.waterLevel;
        this.isFertilized = data.isFertilized;
        this.isPollinated = data.isPollinated;
        this.isTiedUp = data.isTiedUp;
        this._growthTracker = data.growthTracker;
        this._pollinationCycles = data.pollinationCycles;
        this.isFruitVisible = data.isFruitVisible;

        Debug.Log($"[PlantGrowth] Loaded: Stage {currentStage}, TiedUp: {isTiedUp}, FruitVisible: {isFruitVisible}");

        UpdateVisuals();
    }
}
