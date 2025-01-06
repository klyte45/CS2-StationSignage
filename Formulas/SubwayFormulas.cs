using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using Colossal.Entities;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.SceneFlow;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using StationVisuals.Models;
using StationVisuals.Utils;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Color = UnityEngine.Color;
using PublicTransport = Game.Vehicles.PublicTransport;
using SubObject = Game.Objects.SubObject;
using TrainFlags = Game.Vehicles.TrainFlags;
using Transform = Game.Objects.Transform;
using TransportStop = Game.Routes.TransportStop;

namespace StationVisuals.Formulas
{
    public class SubwayFormulas
    {
        private static LinesSystem _linesSystem;
        private static NameSystem _nameSystem;
        private static EntityManager _entityManager;
        
        private static readonly string[] ViaMobilidadeLines = ["5", "8", "9", "17"];
        private const string LinhaUni = "6";

        private const string ViaQuatro = "4";
        
        private const string Transparent = "Transparent";

        private const string Empty = " ";

        public SubwayFormulas()
        {
            _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private static readonly StringDictionary  ModelsDictionary = new()
        {
            { "SubwayCar01", "A" },
            { "SubwayEngine01", "A" },
            { "EU_TrainPassengerCar01", "B" },
            { "EU_TrainPassengerEngine01", "B" },
            { "NA_TrainPassengerCar01", "C" },
            { "NA_TrainPassengerEngine01", "C" },
        };
        
        private static StringDictionary  _destinationsDictionary = new();
        
        private static string GetName(string id)
        {
            return GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var name) ? name : "";
        }

        private static readonly Func<int, LinePanel?> LineBinding = (index) =>
        {
            try
            {
                return GetLine(index);
            }
            catch (Exception e)
            {
                Mod.log.Info(e);
                return null;
            }
        };
        
        private static readonly Func<Entity, int, TransportLineModel> PlatformLineBinding = (entity, platform) =>
        {
            try
            {
                return GetStationLines(entity)[platform];
            }
            catch (Exception e)
            {
                Mod.log.Info(e);
                return new TransportLineModel(
                    "",
                    "",
                    "",
                    Color.clear,
                    Color.clear,
                    [],
                    [],
                    Entity.Null,
                    0,
                    Transparent,
                    "",
                    new float3()
                );
            }
        };
        
        private static readonly Func<TransportLineModel, VehiclePanel> VehiclePanelBinding = (transportLineModel) =>
        {
            try
            {
                return GetVehiclePanel(transportLineModel);
            }
            catch (Exception e)
            {
                Mod.log.Info(e);
                return new VehiclePanel(
                    "",
                    "",
                    "",
                    "",
                    [Transparent, Transparent, Transparent, Transparent, Transparent, Transparent, Transparent, Transparent],
                    "",
                    Transparent,
                    Transparent,
                    Color.clear,
                    ""
                );
            }
        };
        
        private static readonly Func<string> TimeNameBinding = () => GetName("StationVisuals.Time") + DateTime.Now.ToString("HH:mm tt");

        private static LinePanel GetLine(int index)
        {
            _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            var line = _linesSystem.GetTransportLines()[index];
            return new LinePanel(
                GetLineStatusText(line),
                GetLineName(line),
                line.color,
                GetOnPrimaryColor(line.color),
                GetLineStatusColor(line)
            );
        }
        
