using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_WaypointDestinationConnectionsDirty : IComponentData
    {
        public Entity untilWaypoint;
        public uint requestFrame;
    }
}
