using UnityEngine;
using UnityEngine.UI;

public class PollinationManager : MonoBehaviour
{
    public static PollinationManager Instance { get; private set; }

    [Header("UI Feedback")]
    public Image crosshair;
    public Color normalColor = Color.white;
    public Color pollenColor = Color.yellow;

    private bool hasMalePollen = false;
    private GameObject lastMaleFlower;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (crosshair != null)
            crosshair.color = normalColor;
    }

    public void OnFlowerClicked(GameObject flower, string type)
    {
        if (type == "male_flower")
        {
            hasMalePollen = true;
            lastMaleFlower = flower;
            Debug.Log("Pollen collected from male flower.");

            if (crosshair != null)
                crosshair.color = pollenColor;
        }
        else if (type == "female_flower")
        {
            if (hasMalePollen)
            {
                Debug.Log("Pollinated female flower!");
                hasMalePollen = false;

                if (crosshair != null)
                    crosshair.color = normalColor;

                // Trigger pollination effect on the plant
                PlantGrowth plant = flower.GetComponentInParent<PlantGrowth>();
                if (plant != null)
                {
                    plant.isPollinated = true;
                    plant.UpdateVisuals();
                }
            }
            else
            {
                Debug.Log("Need pollen from a male flower first.");
            }
        }
    }

    public void ResetPollen()
    {
        hasMalePollen = false;
        lastMaleFlower = null;
        if (crosshair != null)
            crosshair.color = normalColor;
    }
}
