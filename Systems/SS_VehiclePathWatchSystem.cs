using Colossal.Entities;
using Game;
using Game.Common;
using Game.Pathfind;
using Game.Routes;
using Game.SceneFlow;
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
        private EntityQuery m_vehiclesWithNewPathfind;
        private EntityQuery m_stopsWithoutIncomingInfo;
        private EndFrameBarrier m_endFrameBarrier;
        private static SS_VehiclePathWatchSystem Instance;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 8;
        }

        protected override void OnCreate()
        {
            m_endFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>();
            m_vehiclesWithNewPathfind = GetEntityQuery([
                ComponentType.ReadOnly<SS_DirtyVehicle>()
            ]);
            m_stopsWithoutIncomingInfo = GetEntityQuery(new EntityQueryDesc[]{
                new()
                {
                    All= [
                         ComponentType.ReadOnly<ConnectedRoute>()
                    ],
                    None = [
                         ComponentType.ReadOnly<SS_VehicleIncomingData>()
                    ]
                }
            });

            Mod.HarmonyInstance.Patch(
                typeof(PathfindResultSystem).GetMethod("ProcessResults", Mod.allFlags, null, [typeof(PathfindQueueSystem.ActionList<PathfindAction>), typeof(JobHandle).MakeByRefType(), typeof(JobHandle)], null),
                new HarmonyMethod(GetType().GetMethod(nameof(BeforeProcessResults), Mod.allFlags)));

            Instance = this;

        }

        protected override void OnUpdate()
        {
            if (GameManager.instance.isGameLoading) return;
            if (!m_stopsWithoutIncomingInfo.IsEmptyIgnoreFilter)
            {
                new EmptyPlatformInfoFillerJob
                {
                    m_entityType = GetEntityTypeHandle(),
                    m_connectedLinesHandle = GetBufferTypeHandle<ConnectedRoute>(true),
                    m_routeVehiclesLookup = GetBufferLookup<RouteVehicle>(true),
                    m_pathInformationLookup = GetComponentLookup<PathInformation>(true),
                    m_connectedLookup = GetComponentLookup<Connected>(true),
                    m_ownerLookup = GetComponentLookup<Owner>(true),
                    m_pathOwnerLookup = GetComponentLookup<PathOwner>(true),
                    m_pathElementLookup = GetBufferLookup<PathElement>(true),
                    m_cmdBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                }.ScheduleParallel(m_stopsWithoutIncomingInfo, Dependency).Complete();
                return;
            }
            if (m_vehiclesWithNewPathfind.IsEmptyIgnoreFilter)
            {
                return;
            }

            new VehiclePathChangedJob
            {
                m_entityType = GetEntityTypeHandle(),
                m_dirtyVehicleType = GetComponentTypeHandle<SS_DirtyVehicle>(),
                m_routeVehiclesLookup = GetBufferLookup<RouteVehicle>(true),
                m_pathInformationLookup = GetComponentLookup<PathInformation>(true),
                m_connectedLookup = GetComponentLookup<Connected>(true),
                m_connectedLinesLookup = GetBufferLookup<ConnectedRoute>(true),
                m_ownerLookup = GetComponentLookup<Owner>(true),
                m_pathOwnerLookup = GetComponentLookup<PathOwner>(true),
                m_pathElementLookup = GetBufferLookup<PathElement>(true),
                m_cmdBuffer = m_endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
            }.ScheduleParallel(m_vehiclesWithNewPathfind, Dependency).Complete();
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
            public EntityTypeHandle m_entityType;
            public ComponentTypeHandle<SS_DirtyVehicle> m_dirtyVehicleType;
            public BufferLookup<RouteVehicle> m_routeVehiclesLookup;
            public ComponentLookup<PathInformation> m_pathInformationLookup;
            public ComponentLookup<Connected> m_connectedLookup;
            public BufferLookup<ConnectedRoute> m_connectedLinesLookup;
            public ComponentLookup<Owner> m_ownerLookup;
            public ComponentLookup<PathOwner> m_pathOwnerLookup;
            public BufferLookup<PathElement> m_pathElementLookup;
            public EntityCommandBuffer.ParallelWriter m_cmdBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var dirtyVehicles = chunk.GetNativeArray(ref m_dirtyVehicleType);
                var entities = chunk.GetNativeArray(m_entityType);
                for (int i = 0; i < dirtyVehicles.Length; i++)
                {
                    var vehicle = dirtyVehicles[i];
                    var entity = entities[i];

                    var prevTarget = m_connectedLookup[vehicle.oldTarget].m_Connected;
                    var nextTarget = GetPlatformEntity(entity, m_pathInformationLookup, m_connectedLookup);

                    if (m_connectedLinesLookup.TryGetBuffer(prevTarget, out var routesPrev)) MapIncomingVehicle(unfilteredChunkIndex, prevTarget, ref routesPrev,
                        ref m_ownerLookup, ref m_routeVehiclesLookup, ref m_pathInformationLookup,
                        ref m_pathOwnerLookup, ref m_pathElementLookup, ref m_connectedLookup, ref m_cmdBuffer);
                    if (m_connectedLinesLookup.TryGetBuffer(nextTarget, out var routesNext)) MapIncomingVehicle(unfilteredChunkIndex, nextTarget, ref routesNext,
                        ref m_ownerLookup, ref m_routeVehiclesLookup, ref m_pathInformationLookup,
                        ref m_pathOwnerLookup, ref m_pathElementLookup, ref m_connectedLookup, ref m_cmdBuffer);

                    m_cmdBuffer.RemoveComponent<SS_DirtyVehicle>(unfilteredChunkIndex, entity);
                }
            }
        }

        private struct EmptyPlatformInfoFillerJob : IJobChunk
        {
            public EntityTypeHandle m_entityType;
            public BufferTypeHandle<ConnectedRoute> m_connectedLinesHandle;
            public BufferLookup<RouteVehicle> m_routeVehiclesLookup;
            public ComponentLookup<PathInformation> m_pathInformationLookup;
            public ComponentLookup<Connected> m_connectedLookup;
            public ComponentLookup<Owner> m_ownerLookup;
            public ComponentLookup<PathOwner> m_pathOwnerLookup;
            public BufferLookup<PathElement> m_pathElementLookup;
            public EntityCommandBuffer.ParallelWriter m_cmdBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_entityType);
                var platforms = chunk.GetBufferAccessor(ref m_connectedLinesHandle);
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var connectedRoutes = platforms[i];

                    MapIncomingVehicle(unfilteredChunkIndex, entity, ref connectedRoutes,
                        ref m_ownerLookup, ref m_routeVehiclesLookup, ref m_pathInformationLookup,
                        ref m_pathOwnerLookup, ref m_pathElementLookup, ref m_connectedLookup, ref m_cmdBuffer);
                }
            }
        }

        private static void MapIncomingVehicle(
              int unfilteredChunkIndex,
              Entity platform,
          ref DynamicBuffer<ConnectedRoute> routesOnPlatform,
          ref ComponentLookup<Owner> m_ownerLookup,
          ref BufferLookup<RouteVehicle> m_routeVehiclesLookup,
          ref ComponentLookup<PathInformation> m_pathInformationLookup,
          ref ComponentLookup<PathOwner> m_pathOwnerLookup,
          ref BufferLookup<PathElement> m_pathElementLookup,
          ref ComponentLookup<Connected> m_connectedLookup,
          ref EntityCommandBuffer.ParallelWriter m_cmdBuffer)
        {
            var results = new NativeArray<IncomingVehicle>(4, Allocator.Temp);
            for (int j = 0; j < routesOnPlatform.Length; j++)
            {
                var owner = m_ownerLookup[routesOnPlatform[j].m_Waypoint];
                var vehicles = m_routeVehiclesLookup[owner.m_Owner];
                for (int k = 0; k < vehicles.Length; k++)
                {
                    var destination = GetPlatformEntity(vehicles[k].m_Vehicle, m_pathInformationLookup, m_connectedLookup);
                    if (destination == platform)
                    {
                        var pathInfo = m_pathInformationLookup[vehicles[k].m_Vehicle];
                        var pathOwner = m_pathOwnerLookup[vehicles[k].m_Vehicle];
                        var pathElements = m_pathElementLookup[vehicles[k].m_Vehicle];

                        var distance = pathInfo.m_Distance * (1 - (pathOwner.m_ElementIndex / pathElements.Length));
                        var data = new IncomingVehicle
                        {
                            m_Vehicle = vehicles[k].m_Vehicle,
                            distance = distance
                        };
                        for (int l = 0; l < results.Length; l++)
                        {
                            if (results[l].m_Vehicle == Entity.Null || results[l].distance > distance)
                            {
                                (data, results[l]) = (results[l], data);
                            }
                            if (data.m_Vehicle == Entity.Null) break;
                        }
                    }
                }
            }
            m_cmdBuffer.AddComponent(unfilteredChunkIndex, platform, new SS_VehicleIncomingData
            {
                nextVehicle0 = results[0].m_Vehicle,
                nextVehicle1 = results[1].m_Vehicle,
                nextVehicle2 = results[2].m_Vehicle,
                nextVehicle3 = results[3].m_Vehicle
            });
            results.Dispose();
        }

        private static Entity GetPlatformEntity(
            Entity entity,
            ComponentLookup<PathInformation> m_pathInformationLookup,
            ComponentLookup<Connected> m_connectedLookup)
        {
            return m_connectedLookup[m_pathInformationLookup[entity].m_Destination].m_Connected;
        }

        private struct IncomingVehicle
        {
            public Entity m_Vehicle;
            public float distance;
        }
    }
}