using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Colossal.Entities;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using StationVisuals.Models;
using StationVisuals.Utils;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Color = UnityEngine.Color;
using SubObject = Game.Objects.SubObject;
using TrainFlags = Game.Vehicles.TrainFlags;
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

        private static readonly StringDictionary  ModelsDictionary = new()
        {
            { "SubwayCar01", "A" },
            { "SubwayEngine01", "A" },
            { "EU_TrainPassengerCar01", "B" },
            { "EU_TrainPassengerEngine01", "B" },
            { "NA_TrainPassengerCar01", "C" },
            { "NA_TrainPassengerEngine01", "C" },
        };

        private static readonly Func<int, UITransportLineData?> LineBinding = (index) =>
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
                return null;
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
                return null;
            }
        };
        
        private static readonly Func<UITransportLineData?, string> LineNameBinding = GetLineName;
        
        private static readonly Func<UITransportLineData?, Color> LineColorBinding = GetLineColor;
        
        private static readonly Func<UITransportLineData?, string> LineStatusTextBinding = GetLineStatusText;
        
        private static readonly Func<UITransportLineData?, Color> LineStatusColorBinding = GetLineStatusColor;
        
        private static readonly Func<string> TimeNameBinding = () => "Time: " + DateTime.Now.ToString("HH:mm tt");

        private static UITransportLineData GetLine(int index)
        {
            _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            return _linesSystem.GetTransportLines()[index];
        }
        
        private static void GetLines(EntityManager entityManager, Entity selectedEntity, ref List<TransportLineModel> lineNumberList)
        {
            if (entityManager.TryGetBuffer(selectedEntity, true, out DynamicBuffer<ConnectedRoute> routes))
            {
                foreach (var route in routes) {
                    if (entityManager.TryGetComponent<Owner>(route.m_Waypoint, out var owner))
                    {
                        entityManager.TryGetComponent<RouteNumber>(owner.m_Owner, out var routeNumber);
                        entityManager.TryGetComponent<Game.Routes.Color>(owner.m_Owner, out var routeColor);

                        var lineName = _nameSystem.GetRenderedLabelName(owner.m_Owner).Split(' ').LastOrDefault();
                        var routeString = lineName is { Length: >= 1 and <= 2 } ? lineName : routeNumber.m_Number.ToString();
                        lineNumberList.Add(
                            new TransportLineModel(
                                "Subway", 
                                routeString, 
                                routeColor.m_Color, 
                                GetStops(owner.m_Owner),
                                GetVehicles(owner.m_Owner),
                                route.m_Waypoint
                                )
                            );
                    }
                }
            }
        
            if (entityManager.TryGetBuffer(selectedEntity, true, out DynamicBuffer<SubObject> subObjects))
            {
                foreach (var subObject in subObjects)
                {
                    GetLines(entityManager, subObject.m_SubObject, ref lineNumberList);
                }
            }
        }
        
        private static List<TransportLineModel> GetStationLines(Entity buildingRef)
        {
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var lineNumberList = new List<TransportLineModel>();
            GetLines(_entityManager, buildingRef, ref lineNumberList);

            return lineNumberList;
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
            var title = "Next Train";
            var subtitle = "Average wait time:";
            var message = waitingPassengers.m_AverageWaitingTime + " seconds";
            var occupancyTitle = "";
            var occupancyImagesList = new List<string>();
            RouteVehicle? closestTrain = null;
            foreach (var vehicle in line.Vehicles)
            {
                _entityManager.TryGetComponent<TrainNavigation>(vehicle.m_Vehicle, out var vehiclePosition);
                _entityManager.TryGetComponent<PathInformation>(vehicle.m_Vehicle, out var pathInformation);
                var distance = math.distance(platformPosition.m_Position, vehiclePosition.m_Front.m_Position);
                if (line.Platform == pathInformation.m_Destination)
                {
                    occupancyTitle = "Level of occupancy";
                    _entityManager.TryGetComponent<Controller>(vehicle.m_Vehicle, out var controller);
                    _entityManager.TryGetComponent<Train>(controller.m_Controller, out var train);
                    _entityManager.TryGetBuffer<LayoutElement>(vehicle.m_Vehicle, true, out var layoutElements);
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
                   
                    var closestDistance = distance;
                    closestTrain = vehicle;
                    if (train.m_Flags.Equals(TrainFlags.BoardingLeft) || train.m_Flags.Equals(TrainFlags.BoardingRight))
                    {
                        title = "Train on Platform";
                        subtitle = "Boarding Now";
                        message = "";
                    }
                    else
                    {
                        switch (closestDistance)
                        {
                            case < 72 and > 0:
                                title = "Train on Platform";
                                subtitle = "Prepare for boarding";
                                message = "";
                                break;
                            case > 0:
                                title = "Next Train";
                                subtitle = "Distance to station";
                                message = ((int)closestDistance) + " meters away";
                                break;
                        }
                    }
                }
            }

            var closestTrainName = "";
            if (closestTrain != null)
            {
                closestTrainName = GetTrainNameBinding(closestTrain.Value.m_Vehicle);
            }

            if (occupancyImagesList.Count == 0)
            {
                for (var i = 0; i < 8; i++)
                {
                    occupancyImagesList.Add("Transparent");
                }
            }

            return new VehiclePanel(
                title,
                subtitle,
                message,
                occupancyTitle,
                occupancyImagesList,
                closestTrainName
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
            int index = 0;
    
            for (int i = 0; i < ownerVehicles.Length; ++i)
            {
                if (ownerVehicles[i].m_Vehicle == controller.m_Controller)
                {
                    index = i;
                }
            }

            index++;
            var entityDebugName = _nameSystem.GetDebugName(entityRef);
            var entityName = entityDebugName.TrimEnd(' ').Remove(entityDebugName.LastIndexOf(' ') + 1);
            string letter = "F";
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

        private static string GetLineName(UITransportLineData? transportLineData)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            try
            {
                if (transportLineData == null) return "";
                _entityManager.TryGetComponent<RouteNumber>(transportLineData.Value.entity, out var routeNumber);
                var lineName = _nameSystem.GetName(transportLineData.Value.entity).Translate().Split(' ').LastOrDefault();
                return lineName is { Length: >= 1 and <= 2 } ? lineName : routeNumber.m_Number.ToString();
            }
            catch (Exception e)
            {
                Mod.log.Info(e);
                return "";
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
                if (transportLineData == null) return "";

                if (!transportLineData.Value.active)
                {
                    return "Not operating";
                }

                if (transportLineData.Value.vehicles == 0)
                {
                    return "Operation Stopped";
                }
                
                if (transportLineData.Value.vehicles == 1)
                {
                    return "Reduced speed";
                }
                
                if (transportLineData.Value.usage == 0.0)
                {
                    return "No usage";
                }

                return "Normal Operation";
            }
            catch (Exception e)
            {
                Mod.log.Info(e);
                return "";
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
        
        public static string GetFirstLineName(Entity buildingRef) => 
            LineNameBinding?.Invoke(LineBinding.Invoke(0)) ?? "";
        
        public static string GetSecondLineName(Entity buildingRef) => 
            LineNameBinding?.Invoke(LineBinding.Invoke(1)) ?? "";
        
        public static string GetThirdLineName(Entity buildingRef) => 
            LineNameBinding?.Invoke(LineBinding.Invoke(2)) ?? "";
        
        public static string GetFourthLineName(Entity buildingRef) => 
            LineNameBinding?.Invoke(LineBinding.Invoke(3)) ?? "";
        
        public static string GetFifthLineName(Entity buildingRef) => 
            LineNameBinding?.Invoke(LineBinding.Invoke(4)) ?? "";
        
        public static Color GetFirstLineColor(Entity buildingRef) => 
            LineColorBinding?.Invoke(LineBinding.Invoke(0)) ?? Color.clear;
        
        public static Color GetSecondLineColor(Entity buildingRef) => 
            LineColorBinding?.Invoke(LineBinding.Invoke(1)) ?? Color.clear;
        
        public static Color GetThirdLineColor(Entity buildingRef) => 
            LineColorBinding?.Invoke(LineBinding.Invoke(2)) ?? Color.clear;
        
        public static Color GetFourthLineColor(Entity buildingRef) => 
            LineColorBinding?.Invoke(LineBinding.Invoke(3)) ?? Color.clear;
        
        public static Color GetFifthLineColor(Entity buildingRef) => 
            LineColorBinding?.Invoke(LineBinding.Invoke(4)) ?? Color.clear;
        
        public static string GetFirstLineStatusText(Entity buildingRef) => 
            LineStatusTextBinding?.Invoke(LineBinding.Invoke(0)) ?? "";
        
        public static string GetSecondLineStatusText(Entity buildingRef) => 
            LineStatusTextBinding?.Invoke(LineBinding.Invoke(1)) ?? "";
        
        public static string GetThirdLineStatusText(Entity buildingRef) => 
            LineStatusTextBinding?.Invoke(LineBinding.Invoke(2)) ?? "";
        
        public static string GetFourthLineStatusText(Entity buildingRef) => 
            LineStatusTextBinding?.Invoke(LineBinding.Invoke(3)) ?? "";
        
        public static string GetFifthLineStatusText(Entity buildingRef) => 
            LineStatusTextBinding?.Invoke(LineBinding.Invoke(4)) ?? "";
        
        public static Color GetFirstLineStatusColor(Entity buildingRef) => 
            LineStatusColorBinding?.Invoke(LineBinding.Invoke(0)) ?? Color.clear;
        
        public static Color GetSecondLineStatusColor(Entity buildingRef) => 
            LineStatusColorBinding?.Invoke(LineBinding.Invoke(1)) ?? Color.clear;
        
        public static Color GetThirdLineStatusColor(Entity buildingRef) => 
            LineStatusColorBinding?.Invoke(LineBinding.Invoke(2)) ?? Color.clear;
        
        public static Color GetFourthLineStatusColor(Entity buildingRef) => 
            LineStatusColorBinding?.Invoke(LineBinding.Invoke(3)) ?? Color.clear;
        
        public static Color GetFifthLineStatusColor(Entity buildingRef) => 
            LineStatusColorBinding?.Invoke(LineBinding.Invoke(4)) ?? Color.clear;
        
        public static string GetTimeString(Entity buildingRef) => 
            TimeNameBinding.Invoke() ?? "";
        
        public static string GetLinesStatusMessage(Entity buildingRef) => 
            "Lines Status";
        
        public static string GetWelcomeMessage(Entity buildingRef) => 
            "Welcome to Moema's Subway";
        
        public static string GetFirstPlatformLineOperatorLogo(Entity buildingRef) => 
            "Operator01";
        
        public static string GetSecondPlatformLineOperatorLogo(Entity buildingRef) => 
            "Operator01";
        
        public static string GetFirstPlatformTrainPanelTitle(Entity buildingRef) => 
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 0)).Title;
        
        public static string GetFirstPlatformTrainPanelSubTitle(Entity buildingRef) => 
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 0)).Subtitle;
        
        public static string GetFirstPlatformTrainPanelMessage(Entity buildingRef) => 
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 0)).TrainMessage;
        
        public static string GetFirstPlatformTrainPanelOccupancyTitle(Entity buildingRef) =>
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 0)).OccupancyTitle;
        
        public static string GetFirstPlatformTrainPanelTrainName(Entity buildingRef) => 
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 0)).TrainName;
        
        public static string GetSecondPlatformTrainPanelTitle(Entity buildingRef) => 
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 1)).Title;
        
        public static string GetSecondPlatformTrainPanelSubTitle(Entity buildingRef) => 
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 1)).Subtitle;
        
        public static string GetSecondPlatformTrainPanelMessage(Entity buildingRef) => 
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 1)).TrainMessage;
        
        public static string GetSecondPlatformTrainPanelTrainName(Entity buildingRef) =>
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 1)).TrainName;
        
        public static string GetSecondPlatformTrainPanelOccupancyTitle(Entity buildingRef) => 
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 1)).OccupancyTitle;
        
        public static string GetFirstPlatformCar4Image(Entity buildingRef) => 
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 0)).OccupancyImages[3];
        
        public static string GetFirstPlatformCar5Image(Entity buildingRef) => 
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 0)).OccupancyImages[4];
        
        public static string GetFirstPlatformCar6Image(Entity buildingRef) => 
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 0)).OccupancyImages[5];
        
        public static string GetFirstPlatformCar7Image(Entity buildingRef) => 
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 0)).OccupancyImages[6];
        
        public static string GetFirstPlatformRightEngine(Entity buildingRef) =>
            VehiclePanelBinding.Invoke(PlatformLineBinding.Invoke(buildingRef, 0)).OccupancyImages[7];
    }
}