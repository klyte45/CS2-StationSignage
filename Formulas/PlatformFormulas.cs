using Colossal;
using Colossal.Entities;
using Game.Buildings;
using Game.Common;
using Game.Pathfind;
using Game.Routes;
using StationSignage.Components;
using System.Collections.Generic;
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

        public static UnityEngine.Color GetLineColorOrDefault(Entity transportLine)
        {
            return EntityManager.TryGetComponent<Color>(transportLine,  out var colorData)
                ? colorData.m_Color
                : UnityEngine.Color.white;
        }

        public static Entity GetFirstLineOrDefault(Entity platformStop)
        {
            return EntityManager.TryGetBuffer<ConnectedRoute>(platformStop, true, out var buffer) && buffer.Length != 0
                ? EntityManager.GetComponentData<Owner>(buffer[0].m_Waypoint).m_Owner
                : Entity.Null;
        }

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
