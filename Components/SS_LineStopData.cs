using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_LineStopData : IComponentData
    {
        public Entity lineEntity;
        public int stopIndex;
        public int lineIndex;
        public Entity incomingVehicle;
    }
}
