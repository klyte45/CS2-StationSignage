using Colossal.Entities;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using StationSignage.Components;
using StationSignage.Utils;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using TransportStop = Game.Routes.TransportStop;

namespace StationSignage.Systems
{
    public partial class LinesSystem : GameSystemBase
    {
        internal static LinesSystem Instance { get; private set; }
        public uint GameSimulationFrame => _simulationSystem.frameIndex;

        private PrefabSystem _prefabSystem;
        private NameSystem _nameSystem;
        private SimulationSystem _simulationSystem;
        private EntityQuery _linesQuery;
        private EntityQuery _linesRequiringUpdateQuery;
        private EntityQuery _linesStatusesQuery;

        private readonly Dictionary<TransportType, List<Entity>> cachedLinesGrouped = new();

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 8;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            _simulationSystem = World.GetExistingSystemManaged<SimulationSystem>();
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
            _linesStatusesQuery = GetEntityQuery(new EntityQueryDesc[] {
                new() {
                    All =
                    [
                        ComponentType.ReadOnly<SS_LineStatus>(),
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    ]
                }
            });
            _linesRequiringUpdateQuery = GetEntityQuery(new EntityQueryDesc[] {
                new() {
                    All =
                    [
                        ComponentType.ReadOnly<Route>(),
                        ComponentType.ReadOnly<RouteNumber>(),
                        ComponentType.ReadOnly<TransportLine>(),
                        ComponentType.ReadOnly<RouteWaypoint>(),
                        ComponentType.ReadOnly<PrefabRef>(),
                        ComponentType.ReadOnly<SS_LineStatus>(),
                        ComponentType.ReadOnly<SS_DirtyTransportLine>()
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    ]
                },
                new() {
                    All =
                    [
                        ComponentType.ReadOnly<Route>(),
                        ComponentType.ReadOnly<RouteNumber>(),
                        ComponentType.ReadOnly<TransportLine>(),
                        ComponentType.ReadOnly<RouteWaypoint>(),
                        ComponentType.ReadOnly<PrefabRef>(),
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<SS_LineStatus>(),
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    ]
                }
            });
            _prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
            _nameSystem = World.GetOrCreateSystemManaged<NameSystem>();
        }

        public List<UITransportLineData> GetTransportLines(TransportType type)
        {
            return [.. TransportUIUtils.GetSortedLines(_linesQuery, EntityManager, _prefabSystem)
                .Where(line => line.type == type)
                .OrderBy(line =>
                {
                    EntityManager.TryGetComponent<RouteNumber>(line.entity, out var routeNumber);
                    var lineName = _nameSystem.GetName(line.entity).Translate().Split(' ').LastOrDefault();
                    var routeString = lineName is { Length: >= 1 and <= 2 } ? lineName : routeNumber.m_Number.ToString();
                    return routeString;
                }
                )];
        }
        public int GetTransportLinesCount(TransportType type)
        {
            return TransportUIUtils.GetSortedLines(_linesQuery, EntityManager, _prefabSystem).Where(line => line.type == type).Count();
        }

        protected override void OnUpdate()
        {
            if (!_linesRequiringUpdateQuery.IsEmptyIgnoreFilter)
            {
                new LineStatusUpdateJob
                {
                    entityType = GetEntityTypeHandle(),
                    routeType = GetComponentTypeHandle<Route>(true),
                    routeVehicleLookup = GetBufferLookup<RouteVehicle>(true),
                    passengerLookup = GetBufferLookup<Passenger>(true),
                    numberLookup = GetComponentLookup<RouteNumber>(true),
                    prefabRefLookup = GetComponentLookup<PrefabRef>(true),
                    transportLineLookup = GetComponentLookup<TransportLineData>(true),
                    cmdBuffer = World.GetExistingSystemManaged<EndFrameBarrier>().CreateCommandBuffer().AsParallelWriter()
                }.ScheduleParallel(_linesRequiringUpdateQuery, Dependency).Complete();
            }

        }
        public List<List<Entity>> GetStationLines(Entity buildingRef, string type, int[] skipPlatform = default)
        {
            var result = new NativeList<NativeHashSet<Entity>>(Allocator.Temp);
            try
            {
                FillSearchJob(buildingRef, ref result, type, skipPlatform).Complete();
                var resultList = new List<List<Entity>>();
                for (int i = 0; i < result.Length; i++)
                {
                    var platform = result[i];
                    if (platform.IsCreated)
                    {
                        using var nativeArr = platform.ToNativeArray(Allocator.Temp);
                        resultList.Add([.. nativeArr.ToArray().OrderBy(x => EntityManager.TryGetComponent(x, out RouteNumber rn) ? rn.m_Number : -1)]);
                        platform.Dispose();
                    }
                    else
                    {
                        resultList.Add(null);
                    }
                }
                return resultList;
            }
            finally
            {
                result.Dispose();
            }
        }

