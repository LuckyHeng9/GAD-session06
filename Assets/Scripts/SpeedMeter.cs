using UnityEngine;
using TMPro;

public class SpeedMeter : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI speedText;
    public PlayerController player;

    [Header("Display")]
    public string unit = "km/h";
    public float speedScale = 0.5f;

    void Update()
    {
        if (player == null || speedText == null) return;
        float display = Mathf.Abs(player.GetCurrentSpeed()) * speedScale;
        speedText.text = Mathf.FloorToInt(display) + " " + unit;
    }
}