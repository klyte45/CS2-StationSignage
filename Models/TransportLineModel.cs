using Game.Objects;
using Game.Routes;
using StationSignage.Utils;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace StationSignage.Models;

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
    string operatorName,
    string destination,
    Transform position,
    string connectionsTitle,
    string connectionsLineName,
    List<LineConnection> connections
)
{
    public string Type = type;
    public int TypeInt = type == "Train" ? 1 : 2;
    public string Name = name;
    public string Number = number;
    public UnityEngine.Color Color = color;
    public List<RouteWaypoint> Waypoints = waypoints;
    public readonly List<RouteVehicle> Vehicles = vehicles;
    public Entity Platform = platform;
    public int Index = index;
    public string OperatorName = operatorName;
    public UnityEngine.Color OnPrimaryColor = onPrimaryColor;
    public string Destination = destination;
    public string OperatorIcon = "SquareLogo" + operatorName;
    public string OperatorImage = "WideSideLogo" + operatorName;
    public Transform Position = position;
    public List<LineConnection> Connections = connections;
    public string ConnectionsTitle = connectionsTitle;
    public string ConnectionsLineName = connectionsLineName;

    private LineConnection GetConnectionOrEmpty(int connectionIndex)
    {
        try
        {
            return Connections[connectionIndex];
        }
        catch (Exception)
        {
            return new LineConnection(
                LineUtils.Empty,
                UnityEngine.Color.clear,
                UnityEngine.Color.clear,
                UnityEngine.Color.clear,
                "",
                LineUtils.Transparent
            );
        }
    }
}