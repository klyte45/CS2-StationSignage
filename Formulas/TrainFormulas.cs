using Game.City;
using Game.Prefabs;
using Game.SceneFlow;
using StationSignage.Models;
using Unity.Entities;

namespace StationSignage.Formulas
{
    public class TrainFormulas
    {
        private const TransportType TransportType = Game.Prefabs.TransportType.Train;
        private static CityConfigurationSystem _cityConfigurationSystem;

        public static LinePanel GetFirstLine(Entity buildingRef) => TransportFormulas.LineBinding.Invoke(0, TransportType);
        
        public static LinePanel? GetSecondLine(Entity buildingRef) => TransportFormulas.LineBinding.Invoke(1, TransportType);
        
        public static LinePanel? GetThirdLine(Entity buildingRef) => TransportFormulas.LineBinding.Invoke(2, TransportType);
        
        public static LinePanel? GetFourthLine(Entity buildingRef) => TransportFormulas.LineBinding.Invoke(3, TransportType);
        
        public static LinePanel? GetFifthLine(Entity buildingRef) => TransportFormulas.LineBinding.Invoke(4, TransportType);
        
        public static TransportLineModel GetFirstPlatformLine(Entity buildingRef) => 
            TransportFormulas.PlatformLineBinding.Invoke(buildingRef, 0, TransportType);
        
        public static VehiclePanel GetFirstPlatformVehiclePanel(Entity buildingRef) =>
            TransportFormulas.VehiclePanelBinding.Invoke(TransportFormulas.PlatformLineBinding.Invoke(buildingRef, 0, TransportType), 0);
        
        public static TransportLineModel GetSecondPlatformLine(Entity buildingRef) => 
            TransportFormulas.PlatformLineBinding.Invoke(buildingRef, 1, TransportType);
        
        public static VehiclePanel GetSecondPlatformVehiclePanel(Entity buildingRef) =>
            TransportFormulas.VehiclePanelBinding.Invoke(TransportFormulas.PlatformLineBinding.Invoke(buildingRef, 1, TransportType), 1);
        
        public static TransportLineModel GetThirdPlatformLine(Entity buildingRef) => 
            TransportFormulas.PlatformLineBinding.Invoke(buildingRef, 2, TransportType);
        
        public static VehiclePanel GetThirdPlatformVehiclePanel(Entity buildingRef) =>
            TransportFormulas.VehiclePanelBinding.Invoke(TransportFormulas.PlatformLineBinding.Invoke(buildingRef, 2, TransportType), 2);
        
        public static TransportLineModel GetFourthPlatformLine(Entity buildingRef) => 
            TransportFormulas.PlatformLineBinding.Invoke(buildingRef, 3, TransportType);
        
        public static VehiclePanel GetFourthPlatformVehiclePanel(Entity buildingRef) =>
            TransportFormulas.VehiclePanelBinding.Invoke(TransportFormulas.PlatformLineBinding.Invoke(buildingRef, 3, TransportType), 3);
        
        private static string GetName(string id)
        {
            return GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var name) ? name : "";
        }
        
        public static string GetWelcomeMessage(Entity buildingRef)
        {
            _cityConfigurationSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CityConfigurationSystem>();
            return GetName("StationSignage.WelcomeTrain").Replace("%s", _cityConfigurationSystem.cityName);
        }
    }
}