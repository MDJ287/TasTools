using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using UnityEngine;

namespace TasTools
{

    [HarmonyPatch]
    public class MiscPatches
    {
        // scene loading

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadManager), nameof(LoadManager.ReloadSceneImmediate))]
        private static void LoadManager_ReloadSceneImmediate()
        {
            if (TasTools.Instance.forceLoadFromTitle)
            {
                LoadManager.s_previousScene = OWScene.TitleScreen;
            }
        }

        // qm collapse

        [HarmonyPrefix]
        [HarmonyPatch(typeof(QuantumMoon), nameof(QuantumMoon.ChangeQuantumState))]
        private static void QuantumMoon_ChangeQuantumState()
        {
            TasTools.console.WriteLine($"{TasTools.GetFrame()}: QUANTUM MOON STATE CHANGING!", MessageType.Info);
        }
    }
}
