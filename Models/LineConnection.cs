using UnityEngine;

namespace StationSignage.Models;

public struct LineConnection(
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
    public Color BackgroundColor => backgroundColor;
    public Color OnPrimaryColor => onPrimaryColor;
    public string Type => type;    
    public string OperatorIcon => operatorIcon;
}