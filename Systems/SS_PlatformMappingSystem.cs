using Colossal.Entities;
using Game;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.SceneFlow;
using Game.Tools;
using StationSignage.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Transform = Game.Objects.Transform;
using TransportStop = Game.Routes.TransportStop;

public partial class SS_PlatformMappingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (GameManager.instance.isGameLoading) return;
        if (!m_connectableRoutesNotMapped.IsEmptyIgnoreFilter)
        {
            using var output = new NativeParallelHashSet<PairEntityRoute>(m_connectableRoutesNotMapped.CalculateEntityCount() * 2, Allocator.Temp);
            var cmdBuffer = m_endFrameBarrier.CreateCommandBuffer();
            Dependency = new StopMappingJob
            {
                m_attachedLookup = GetComponentLookup<Attached>(true),
                m_ownerLookup = GetComponentLookup<Owner>(true),
                m_EntityType = GetEntityTypeHandle(),
                m_cmdBuffer = cmdBuffer.AsParallelWriter(),
                m_hashSet = output.AsParallelWriter()
            }.ScheduleParallel(m_connectableRoutesNotMapped, Dependency);
            Dependency.Complete();
            using var outputArray = output.ToNativeArray(Allocator.Temp);

            var valuesToSet = outputArray.ToArray().GroupBy(x => x.target).ToDictionary(x => x.Key, x => x.Select(x => x.route).ToHashSet());
            Dictionary<TransportType, byte> transportTypeCounter = new();
            foreach (var kvp in valuesToSet)
            {
                var valuesToAdd = kvp.Value;
                DynamicBuffer<SS_PlatformMappingLink> buffer;
                if (EntityManager.TryGetBuffer<SS_PlatformMappingLink>(kvp.Key, true, out var currentBuffer))
                {
                    for (int i = 0; i < currentBuffer.Length; i++)
                    {
                        valuesToAdd.Add(currentBuffer[i]);
                    }
                    buffer = cmdBuffer.SetBuffer<SS_PlatformMappingLink>(kvp.Key);
                    buffer.Clear();
                }
                else
                {
                    buffer = cmdBuffer.AddBuffer<SS_PlatformMappingLink>(kvp.Key);
                }
                var buildingTransform = EntityManager.GetComponentData<Transform>(kvp.Key);

                var matrixTransform = Matrix4x4.TRS(buildingTransform.m_Position, buildingTransform.m_Rotation, Vector3.one).inverse;

                var sortedValues = valuesToAdd
                    .Select(routePlatformData => (routePlatformData, matrixTransform.MultiplyPoint(EntityManager.GetComponentData<Transform>(routePlatformData.platformData).m_Position), GetTransportType(routePlatformData.platformData)))
                    .OrderByDescending(item => item.Item2.y)
                    .ThenByDescending(item => item.Item2.z)
                    .ThenBy(item => item.Item2.x)
                    .ToList();

                byte counterPlatform = 0;
                byte counterRail = 0;
                transportTypeCounter.Clear();

                foreach (var route in sortedValues)
                {
                    buffer.Add(route.routePlatformData);
                    transportTypeCounter.TryGetValue(route.Item3, out var counter);
                    var data = new SS_PlatformData
                    {
                        type = route.Item3,
                        overallNumber = ++counterPlatform,
                        railsPlatformNumber = route.Item3 == TransportType.Train || route.Item3 == TransportType.Subway ? ++counterRail : (byte)0,
                        transportTypePlatformNumber = ++counter
                    };
                    transportTypeCounter[route.Item3] = counter;
                    cmdBuffer.AddComponent(route.routePlatformData.platformData, data);
                    cmdBuffer.AddComponent<SS_DirtyTransportLine>(route.routePlatformData.platformData);
                }

            }
        }
    }

    private TransportType GetTransportType(Entity platformData)
    {
        if (EntityManager.HasComponent<TrainStop>(platformData)) return TransportType.Train;
        if (EntityManager.HasComponent<SubwayStop>(platformData)) return TransportType.Subway;
        if (EntityManager.HasComponent<AirplaneStop>(platformData)) return TransportType.Airplane;
        if (EntityManager.HasComponent<BusStop>(platformData)) return TransportType.Bus;
        if (EntityManager.HasComponent<TramStop>(platformData)) return TransportType.Tram;
        if (EntityManager.HasComponent<ShipStop>(platformData)) return TransportType.Ship;
        return TransportType.None;

    }

    private struct PairEntityRoute : IEquatable<PairEntityRoute>
    {
        public readonly Entity target;
        public readonly SS_PlatformMappingLink route;

        public PairEntityRoute(Entity target, SS_PlatformMappingLink route)
        {
            this.target = target;
            this.route = route;
        }

        public override bool Equals(object obj) => obj is PairEntityRoute route && Equals(route);

        public bool Equals(PairEntityRoute other) => target.Equals(other.target) &&
                   route.Equals(other.route);

        public override int GetHashCode() => HashCode.Combine(target, route);

        public static bool operator ==(PairEntityRoute left, PairEntityRoute right) => left.Equals(right);

        public static bool operator !=(PairEntityRoute left, PairEntityRoute right) => !(left == right);
    }

    [BurstCompile]
    private struct StopMappingJob : IJobChunk
    {
        public EntityTypeHandle m_EntityType;
        public ComponentLookup<Owner> m_ownerLookup;
        public ComponentLookup<Attached> m_attachedLookup;
        public EntityCommandBuffer.ParallelWriter m_cmdBuffer;
        public NativeParallelHashSet<PairEntityRoute>.ParallelWriter m_hashSet;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(m_EntityType);

            for (int i = 0; i < entities.Length; i++)
            {
                if (m_ownerLookup.TryGetComponent(entities[i], out var owner) && owner.m_Owner != Entity.Null)
                {
                    while (m_ownerLookup.TryGetComponent(owner.m_Owner, out var parentOwner))
                    {
                        owner = parentOwner;
                    }
                    m_hashSet.Add(new(owner.m_Owner, new SS_PlatformMappingLink(entities[i])));
                }
                if (m_attachedLookup.TryGetComponent(entities[i], out Attached parent) && parent.m_Parent != Entity.Null)
                {
                    var target = parent.m_Parent;
                    while (m_ownerLookup.TryGetComponent(target, out var parentOwner))
                    {
                        target = parentOwner.m_Owner;
                    }
                    m_hashSet.Add(new(target, new SS_PlatformMappingLink(entities[i])));
                }
            }

        }
    }

    private EntityQuery m_connectableRoutesNotMapped;
    private EndFrameBarrier m_endFrameBarrier;

    protected override void OnCreate()
    {
        m_endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
        m_connectableRoutesNotMapped = GetEntityQuery(new EntityQueryDesc[] {
                new() {
                    Any =
                    [
                        ComponentType.ReadOnly<TransportStop>(),
                        ComponentType.ReadOnly<TrainStop>(),
                        ComponentType.ReadOnly<AirplaneStop>(),
                        ComponentType.ReadOnly<BusStop>(),
                        ComponentType.ReadOnly<TramStop>(),
                        ComponentType.ReadOnly<ShipStop>(),
                        ComponentType.ReadOnly<SubwayStop>(),
                    ],
                    All =
                    [
                        ComponentType.ReadOnly<Owner>(),
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<SS_PlatformData>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    ]
                },
                new() {
                    Any =
                    [
                        ComponentType.ReadOnly<TransportStop>(),
                        ComponentType.ReadOnly<TrainStop>(),
                        ComponentType.ReadOnly<AirplaneStop>(),
                        ComponentType.ReadOnly<BusStop>(),
                        ComponentType.ReadOnly<TramStop>(),
                        ComponentType.ReadOnly<ShipStop>(),
                        ComponentType.ReadOnly<SubwayStop>(),
                    ],
                    All =
                    [
                        ComponentType.ReadOnly<Attached>(),
                    ],
                    None =
                    [
                        ComponentType.ReadOnly<SS_PlatformData>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Deleted>(),
                    ]
                }
            }
        );

        CheckedStateRef.RequireForUpdate(m_connectableRoutesNotMapped);
    }
}