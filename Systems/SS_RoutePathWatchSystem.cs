using Game.Common;
using Game.Pathfind;
using Game.Routes;
using StationSignage.Components;
using System;
using System.Diagnostics;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace StationSignage.Systems
{
    public partial class SS_RoutePathWatchSystem : SystemBase
    {
        private EntityQuery m_PathReadyQuery;
        private ModificationBarrier1 m_ModificationBarrier1;

        protected override void OnUpdate()
        {
            if (m_PathReadyQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            new RoutePathReadyJob
            {
                m_PathUpdatedType = GetComponentTypeHandle<PathUpdated>(true),
                m_RouteWaypointType = GetBufferTypeHandle<RouteWaypoint>(true),
                m_RouteSegmentType = GetBufferTypeHandle<RouteSegment>(true),
                m_OwnerLookup = GetComponentLookup<Owner>(true),
                m_routeLookup = GetComponentLookup<Route>(true),
                m_cmdBuffer = m_ModificationBarrier1.CreateCommandBuffer().AsParallelWriter(),
            }.ScheduleParallel(m_PathReadyQuery, Dependency).Complete();
        }

        protected override void OnCreate()
        {
            m_ModificationBarrier1 = World.GetExistingSystemManaged<ModificationBarrier1>();
            m_PathReadyQuery = GetEntityQuery([
                ComponentType.ReadOnly<Event>(),
                ComponentType.ReadOnly<PathUpdated>()
            ]);
        }

        private struct RoutePathReadyJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<PathUpdated> m_PathUpdatedType;

            [ReadOnly]
            public BufferTypeHandle<RouteWaypoint> m_RouteWaypointType;

            [ReadOnly]
            public BufferTypeHandle<RouteSegment> m_RouteSegmentType;

            public ComponentLookup<Owner> m_OwnerLookup;
            public ComponentLookup<Route> m_routeLookup;

            public EntityCommandBuffer.ParallelWriter m_cmdBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {

                var pathUpdatedArray = chunk.GetNativeArray(ref m_PathUpdatedType);
                for (int i = 0; i < pathUpdatedArray.Length; i++)
                {
                    PathUpdated pathUpdated = pathUpdatedArray[i];
                    if (m_OwnerLookup.TryGetComponent(pathUpdated.m_Owner, out var owner) && m_routeLookup.HasComponent(owner.m_Owner))
                    {
                        m_cmdBuffer.AddComponent<SS_Dirty>(unfilteredChunkIndex, owner.m_Owner);
                    }
                }
            }
        }
    }
}