using Game;
using Game.Common;
using Game.Pathfind;
using Game.Routes;
using Game.Simulation;
using StationSignage.Components;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace StationSignage.Formulas
{
    public partial class SS_IncomingVehicleSystem : GameSystemBase
    {
        private uint CurrentFrame => m_simulationSystem.frameIndex >> 3;

        private readonly NativeHashMap<Entity, VehicleTvData> cachedPanelInfo = [];
        private readonly HashSet<Entity> UpdateRequests = [];
        private SimulationSystem m_simulationSystem;
        public static SS_IncomingVehicleSystem Instance { get; private set; }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 4;
        }

        public VehicleTvData GetTvInformation(Entity e)
        {
            return default;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            m_simulationSystem = World.GetExistingSystemManaged<SimulationSystem>();

        }

        protected override void OnUpdate()
        {
            if (UpdateRequests.Count > 0)
            {

            }
        }

        public enum VehicleStatusDescription
        {
            TrainOnPlatform,
            BoardingNow,
            PrepareForBoarding,
            NextTrain,
            DistanceToStation
        }

        public readonly struct VehicleTvData(
            VehicleStatusDescription title,
            VehicleStatusDescription subtitle,
            int distance,
            NativeList<int> occupancyLevels,
            uint cacheFrame
        )
        {
            public readonly VehicleStatusDescription Title = title;
            public readonly VehicleStatusDescription Subtitle = subtitle;
            public readonly int Distance = distance;
            public readonly int OccupancyLevels0 = occupancyLevels.Length > 0 ? occupancyLevels[0] : 0;
            public readonly int OccupancyLevels1 = occupancyLevels.Length > 1 ? occupancyLevels[1] : 0;
            public readonly int OccupancyLevels2 = occupancyLevels.Length > 2 ? occupancyLevels[2] : 0;
            public readonly int OccupancyLevels3 = occupancyLevels.Length > 3 ? occupancyLevels[3] : 0;
            public readonly int OccupancyLevels4 = occupancyLevels.Length > 4 ? occupancyLevels[4] : 0;
            public readonly int OccupancyLevels5 = occupancyLevels.Length > 5 ? occupancyLevels[5] : 0;
            public readonly int OccupancyLevels6 = occupancyLevels.Length > 6 ? occupancyLevels[6] : 0;
            public readonly int OccupancyLevels7 = occupancyLevels.Length > 7 ? occupancyLevels[7] : 0;
            public readonly uint CacheFrame = cacheFrame;

            public readonly int this[int index] => index switch
            {
                0 => OccupancyLevels0,
                1 => OccupancyLevels1,
                2 => OccupancyLevels2,
                3 => OccupancyLevels3,
                4 => OccupancyLevels4,
                5 => OccupancyLevels5,
                6 => OccupancyLevels6,
                7 => OccupancyLevels7,
                _ => throw new System.IndexOutOfRangeException("Index out of range for occupancy levels.")
            };

            public readonly bool IsValid() => CacheFrame == Instance.CurrentFrame;
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
                        m_cmdBuffer.AddComponent<SS_DirtyTransportLine>(unfilteredChunkIndex, owner.m_Owner);
                    }
                }
            }
        }
    }




}
