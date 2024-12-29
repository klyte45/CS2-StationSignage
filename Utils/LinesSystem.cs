using System.Collections.Generic;
using System.Linq;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.UI.InGame;
using Unity.Entities;

namespace StationVisuals.Utils
{
    public partial class LinesSystem : SystemBase
    {
        
        private static PrefabSystem _prefabSystem;
        private static EntityManager _entityManager;
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

        public List<UITransportLineData> GetTransportLines()
        {
            _prefabSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            return TransportUIUtils.GetSortedLines(_linesQuery, _entityManager, _prefabSystem)
                .Where(line => line.type == TransportType.Subway)
                .ToList();
        }

        protected override void OnUpdate()
        {
            
        }
    }
}