        private static void GetLines(EntityManager entityManager, Entity selectedEntity, int index, ref List<TransportLineModel> lineNumberList)
        {
            if (entityManager.TryGetBuffer(selectedEntity, true, out DynamicBuffer<ConnectedRoute> routes))
            {
                foreach (var route in routes) {
                    if (entityManager.TryGetComponent<Owner>(route.m_Waypoint, out var owner))
                    {
                        entityManager.TryGetComponent<RouteNumber>(owner.m_Owner, out var routeNumber);
                        entityManager.TryGetComponent<Game.Routes.Color>(owner.m_Owner, out var routeColor);
                        entityManager.TryGetComponent<Transform>(selectedEntity, out var transform);
                        var operatorLogo = "Operator01";

                        var fullLineName = _nameSystem.GetName(owner.m_Owner).Translate();

                        var lineName = fullLineName.Split(' ').LastOrDefault();
                        var routeString = lineName is { Length: >= 1 and <= 2 } ? lineName : routeNumber.m_Number.ToString();
                        if (ViaMobilidadeLines.Contains(routeString))
                        {
                            operatorLogo = "Operator02";
                        } else if (ViaQuatro == routeString)
                        {
                            operatorLogo = "Operator03";
                        } else if (LinhaUni == routeString)
                        {
                            operatorLogo = "Operator04";
                        }

                        var stops = GetStops(owner.m_Owner);
                        lineNumberList.Add(
                            new TransportLineModel(
                                "Subway",
                                fullLineName,
                                routeString,
                                routeColor.m_Color,
                                GetOnPrimaryColor(routeColor.m_Color),
                                GetStops(owner.m_Owner),
                                GetVehicles(owner.m_Owner),
                                route.m_Waypoint,
                                index,
                                operatorLogo,
                                GetDestinationBinding(route.m_Waypoint, stops, index),
                                transform.m_Position
                            )
                        );
                    }
                }
            }
        
            if (entityManager.TryGetBuffer(selectedEntity, true, out DynamicBuffer<SubObject> subObjects))
            {
                for (var i = 0; i < subObjects.Length; ++i)
                {
                    GetLines(entityManager, subObjects[i].m_SubObject, i, ref lineNumberList);
                }
            }
        }
        
        private static Color GetOnPrimaryColor(Color color)
        {
            var luminance = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
            return luminance > 0.5 ? Color.black : Color.white;
        }
        
