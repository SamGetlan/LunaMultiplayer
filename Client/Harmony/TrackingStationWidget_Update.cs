﻿using Harmony;
using KSP.UI.Screens;
using LunaClient.Events;
using LunaCommon.Enums;
// ReSharper disable All

namespace LunaClient.Harmony
{
    /// <summary>
    /// This harmony patch is intended to trigger an event when drawing a the vessel widgets in the map view
    /// </summary>
    [HarmonyPatch(typeof(TrackingStationWidget))]
    [HarmonyPatch("Update")]
    public class TrackingStationWidget_Update
    {
        [HarmonyPostfix]
        private static void PostUpdate(TrackingStationWidget __instance)
        {
            if (MainSystem.NetworkState < ClientState.Connected) return;

            LabelEvent.onMapWidgetTextProcessed.Fire(__instance);
        }
    }
}
