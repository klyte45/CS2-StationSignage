using UnityEngine;

namespace StationSignage.Models;

public class LineConnection(
    string number,
    Color color,
    Color onPrimaryColor,
    Color backgroundColor,
    string type,
    string operatorIcon
)
{
    public string Number => number;
    public Color Color => color;
    public Color OnPrimaryColor => onPrimaryColor;
    public Color BackgroundColor => backgroundColor;
    public string Type => type;
    
    public string OperatorIcon => operatorIcon;
}