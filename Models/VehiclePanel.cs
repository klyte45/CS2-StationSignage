using System.Collections.Generic;
using System.Linq;
using StationSignage.Formulas;
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
    public List<int> OccupancyImages = occupancyRates;
    public string TrainName = trainName;
    public string WheelchairIcon = wheelchairIcon;
    public string BikeIcon = bikeIcon;
    public Color BackgroundColor = backgroundColor;
    public string Footer = footer;
    public Color TrainNameTextColor = trainNameTextColor;
    public Color TrainMessageTextColor = trainMessageTextColor;
    public Color OccupancyTitleTextColor = occupancyTitleTextColor;
}