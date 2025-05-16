using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Colossal.Entities;
using Game.Buildings;
using Game.Common;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.SceneFlow;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using JetBrains.Annotations;
using StationSignage.Models;
using StationSignage.Utils;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Color = UnityEngine.Color;
using PublicTransport = Game.Vehicles.PublicTransport;
using SubObject = Game.Objects.SubObject;
using Transform = Game.Objects.Transform;
using TransportStop = Game.Routes.TransportStop;

namespace StationSignage.Formulas;

public static class TransportFormulas
{
    
    private static LinesSystem _linesSystem;
    private static NameSystem _nameSystem;
    private static EntityManager _entityManager;
    public const string SubwayEntityName = "SubwayLine";
    private const string TrainEntityName = "Train";
    private static readonly Regex LettersRegex = new Regex("[^a-zA-Z]+");
    private static readonly Dictionary<int, Matrix4x4> OwnerCache = new();
    
     private static Dictionary<Entity, RouteWaypoint?> _destinationsDictionary = new();
        
        private static string GetName(string id)
        {
            return GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var name) ? name : "";
        }

        public static readonly Func<int, string, LinePanel?> LineBinding = (index, type) =>
        {
            try
            {
                return GetLine(index, type);
            }
            catch (Exception e)
            {
                return null;
            }
        };

        private const string Transparent = LineUtils.Transparent;

        private static LinePanel GetLine(int index, string type)
        {
            _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            var line = _linesSystem.GetTransportLines(type)[index];
            return new LinePanel(
                GetLineStatusText(line),
                LineUtils.GetRouteName(line.entity).Item2,
                line.color,
                GetOnPrimaryColor(line.color),
                GetLineStatusColor(line)
            );
        }
        
        public static int GetLineCount(string type)
        {
            _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            return _linesSystem.GetTransportLines(type).Count;
        }
        
        private static void GetLines(
            Entity selectedEntity,
            string type,
            int index,
            ref List<TransportLineModel> lineNumberList, 
            bool getConnections
            )
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (_entityManager.TryGetBuffer(selectedEntity, true, out DynamicBuffer<ConnectedRoute> routes))
            {
                foreach (var route in routes) {
                    if (_entityManager.TryGetComponent<Owner>(route.m_Waypoint, out var owner))
                    {
                        _entityManager.TryGetComponent<Game.Routes.Color>(owner.m_Owner, out var routeColor);
                        _entityManager.TryGetComponent<Transform>(selectedEntity, out var transform);
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
                    if (_entityManager.TryGetComponent<TransportStop>(selectedEntity, out _))
                    {
                        _entityManager.TryGetComponent<Transform>(selectedEntity, out var transform);
                        lineNumberList.Add(GetEmptyStop(transform, selectedEntity));
                    }
                }
            }
            else
            {
                if (_entityManager.TryGetComponent<TransportStop>(selectedEntity, out _))
                {
                    
                    _entityManager.TryGetComponent<Transform>(selectedEntity, out var transform);
                    lineNumberList.Add(GetEmptyStop(transform, selectedEntity));
                }
            }
            
        
            if (_entityManager.TryGetBuffer(selectedEntity, true, out DynamicBuffer<SubObject> subObjects))
            {
                for (var i = 0; i < subObjects.Length; ++i)
                {
                    GetLines(subObjects[i].m_SubObject, type, i, ref lineNumberList, getConnections);
                }
            }
        }

        private static string GetStopLineType(Entity entity)
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

        private static TransportLineModel GetEmptyStop(Transform position, Entity selectedEntity)
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

        private static string GetOperator(string type, string routeName)
        {
            if (type == TrainEntityName)
            {
                return DisplayFormulas.GetTrainOperator(routeName);
            }
            return DisplayFormulas.GetSubwayOperator(routeName);
        }
        
        private static List<LineConnection> GetConnectionLines(Entity waypoint, string type)
        {
            _entityManager.TryGetComponent<Connected>(waypoint, out var connected);
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
        
        private static Color GetOnPrimaryColor(Color color)
        {
            var luminance = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
            return luminance > 0.5 ? Color.black : Color.white;
        }
        
        public static List<TransportLineModel> GetStationLines(Entity buildingRef, string type, bool getConnections)
        {
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var lineNumberList = new List<TransportLineModel>();
            var buildingOwner = GetOwnerRecursive(buildingRef);
            GetLines(buildingOwner, type, 0, ref lineNumberList, getConnections);
            if (lineNumberList.Count == 0 && _entityManager.TryGetBuffer(buildingOwner, true, out DynamicBuffer<InstalledUpgrade> upgrades))
            {
                for (var i = 0; i < upgrades.Length; ++i)
                {
                    GetLines(upgrades[i].m_Upgrade, type, i, ref lineNumberList, getConnections);
                }
            }
            _entityManager.TryGetComponent<Transform>(buildingRef, out var transform);
            SortByOwner(lineNumberList, transform);
            return lineNumberList;
        }
        
        private static void SortByOwner(List<TransportLineModel> subObjects, Transform owner)
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
        
        private static int CompareTransforms(Vector3 aPos, Vector3 bPos, Matrix4x4 inverseMatrix)
        {
            var aLocal = inverseMatrix.MultiplyPoint3x4(aPos);
            var bLocal = inverseMatrix.MultiplyPoint3x4(bPos);

            var compare = bLocal.z.CompareTo(aLocal.z);
            if (compare != 0) return compare;

            compare = aLocal.x.CompareTo(bLocal.x);
            return compare != 0 ? compare : aLocal.y.CompareTo(bLocal.y);
        }
        
        private static List<RouteWaypoint> GetStops(Entity entity)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var buffer = _entityManager.GetBuffer<RouteWaypoint>(entity, true);
            var waypoints = new List<RouteWaypoint>();
            for (var index = 0; index < buffer.Length; ++index)
            {
                if (_entityManager.TryGetComponent(buffer[index].m_Waypoint, out Connected component) &&
                    _entityManager.HasComponent<TransportStop>(component.m_Connected) &&
                    !_entityManager.HasComponent<TaxiStand>(component.m_Connected))
                {
                    waypoints.Add(buffer[index]);
                }
            }
            return waypoints;
        }
        
        private static List<RouteWaypoint> GetSingleStops(List<RouteWaypoint> waypoints)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            var singleStops = new Dictionary<Entity, List<RouteWaypoint>>();

            for (var i = 0; i < waypoints.Count; ++i)
            {
                if (_entityManager.TryGetComponent<Connected>(waypoints[i].m_Waypoint, out var connected))
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
        
        private static List<LineConnection> GetConnections(
            Entity platform,
            string type,
            List<RouteWaypoint> routeWaypoints,
            List<RouteWaypoint> singleStops
        )
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
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

        private static Entity GetOwnerRecursive(Entity entity)
        {
            return _entityManager.TryGetComponent<Owner>(entity, out var owner) ? GetOwnerRecursive(owner.m_Owner) : entity;
        }
        
        private static List<RouteVehicle> GetVehicles(Entity entity)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _entityManager.TryGetBuffer<RouteVehicle>(entity, true, out var vehicles);
            var vehiclesList = new List<RouteVehicle>();
            foreach (var vehicle in vehicles)
            {
                vehiclesList.Add(vehicle);
            }
            return vehiclesList;
        }

        public static VehiclePanel GetVehiclePanel(TransportLineModel line, int platformNumber, Dictionary<string, string> vars)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _entityManager.TryGetComponent<Position>(line.Platform, out var platformPosition);
            _entityManager.TryGetComponent<WaitingPassengers>(line.Platform, out var waitingPassengers);
            var title = GetName("StationSignage.NextTrain");
            var subtitle = GetName("StationSignage.AverageWaitTime");
            var message = FormatWaitTime(waitingPassengers.m_AverageWaitingTime);
            var messageColor = Color.white;
            var occupancyTitle = GetName("StationSignage.LevelOfOccupancy");
            var bikeIcon = Transparent;
            var wheelchairIcon = Transparent;
            var occupancyImagesList = new List<string>();
            var footer = DisplayFormulas.GetWelcomeMessage(line.Type);
            if (_destinationsDictionary.ContainsKey(line.Platform))
            {
                footer = GetRouteBuildingName(_destinationsDictionary[line.Platform]);
            }
            RouteVehicle? closestTrain = null;
            float closestDistance = float.MaxValue;
            foreach (var vehicle in line.Vehicles)
            {
                if (_entityManager.TryGetComponent<TrainNavigation>(vehicle.m_Vehicle, out var vehiclePosition) &&
                    _entityManager.TryGetComponent<PathInformation>(vehicle.m_Vehicle, out var pathInformation))
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
                    _entityManager.TryGetComponent<Controller>(closestTrain.Value.m_Vehicle, out var controller);
                    _entityManager.TryGetComponent<PublicTransport>(controller.m_Controller, out var publicTransport);
                    _entityManager.TryGetBuffer<LayoutElement>(closestTrain.Value.m_Vehicle, true, out var layoutElements);
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
                        _entityManager.TryGetComponent<PrefabRef>(layoutElement.m_Vehicle, out var prefabRef);
                        _entityManager.TryGetComponent<PublicTransportVehicleData>(prefabRef.m_Prefab, out var vehicleData);
                        _entityManager.TryGetBuffer<Passenger>(layoutElement.m_Vehicle, true, out var passengers);
                        var filledPercentage = (passengers.Length * 100) / vehicleData.m_PassengerCapacity;
                        var carType = "Car";
                        var engineDirection = "";
                        if (index == 0)
                        {
                            carType = "Engine";
                            engineDirection = "Left";
                        }

                        if (index == layoutElements.Length - 1)
                        {
                            carType = "Engine";
                            engineDirection = "Right";
                        }

                        var capacity = filledPercentage switch
                        {
                            <= 5 => "Empty",
                            <= 30 => "Low",
                            <= 50 => "Medium",
                            > 50 => "Filled"
                        };
                        occupancyImagesList.Add(capacity + "Capacity" + engineDirection + carType);
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
                    occupancyImagesList.Add(Transparent);
                }
            }

            return new VehiclePanel(
                vars,
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
        
        private static string FormatWaitTime(int seconds)
        {
            if (seconds < 60)
            {
                return seconds + " " + GetName("StationSignage.Seconds");
            }
            var time = TimeSpan.FromSeconds(seconds);
            return time.ToString(@"mm\:ss") + " " + GetName("StationSignage.Minutes");
        }

        private static string GetDistance(float distance)
        {
            if (distance >= 1000)
            {
                var distanceInKilometers = distance / 1000f;
                return $"{distanceInKilometers:0.0}" + GetName("StationSignage.KilometersAway");
            }
            return $"{distance:0}" + GetName("StationSignage.MetersAway");
        }
        
        private static string GetRouteBuildingName(RouteWaypoint? routeWaypoint)
        {
            if (routeWaypoint == null) return "";
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _entityManager.TryGetComponent<Connected>(routeWaypoint.Value.m_Waypoint, out var connected);
            _entityManager.TryGetComponent<Owner>(connected.m_Connected, out var owner);
            if (_entityManager.TryGetComponent<Owner>(owner.m_Owner, out var buildingOwner))
            {
                return _nameSystem.GetName(buildingOwner.m_Owner).Translate();
            }
            return _nameSystem.GetName(owner.m_Owner).Translate();
        }
        
        private static RouteWaypoint? GetDestinationBinding(Entity vehicle, List<RouteWaypoint> stops)
        {
            try
            {
                _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();

                _entityManager.TryGetComponent<Controller>(vehicle, out var controller);
                _entityManager.TryGetBuffer<LayoutElement>(controller.m_Controller, true, out var layoutElements);
            
            
                _entityManager.TryGetComponent<TrainNavigation>(controller.m_Controller, out var trainNavigation);
                var closestDistance = float3.zero;
                if (_entityManager.TryGetComponent<Position>(stops.FirstOrDefault().m_Waypoint, out var firstStationPosition))
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
            catch (Exception e)
            {
                return null;
            }
        }
        
        [CanBeNull]
        private static Tuple<RouteWaypoint?, string> GetDestinationBinding(Entity platform, List<RouteWaypoint> stops, int platformNumber)
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
            catch (Exception e)
            {
                return null;
            }
        }
        
        private static string GetLineStatusText(UITransportLineData? transportLineData)
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
        
        private static Color GetLineStatusColor(UITransportLineData? transportLineData)
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
}