        public int GetLinesCount(Entity building, string type)
        {
            return GetStationLines(building, type).SelectMany(x => x).GroupBy(x => x).Count();
        }
        private Entity GetOwnerRecursive(Entity entity) => EntityManager.TryGetComponent<Owner>(entity, out var owner) ? GetOwnerRecursive(owner.m_Owner) : entity;
        public List<Entity> GetConnectionLines(Entity waypoint, string type)
        {
            EntityManager.TryGetComponent<Connected>(waypoint, out var connected);
            var building = GetOwnerRecursive(connected.m_Connected);
            return [.. GetStationLines(building, type).SelectMany(x => x).GroupBy(x => x).Select(x => x.Key)];
        }
        private JobHandle FillSearchJob(Entity buildingRef, ref NativeList<NativeHashSet<Entity>> result, string type, int[] skipPlatformArray)
        {
            var skipPlatform = new NativeArray<int>(skipPlatformArray ?? [], Allocator.TempJob);
            var handle = type switch
            {
                "Bus" => FillJob<BusStop>(buildingRef, ref result, ref skipPlatform).Schedule(Dependency),
                "Train" => FillJob<TrainStop>(buildingRef, ref result, ref skipPlatform).Schedule(Dependency),
                "Tram" => FillJob<TramStop>(buildingRef, ref result, ref skipPlatform).Schedule(Dependency),
                "Ship" => FillJob<ShipStop>(buildingRef, ref result, ref skipPlatform).Schedule(Dependency),
                "Airplane" => FillJob<AirplaneStop>(buildingRef, ref result, ref skipPlatform).Schedule(Dependency),
                "Subway" => FillJob<SubwayStop>(buildingRef, ref result, ref skipPlatform).Schedule(Dependency),
                _ => FillJob<TransportStop>(buildingRef, ref result, ref skipPlatform).Schedule(Dependency), //list all
            };
            skipPlatform.Dispose(handle);
            return handle;
        }
        private LinesFromStationJob<TStop> FillJob<TStop>(Entity buildingRef, ref NativeList<NativeHashSet<Entity>> result, ref NativeArray<int> skipPlatform) where TStop : unmanaged, IComponentData
        {
            return new LinesFromStationJob<TStop>
            {
                m_buildingOwner = buildingRef,
                m_connectedRouteLookup = GetBufferLookup<ConnectedRoute>(true),
                m_platformMappingLinkLookup = GetBufferLookup<SS_PlatformMappingLink>(true),
                m_transportLineDataLookup = GetComponentLookup<Owner>(true),
                m_stopTypeDataLookup = GetComponentLookup<TStop>(true),
                m_output = result,
                m_platformDataLookup = GetComponentLookup<SS_PlatformData>(true),
                m_skipPlatforms = skipPlatform
            };
        }

        private struct LinesFromStationJob<TStop> : IJob where TStop : unmanaged, IComponentData
        {
            public Entity m_buildingOwner;
            public NativeList<NativeHashSet<Entity>> m_output;
            public BufferLookup<SS_PlatformMappingLink> m_platformMappingLinkLookup;
            public ComponentLookup<SS_PlatformData> m_platformDataLookup;
            public BufferLookup<ConnectedRoute> m_connectedRouteLookup;
            public ComponentLookup<Owner> m_transportLineDataLookup;
            public ComponentLookup<TStop> m_stopTypeDataLookup;
            public NativeArray<int> m_skipPlatforms;

            public readonly void Execute()
            {
                if (m_platformMappingLinkLookup.TryGetBuffer(m_buildingOwner, out var platformMappingLinks))
                {
                    for (int i = 0; i < platformMappingLinks.Length; i++)
                    {
                        SS_PlatformMappingLink platformMappingLink = platformMappingLinks[i];
                        if (m_stopTypeDataLookup.HasComponent(platformMappingLink.platformData)
                            && m_platformDataLookup.TryGetComponent(platformMappingLink.platformData, out var data)
                            && !IsExclusion(data.overallNumber)
                            && m_connectedRouteLookup.TryGetBuffer(platformMappingLink.platformData, out var connectedRoutes))
                        {
                            var resultList = new NativeHashSet<Entity>(connectedRoutes.Length, Allocator.Temp);
                            for (int j = 0; j < connectedRoutes.Length; j++)
                            {
                                var connectedRoute = connectedRoutes[j];
                                resultList.Add(m_transportLineDataLookup[connectedRoute.m_Waypoint].m_Owner);
                            }
                            m_output.Add(resultList);
                        }
                        else
                        {
                            m_output.Add(default);
                        }
                    }
                }
            }

            public readonly bool IsExclusion(int platformIdx)
            {
                for (int i = 0; i < m_skipPlatforms.Length; i++)
                {
                    if (m_skipPlatforms[i] == platformIdx) return true;
                }
                return false;
            }
        }

