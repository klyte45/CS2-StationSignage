using System.Collections.Generic;
using UnityEngine;

namespace StationSignage.Models;

public class VehiclePanel(
    string title, 
    string subtitle, 
    string trainMessage,
    string occupancyTitle,
    List<string> occupancyImages,
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
    public List<string> OccupancyImages = occupancyImages;
    public string TrainName = trainName;
    public string WheelchairIcon = wheelchairIcon;
    public string BikeIcon = bikeIcon;
    public Color BackgroundColor = backgroundColor;
    public string Footer = footer;
    public Color TrainNameTextColor = trainNameTextColor;
    public Color TrainMessageTextColor = trainMessageTextColor;
    public Color OccupancyTitleTextColor = occupancyTitleTextColor;
    
    public string GetCarImage1()
    {
        return OccupancyImages[0];
    }
    
    public string GetCarImage2()
    {
        return OccupancyImages[1];
    }
    
    public string GetCarImage3()
    {
        return OccupancyImages[2];
    }
    
    public string GetCarImage4()
    {
        return OccupancyImages[3];
    }
    
    public string GetCarImage5()
    {
        return OccupancyImages[4];
    }
    
    public string GetCarImage6()
    {
        return OccupancyImages[5];
    }
    
    public string GetCarImage7()
    {
        return OccupancyImages[6];
    }
    
    public string GetCarImage8()
    {
        return OccupancyImages[7];
    }
}