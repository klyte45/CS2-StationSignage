using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_PlatformMappingLink(Entity platformData) : IBufferElementData
    {
        public Entity platformData = platformData;
    }
}
