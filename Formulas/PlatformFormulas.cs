﻿using Colossal;
using Colossal.Entities;
using Game.Common;
using Game.Pathfind;
using Game.Routes;
using StationSignage.Components;
using StationSignage.Enums;
using StationSignage.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace StationSignage.Formulas
{
    public static class PlatformFormulas
    {
        private static EntityManager? entityManager;

        private static EntityManager EntityManager
        {
            get
            {
                if (!entityManager.HasValue)
                {
                    entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                }
                return entityManager.Value;
            }
        }

        public static Entity GetPlatform(Entity building, Dictionary<string, string> vars)
        {
            if (EntityManager.HasComponent<SS_PlatformData>(building)) return building;
            building = EntityUtils.FindTopOwnership(building, EntityManager);
            if (vars.TryGetValue(LinesFormulas.PLATFORM_VAR, out var platformStr) && byte.TryParse(platformStr, out var platform))
            {

                if (EntityManager.TryGetBuffer<SS_PlatformMappingLink>(building, true, out var buffer) && platform <= buffer.Length)
                {
                    return buffer[platform - 1].platformData;
                }
            }
            return Entity.Null; // Return null if no matching platform is found
        }

        public static Entity GetIncomingTrainDestinationForPlatform(Entity platform)
        {
            if (EntityManager.TryGetComponent<SS_VehicleIncomingData>(platform, out var buffer))
            {
                if (buffer.nextVehicle0 != Entity.Null)
                {
                    return EntityManager.TryGetComponent<PathInformation>(buffer.nextVehicle0, out var pathInfo)
                        ? pathInfo.m_Destination
                        : default;
                }
            }
            return EntityManager.TryGetBuffer<ConnectedRoute>(platform, true, out var connectedRoutes) && connectedRoutes.Length > 0
                ? connectedRoutes[0].m_Waypoint
                : Entity.Null;
        }

        public static Entity GetFirstLineOrDefault(Entity platformStop)
        {
            return EntityManager.TryGetBuffer<ConnectedRoute>(platformStop, true, out var buffer) && buffer.Length != 0
                ? EntityManager.GetComponentData<Owner>(buffer[0].m_Waypoint).m_Owner
                : Entity.Null;
        }

        public static SS_WaypointDestinationConnections GetDestinationByIdx(Entity platformStop, Dictionary<string, string> vars)
        {
            return vars.TryGetValue(LinesFormulas.CURRENT_INDEX_VAR, out var idxStr) && byte.TryParse(idxStr, out var idx)
                ? GetPlatformConnections(platformStop, vars).ElementAtOrDefault(idx)
                : default;
        }

        public static List<SS_WaypointDestinationConnections> GetPlatformConnections(Entity platformStop, Dictionary<string, string> vars)
        {
            if (SS_PlatformMappingSystem.CacheVersion != connectionsVersionCache)
            {
                _cachedConnections.Clear();
                connectionsVersionCache = SS_PlatformMappingSystem.CacheVersion;
            }
            TransportTypeByImportance lowestPriority = TransportTypeByImportance.LessPrioritary;
            TransportTypeByImportance highestPriority = TransportTypeByImportance.MostPrioritary;
            if (vars.TryGetValue("lowPr", out var filterStr) && Enum.TryParse<TransportTypeByImportance>(filterStr, out var parsedVal))
            {
                lowestPriority = parsedVal;
            }
            if (vars.TryGetValue("highPr", out filterStr) && Enum.TryParse(filterStr, out parsedVal))
            {
                highestPriority = parsedVal;
            }
            if (lowestPriority < highestPriority) (lowestPriority, highestPriority) = (highestPriority, lowestPriority);
            if (!_cachedConnections.TryGetValue((platformStop, lowestPriority, highestPriority), out var connections))
            {
                HashSet<Entity> excludedLines = [];
                var owner = EntityUtils.FindTopOwnership(platformStop, EntityManager);
                if (EntityManager.TryGetBuffer<SS_PlatformMappingLink>(owner, true, out var platformData))
                {
                    for (int i = 0; i < platformData.Length; i++)
                    {
                        var platform = platformData[i].platformData;
                        if (EntityManager.TryGetBuffer<ConnectedRoute>(platform, true, out var routesConn))
                        {
                            for (int j = 0; j < routesConn.Length; j++)
                            {
                                if (EntityManager.TryGetComponent<Owner>(routesConn[j].m_Waypoint, out var passingLine))
                                {
                                    excludedLines.Add(passingLine.m_Owner);
                                }
                            }
                        }
                    }
                }
                if (EntityManager.TryGetBuffer<ConnectedRoute>(owner, true, out var routes))
                {
                    for (int j = 0; j < routes.Length; j++)
                    {
                        if (EntityManager.TryGetComponent<Owner>(routes[j].m_Waypoint, out var passingLine))
                        {
                            excludedLines.Add(passingLine.m_Owner);
                        }
                    }
                }
                EntityManager.TryGetBuffer<SS_WaypointDestinationConnections>(platformStop, true, out var connectionsBuffer);
                connections = [];
                for (int i = 0; i < connectionsBuffer.Length; i++)
                {
                    var connection = connectionsBuffer[i];
                    if (!excludedLines.Contains(connection.line) && connection.Importance <= lowestPriority && connection.Importance >= highestPriority)
                    {
                        connections.Add(connection);
                    }
                }
                _cachedConnections[(platformStop, lowestPriority, highestPriority)] = connections;
            }
            return connections;
        }

        private static Dictionary<(Entity platform, TransportTypeByImportance low, TransportTypeByImportance high), List<SS_WaypointDestinationConnections>> _cachedConnections = [];
        private static uint connectionsVersionCache = 0;


        public static ServiceOperator GetFirstLineOperatorOrDefault(Entity platformStop)
        {
            bool isMetro = EntityManager.HasComponent<SubwayStop>(platformStop);
            return !EntityManager.TryGetBuffer<ConnectedRoute>(platformStop, true, out var buffer) || buffer.Length == 0
                ? Mod.m_Setting.LineOperatorCityDropdown switch
                {
                    Settings.LineOperatorCityOptions.Generic => ServiceOperator.Default,
                    Settings.LineOperatorCityOptions.SaoPaulo => isMetro ? ServiceOperator.Operator01 : EntityManager.HasComponent<TrainStop>(platformStop) ? ServiceOperator.Operator05 : ServiceOperator.Default,
                    Settings.LineOperatorCityOptions.NewYork => isMetro ? ServiceOperator.MTAOperator : ServiceOperator.Default,
                    Settings.LineOperatorCityOptions.London => isMetro ? ServiceOperator.UndergroundOperator : ServiceOperator.Default,
                    _ => ServiceOperator.Default
                }
                : Mod.m_Setting.LineOperatorCityDropdown switch
                {
                    Settings.LineOperatorCityOptions.Generic => ServiceOperator.Default,
                    Settings.LineOperatorCityOptions.SaoPaulo => EntityManager.GetComponentData<SS_LineStatus>(EntityManager.GetComponentData<Owner>(buffer[0].m_Waypoint).m_Owner).operatorSP,
                    Settings.LineOperatorCityOptions.NewYork => isMetro ? ServiceOperator.MTAOperator : ServiceOperator.Default,
                    Settings.LineOperatorCityOptions.London => isMetro ? ServiceOperator.UndergroundOperator : ServiceOperator.Default,
                    _ => ServiceOperator.Default
                };
        }
    }
}
