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
        else if (day == 3)
        {
            int polReq = 5;
            int polCount = DayProgressionManager.Instance.CurrentPollinatedCount;
            status = "DAY 3 - POLLINATION PHASE\n\n";
            status += $"- Water Plants: {watered}/{planted} " + (watered >= planted ? "✔" : "") + "\n";
            status += $"- Pollinate: {polCount}/{polReq} " + (polCount >= polReq ? "✔" : "") + "\n";

            if (DayProgressionManager.Instance.IsDayEndEnabled)
            {
                status += "\nTASKS COMPLETE. CLICK TO REPORT.";
            }
        }
        else if (day >= 6)
        {
            status = $"DAY {day} - HARVEST DAY!\n\n";
            status += $"- Water Plants: {watered}/{planted} " + (watered >= planted ? "✔" : "") + "\n";
            status += "- Harvest Melons!\n";
            status += "- Fill the Bucket to win.";

            if (DayProgressionManager.Instance.IsDayEndEnabled)
            {
                status += "\n\nTASKS COMPLETE. CLICK TO REPORT.";
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
