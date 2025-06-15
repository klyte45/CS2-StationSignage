using StationSignage.Models;
using StationSignage.Systems;
using System.Collections.Generic;
using Unity.Entities;

namespace StationSignage.Formulas;

public static class TransportFormulas
{

    private static TransportUtilitySystem _transportUtilitySystem;

    public static int GetLineCount(string type) => (_transportUtilitySystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TransportUtilitySystem>()).GetLineCount(type);

    public static List<TransportLineModel> GetStationLines(Entity buildingRef, string type, bool getConnections) => (_transportUtilitySystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TransportUtilitySystem>()).GetStationLines(buildingRef, type, getConnections);
    public static VehiclePanel GetVehiclePanel(TransportLineModel line, int platformNumber, Dictionary<string, string> vars) => (_transportUtilitySystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TransportUtilitySystem>()).GetVehiclePanel(line, platformNumber, vars);
}