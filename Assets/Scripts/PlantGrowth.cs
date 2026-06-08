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

    private float _growthTracker = 0f; // Tracks progress towards next stage

    public bool isWatered => waterLevel > 0.5f;
    public bool hasFruit => isPollinated && currentStage >= 6;

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

    public void ProgressStage()
    {
        // Require water for growth
        if (!isWatered)
        {
            Debug.Log("Growth halted: Plant needs water.");
            return;
        }

        // Halt growth at Stage 3 if not pollinated
        if (currentStage == 3 && !isPollinated)
        {
            Debug.Log("Growth halted: Plant needs pollination at Stage 3.");
            return;
        }

        // Calculate growth amount
        // Fertilized = 1 stage/day
        // Not fertilized = 0.5 stage/day (takes 2 days)
        float growthAmount = isFertilized ? 1f : 0.5f;
        _growthTracker += growthAmount;

        if (_growthTracker >= 1f)
        {
            if (currentStage < stageParents.Length - 1)
            {
                currentStage++;
                _growthTracker = 0f;
                waterLevel = 0f; // Consume water on growth
                UpdateVisuals();
                Debug.Log($"Plant grew to stage {currentStage}");
            }
        }
        else
        {
            waterLevel = 0f; // Still consume water even if didn't reach next stage
            Debug.Log("Plant is growing... (Need one more day without fertilizer)");
        }
    }

    public void TieUp()
    {
        if (currentStage >= 3)
        {
            isTiedUp = true;
            UpdateVisuals();
        }
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
                // Toggle children based on name and state
                foreach (Transform child in stageParents[i].transform)
                {
                    if (child.name == "fruithanging")
                        child.gameObject.SetActive(hasFruit);
                    else if (child.name == "tiedup")
                        child.gameObject.SetActive(!hasFruit && isTiedUp);
                    else if (child.name == "unkempt")
                        child.gameObject.SetActive(!hasFruit && !isTiedUp);
                }
            }
        }
    }
}