        private static List<TransportLineModel> GetStationLines(Entity buildingRef)
        {
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var lineNumberList = new List<TransportLineModel>();
            GetLines(_entityManager, buildingRef, 0, ref lineNumberList);
            _entityManager.TryGetComponent<Transform>(buildingRef, out var transform);

            return lineNumberList.OrderBy(x => math.distance(transform.m_Position, x.Position)).ToList();
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
            return waypoints.Take((waypoints.Count / 2) + 1).ToList();
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

        private static VehiclePanel GetVehiclePanel(TransportLineModel line)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _entityManager.TryGetComponent<Position>(line.Platform, out var platformPosition);
            _entityManager.TryGetComponent<WaitingPassengers>(line.Platform, out var waitingPassengers);
            var platformDebugName = _nameSystem.GetDebugName(line.Platform);
            var title = GetName("StationVisuals.NextTrain");
            var subtitle = GetName("StationVisuals.AverageWaitTime");
            var message = waitingPassengers.m_AverageWaitingTime + GetName("StationVisuals.Seconds");
            var occupancyTitle = Empty;
            var bikeIcon = Transparent;
            var wheelchairIcon = Transparent;
            var occupancyImagesList = new List<string>();
            var footer = GetWelcomeMessage(Entity.Null);
            if (_destinationsDictionary.ContainsKey(platformDebugName))
            {
                footer = _destinationsDictionary[platformDebugName];
            }
            RouteVehicle? closestTrain = null;
            foreach (var vehicle in line.Vehicles)
            {
                _entityManager.TryGetComponent<TrainNavigation>(vehicle.m_Vehicle, out var vehiclePosition);
                _entityManager.TryGetComponent<PathInformation>(vehicle.m_Vehicle, out var pathInformation);
                var closestDistance = math.distance(platformPosition.m_Position, vehiclePosition.m_Front.m_Position);
                if (line.Platform == pathInformation.m_Destination)
                {
                    occupancyTitle = GetName("StationVisuals.LevelOfOccupancy");
                    bikeIcon = "BikeRoundIcon";
                    wheelchairIcon = "WheelchairRoundIcon";
                    _entityManager.TryGetComponent<Controller>(vehicle.m_Vehicle, out var controller);
                    _entityManager.TryGetComponent<PublicTransport>(controller.m_Controller, out var publicTransport);
                    _entityManager.TryGetBuffer<LayoutElement>(vehicle.m_Vehicle, true, out var layoutElements);
                    footer = GetDestinationBinding(pathInformation, line.Waypoints);
                    if (publicTransport.m_State.HasFlag(PublicTransportFlags.Arriving))
                    {
                        _destinationsDictionary[platformDebugName] = GetDestinationBinding(pathInformation, line.Waypoints);
                    }
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
                   
                    closestTrain = vehicle;
                    if (publicTransport.m_State.HasFlag(PublicTransportFlags.Boarding))
                    {
                        title = GetName("StationVisuals.TrainOnPlatform");
                        subtitle = GetName("StationVisuals.BoardingNow");
                        message = Empty;
                    }
                    else
                    {
                        switch (closestDistance)
                        {
                            case <= 100 and > 0:
                                title = GetName("StationVisuals.TrainOnPlatform");
                                subtitle = GetName("StationVisuals.PrepareForBoarding");
                                message = Empty;
                                break;
                            case > 0:
                                title = GetName("StationVisuals.NextTrain");
                                subtitle = GetName("StationVisuals.DistanceToStation");
                                message = ((int)closestDistance) + GetName("StationVisuals.MetersAway");
                                break;
                        }
                    }
                    break;
                }
            }

            var closestTrainName = Empty;
            if (closestTrain != null)
            {
                closestTrainName = GetTrainNameBinding(closestTrain.Value.m_Vehicle);
            }

            if (occupancyImagesList.Count == 0)
            {
                wheelchairIcon = Transparent;
                bikeIcon = Transparent;
                for (var i = 0; i < 8; i++)
                {
                    occupancyImagesList.Add(Transparent);
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
                footer
            );
        }
        
        private static string GetTrainNameBinding(Entity entityRef)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            _entityManager.TryGetComponent<Controller>(entityRef, out var controller);
            _entityManager.TryGetComponent<Owner>(controller.m_Controller, out var owner);
            _entityManager.TryGetBuffer<LayoutElement>(controller.m_Controller, true, out var layoutElements);
            _entityManager.TryGetBuffer<OwnedVehicle>(owner.m_Owner, true, out var ownerVehicles);
            var index = 0;
    
            for (var i = 0; i < ownerVehicles.Length; ++i)
            {
                if (ownerVehicles[i].m_Vehicle == controller.m_Controller)
                {
                    index = i;
                }
            }

            index++;
            var entityDebugName = _nameSystem.GetDebugName(entityRef);
            var entityName = entityDebugName.TrimEnd(' ').Remove(entityDebugName.LastIndexOf(' ') + 1);
            var letter = "F";
            if (ModelsDictionary.ContainsKey(entityName))
            {
                letter = ModelsDictionary[entityName];
            }

            if (entityName.Contains("Subway"))
            {
                letter = "A";
            }
            if (entityName.Contains("EU_Train"))
            {
                letter = "B";
            }
            if (entityName.Contains("NA_Train"))
            {
                letter = "C";
            }
            if (index < 10)
            {
                return letter + "0" + index;
            }

            return letter + index;
        }
        
        private static string GetRouteBuildingName(RouteWaypoint routeWaypoint)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _entityManager.TryGetComponent<Connected>(routeWaypoint.m_Waypoint, out var connected);
            _entityManager.TryGetComponent<Owner>(connected.m_Connected, out var owner);
            if (_entityManager.TryGetComponent<Owner>(owner.m_Owner, out var buildingOwner))
            {
                return _nameSystem.GetName(buildingOwner.m_Owner).Translate();
            }
            return _nameSystem.GetName(owner.m_Owner).Translate();
        }
        
        private static string GetDestinationBinding(PathInformation path, List<RouteWaypoint> stops)
        {
            try
            {
                _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
                
                var originIndex = stops.FindIndex(x => x.m_Waypoint == path.m_Origin);
                var destinationIndex = stops.FindIndex(x => x.m_Waypoint == path.m_Destination);
            
                if (originIndex < destinationIndex)
                {
                    return GetRouteBuildingName(stops[stops.Count - 1]);
                }

                return GetRouteBuildingName(stops[0]);
            }
            catch (Exception e)
            {
                return "";
            }
        }
        
