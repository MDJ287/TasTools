using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using UnityEngine;

namespace TasTools
{

    [HarmonyPatch]
    public class NewInputHandling
    {
        // AXIS

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OWInput), nameof(OWInput.GetAxisValue))]
        private static bool OWInput_GetAxisValue(IInputCommands command, out Vector2 __result)
        {
            __result = Vector2.zero;
            if (!TasTools.OverrideControls()) return true;
            if (!TasTools.AreInputsAllowed()) return false;
            if (command == InputLibrary.look)
            {
                __result = TasTools.lookAxis;
            }
            else if (command == InputLibrary.moveXZ)
            {
                __result = TasTools.walkAxis;
            }
            return false;
        }

        // NEWLY PRESSED/RELEASED

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OWInput), nameof(OWInput.IsNewlyPressed))]
        private static bool OWInput_IsNewlyPressed(IInputCommands command, out bool __result)
        {
            __result = false;
            if (!TasTools.OverrideControls()) return true;
            if (!TasTools.AreInputsAllowed()) return false;
            if (TasTools.Instance.currentTasFrame == 0 && command == InputLibrary.interact)
            {
                __result = true;
            }
            foreach (IInputCommands input in TasTools.RePressedCommands)
            {
                if (input == command)
                {
                    __result = true;
                    return false;
                }
            }
            foreach (IInputCommands input in TasTools.inputCommands)
            {
                foreach (IInputCommands prevInput in TasTools.lastFrameCommands)
                {
                    if (prevInput == command && input == command) return false;
                }
                if (input == command) __result = true;
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OWInput), nameof(OWInput.IsNewlyReleased))]
        private static bool OWInput_IsNewlyReleased(IInputCommands command, out bool __result)
        {
            __result = false;
            if (!TasTools.OverrideControls()) return true;
            if (!TasTools.AreInputsAllowed()) return false;
            foreach (IInputCommands input in TasTools.RePressedCommands)
            {
                if (input == command)
                {
                    __result = true;
                    return false;
                }
            }
            foreach (IInputCommands input in TasTools.lastFrameCommands)
            {
                foreach (IInputCommands newInput in TasTools.inputCommands)
                {
                    if (newInput == command && input == command) return false;
                }
                if (input == command) __result = true;
            }
            return false;
        }

        // IS PRESSED

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OWInput), "IsPressed", [typeof(IInputCommands), typeof(float)])]
        private static bool OWInput_IsPressed(IInputCommands command, out bool __result)
        {
            __result = false;
            if (!TasTools.OverrideControls()) return true;
            if (!TasTools.AreInputsAllowed()) return false;
            foreach (IInputCommands input in TasTools.inputCommands)
            {
                if (input == command) __result = true;
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OWInput), "IsPressed", [typeof(IInputCommands), typeof(InputMode), typeof(float)])]
        private static bool OWInput_IsPressed_InputMode(IInputCommands command, out bool __result)
        {
            __result = false;
            if (!TasTools.OverrideControls()) return true;
            if (!TasTools.AreInputsAllowed()) return false;
            foreach (IInputCommands input in TasTools.inputCommands)
            {
                if (input == command) __result = true;
            }
            return false;
        }
    }

}
