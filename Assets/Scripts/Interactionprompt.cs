using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Greenhouse Project - Interaction Prompt
/// Menampilkan teks prompt di bawah crosshair saat pemain
/// melihat ke arah objek interaktif.
/// </summary>
public class InteractionPrompt : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────

    [Header("── UI ──")]
    [Tooltip("Panel container prompt (background gelap)")]
    public GameObject promptPanel;

    [Tooltip("Teks prompt utama, contoh: [Klik] Ambil Penyiram")]
    public TextMeshProUGUI promptText;

    [Header("── Raycast ──")]
    [Tooltip("Drag Camera transform — sama dengan yang di ClickEvent")]
    public Transform cameraTransform;

    [Tooltip("Jarak deteksi — samakan dengan range di ClickEvent")]
    public float range = 100f;

    [Header("── ClickEvent Reference ──")]
    [Tooltip("Drag GameObject yang ada script ClickEvent")]
    public ClickEvent clickEvent;

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        // Validasi referensi di awal
        if (promptPanel == null)
            Debug.LogError("[InteractionPrompt] promptPanel belum di-assign di Inspector!");
        if (promptText == null)
            Debug.LogError("[InteractionPrompt] promptText belum di-assign di Inspector!");
        if (cameraTransform == null)
            Debug.LogError("[InteractionPrompt] cameraTransform belum di-assign di Inspector!");
        if (clickEvent == null)
            Debug.LogWarning("[InteractionPrompt] clickEvent belum di-assign — isHolding selalu false.");

        HidePrompt();
    }

    void Update()
    {
        // Jangan tampilkan prompt saat pause
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            HidePrompt();
            return;
        }

        if (cameraTransform == null) return;

        CheckInteractable();
    }

    // ─────────────────────────────────────────────
    // CORE
    // ─────────────────────────────────────────────

    void CheckInteractable()
    {
        RaycastHit hit;
        bool didHit = false;

        // Use the prioritized hit logic from ClickEvent if available
        if (clickEvent != null)
        {
            didHit = clickEvent.GetPrioritizedHit(out hit);
        }
        else
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            didHit = Physics.Raycast(ray, out hit, range);
        }

        if (!didHit)
        {
            HidePrompt();
            return;
        }

        // Special check for Computer
        if (hit.collider.gameObject.name == "ComputerCollider" || hit.collider.name.ToLower().Contains("computer") || hit.collider.name.ToLower().Contains("pc"))
        {
            if (DayProgressionManager.Instance != null)
            {
                if (DayProgressionManager.Instance.IsViewingTasks)
                {
                    ShowPrompt("[Klik Kiri] Kembali");
                }
                else if (DayProgressionManager.Instance.IsDayEndEnabled)
                {
                    ShowPrompt("[Klik Kiri] Selesaikan Hari");
                }
                else
                {
                    ShowPrompt("[Klik Kiri] Lihat Tugas");
                }
            }
            return;
        }

        CustomProperties props = hit.collider.GetComponentInParent<CustomProperties>();
        GameObject held = GetHeldObject();

        string prompt = GetPromptText(hit.collider.gameObject, props != null ? props.properties : null, held);

        if (!string.IsNullOrEmpty(prompt))
        {
            ShowPrompt(prompt);
        }
        else
        {
            HidePrompt();
        }
    }

    /// <summary>
    /// Tentukan teks prompt berdasarkan property objek dan status holding.
    /// </summary>
    string GetPromptText(GameObject hitObj, string[] targetProperties, GameObject held)
    {
        if (held != null)
        {
            CustomProperties heldProps = held.GetComponentInChildren<CustomProperties>();
            bool isSeed = heldProps != null && System.Array.Exists(heldProps.properties, p => p == "seed");
            bool isWateringCan = heldProps != null && (System.Array.Exists(heldProps.properties, p => p == "watering_can") || held.name.Contains("watering_can"));
            bool isFertilizer = heldProps != null && System.Array.Exists(heldProps.properties, p => p == "fertilizer");
            bool isMelon = heldProps != null && System.Array.Exists(heldProps.properties, p => p == "melon");

            if (isMelon && targetProperties != null && System.Array.Exists(targetProperties, p => p == "bucket"))
                return "[Klik Kanan]\nLepas di Ember";

            if (isFertilizer && targetProperties != null && System.Array.Exists(targetProperties, p => p == "dirt"))
            {
                if (System.Array.Exists(targetProperties, p => p == "fertilized"))
                    return "Sudah dipupuk";
                return "[Klik Kiri]\nBeri Pupuk";
            }

            if (isSeed && targetProperties != null && System.Array.Exists(targetProperties, p => p == "dirt"))
                return "[Klik Kiri] Tanam Benih";

            if (isWateringCan && targetProperties != null && System.Array.Exists(targetProperties, p => p == "planted"))
                return "[Klik Kiri] Siram Tanaman";

            return null;
        }

        if (targetProperties == null) return null;

        // Tidak memegang apapun
        if (System.Array.Exists(targetProperties, p => p == "seed_storage"))
            return "[Klik Kiri] Ambil Benih";

        if (System.Array.Exists(targetProperties, p => p == "fertilizer_storage"))
            return "[Klik Kiri] Ambil Pupuk";

        if (System.Array.Exists(targetProperties, p => p == "pickup"))
            return "[Klik Kiri] Ambil Objek";

        if (System.Array.Exists(targetProperties, p => p == "dirt"))
            return "[Klik Kiri] Periksa Tanah";

        if (System.Array.Exists(targetProperties, p => p == "planted"))
            return "[Klik Kiri] Periksa Tanaman";

        if (System.Array.Exists(targetProperties, p => p == "melon_fruit"))
        {
            PlantGrowth pg = hitObj.GetComponentInParent<PlantGrowth>();
            if (pg != null && pg.isFruitVisible)
            {
                return "[Klik Kiri] Panen Melon";
            }
        }

        if (System.Array.Exists(targetProperties, p => p == "male_flower"))
        {
            if (PollinationManager.Instance != null && PollinationManager.Instance.HasPollen)
                return "Sudah bawa polen";
            return "[Klik Kiri] Ambil Polen";
        }

        if (System.Array.Exists(targetProperties, p => p == "female_flower"))
        {
            PlantGrowth pg = hitObj.GetComponentInParent<PlantGrowth>();
            
            if (pg != null && pg.isPollinated)
            {
                return "Sudah diserbuki";
            }

            if (pg != null && pg.currentStage == 6)
            {
                return "Bunga sudah layu";
            }

            if (PollinationManager.Instance != null && PollinationManager.Instance.HasPollen)
                return "[Klik Kiri] Penyerbukan";
            return "Butuh polen bunga jantan";
        }

        return null;
    }

    // ─────────────────────────────────────────────
    // SHOW / HIDE
    // ─────────────────────────────────────────────

    void ShowPrompt(string text)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        if (promptText  != null) 
        {
            string formatted = text.Replace("] ", "]\n");
            promptText.text = formatted;
        }
    }

    void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // HELPER
    // ─────────────────────────────────────────────

    /// <summary>
    /// Cek apakah player sedang memegang objek via reflection ke ClickEvent.
    /// </summary>
    GameObject GetHeldObject()
    {
        if (clickEvent == null) return null;

        var field = typeof(ClickEvent).GetField("heldObject",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field == null) return null;

        return field.GetValue(clickEvent) as GameObject;
    }
}