        private static string GetDestinationBinding(Entity platform, List<RouteWaypoint> stops, int platformNumber)
        {
            try
            {
                var platformName = _nameSystem.GetDebugName(platform);
                if (_destinationsDictionary.ContainsKey(platformName))
                {
                    return _destinationsDictionary[platformName];
                }

                var index = platformNumber switch
                {
                    0 => stops.Count - 1,
                    _ => 0
                };

                return GetRouteBuildingName(stops[index]);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        private static string GetLineName(UITransportLineData? transportLineData)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            try
            {
                if (transportLineData == null) return Empty;
                _entityManager.TryGetComponent<RouteNumber>(transportLineData.Value.entity, out var routeNumber);
                var lineName = _nameSystem.GetName(transportLineData.Value.entity).Translate().Split(' ').LastOrDefault();
                return lineName is { Length: >= 1 and <= 2 } ? lineName : routeNumber.m_Number.ToString();
            }
            catch (Exception e)
            {
                Mod.log.Info(e);
                return Empty;
            }
        }
        
        private static Color GetLineColor(UITransportLineData? transportLineData)
        {
            try
            {
                return transportLineData?.color ?? Color.clear;
            }
            catch (Exception e)
            {
                Mod.log.Info(e);
                return Color.clear;
            }
        }
        
        private static string GetLineStatusText(UITransportLineData? transportLineData)
        {
            try
            {
                if (transportLineData == null) return Empty;

                if (!transportLineData.Value.active)
                {
                    return GetName("StationVisuals.NotOperating");
                }

                if (transportLineData.Value.vehicles == 0)
                {
                    return GetName("StationVisuals.OperationStopped");
                }
                
                if (transportLineData.Value.vehicles == 1)
                {
                    return GetName("StationVisuals.ReducedSpeed");
                }
                
                if (transportLineData.Value.usage == 0.0)
                {
                    return GetName("StationVisuals.NoUsage");
                }

                return GetName("StationVisuals.NormalOperation");
            }
            catch (Exception e)
            {
                Mod.log.Info(e);
                return Empty;
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
        
        public static LinePanel GetFirstLine(Entity buildingRef) => LineBinding.Invoke(0);
        
        public static LinePanel? GetSecondLine(Entity buildingRef) => LineBinding.Invoke(1);
        
        public static LinePanel? GetThirdLine(Entity buildingRef) => LineBinding.Invoke(2);
        
        public static LinePanel? GetFourthLine(Entity buildingRef) => LineBinding.Invoke(3);
        
        public static LinePanel? GetFifthLine(Entity buildingRef) => LineBinding.Invoke(4);
        
        public static string GetTimeString(Entity buildingRef) => 
            TimeNameBinding.Invoke() ?? Empty;
        
        public static string GetLinesStatusMessage(Entity buildingRef) => 
            GetName("StationVisuals.LineStatus");

        public static string GetWelcomeMessage(Entity buildingRef) => 
            "Welcome to Moema's Subway";
        
        public static TransportLineModel GetFirstPlatformLine(Entity buildingRef) => 
            PlatformLineBinding.Invoke(buildingRef, 0);
        
        public static VehiclePanel GetFirstPlatformVehiclePanel(Entity buildingRef) =>
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 0));
        
        public static TransportLineModel GetSecondPlatformLine(Entity buildingRef) => 
            PlatformLineBinding.Invoke(buildingRef, 1);
        
        public static VehiclePanel GetSecondPlatformVehiclePanel(Entity buildingRef) =>
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 1));
        
        public static TransportLineModel GetThirdPlatformLine(Entity buildingRef) => 
            PlatformLineBinding.Invoke(buildingRef, 2);
        
        public static VehiclePanel GetThirdPlatformVehiclePanel(Entity buildingRef) =>
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 2));
        
        public static TransportLineModel GetFourthPlatformLine(Entity buildingRef) => 
            PlatformLineBinding.Invoke(buildingRef, 3);
        
        public static VehiclePanel GetFourthPlatformVehiclePanel(Entity buildingRef) =>
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 3));
    }
}