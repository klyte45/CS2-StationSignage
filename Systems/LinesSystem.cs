using Colossal.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using StationSignage.Models;
using StationSignage.Utils;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace StationSignage.Systems
{
    public partial class LinesSystem : SystemBase
    {
        public enum InternalTransportType
        {
            Train = 1,
            Subway = 2
        }

        public enum ServiceOperator
        {
            Default,
            Operator01,
            Operator02,
            Operator03,
            Operator04,
            Operator05,
            UndergroundOperator,
            MTAOperator,
        }

        private static PrefabSystem _prefabSystem;
        private static NameSystem _nameSystem;
        private EntityQuery _linesQuery;

        private Dictionary<int, TransportLineModel> cachedLineValues;

        protected override void OnCreate()
        {
            base.OnCreate();
            _linesQuery = GetEntityQuery(new EntityQueryDesc[] {
                new() {
                    All =
                    [
                        ComponentType.ReadOnly<Route>(),
                        ComponentType.ReadOnly<RouteNumber>(),
                        ComponentType.ReadOnly<TransportLine>(),
                        ComponentType.ReadOnly<RouteWaypoint>(),
                        ComponentType.ReadOnly<PrefabRef>()
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    ]
                }
            });
            _prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
            _nameSystem = World.GetOrCreateSystemManaged<NameSystem>();            
        }

        public List<UITransportLineData> GetTransportLines(string type)
        {
            return TransportUIUtils.GetSortedLines(_linesQuery, EntityManager, _prefabSystem)
                .Where(line => line.type.ToString() == type)
                .OrderBy(line =>
                {
                    EntityManager.TryGetComponent<RouteNumber>(line.entity, out var routeNumber);
                    var lineName = _nameSystem.GetName(line.entity).Translate().Split(' ').LastOrDefault();
                    var routeString = lineName is { Length: >= 1 and <= 2 } ? lineName : routeNumber.m_Number.ToString();
                    return routeString;
                }
                )
                .ToList();
        }
        public int GetTransportLinesCount(string type)
        {
            return TransportUIUtils.GetSortedLines(_linesQuery, EntityManager, _prefabSystem)
                .Where(line => line.type.ToString() == type).Count();
        }

        protected override void OnUpdate()
        {

        }
    }
}