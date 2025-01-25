using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using Unity.Entities;

namespace StationSignage.Utils
{
    public partial class LinesSystem : SystemBase
    {
        
        private static PrefabSystem _prefabSystem;
        private static EntityManager _entityManager;
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
        }

        public List<UITransportLineData> GetTransportLines(TransportType type)
        {
            _prefabSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            return TransportUIUtils.GetSortedLines(_linesQuery, _entityManager, _prefabSystem)
                .Where(line => line.type == type)
                .OrderBy(line => {
                        _entityManager.TryGetComponent<RouteNumber>(line.entity, out var routeNumber);
                        var lineName = _nameSystem.GetName(line.entity).Translate().Split(' ').LastOrDefault();
                        var routeString = lineName is { Length: >= 1 and <= 2 } ? lineName : routeNumber.m_Number.ToString();
                        return routeString;
                    }
                )
                .ToList();
        }

        protected override void OnUpdate()
        {
            
        }
    }
}