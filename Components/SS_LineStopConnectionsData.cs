using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_LineStopConnectionsData :IBufferElementData
    {
        public Entity referenceLine;
        public ushort nearestStopsAhead;
    }
}
