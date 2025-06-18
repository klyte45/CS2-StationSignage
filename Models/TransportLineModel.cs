using System;
using System.Collections.Generic;
using Game.Objects;
using Game.Routes;
using StationSignage.Components;
using StationSignage.Utils;
using Unity.Entities;
using Unity.Mathematics;

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
    ServiceOperator operatorName,
    string destination,
    Transform position,
    string connectionsTitle,
    string connectionsLineName,
    List<LineConnection> connections
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
    public ServiceOperator OperatorName = operatorName;
    public UnityEngine.Color OnPrimaryColor = onPrimaryColor;
    public string Destination = destination;
    public string OperatorIcon = operatorName + "Icon";
    public string OperatorImage = operatorName.ToString();
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