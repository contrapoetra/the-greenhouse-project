using UnityEngine;
using TMPro;

/// <summary>
/// Simple script to update computer screen with current task progress.
/// </summary>
public class ComputerTaskUI : MonoBehaviour
{
    public TextMeshProUGUI taskListText;
    
    void Update()
    {
        if (taskListText == null || DayProgressionManager.Instance == null) return;

        int day = DayProgressionManager.Instance.CurrentDay;
        int req = DayProgressionManager.Instance.requiredPlants;
        int planted = DayProgressionManager.Instance.CurrentPlantedCount;
        int watered = DayProgressionManager.Instance.CurrentWateredCount;

        string status = "";
        if (day == 0)
        {
            status = "DAY 0 - INITIAL PLANTING\n\n";
            status += $"- Plant Seeds: {planted}/{req} " + (planted >= req ? "✔" : "") + "\n";
            status += $"- Water Plants: {watered}/{planted} " + (watered >= planted && planted >= req ? "✔" : "") + "\n";
            
            if (DayProgressionManager.Instance.IsDayEndEnabled)
            {
                status += "\nTASKS COMPLETE. CLICK TO REPORT.";
            }
        }
        else
        {
            status = $"DAY {day} - MAINTENANCE\n\n";
            status += $"- Water all {planted} plants: {watered}/{planted}";
        }

        taskListText.text = status;
    }
}
