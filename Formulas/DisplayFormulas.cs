using Game.City;
using Game.SceneFlow;
using StationSignage.Components;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace StationSignage.Formulas;

public class DisplayFormulas
{
    private static CityConfigurationSystem _cityConfigurationSystem;
    private const string Square = "LineBgSquare";
    private const string Circle = "LineBgCircle";
    private const string ViaQuatroOperator = "Operator03";
    private static readonly string[] ViaMobilidadeLines = ["4", "5", "8", "9", "15"];
    private static readonly string[] LinhaUniLines = ["6"];

    public static int GetTvChannel(Entity _, Dictionary<string, string> vars)
    {
        vars.TryGetValue("tvCh", out var channelStr);
        if (int.TryParse(channelStr, out var channel))
        {
            return channel;
        }
        return 0; // Default channel if parsing fails
    }
    public static string GetTvChannelFooter(Entity _, Dictionary<string, string> vars)
    {
        vars.TryGetValue("tvCh", out var channelStr);
        if (int.TryParse(channelStr, out var channel))
        {
            return channel switch
            {
                1 => LinesUtils.GetPlatformVehiclePanel(_, vars).Footer,
                _ => GetWelcomeMessage(vars)
            };
        }
        return GetWelcomeMessage(vars); // Default channel if parsing fails
    }

    public static string GetLineBackgroundShape(Entity buildingRef)
    {
        return Mod.m_Setting.LineIndicatorShapeDropdown switch
        {
            Settings.LineIndicatorShapeOptions.Square => Square,
            Settings.LineIndicatorShapeOptions.Circle => Circle,
            _ => Square
        };//&StationSignage.Formulas.LinesUtils;GetPlatformVehiclePanel.TrainNameTextColor
    }

    public static string GetShapeIcon(Entity buildingRef)
    {
        return Mod.m_Setting.LineIndicatorShapeDropdown switch
        {
            Settings.LineIndicatorShapeOptions.Square => Square,
            Settings.LineIndicatorShapeOptions.Circle => Circle,
            _ => Square
        };
    }

    private static string GetWelcomeMessage(Dictionary<string, string> vars)
    {
        vars.TryGetValue("lineType", out var lineType);
        _cityConfigurationSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CityConfigurationSystem>();
        return GetWelcomeMessage(lineType);
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

    public static string GetWelcomeMessage(string lineType)
    {
        _cityConfigurationSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CityConfigurationSystem>();
        return lineType == "Subway"
            ? GetName("StationSignage.WelcomeSubway").Replace("%s", _cityConfigurationSystem.cityName)
            : GetName("StationSignage.WelcomeTrain").Replace("%s", _cityConfigurationSystem.cityName);
    }

    private static string GetName(string id)
        => GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var name) ? name : "";

    public static ServiceOperator GetSubwayOperator(string routeName)
        => Mod.m_Setting.LineOperatorCityDropdown switch
        {
            Settings.LineOperatorCityOptions.Generic => ServiceOperator.Default,
            Settings.LineOperatorCityOptions.SaoPaulo => GetSaoPauloSubwayOperator(routeName),
            Settings.LineOperatorCityOptions.NewYork => GetNewYorkSubwayOperator(routeName),
            Settings.LineOperatorCityOptions.London => GetLondonSubwayOperator(routeName),
            _ => ServiceOperator.Default//&StationSignage.Formulas.LinesUtils;GetLineStatus.Color //&StationSignage.Formulas.DisplayFormulas;GetLineBackgroundShape
        };

    public static ServiceOperator GetTrainOperator(string routeName)
        => Mod.m_Setting.LineOperatorCityDropdown switch
        {
            Settings.LineOperatorCityOptions.Generic => ServiceOperator.Default,
            Settings.LineOperatorCityOptions.SaoPaulo => GetSaoPauloTrainOperator(routeName),
            _ => ServiceOperator.Default
        };

    private static ServiceOperator GetSaoPauloSubwayOperator(string lineOperator)
        => lineOperator == ViaQuatroOperator ? ServiceOperator.Operator03
            : ViaMobilidadeLines.Where(y => y == lineOperator).ToList().Count > 0 ? ServiceOperator.Operator02
            : LinhaUniLines.Where(y => y == lineOperator).ToList().Count > 0 ? ServiceOperator.Operator04
            : ServiceOperator.Operator01;

    private static ServiceOperator GetSaoPauloTrainOperator(string lineOperator)
        => ViaMobilidadeLines.Where(y => y == lineOperator).ToList().Count > 0 ? ServiceOperator.Operator02 : ServiceOperator.Operator05;

    private static ServiceOperator GetNewYorkSubwayOperator(string lineOperator) => ServiceOperator.MTAOperator;

    private static ServiceOperator GetLondonSubwayOperator(string lineOperator) => ServiceOperator.UndergroundOperator;


    public static string GetLineStatusText(LineOperationStatus lineStatus) => GetName($"StationSignage.{lineStatus}");

    public static Color GetLineStatusColor(LineOperationStatus lineStatus)
    {

        return lineStatus switch
        {
            LineOperationStatus.OperationStopped or LineOperationStatus.NotOperating => Color.red,
            LineOperationStatus.ReducedSpeed or LineOperationStatus.NoUsage => Color.yellow,
            _ => Color.green
        };
    }
}