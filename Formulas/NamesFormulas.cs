using System;
using Colossal.Entities;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.SceneFlow;
using Game.UI;
using StationVisuals.Utils;
using Unity.Entities;

namespace StationVisuals.Formulas;

public class NamesFormulas
{
    private static LinesSystem _linesSystem;
    private static NameSystem _nameSystem;
    private static EntityManager _entityManager;
    
    public NamesFormulas()
    {
        _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
        _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    
    private static string GetName(string id)
    {
          _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
        _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        return GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var name) ? name : "";
    }
    
    private static readonly Func<Entity, string> GetMainBuildingNameBinding = (buildingRef) =>
    {
        _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
        _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _entityManager.TryGetComponent<Owner>(buildingRef, out var owner);
        return _nameSystem.GetRenderedLabelName(owner.m_Owner);
    };

    private static readonly Func<Entity, string> GetBuildingNameBinding = (buildingRef) =>
    {
        _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
        _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        return _nameSystem.GetRenderedLabelName(buildingRef);
    };
    
    private static readonly Func<Entity, string> GetBuildingRoadNameBinding = (buildingRef) =>
    {
        _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
        _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _entityManager.TryGetComponent<Building>(buildingRef, out var building);
        _entityManager.TryGetComponent<Aggregated>(building.m_RoadEdge, out var aggregated);
        return _nameSystem.GetRenderedLabelName(aggregated.m_Aggregate);
    };

    private static readonly Func<Entity, string> GetExitNameBinding = _ => GetName("StationVisuals.Exit");
    
    private static readonly Func<Entity, string> GetBoardingNameBinding = _ => GetName("StationVisuals.Boarding");
    
    public static string GetMainBuildingName(Entity buildingRef) => GetMainBuildingNameBinding(buildingRef);
    
    public static string GetBuildingName(Entity buildingRef) => GetBuildingNameBinding(buildingRef);
    
    public static string GetExitName(Entity buildingRef) => GetExitNameBinding(buildingRef);
    
    public static string GetBoardingName(Entity buildingRef) => GetBoardingNameBinding(buildingRef);
    
    public static string GetBuildingRoadName(Entity buildingRef) => GetBuildingRoadNameBinding(buildingRef);
    
    public static string GetPlatform1Name(Entity buildingRef) => GetName("StationVisuals.Platform") + "1";
    
    public static string GetPlatform2Name(Entity buildingRef) => GetName("StationVisuals.Platform") + "2";
}