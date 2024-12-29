using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Game.Common;
using Game.Routes;
using Game.UI;
using Game.UI.InGame;
using StationVisuals.Utils;
using Unity.Entities;

namespace StationVisuals.Formulas
{
    public class SubwayFormulas
    {
        private static LinesSystem _linesSystem;
        private static NameSystem _nameSystem;
        private static EntityManager _entityManager;

        private static readonly Func<Entity, string> FirstLineNameBinding = (buildingRef) =>
        {
            try
            {
                return String.Join(", ", GetStops(_entityManager, GetLine(0).entity).Select(GetRouteBuildingName));
            }
            catch (Exception e)
            {
                Mod.log.Info(e);
                return "exception";
            }
        };
        
        private static readonly Func<Entity, string> FirstLineActiveBinding = (buildingRef) =>
            GetLine(0).active.ToString();
        
        private static readonly Func<Entity, string> SecondLineNameBinding = (buildingRef) =>
            _nameSystem.GetName(GetLine(0).entity).Translate();

        private static UITransportLineData GetLine(int index)
        {
            _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            return _linesSystem.GetTransportLines()[index];
        }
        
        private static List<RouteWaypoint> GetStops(EntityManager entityManager, Entity entity)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var buffer = entityManager.GetBuffer<RouteWaypoint>(entity, true);
            var waypoints = new List<RouteWaypoint>();
            for (var index = 0; index < buffer.Length; ++index)
            {
                if (entityManager.TryGetComponent(buffer[index].m_Waypoint, out Connected component) &&
                    entityManager.HasComponent<TransportStop>(component.m_Connected) &&
                    !entityManager.HasComponent<TaxiStand>(component.m_Connected))
                {
                    waypoints.Add(buffer[index]);
                }
            }
            return waypoints.Take((waypoints.Count / 2) + 1).ToList();
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
        
        public static string GetFirstLineName(Entity buildingRef) => 
            FirstLineNameBinding?.Invoke(buildingRef) ?? "????";
        
        public static string GetFirstActive(Entity buildingRef) => 
            FirstLineActiveBinding?.Invoke(buildingRef) ?? "????";
        
        public static string GetSecondLineName(Entity buildingRef) => 
            SecondLineNameBinding?.Invoke(buildingRef) ?? "????";
    }
}