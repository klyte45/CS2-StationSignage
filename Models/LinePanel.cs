namespace StationVisuals.Models;

public class LinePanel(
    string status,
    string number,
    UnityEngine.Color color,
    UnityEngine.Color onPrimaryColor,
    UnityEngine.Color statusColor
)
{
    public string Status = status;
    public string Number = number;
    public UnityEngine.Color Color = color;
    public UnityEngine.Color OnPrimaryColor = onPrimaryColor;
    public UnityEngine.Color StatusColor = statusColor;
}