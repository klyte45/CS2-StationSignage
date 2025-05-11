using System.Collections.Generic;
using System.Linq;
using Game.City;
using Game.SceneFlow;
using StationSignage.Utils;
using Unity.Entities;

namespace StationSignage.Formulas;

public class DisplayFormulas
{
    
    private static CityConfigurationSystem _cityConfigurationSystem;

    public static string GetLineBackgroundShape(Entity buildingRef)
    {
        return "Square";
    }
    
    public static string GetWelcomeMessage(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue("lineType", out var lineType);
        _cityConfigurationSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CityConfigurationSystem>();
        return GetWelcomeMessage(lineType);
    }
    
    public static string GetImage(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue("$idx", out var idxStr);
        int.TryParse(idxStr, out var idx);
        return GetImageList(buildingRef, vars).ElementAtOrDefault(idx);
    }
    
    public static string GetPlatformImage(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue("platform", out var platformStr);
        int.TryParse(platformStr, out var idx);
        return "Circle" + (idx + 1);
    }
    
    public static HashSet<string> GetImageList(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue("images", out var images);
        if (string.IsNullOrWhiteSpace(images))
            return [];

        return images
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet();
    }
    
    public static string GetWelcomeMessage(string lineType)
    {
        _cityConfigurationSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CityConfigurationSystem>();
        if (lineType == "Subway")
        {
            return GetName("StationSignage.WelcomeSubway").Replace("%s", _cityConfigurationSystem.cityName); 
        }

        return GetName("StationSignage.WelcomeTrain").Replace("%s", _cityConfigurationSystem.cityName);
    }
    
    private static string GetName(string id)
    {
        return GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var name) ? name : "";
    }
}