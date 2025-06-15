using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using StationSignage.Utils;
using Unity.Entities;

namespace StationSignage.Systems
{
    public partial class LinesSystem : SystemBase
    {
        
        private static PrefabSystem _prefabSystem;
        private static NameSystem _nameSystem;
        private EntityQuery _linesQuery;

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
                .OrderBy(line => {
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