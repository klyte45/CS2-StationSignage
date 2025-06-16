using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace StationSignage.Models;

public class VehiclePanel(
    string title,
    string subtitle,
    string trainMessage,
    string occupancyTitle,
    List<int> occupancyRates,
    string trainName,
    string wheelchairIcon,
    string bikeIcon,
    Color backgroundColor,
    string footer,
    Color trainMessageTextColor,
    Color trainNameTextColor,
    Color occupancyTitleTextColor
)
{
    public readonly string Title = title;
    public readonly string Subtitle = subtitle;
    public readonly string TrainMessage = trainMessage;
    public string OccupancyTitle = occupancyTitle;
    public List<int> OccupancyRates = occupancyRates;
    public string TrainName = trainName;
    public string WheelchairIcon = wheelchairIcon;
    public string BikeIcon = bikeIcon;
    public Color BackgroundColor = backgroundColor;
    public string Footer = footer;
    public Color TrainNameTextColor = trainNameTextColor;
    public Color TrainMessageTextColor = trainMessageTextColor;
    public Color OccupancyTitleTextColor = occupancyTitleTextColor;

    public string GetCarImage(Dictionary<string, string> vars)
    {
        vars.TryGetValue("$idx", out var car);
        if (int.TryParse(car, out var carInt))
        {
            if (carInt == 0)
            {
                return "CapacityStartEngine";
            }
            else if (carInt == OccupancyRates.Count - 1)
            {
                return "CapacityEndEngine";
            }
        }
        return "CapacityCar"; // Default image if no car is specified
    }
    public float3 GetCarCapacityScale(Dictionary<string, string> vars)
    {
        vars.TryGetValue("$idx", out var car);
        if (int.TryParse(car, out var carInt) && carInt < OccupancyRates.Count)
        {
            return new float3(1, OccupancyRates[carInt] / 100f, 1);
        }
        return default; // Default image if no car is specified
    }
    public Color GetCarCapacityColor(Dictionary<string, string> vars)
    {
        vars.TryGetValue("$idx", out var car);
        if (int.TryParse(car, out var carInt) && carInt < OccupancyRates.Count)
        {
            return OccupancyRates[carInt] switch
            {
                < 30 => new Color(0.2f, 0.8f, 0.2f), // Green for low occupancy
                < 70 => new Color(1f, 1f, 0.2f), // Yellow for medium occupancy
                _ => new Color(0.8f, 0.2f, 0.2f) // Red for high occupancy
            };
        }
        return default; // Default image if no car is specified
    }
}