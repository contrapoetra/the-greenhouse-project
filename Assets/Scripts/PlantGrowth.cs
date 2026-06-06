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
    public bool hasFruit = false;

    void Start()
    {
        UpdateVisuals();
    }

    public void ProgressStage()
    {
        if (currentStage < stageParents.Length - 1)
        {
            currentStage++;
            UpdateVisuals();
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