        public List<Entity> GetCityLines(TransportType tt, bool acceptCargo, bool acceptPassenger)
        {
            var response = new NativeList<LineStatusGetJobResponseItem>(_linesStatusesQuery.CalculateEntityCount(), Allocator.Temp);
            try
            {
                new LineStatusGetJob
                {
                    response = response.AsParallelWriter(),
                    entityType = GetEntityTypeHandle(),
                    routeNumberType = GetComponentTypeHandle<RouteNumber>(true),
                    statusLookup = GetComponentTypeHandle<SS_LineStatus>(true),
                    accepted = tt,
                    acceptCargo = acceptCargo,
                    acceptPassenger = acceptPassenger
                }.ScheduleParallel(_linesStatusesQuery, Dependency).Complete();
                return [.. response.AsArray()
                    .OrderBy(x=> x.status.type)
                    .ThenBy(x => x.route.m_Number)
                    .Select(x=>x.entity)];
            }
            finally
            {
                response.Dispose();
            }
        }

        private struct LineStatusGetJobResponseItem
        {
            public Entity entity;
            public SS_LineStatus status;
            public RouteNumber route;
        }

        private struct LineStatusGetJob : IJobChunk
        {
            public NativeList<LineStatusGetJobResponseItem>.ParallelWriter response;
            public EntityTypeHandle entityType;
            public ComponentTypeHandle<RouteNumber> routeNumberType;
            public ComponentTypeHandle<SS_LineStatus> statusLookup;
            public TransportType accepted;
            public bool acceptCargo;
            public bool acceptPassenger;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(entityType);
                var routeNumbers = chunk.GetNativeArray(ref routeNumberType);
                var statuses = chunk.GetNativeArray(ref statusLookup);
                for (int i = 0; i < entities.Length; i++)
                {
                    var status = statuses[i];
                    if ((accepted == TransportType.None || status.type == accepted) &&
                       acceptCargo == status.isCargo &&
                       acceptPassenger == status.isPassenger)
                    {
                        var entity = entities[i];

                        response.AddNoResize(new LineStatusGetJobResponseItem
                        {
                            entity = entity,
                            status = status,
                            route = routeNumbers[i]
                        });
                    }
                }
            }
        }

        private struct LineStatusUpdateJob : IJobChunk
        {
            public EntityTypeHandle entityType;
            public ComponentTypeHandle<Route> routeType;
            public BufferLookup<RouteVehicle> routeVehicleLookup;
            public BufferLookup<Passenger> passengerLookup;
            public ComponentLookup<RouteNumber> numberLookup;
            public ComponentLookup<PrefabRef> prefabRefLookup;
            public ComponentLookup<TransportLineData> transportLineLookup;
            public EntityCommandBuffer.ParallelWriter cmdBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(entityType);
                var routes = chunk.GetNativeArray(ref routeType);
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var route = routes[i];

                    var tlData = transportLineLookup[prefabRefLookup[entity]];
                    var lineType = tlData.m_TransportType;

                    var newStatus = new SS_LineStatus
                    {
                        actualInterval = 1,
                        expectedInterval = 1,
                        lineOperationStatus = GetOperationStatus(entity, route),
                        operatorSP = GetSaoPauloOperator(lineType, numberLookup[entity].m_Number),
                        type = lineType,
                        isCargo = tlData.m_CargoTransport,
                        isPassenger = tlData.m_PassengerTransport
                    };
                    cmdBuffer.AddComponent(unfilteredChunkIndex, entity, newStatus);
                    cmdBuffer.RemoveComponent<SS_DirtyTransportLine>(unfilteredChunkIndex, entity);
                }
            }

            private static ServiceOperator GetSaoPauloOperator(TransportType transportType, int routeNumber)
                => transportType switch
                {
                    TransportType.Subway => routeNumber switch
                    {
                        4 => ServiceOperator.Operator03,
                        5 or 8 or 9 or 15 => ServiceOperator.Operator02,
                        6 => ServiceOperator.Operator04,
                        _ => ServiceOperator.Operator01
                    },
                    TransportType.Train => routeNumber switch
                    {
                        5 or 8 or 9 or 15 => ServiceOperator.Operator02,
                        _ => ServiceOperator.Operator05
                    },
                    _ => ServiceOperator.Default
                };

            private LineOperationStatus GetOperationStatus(Entity e, Route route)
            {
                if (RouteUtils.CheckOption(route, RouteOption.Inactive))
                {
                    return LineOperationStatus.NotOperating;
                }
                var vehicles = routeVehicleLookup[e];
                if (vehicles.Length == 0)
                {
                    return LineOperationStatus.OperationStopped;
                }

                if (vehicles.Length == 1)
                {
                    return LineOperationStatus.ReducedSpeed;
                }

                for (int i = 0; i < vehicles.Length; i++)
                {
                    if (passengerLookup[vehicles[i].m_Vehicle].Length != 0)
                    {
                        return LineOperationStatus.NormalOperation;
                    }
                }

                return LineOperationStatus.NoUsage;
            }
        }
    }
}