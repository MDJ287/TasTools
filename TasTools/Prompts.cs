using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using UnityEngine;

namespace TasTools
{

    [HarmonyPatch]
    public class Prompts
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCameraEffectController), nameof(PlayerCameraEffectController.OnStartOfTimeLoop))]
        private static void PlayerCameraEffectController_OnStartOfTimeLoop()
        {
            TasTools.Instance.tasStartPrompt = new ScreenPrompt(InputLibrary.cancel, "Playback TAS");
            Locator.GetPromptManager().AddScreenPrompt(TasTools.Instance.tasStartPrompt, PromptPosition.Center);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerCameraEffectController), nameof(PlayerCameraEffectController.Update))]
        private static void PlayerCameraEffectController_Update(PlayerCameraEffectController __instance)
        {
            if (__instance._waitForWakeInput && LateInitializerManager.isDoneInitializing)
            {
                if (!TasTools.Instance.tasStartPrompt.IsVisible())
                {
                    TasTools.Instance.tasStartPrompt.SetVisibility(true);
                }
            }
            if (OWInput.IsNewlyPressed(InputLibrary.interact, InputMode.All))
            {
                Locator.GetPromptManager().RemoveScreenPrompt(TasTools.Instance.tasStartPrompt);
            }
        }
    }

}
