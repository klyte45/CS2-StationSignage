using System.Collections.Generic;
using System.Drawing;
using Game.Routes;
using Game.Vehicles;
using Unity.Entities;

namespace StationVisuals.Models;

public class TransportLineModel(
    string type, 
    string name, 
    UnityEngine.Color color,
    List<RouteWaypoint> waypoints,
    List<RouteVehicle> vehicles,
    Entity platform
)
{
    public string Type = type;
    public string Name = name;
    public UnityEngine.Color Color = color;
    public List<RouteWaypoint> Waypoints = waypoints;
    public readonly List<RouteVehicle> Vehicles = vehicles;
    public Entity Platform = platform;
}