using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using StationSignage.Models;
using StationSignage.Utils;
using Unity.Entities;
using UnityEngine;

namespace StationSignage.Formulas;

public static class LinesUtils
{

    public const string LINETYPE_VAR = "lineType";
    public const string CURRENT_INDEX_VAR = "$idx";
    public const string PLATFORM_VAR = "platform";
    public const string TRAIN_HALF_VAR = "trainHalf";

    [CanBeNull]
    public static LinePanel GetLineStatus(Entity buildingRef, Dictionary<string, string> vars)
    {
        return vars.TryGetValue(CURRENT_INDEX_VAR, out var idxStr) && 
               int.TryParse(idxStr, out var idx) && vars.TryGetValue(LINETYPE_VAR, out var lineType) ?
        TransportFormulas.LineBinding.Invoke(idx, lineType) : null;
    }
    
    public static int GetLineCount(Entity buildingRef, Dictionary<string, string> vars)
        => vars.TryGetValue(LINETYPE_VAR, out var lineType) ? TransportFormulas.GetLineCount(lineType) : -1;
    
    public static int PlatformHasLine(Entity buildingRef, Dictionary<string, string> vars)
    {
        var line = GetPlatformLine(buildingRef, vars);
        return line != null && line.Number != LineUtils.Empty ? 1 : -1;
    }
    
    public static int CenterPlatformHasLine(Entity buildingRef, Dictionary<string, string> vars)
    {
        var platformLeft = GetPlatformLeft(buildingRef, vars);
        var platformRight = GetPlatformRight(buildingRef, vars);
        return (platformLeft.Number != LineUtils.Empty || platformRight.Number != LineUtils.Empty) ? 1 : -1;
    }
    
    public static Color GetCenterPlatformLineColor(Entity buildingRef, Dictionary<string, string> vars)
    {
        var platformLeft = GetPlatformLeft(buildingRef, vars);
        var platformRight = GetPlatformRight(buildingRef, vars);
        var platform = GetPlatformLine(buildingRef, vars);
        if (platform != null)
        {
            return platform.Color;
        }
        
        return platformRight?.Color != platformLeft?.Color ? Color.black : platformRight?.Color ?? Color.black;
    }
    
    [CanBeNull]
    public static TransportLineModel GetPlatformLine(Entity buildingRef, Dictionary<string, string> vars)
    {
        if (vars.TryGetValue("platformPosition", out var platformPosition))
        {
            return platformPosition == "right" ? GetPlatformRight(buildingRef, vars) : GetPlatformLeft(buildingRef, vars);
        }
        return vars.TryGetValue(PLATFORM_VAR, out var idxStr) && 
               int.TryParse(idxStr, out var platform) && vars.TryGetValue(LINETYPE_VAR, out var lineType) ?
            TransportFormulas.GetStationLines(buildingRef, lineType, false).ElementAtOrDefault(platform) : null;
    }
    
    [CanBeNull]
    public static TransportLineModel GetPlatformLineCenter(Entity buildingRef, Dictionary<string, string> vars)
    {
        var platformLeft = GetPlatformLeft(buildingRef, vars);
        var platformRight = GetPlatformRight(buildingRef, vars);

        if (vars.TryGetValue("platformPosition", out var platformPosition))
        {
            var preferRight = platformPosition == "right";
            var rightPlatformValid = platformRight?.Name != LineUtils.Empty;
            var leftPlatformValid = platformLeft?.Name != LineUtils.Empty;

            return preferRight 
                ? (rightPlatformValid ? platformRight : platformLeft)
                : (leftPlatformValid ? platformLeft : platformRight);
        }

        return platformLeft?.Name != LineUtils.Empty ? platformLeft : platformRight;
    }
    
    [CanBeNull]
    public static TransportLineModel GetPlatformOrDefault(Entity buildingRef, Dictionary<string, string> vars)
    {
        var platform = GetPlatformLine(buildingRef, vars);
        if (platform == null || platform.Name == LineUtils.Empty)
        {
            vars.TryGetValue(PLATFORM_VAR, out var idxStr);
            vars.TryGetValue(LINETYPE_VAR, out var lineType);
            var stationLines = TransportFormulas.GetStationLines(buildingRef, lineType, false);
            return stationLines.Count >= 1 ? stationLines.First() : null;
        }

        return platform;
    }
    
    private static TransportLineModel GetPlatformLeft(Entity buildingRef, Dictionary<string, string> vars)
    {
        return vars.TryGetValue("platformLeft", out var idxStr) && 
               int.TryParse(idxStr, out var platform) && vars.TryGetValue(LINETYPE_VAR, out var lineType) ?
            TransportFormulas.GetStationLines(buildingRef, lineType, false).ElementAtOrDefault(platform) : null;
    }
    
    private static TransportLineModel GetPlatformRight(Entity buildingRef, Dictionary<string, string> vars)
    {
        return vars.TryGetValue("platformRight", out var idxStr) && 
               int.TryParse(idxStr, out var platform) && vars.TryGetValue(LINETYPE_VAR, out var lineType) ?
            TransportFormulas.GetStationLines(buildingRef, lineType, false).ElementAtOrDefault(platform) : null;
    }
    
    [CanBeNull]
    public static List<TransportLineModel> GetLineConnections(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue(LINETYPE_VAR, out var lineType);
        var lines = TransportFormulas.GetStationLines(buildingRef, lineType, false);
        if (vars.TryGetValue(PLATFORM_VAR, out var platformIdxStr))
        {
            int.TryParse(platformIdxStr, out var idx);
            lines.RemoveAt(idx);
        }
        if (vars.TryGetValue("platformRight", out var platformRightIdxStr))
        {
            int.TryParse(platformRightIdxStr, out var idx);
            lines.RemoveAt(idx);
        }
        if (vars.TryGetValue("platformLeft", out var platformLeftIdxStr))
        {
            int.TryParse(platformLeftIdxStr, out var idx);
            lines.RemoveAt(idx);
        }

        lines.RemoveAll(x => x.Name == LineUtils.Empty);
        return lines;
    }
    
    [CanBeNull]
    public static TransportLineModel GetLineConnection(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue(CURRENT_INDEX_VAR, out var idxStr);
        int.TryParse(idxStr, out var idx);
        return GetLineConnections(buildingRef, vars)?[idx];
    }
    
    public static string GetLineConnectionName(Entity buildingRef, Dictionary<string, string> vars)
    {
        var connections = GetLineConnections(buildingRef, vars);
        if (connections != null && connections.All(x => x.Name == connections[0].Name))
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
            TransportFormulas.GetVehiclePanel(GetPlatformLine(buildingRef, vars), idx, vars) : null;
    }
}