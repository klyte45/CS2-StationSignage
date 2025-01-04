using System.Collections.Generic;
using System.Drawing;
using Game.Routes;
using Game.Vehicles;
using Unity.Entities;

namespace StationVisuals.Models;

public class TransportLineModel(
    string type, 
    string name, 
    string number,
    UnityEngine.Color color,
    UnityEngine.Color onPrimaryColor,
    List<RouteWaypoint> waypoints,
    List<RouteVehicle> vehicles,
    Entity platform,
    int index,
    string operatorName
)
{
    public string Type = type;
    public string Name = name;
    public string Number = number;
    public UnityEngine.Color Color = color;
    public List<RouteWaypoint> Waypoints = waypoints;
    public readonly List<RouteVehicle> Vehicles = vehicles;
    public Entity Platform = platform;
    public int Index = index;
    public string OperatorName = operatorName;
    public UnityEngine.Color OnPrimaryColor = onPrimaryColor;
}