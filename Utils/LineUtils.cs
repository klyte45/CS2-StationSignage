using System;
using System.Collections.Specialized;
using System.Linq;
using Colossal.Entities;
using Game.Common;
using Game.Routes;
using Game.UI;
using Game.Vehicles;
using Unity.Entities;

namespace StationSignage.Utils;

public static class LineUtils
{
    
    private static LinesSystem _linesSystem;
    private static NameSystem _nameSystem;
    private static EntityManager _entityManager;
    
    private static readonly string[] ViaMobilidadeLines = ["5", "8", "9", "17"];
    
    private const string LinhaUni = "6";

    private const string ViaQuatro = "4";
        
    public const string Transparent = "Transparent";

    public const string Empty = "                                                                                                                                                                                                  .";

    public const string Icon = "Icon";
    
    private const string Logo = "Logo";
    
    private static readonly StringDictionary ModelsDictionary = new()
    {
        { "SubwayCar01", "A" },
        { "SubwayEngine01", "A" },
        { "EU_TrainPassengerCar01", "B" },
        { "EU_TrainPassengerEngine01", "B" },
        { "NA_TrainPassengerCar01", "C" },
        { "NA_TrainPassengerEngine01", "C" },
    };

    public static string GetSubwayOperator(string routeString)
    {
        var subwayOperator = "Operator01";
        if (ViaMobilidadeLines.Contains(routeString))
        {
            subwayOperator = "Operator02";
        } else if (ViaQuatro == routeString)
        {
            subwayOperator = "Operator03";
        } else if (LinhaUni == routeString)
        {
            subwayOperator = "Operator04";
        }

        return subwayOperator;
    }
    
    public static Tuple<string, string> GetRouteName(Entity entity)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var nameSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        var fullLineName = nameSystem.GetName(entity).Translate();
        entityManager.TryGetComponent<RouteNumber>(entity, out var routeNumber);
        var lineName = fullLineName.Split(' ').LastOrDefault();
        var routeName = lineName is { Length: >= 1 and <= 2 } ? lineName : routeNumber.m_Number.ToString();
        return Tuple.Create(fullLineName, routeName);
    }

    public static string GetTrainName(Entity entity)
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        _entityManager.TryGetComponent<Controller>(entity, out var controller);
        _entityManager.TryGetComponent<Owner>(controller.m_Controller, out var owner);
        _entityManager.TryGetBuffer<LayoutElement>(controller.m_Controller, true, out var layoutElements);
        _entityManager.TryGetBuffer<OwnedVehicle>(owner.m_Owner, true, out var ownerVehicles);

        var index = 0;
    
        for (var i = 0; i < ownerVehicles.Length; ++i)
        {
            if (ownerVehicles[i].m_Vehicle == controller.m_Controller)
            {
                index = i;
            }
        }

        index++;
        var entityDebugName = _nameSystem.GetDebugName(entity);
        var entityName = entityDebugName.TrimEnd(' ').Remove(entityDebugName.LastIndexOf(' ') + 1);
        var letter = "F";
        if (ModelsDictionary.ContainsKey(entityName))
        {
            letter = ModelsDictionary[entityName];
        }

        if (entityName.Contains("Subway"))
        {
            letter = "A";
        }
        if (entityName.Contains("EU_Train"))
        {
            letter = "B";
        }
        if (entityName.Contains("NA_Train"))
        {
            letter = "C";
        }
        if (index < 10)
        {
            return letter + "0" + index;
        }

        return letter + index;
    }
}