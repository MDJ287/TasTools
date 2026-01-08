using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TasTools
{

    [HarmonyPatch]
    public class Prompts
    {
        // prompt methods mostly taken from FreeCam mod
        public static ScreenPrompt CreatePrompt(string text, KeyCode keyCode)
        {
            var texture = ButtonPromptLibrary.SharedInstance.GetButtonTexture(keyCode);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            sprite.name = texture.name;

            var prompt = new ScreenPrompt(text, sprite);

            return prompt;
        }

        // patches

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerCameraEffectController), nameof(PlayerCameraEffectController.OnStartOfTimeLoop))]
        private static void PlayerCameraEffectController_OnStartOfTimeLoop()
        {;
            TasTools.Instance.tasStartPrompt = CreatePrompt("Playback TAS", KeyCode.Q);
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
