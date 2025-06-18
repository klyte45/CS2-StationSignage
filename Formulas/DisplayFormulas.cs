using Colossal.Entities;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.SceneFlow;
using StationSignage.BridgeWE;
using StationSignage.Components;
using StationSignage.Systems;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace StationSignage.Formulas;

public class DisplayFormulas
{
    private static CityConfigurationSystem _cityConfigurationSystem;
    private const string Square = "LineBgSquare";
    private const string Circle = "LineBgCircle";

    private static World refWorld;

    private static EntityManager EntityManager => (refWorld ??= World.DefaultGameObjectInjectionWorld).EntityManager;

    public static int GetTvChannel(Entity _, Dictionary<string, string> vars)
    {
        vars.TryGetValue("tvCh", out var channelStr);
        if (int.TryParse(channelStr, out var channel))
        {
            return channel;
        }
        return 0; // Default channel if parsing fails
    }
    public static Color GetTvBarColors(Entity _, Dictionary<string, string> vars)
    {
        vars.TryGetValue("tvCh", out var channelStr);
        int.TryParse(channelStr, out var channel);
        return channel switch
        {
            1 => EntityManager.TryGetComponent<Owner>(PlatformFormulas.GetIncomingTrainDestinationForPlatform(PlatformFormulas.GetPlatform(_, vars)), out var owner)
                && EntityManager.TryGetComponent<Game.Routes.Color>(owner.m_Owner, out var routeColor)
                    ? routeColor.m_Color
                    : Color.white,
            _ => Color.white
        }; // Default channel if parsing fails
    }
    public static string GetTvChannelFooter(Entity _, Dictionary<string, string> vars)
    {
        vars.TryGetValue("tvCh", out var channelStr);
        int.TryParse(channelStr, out var channel);
        return channel switch
        {
            1 => WERouteFn.GetWaypointStaticDestinationName(PlatformFormulas.GetIncomingTrainDestinationForPlatform(PlatformFormulas.GetPlatform(_, vars))),
            _ => GetWelcomeMessage(LinesSystem.Instance.EntityManager.GetComponentData<SS_PlatformData>(PlatformFormulas.GetPlatform(_, vars)).type)
        };
    }


    public static string GetLineBackgroundShape(Entity buildingRef)
    {
        return Mod.m_Setting.LineIndicatorShapeDropdown switch
        {
            Settings.LineIndicatorShapeOptions.Square => Square,
            Settings.LineIndicatorShapeOptions.Circle => Circle,
            _ => Square
        };
    }

    public static string GetImage(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue("$idx", out var idxStr);
        return vars.TryGetValue("img_" + idxStr, out var images) ? images : "";
    }
    public static int GetImageCount(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue("img_ct", out var idxStr);
        return int.TryParse(idxStr, out var ct) ? ct : 0;
    }

    public static string GetPlatformImage(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue("platform", out var platformStr);
        int.TryParse(platformStr, out var idx);
        return "Circle" + (idx + 1);
    }

    public static string GetWelcomeMessage(TransportType lineType)
    {
        _cityConfigurationSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CityConfigurationSystem>();
        return lineType switch
        {
            TransportType.Subway => GetName("StationSignage.WelcomeSubway").Replace("%s", _cityConfigurationSystem.cityName),
            TransportType.Train => GetName("StationSignage.WelcomeTrain").Replace("%s", _cityConfigurationSystem.cityName),
            _ => GetName("StationSignage.WelcomeCity").Replace("%s", _cityConfigurationSystem.cityName)
        };
    }

    private static string GetName(string id)
        => GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var name) ? name : "";

    public static string GetOperatorImageIcon(ServiceOperator serviceOperator) => "SquareLogo" + (serviceOperator == ServiceOperator.Default ? "" : serviceOperator.ToString());
    public static string GetOperatorImageWide(ServiceOperator serviceOperator) => "WideSideLogo" + (serviceOperator == ServiceOperator.Default ? "" : serviceOperator.ToString());

    public static string GetLineStatusText(LineOperationStatus lineStatus) => GetName($"StationSignage.{lineStatus}");

    public static Color GetLineStatusColor(LineOperationStatus lineStatus) => lineStatus switch
    {
        LineOperationStatus.OperationStopped or LineOperationStatus.NotOperating => Color.red,
        LineOperationStatus.ReducedSpeed or LineOperationStatus.NoUsage => Color.yellow,
        _ => Color.green
    };
}