using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_Dirty : IComponentData
    {
    }
    public struct SS_DirtyVehicle : IComponentData
    {
        internal Entity oldTarget;
    }
}
