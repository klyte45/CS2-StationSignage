using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.SceneFlow;
using Game.Simulation;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using JetBrains.Annotations;
using StationSignage.Formulas;
using StationSignage.Models;
using StationSignage.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Color = UnityEngine.Color;
using PublicTransport = Game.Vehicles.PublicTransport;
using SubObject = Game.Objects.SubObject;
using Transform = Game.Objects.Transform;
using TransportStop = Game.Routes.TransportStop;
namespace StationSignage.Systems;

public partial class TransportUtilitySystem : GameSystemBase
{
    protected override void OnUpdate()
    {

    }
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = World.GetExistingSystemManaged<SimulationSystem>();
        _linesSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
        _nameSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        LineBinding = (index, type) =>
        {
            try
            {
                return GetLine(index, type);
            }
            catch
            {
                return null;
            }
        };
    }

    private uint CacheGenerationFrame = 0;
    private uint CurrentCacheFrame => m_SimulationSystem.frameIndex >> 7;

    private LinesSystem _linesSystem;
    private NameSystem _nameSystem;
    public const string SubwayEntityName = "SubwayLine";
    private string TrainEntityName = "Train";
    private readonly Regex LettersRegex = new Regex("[^a-zA-Z]+");
    private readonly Dictionary<int, Matrix4x4> OwnerCache = new();

    private static Dictionary<Entity, RouteWaypoint?> _destinationsDictionary = new();
    private SimulationSystem m_SimulationSystem;
    public Func<int, string, LinePanel> LineBinding;
    private const string Transparent = LineUtils.Transparent;

    private readonly Dictionary<(Entity, string, bool), List<TransportLineModel>> cachedStationLines = new();
    private readonly Dictionary<Entity, VehiclePanel> cachedPlatformPanel = new();



    #region Public methods
    public int GetLineCount(string type) => _linesSystem.GetTransportLinesCount(type);
    public List<TransportLineModel> GetStationLines(Entity buildingRef, string type, bool getConnections)
    {
        CheckCacheIsValid();
        if (cachedStationLines.TryGetValue((buildingRef, type, getConnections), out var cachedLines))
        {
            return cachedLines;
        }
        var lineNumberList = new List<TransportLineModel>();
        var buildingOwner = GetOwnerRecursive(buildingRef);
        GetLines(buildingOwner, type, 0, ref lineNumberList, getConnections);
        if (lineNumberList.Count == 0 && EntityManager.TryGetBuffer(buildingOwner, true, out DynamicBuffer<InstalledUpgrade> upgrades))
        {
            for (var i = 0; i < upgrades.Length; ++i)
            {
                GetLines(upgrades[i].m_Upgrade, type, i, ref lineNumberList, getConnections);
            }
        }
        EntityManager.TryGetComponent<Transform>(buildingRef, out var transform);
        SortByOwner(lineNumberList, transform);
        cachedStationLines[(buildingRef, type, getConnections)] = lineNumberList;
        return lineNumberList;
    }


    public VehiclePanel GetVehiclePanel(TransportLineModel line, int platformNumber)
    {
        CheckCacheIsValid();
        if (cachedPlatformPanel.TryGetValue(line.Platform, out var cached)) return cached;
        var panel = RecalculateGetVehiclePanel(line, platformNumber);
        cachedPlatformPanel[line.Platform] = panel;
        return panel;
    }

    #endregion



    private void CheckCacheIsValid()
    {
        if (CacheGenerationFrame != CurrentCacheFrame)
        {
            CacheGenerationFrame = CurrentCacheFrame;
            cachedStationLines.Clear();
            cachedPlatformPanel.Clear();
        }
    }
    private string GetName(string id)
    {
        return GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var name) ? name : "";

    }


    private LinePanel GetLine(int index, string type)
    {
        var line = _linesSystem.GetTransportLines(type).ElementAtOrDefault(index);
        return new LinePanel(
            GetLineStatusText(line),
            LineUtils.GetRouteName(line.entity).Item2,
            line.color,
            GetOnPrimaryColor(line.color),
            GetLineStatusColor(line)
        );
    }


    private void GetLines(
        Entity selectedEntity,
        string type,
        int index,
        ref List<TransportLineModel> lineNumberList,
        bool getConnections
        )
    {
        if (EntityManager.TryGetBuffer(selectedEntity, true, out DynamicBuffer<ConnectedRoute> routes))
        {
            foreach (var route in routes)
            {
                if (EntityManager.TryGetComponent<Owner>(route.m_Waypoint, out var owner))
                {
                    EntityManager.TryGetComponent<Game.Routes.Color>(owner.m_Owner, out var routeColor);
                    EntityManager.TryGetComponent<Transform>(selectedEntity, out var transform);
                    var routeName = LineUtils.GetRouteName(owner.m_Owner);

                    var stops = GetStops(owner.m_Owner);
                    var singleStops = GetSingleStops(stops);
                    var connections = new List<LineConnection>();
                    if (getConnections)
                    {
                        connections = GetConnections(route.m_Waypoint, type, stops, singleStops);
                    }
                    var connectionsTitle = NamesFormulas.GetConnectionsName(Entity.Null);
                    var connectionsLineName = "";
                    if (connections.Count == 0)
                    {
                        connectionsTitle = LineUtils.Empty;
                        connectionsLineName = routeName.Item1;
                    }
                    var lineType = GetStopLineType(selectedEntity);
                    lineNumberList.Add(
                        new TransportLineModel(
                            lineType,
                            routeName.Item1,
                            routeName.Item2,
                            routeColor.m_Color,
                            GetOnPrimaryColor(routeColor.m_Color),
                            singleStops,
                            GetVehicles(owner.m_Owner),
                            route.m_Waypoint,
                            index,
                            GetOperator(lineType, routeName.Item2),
                            GetDestinationBinding(route.m_Waypoint, stops, index)?.Item2,
                            transform,
                            connectionsTitle,
                            connectionsLineName,
                            connections
                        )
                    );
                }
            }

            if (routes.IsEmpty)
            {
                if (EntityManager.TryGetComponent<TransportStop>(selectedEntity, out _))
                {
                    EntityManager.TryGetComponent<Transform>(selectedEntity, out var transform);
                    lineNumberList.Add(GetEmptyStop(transform, selectedEntity));
                }
            }
        }
        else
        {
            if (EntityManager.TryGetComponent<TransportStop>(selectedEntity, out _))
            {

                EntityManager.TryGetComponent<Transform>(selectedEntity, out var transform);
                lineNumberList.Add(GetEmptyStop(transform, selectedEntity));
            }
        }


        if (EntityManager.TryGetBuffer(selectedEntity, true, out DynamicBuffer<SubObject> subObjects))
        {
            for (var i = 0; i < subObjects.Length; ++i)
            {
                GetLines(subObjects[i].m_SubObject, type, i, ref lineNumberList, getConnections);
            }
        }
    }

    private string GetStopLineType(Entity entity)
    {
        var entityDebugName = _nameSystem.GetDebugName(entity);
        var entityName = LettersRegex.Replace(entityDebugName, "");
        var lineType = "Subway";
        if (entityName.Contains("Train"))
        {
            lineType = "Train";
        }

        return lineType;
    }

    private TransportLineModel GetEmptyStop(Transform position, Entity selectedEntity)
    {
        return new TransportLineModel(
            GetStopLineType(selectedEntity),
            LineUtils.Empty,
            LineUtils.Empty,
            Color.black,
            Color.white,
            [],
            [],
            Entity.Null,
            0,
            LineUtils.Transparent,
            LineUtils.Empty,
            position,
            LineUtils.Empty,
            LineUtils.Empty,
            []
        );
    }

    private string GetOperator(string type, string routeName)
    {
        if (type == TrainEntityName)
        {
            return DisplayFormulas.GetTrainOperator(routeName);
        }
        return DisplayFormulas.GetSubwayOperator(routeName);
    }

    private List<LineConnection> GetConnectionLines(Entity waypoint, string type)
    {
        EntityManager.TryGetComponent<Connected>(waypoint, out var connected);
        var building = GetOwnerRecursive(connected.m_Connected);
        var lines = GetStationLines(building, type, false);
        return lines.Select(line =>
            new LineConnection(
                line.Number,
                line.Color,
                line.OnPrimaryColor,
                Color.white,
                line.Type,
                line.OperatorIcon
            )
        ).ToList();
    }

    private Color GetOnPrimaryColor(Color color)
    {
        var luminance = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        return luminance > 0.5 ? Color.black : Color.white;
    }



    private void SortByOwner(List<TransportLineModel> subObjects, Transform owner)
    {
        if (subObjects is not { Count: > 1 })
            return;

        var ownerId = owner.m_Position.GetHashCode() ^ owner.m_Rotation.GetHashCode();
        if (!OwnerCache.TryGetValue(ownerId, out var inverseMatrix))
        {
            inverseMatrix = Matrix4x4.TRS(
                owner.m_Position,
                owner.m_Rotation,
                Vector3.one
            ).inverse;

            OwnerCache[ownerId] = inverseMatrix;
        }

        subObjects.Sort((a, b) =>
        {
            var aIsTrain = a.Type == "Train";
            var bIsTrain = b.Type == "Train";
            if (aIsTrain != bIsTrain)
                return aIsTrain ? -1 : 1;

            return CompareTransforms(a.Position.m_Position, b.Position.m_Position, inverseMatrix);
        });
    }

    private int CompareTransforms(Vector3 aPos, Vector3 bPos, Matrix4x4 inverseMatrix)
    {
        var aLocal = inverseMatrix.MultiplyPoint3x4(aPos);
        var bLocal = inverseMatrix.MultiplyPoint3x4(bPos);

        var compare = bLocal.z.CompareTo(aLocal.z);
        if (compare != 0) return compare;

        compare = aLocal.x.CompareTo(bLocal.x);
        return compare != 0 ? compare : aLocal.y.CompareTo(bLocal.y);
    }

    private List<RouteWaypoint> GetStops(Entity entity)
    {
        if (EntityManager.TryGetBuffer<RouteWaypoint>(entity, true, out var buffer))
        {
            var waypoints = new List<RouteWaypoint>();
            for (var index = 0; index < buffer.Length; ++index)
            {
                if (EntityManager.TryGetComponent(buffer[index].m_Waypoint, out Connected component)
                    && EntityManager.TryGetComponent<TransportStop>(component.m_Connected, out _)
                            && !EntityManager.TryGetComponent<TaxiStand>(component.m_Connected, out _)
                            )
                {
                    waypoints.Add(buffer[index]);
                }
            }
            return waypoints;
        }
        return [];
    }

    private List<RouteWaypoint> GetSingleStops(List<RouteWaypoint> waypoints)
    {

        var singleStops = new Dictionary<Entity, List<RouteWaypoint>>();

        for (var i = 0; i < waypoints.Count; ++i)
        {
            if (EntityManager.TryGetComponent<Connected>(waypoints[i].m_Waypoint, out var connected))
            {
                var owner = GetOwnerRecursive(connected.m_Connected);
                if (singleStops.ContainsKey(owner))
                {
                    singleStops[owner].Add(waypoints[i]);
                }
                else
                {
                    singleStops[owner] = [waypoints[i]];
                }
            }
        }
        return singleStops
            .Where(x => x.Value.Count == 1)
            .Select(x => x.Value.FirstOrDefault())
            .ToList();
    }

    private List<LineConnection> GetConnections(
        Entity platform,
        string type,
        List<RouteWaypoint> routeWaypoints,
        List<RouteWaypoint> singleStops
    )
    {
        var stopIndex = 0;
        var lastStopIndex = 0;
        RouteWaypoint? lastStopWaypoint = null;
        if (_destinationsDictionary.TryGetValue(platform, out var value))
        {
            lastStopWaypoint = value;
        }

        for (var index = 0; index < routeWaypoints.Count; ++index)
        {
            if (platform == routeWaypoints[index].m_Waypoint)
            {
                stopIndex = index;
            }

            if (lastStopWaypoint != null && lastStopWaypoint.Value.m_Waypoint == routeWaypoints[index].m_Waypoint)
            {
                lastStopIndex = index;
            }
        }

        int startIndex;
        int endIndex;
        if (stopIndex > lastStopIndex)
        {
            startIndex = lastStopIndex;
            endIndex = stopIndex;
        }
        else
        {
            startIndex = stopIndex;
            endIndex = lastStopIndex;
        }
        var lines = new List<LineConnection>();
        for (var index = startIndex; index < endIndex; ++index)
        {
            lines.AddRange(GetConnectionLines(routeWaypoints[index].m_Waypoint, type));
        }

        return lines
            .GroupBy(x => x.Number + x.Type)
            .Select(d => d.First())
            .ToList();
    }

    private Entity GetOwnerRecursive(Entity entity)
    {
        return EntityManager.TryGetComponent<Owner>(entity, out var owner) ? GetOwnerRecursive(owner.m_Owner) : entity;
    }

    private List<RouteVehicle> GetVehicles(Entity entity)
    {
        EntityManager.TryGetBuffer<RouteVehicle>(entity, true, out var vehicles);
        var vehiclesList = new List<RouteVehicle>();
        foreach (var vehicle in vehicles)
        {
            vehiclesList.Add(vehicle);
        }
        return vehiclesList;
    }

    private VehiclePanel RecalculateGetVehiclePanel(TransportLineModel line, int platformNumber)
    {
        EntityManager.TryGetComponent<Position>(line.Platform, out var platformPosition);
        EntityManager.TryGetComponent<WaitingPassengers>(line.Platform, out var waitingPassengers);
        var title = GetName("StationSignage.NextTrain");
        var subtitle = GetName("StationSignage.AverageWaitTime");
        var message = FormatWaitTime(waitingPassengers.m_AverageWaitingTime);
        var messageColor = Color.white;
        var occupancyTitle = GetName("StationSignage.LevelOfOccupancy");
        var bikeIcon = Transparent;
        var wheelchairIcon = Transparent;
        var occupancyImagesList = new List<int>();
        var footer = DisplayFormulas.GetWelcomeMessage(line.Type);
        if (_destinationsDictionary.ContainsKey(line.Platform))
        {
            footer = GetRouteBuildingName(_destinationsDictionary[line.Platform]);
        }
        RouteVehicle? closestTrain = null;
        float closestDistance = float.MaxValue;
        foreach (var vehicle in line.Vehicles)
        {
            if (EntityManager.TryGetComponent<TrainNavigation>(vehicle.m_Vehicle, out var vehiclePosition) &&
                EntityManager.TryGetComponent<PathInformation>(vehicle.m_Vehicle, out var pathInformation))
            {
                // Check if the platform matches the vehicle's destination
                if (line.Platform == pathInformation.m_Destination)
                {
                    // Calculate the distance between the platform and the vehicle's front position
                    float distance = math.distance(platformPosition.m_Position, vehiclePosition.m_Front.m_Position);

                    // Update the closest vehicle if this one is closer
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTrain = vehicle;
                    }
                }
            }
        }

        if (closestTrain != null)
        {
            bikeIcon = "BikeRoundIcon";
            wheelchairIcon = "WheelchairRoundIcon";
            EntityManager.TryGetComponent<Controller>(closestTrain.Value.m_Vehicle, out var controller);
            EntityManager.TryGetComponent<PublicTransport>(controller.m_Controller, out var publicTransport);
            EntityManager.TryGetBuffer<LayoutElement>(closestTrain.Value.m_Vehicle, true, out var layoutElements);
            var destinationName =
                GetRouteBuildingName(GetDestinationBinding(closestTrain.Value.m_Vehicle, line.Waypoints));
            if (destinationName is { Length: > 1 })
            {
                footer = destinationName;
            }
            _destinationsDictionary[line.Platform] = GetDestinationBinding(closestTrain.Value.m_Vehicle, line.Waypoints);
            for (var index = 0; index < layoutElements.Length; ++index)
            {
                var layoutElement = layoutElements[index];
                EntityManager.TryGetComponent<PrefabRef>(layoutElement.m_Vehicle, out var prefabRef);
                EntityManager.TryGetComponent<PublicTransportVehicleData>(prefabRef.m_Prefab, out var vehicleData);
                EntityManager.TryGetBuffer<Passenger>(layoutElement.m_Vehicle, true, out var passengers);
                var filledPercentage = (passengers.Length * 100) / vehicleData.m_PassengerCapacity;
                var carType = "Car";
                var engineDirection = "";
                occupancyImagesList.Add(filledPercentage);
            }

            if (publicTransport.m_State.HasFlag(PublicTransportFlags.Boarding))
            {
                title = GetName("StationSignage.TrainOnPlatform");
                subtitle = GetName("StationSignage.BoardingNow");
                messageColor = Color.clear;
            }
            else
            {
                switch (closestDistance)
                {
                    case <= 100 and > 0:
                        title = GetName("StationSignage.TrainOnPlatform");
                        subtitle = GetName("StationSignage.PrepareForBoarding");
                        messageColor = Color.clear;
                        break;
                    case > 0:
                        title = GetName("StationSignage.NextTrain");
                        subtitle = GetName("StationSignage.DistanceToStation");
                        message = GetDistance(closestDistance);
                        messageColor = Color.yellow;
                        break;
                }
            }
        }

        var closestTrainName = "Train Model";
        var trainNameColor = Color.clear;
        if (closestTrain != null)
        {
            closestTrainName = LineUtils.GetTrainName(closestTrain.Value.m_Vehicle);
            trainNameColor = Color.yellow;
        }

        var levelOccupancyColor = Color.white;
        if (occupancyImagesList.Count == 0)
        {
            levelOccupancyColor = Color.clear;
            wheelchairIcon = Transparent;
            bikeIcon = Transparent;
            for (var i = 0; i < 8; i++)
            {
                occupancyImagesList.Add(0);
            }
        }

        return new VehiclePanel(
            title,
            subtitle,
            message,
            occupancyTitle,
            occupancyImagesList,
            closestTrainName,
            wheelchairIcon,
            bikeIcon,
            Color.black,
            footer,
            messageColor,
            trainNameColor,
            levelOccupancyColor
        );
    }

    private string FormatWaitTime(int seconds)
    {
        if (seconds < 60)
        {
            return seconds + " " + GetName("StationSignage.Seconds");
        }
        var time = TimeSpan.FromSeconds(seconds);
        return time.ToString(@"mm\:ss") + " " + GetName("StationSignage.Minutes");
    }

    private string GetDistance(float distance)
    {
        if (distance >= 1000)
        {
            var distanceInKilometers = distance / 1000f;
            return $"{distanceInKilometers:0.0}" + GetName("StationSignage.KilometersAway");
        }
        return $"{distance:0}" + GetName("StationSignage.MetersAway");
    }

    private string GetRouteBuildingName(RouteWaypoint? routeWaypoint)
    {
        if (routeWaypoint == null) return "";
        EntityManager.TryGetComponent<Connected>(routeWaypoint.Value.m_Waypoint, out var connected);
        EntityManager.TryGetComponent<Owner>(connected.m_Connected, out var owner);
        if (EntityManager.TryGetComponent<Owner>(owner.m_Owner, out var buildingOwner))
        {
            return _nameSystem.GetName(buildingOwner.m_Owner).Translate();
        }
        return _nameSystem.GetName(owner.m_Owner).Translate();
    }

    private RouteWaypoint? GetDestinationBinding(Entity vehicle, List<RouteWaypoint> stops)
    {
        try
        {
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();

            EntityManager.TryGetComponent<Controller>(vehicle, out var controller);
            EntityManager.TryGetBuffer<LayoutElement>(controller.m_Controller, true, out var layoutElements);


            EntityManager.TryGetComponent<TrainNavigation>(controller.m_Controller, out var trainNavigation);
            var closestDistance = float3.zero;
            if (EntityManager.TryGetComponent<Position>(stops.FirstOrDefault().m_Waypoint, out var firstStationPosition))
            {
                closestDistance = firstStationPosition.m_Position;
            }
            var distanceFront = math.distance(closestDistance, trainNavigation.m_Front.m_Position);
            var distanceBack = math.distance(closestDistance, trainNavigation.m_Rear.m_Position);

            if (distanceFront < distanceBack)
            {
                return stops[0];
            }

            return stops[stops.Count - 1];
        }
        catch
        {
            return null;
        }
    }

    [CanBeNull]
    private Tuple<RouteWaypoint?, string> GetDestinationBinding(Entity platform, List<RouteWaypoint> stops, int platformNumber)
    {
        try
        {
            if (_destinationsDictionary.ContainsKey(platform))
            {
                return Tuple.Create(_destinationsDictionary[platform], GetRouteBuildingName(_destinationsDictionary[platform]));
            }

            var index = platformNumber switch
            {
                0 => 0,
                _ => stops.Count - 1
            };

            return Tuple.Create<RouteWaypoint?, string>(stops[index], GetRouteBuildingName(stops[index]));
        }
        catch
        {
            return null;
        }
    }

    private string GetLineStatusText(UITransportLineData? transportLineData)
    {
        try
        {
            if (transportLineData == null) return LineUtils.Empty;

            if (!transportLineData.Value.active)
            {
                return GetName("StationSignage.NotOperating");
            }

            if (transportLineData.Value.vehicles == 0)
            {
                return GetName("StationSignage.OperationStopped");
            }

            if (transportLineData.Value.vehicles == 1)
            {
                return GetName("StationSignage.ReducedSpeed");
            }

            if (transportLineData.Value.usage == 0.0)
            {
                return GetName("StationSignage.NoUsage");
            }

            return GetName("StationSignage.NormalOperation");
        }
        catch (Exception e)
        {
            Mod.log.Info(e);
            return LineUtils.Empty;
        }
    }

    private Color GetLineStatusColor(UITransportLineData? transportLineData)
    {
        try
        {
            if (transportLineData == null) return Color.clear;

            if (!transportLineData.Value.active)
            {
                return Color.red;
            }

            if (transportLineData.Value.vehicles == 0)
            {
                return Color.red;
            }

            if (transportLineData.Value.vehicles == 1)
            {
                return Color.yellow;
            }

            if (transportLineData.Value.usage == 0.0)
            {
                return Color.yellow;
            }

            return Color.green;
        }
        catch (Exception e)
        {
            Mod.log.Info(e);
            return Color.clear;
        }
    }
}//*/


