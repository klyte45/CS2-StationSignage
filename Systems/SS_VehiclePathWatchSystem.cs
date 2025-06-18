using Colossal.Entities;
using Game;
using Game.Common;
using Game.Pathfind;
using Game.Routes;
using HarmonyLib;
using StationSignage.Components;
using System;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace StationSignage.Systems
{
    public partial class SS_VehiclePathWatchSystem : GameSystemBase
    {
        private EntityQuery m_PathReadyQuery;
        private EndFrameBarrier m_endFrameBarrier;
        private static SS_VehiclePathWatchSystem Instance;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 8;
        }

        protected override void OnUpdate()
        {
            if (m_PathReadyQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            //new VehiclePathChangedJob
            //{
            //    m_PathUpdatedType = GetComponentTypeHandle<PathUpdated>(true),
            //    m_RouteWaypointType = GetBufferTypeHandle<RouteWaypoint>(true),
            //    m_RouteSegmentType = GetBufferTypeHandle<RouteSegment>(true),
            //    m_OwnerLookup = GetComponentLookup<Owner>(true),
            //    m_routeLookup = GetComponentLookup<Route>(true),
            //    m_cmdBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
            //}.ScheduleParallel(m_PathReadyQuery, Dependency).Complete();
        }

        protected override void OnCreate()
        {
            m_endFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>();
            m_PathReadyQuery = GetEntityQuery([
                ComponentType.ReadOnly<SS_DirtyVehicle>()
            ]);

            Mod.HarmonyInstance.Patch(
                typeof(PathfindResultSystem).GetMethod("ProcessResults", Mod.allFlags, null, [typeof(PathfindQueueSystem.ActionList<PathfindAction>), typeof(JobHandle).MakeByRefType(), typeof(JobHandle)], null),
                new HarmonyMethod(GetType().GetMethod(nameof(BeforeProcessResults), Mod.allFlags)));

            Instance = this;

        }

        private static void BeforeProcessResults(PathfindQueueSystem.ActionList<PathfindAction> list)
        {
            var em = Instance.EntityManager;
            for (int i = 0; i < list.m_Items.Count; i++)
            {
                var action = list.m_Items[i];
                EntityCommandBuffer cmdBuffer = default;
                if ((action.m_Flags & PathFlags.Scheduled) != 0
                    && action.m_Action.data.m_State == PathfindActionState.Completed
                    && em.HasComponent<CurrentRoute>(action.m_Owner)
                    && em.TryGetComponent<PathInformation>(action.m_Owner, out var pathInformation))
                {
                    if (!cmdBuffer.IsCreated)
                    {
                        cmdBuffer = Instance.m_endFrameBarrier.CreateCommandBuffer();
                    }
                    cmdBuffer.AddComponent(action.m_Owner, new SS_DirtyVehicle
                    {
                        oldTarget = pathInformation.m_Destination,
                    });
                }
            }
        }

        private struct VehiclePathChangedJob : IJobChunk
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
                    Mod.log.Info($"{i} == {pathUpdatedArray[i].m_Owner} {pathUpdatedArray[i].m_Data.m_Position1} {pathUpdatedArray[i].m_Data.m_Position2}");
                }
            }
        }
    }
}