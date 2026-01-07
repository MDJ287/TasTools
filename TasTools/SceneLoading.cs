using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using UnityEngine;

namespace TasTools
{

    [HarmonyPatch]
    public class SceneLoading
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadManager), nameof(LoadManager.ReloadSceneImmediate))]
        private static void LoadManager_ReloadSceneImmediate()
        {
            if (TasTools.Instance.forceLoadFromTitle)
            {
                LoadManager.s_previousScene = OWScene.TitleScreen;
            }
        }
    }

}
