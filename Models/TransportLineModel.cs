using System;
using System.Collections.Generic;
using Game.Routes;
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
    string operatorName,
    string destination,
    float3 position,
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
    public string OperatorName = operatorName;
    public UnityEngine.Color OnPrimaryColor = onPrimaryColor;
    public string Destination = destination;
    public string OperatorIcon = operatorName + "Icon";
    public string OperatorImage = operatorName;
    public float3 Position = position;
    public List<LineConnection> Connections = connections;
    public string ConnectionsTitle = connectionsTitle;
    public string ConnectionsLineName = connectionsLineName;
    
    public string GetSubwayConnectionOperatorIcon() => OperatorIcon + "Icon";
    
    public string GetTrainConnectionOperatorIcon() => OperatorIcon + "Icon";

    public LineConnection GetFirstSubwayConnection() => GetConnectionOrEmpty(0);
    
    public LineConnection GetSecondSubwayConnection() => GetConnectionOrEmpty(1);
    
    public LineConnection GetThirdSubwayConnection() => GetConnectionOrEmpty(2);
    
    public LineConnection GetFourthSubwayConnection() => GetConnectionOrEmpty(3);
    
    public LineConnection GetFirstTrainConnection() => GetConnectionOrEmpty(0);
    
    public LineConnection GetSecondTrainConnection() => GetConnectionOrEmpty(1);
    
    public LineConnection GetThirdTrainConnection() => GetConnectionOrEmpty(2);
    
    public LineConnection GetFourthTrainConnection() => GetConnectionOrEmpty(3);

    private LineConnection GetConnectionOrEmpty(int connectionIndex)
    {
        try
        {
            return Connections[connectionIndex];
        }
        catch (Exception)
        {
            return new LineConnection(
                "1",
                UnityEngine.Color.yellow,
                UnityEngine.Color.black,
                UnityEngine.Color.white,
                ""
            );
        }
    }
}