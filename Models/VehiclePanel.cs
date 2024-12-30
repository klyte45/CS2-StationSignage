using System.Collections.Generic;

namespace StationVisuals.Models;

public class VehiclePanel(
    string title, 
    string subtitle, 
    string trainMessage,
    string occupancyTitle,
    List<string> occupancyImages,
    string trainName
)
{
    public readonly string Title = title;
    public readonly string Subtitle = subtitle;
    public readonly string TrainMessage = trainMessage;
    public string OccupancyTitle = occupancyTitle;
    public List<string> OccupancyImages = occupancyImages;
    public string TrainName = trainName;
}