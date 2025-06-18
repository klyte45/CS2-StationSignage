using Colossal.Entities;
using Game.Routes;
using Game.UI;
using StationSignage.BridgeWE;
using StationSignage.Components;
using StationSignage.Systems;
using StationSignage.Utils;
using Unity.Entities;

namespace StationSignage.Formulas
{
    public static class ComponentDataGetters
    {
        private static LinesSystem _linesSystem;
        private static NameSystem _nameSystem;
        public static SS_LineStatus GetLineStatus(Entity line)
        {
            _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<LinesSystem>();
            _linesSystem.EntityManager.TryGetComponent(line, out SS_LineStatus status);
            return status;
        }
        public static UnityEngine.Color GetLineColor(Entity line)
        {
            _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<LinesSystem>();
            _linesSystem.EntityManager.TryGetComponent(line, out Game.Routes.Color status);
            return status.m_Color;
        }

        public static string GetLineAcronym(Entity line)
        {
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            return (Mod.m_Setting.LineDisplayNameDropdown) switch
            {
                Settings.LineDisplayNameOptions.Custom => GetSmallLineName(_nameSystem.GetName(line).Translate(), line),
                Settings.LineDisplayNameOptions.WriteEverywhere => WERouteFn.GetTransportLineNumber(line),
                Settings.LineDisplayNameOptions.Generated => _nameSystem.EntityManager.TryGetComponent(line, out RouteNumber routeNumber) ? routeNumber.m_Number.ToString() : "?",
                _ => "???",
            };
        }
        private static string GetSmallLineName(string fullLineName, Entity entity)
            => fullLineName is { Length: >= 1 and <= 3 } ? fullLineName : _nameSystem.EntityManager.TryGetComponent(entity, out RouteNumber routeNumber) ? routeNumber.m_Number.ToString() : "??";
    }
}
