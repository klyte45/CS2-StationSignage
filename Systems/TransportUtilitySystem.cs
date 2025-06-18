using Colossal.Entities;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Game.SceneFlow;
using Game.UI;
using StationSignage.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using TransportStop = Game.Routes.TransportStop;
namespace StationSignage.Systems;

public partial class TransportUtilitySystem : GameSystemBase
{
    protected override void OnUpdate()
    {

    }
    protected override void OnCreate()
    {
        base.OnCreate();
        //m_SimulationSystem = World.GetExistingSystemManaged<SimulationSystem>();
        m_linesSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LinesSystem>();
        m_nameSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
    }

    //private uint CacheGenerationFrame = 0;
    //private uint CurrentCacheFrame => m_SimulationSystem.frameIndex >> 7;

    private LinesSystem m_linesSystem;
    private NameSystem m_nameSystem;
    //private SimulationSystem m_SimulationSystem;

    public const string SubwayEntityName = "SubwayLine";



    #region Public methods
    public int GetLineCount(string type) => m_linesSystem.GetTransportLinesCount(Enum.TryParse(type, out TransportType tt) ? tt : TransportType.None);




    #endregion



    public string GetName(string id) => GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var name) ? name : "";
  





    //private string FormatWaitTime(int seconds)
    //{
    //    if (seconds < 60)
    //    {
    //        return seconds + " " + GetName("StationSignage.Seconds");
    //    }
    //    var time = TimeSpan.FromSeconds(seconds);
    //    return time.ToString(@"mm\:ss") + " " + GetName("StationSignage.Minutes");
    //}

    //private string GetDistance(float distance)
    //{
    //    if (distance >= 1000)
    //    {
    //        var distanceInKilometers = distance / 1000f;
    //        return $"{distanceInKilometers:0.0}" + GetName("StationSignage.KilometersAway");
    //    }
    //    return $"{distance:0}" + GetName("StationSignage.MetersAway");
    //}
}//*/


