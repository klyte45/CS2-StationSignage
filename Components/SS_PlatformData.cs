using Game.Prefabs;
using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_PlatformData : IComponentData
    {
        public byte overallNumber;
        public TransportType type;
        public byte transportTypePlatformNumber;
        public byte railsPlatformNumber;

        public override string ToString() => $"PlatformData: {type} Overall: {overallNumber} TransportTypeNumber: {transportTypePlatformNumber} RailsPlatformNumber: {railsPlatformNumber}";
    }
}
