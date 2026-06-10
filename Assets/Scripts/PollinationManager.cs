using UnityEngine;
using UnityEngine.UI;

public class PollinationManager : MonoBehaviour
{
    public static PollinationManager Instance { get; private set; }

    [Header("UI Feedback")]
    public Image crosshair;
    public Sprite normalSprite;
    public Sprite pollenSprite;
    public Color normalColor = Color.white;
    public Color pollenColor = Color.yellow;

    private bool _hasMalePollen = false;
    public bool HasPollen => _hasMalePollen;
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
        UpdateCrosshair();
    }

    public void OnFlowerClicked(GameObject flower, string type)
    {
        if (type == "male_flower")
        {
            _hasMalePollen = true;
            lastMaleFlower = flower;
            Debug.Log("Pollen collected from male flower.");
            UpdateCrosshair();
        }
        else if (type == "female_flower")
        {
            if (_hasMalePollen)
            {
                Debug.Log("Pollinated female flower!");
                _hasMalePollen = false;
                UpdateCrosshair();

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

    public void RestorePollenState(bool hasPollen)
    {
        _hasMalePollen = hasPollen;
        UpdateCrosshair();
    }

    public void ResetPollen()
    {
        _hasMalePollen = false;
        lastMaleFlower = null;
        UpdateCrosshair();
    }

    private void UpdateCrosshair()
    {
        if (crosshair == null) return;

        if (_hasMalePollen)
        {
            crosshair.color = pollenColor;
            if (pollenSprite != null) crosshair.sprite = pollenSprite;
        }
        else
        {
            crosshair.color = normalColor;
            if (normalSprite != null) crosshair.sprite = normalSprite;
        }
    }
}
