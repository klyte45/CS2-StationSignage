using Game.Prefabs;
using Game.Routes;
using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_LineStatus : IComponentData
    {
        public TransportType type;
        public Color color;
        public Color contrastColor;
        public ServiceOperator operatorSP;
        public int expectedInterval;
        public int actualInterval;
    }
}
