using System.Collections.Generic;
using System.Linq;
using StationSignage.Formulas;
using UnityEngine;

namespace StationSignage.Models;

public class VehiclePanel(
    Dictionary<string, string> vars,
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
    
    //public string GetCarImage()
    //{
    //    vars.TryGetValue(LinesUtils.TRAIN_HALF_VAR, out var trainHalf);
    //    var useFirstHalf = trainHalf != "2";
    //    var splitIndex = OccupancyImages.Count / 2;
    //    vars.TryGetValue(LinesUtils.CURRENT_INDEX_VAR, out var index);
    //    int.TryParse(index, out var intIndex);
    //    var workingPart = useFirstHalf ? 
    //        OccupancyImages.Take(splitIndex).ToList() : 
    //        OccupancyImages.Skip(splitIndex).ToList();
    //    return workingPart[intIndex];
    //}
}