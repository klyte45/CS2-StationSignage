using JetBrains.Annotations;
using StationSignage.Models;
using StationSignage.Systems;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace StationSignage.Formulas;

public static class LinesUtils
{

    public const string LINETYPE_VAR = "lineType";
    public const string CURRENT_INDEX_VAR = "$idx";
    public const string PLATFORM_VAR = "platform";
    public const string TRAIN_HALF_VAR = "trainHalf";


    public static int GetLineCount(Entity buildingRef, Dictionary<string, string> vars)
    {
        return vars.TryGetValue(LINETYPE_VAR, out var lineType) ? LinesSystem.Instance.GetLinesCount(buildingRef, lineType) : -1;
    }


    [CanBeNull]
    public static List<List<Entity>> GetLineConnections(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue(LINETYPE_VAR, out var lineType);
        var platformsToRemove = new HashSet<int>();
        if (vars.TryGetValue(PLATFORM_VAR, out var platformIdxStr))
        {
            if (int.TryParse(platformIdxStr, out var idx))
            {
                platformsToRemove.Add(idx);
            }
        }
        if (vars.TryGetValue("platformRight", out var platformRightIdxStr))
        {
            if (int.TryParse(platformRightIdxStr, out var idx))
            {
                platformsToRemove.Add(idx);
            }
        }
        if (vars.TryGetValue("platformLeft", out var platformLeftIdxStr))
        {
            if (int.TryParse(platformLeftIdxStr, out var idx))
            {
                platformsToRemove.Add(idx);
            }
        }

        var lines = LinesSystem.Instance.GetStationLines(buildingRef, lineType, [.. platformsToRemove]);
        return lines;
    }

    [CanBeNull]
    public static List<Entity> GetLineConnection(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue(CURRENT_INDEX_VAR, out var idxStr);
        int.TryParse(idxStr, out var idx);
        return GetLineConnections(buildingRef, vars)?[idx];
    }

    public static string GetLineConnectionName(Entity buildingRef, Dictionary<string, string> vars)
    {
        var connections = GetLineConnections(buildingRef, vars);
        if (connections != null && connections.All(x => x == connections[0]))
        {
            return NamesFormulas.GetBoardingName(buildingRef);
        }
        return NamesFormulas.GetTransferName(buildingRef);
    }

    [CanBeNull]
    public static VehiclePanel GetPlatformVehiclePanel(Entity buildingRef, Dictionary<string, string> vars)
    {
        return vars.TryGetValue(PLATFORM_VAR, out var idxStr) &&
               int.TryParse(idxStr, out var idx) && vars.TryGetValue(LINETYPE_VAR, out var lineType) ?
            null : null;
    }
}