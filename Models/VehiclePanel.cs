using System.Collections.Generic;
using UnityEngine;

namespace StationVisuals.Models;

public class VehiclePanel(
    string title, 
    string subtitle, 
    string trainMessage,
    string occupancyTitle,
    List<string> occupancyImages,
    string trainName,
    string wheelchairIcon,
    string bikeIcon,
    Color backgroundColor
)
{
    public readonly string Title = title;
    public readonly string Subtitle = subtitle;
    public readonly string TrainMessage = trainMessage;
    public string OccupancyTitle = occupancyTitle;
    public List<string> OccupancyImages = occupancyImages;
    public string TrainName = trainName;
    public string WheelchairIcon = wheelchairIcon;
    public string BikeIcon = bikeIcon;
    public Color BackgroundColor = backgroundColor;
}