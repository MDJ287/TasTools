using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TasTools
{
    public class TasTools : ModBehaviour
    {
        public static TasTools Instance;
        public static IModConsole console;

        StreamReader fileReader;

        public bool isReplayingTas = false;
        public int currentTasFrame = 0;
        private bool isAsleep = true;
        public bool isInSolarSystem = false;
        public bool isGamePaused = true;
        public bool forceLoadFromTitle = false;

        private bool addFramePrompt = true;
        private bool waitForLoading = false;

        public int remainingFramesOfSameInputs = 0;

        public static Vector2 walkAxis = new(0,0);
        public static Vector2 lookAxis = new(0,0);
        public static IInputCommands[] inputCommands = [];
        public static IInputCommands[] lastFrameCommands = [];
        public static IInputCommands[] RePressedCommands = [];

        public ScreenPrompt tasStartPrompt;
        private ScreenPrompt framePrompt;

        // start

        public void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            console = ModHelper.Console;

            new Harmony("MDJ287.TasTools").PatchAll(Assembly.GetExecutingAssembly());

            OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen);
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;

            GlobalMessenger.AddListener("GamePaused", new Callback(this.OnGamePaused));
            GlobalMessenger.AddListener("GameUnpaused", new Callback(this.OnGameUnpaused));
            GlobalMessenger.AddListener("WakeUp", new Callback(this.OnWakeUp));
        }

        // load

        public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
        {
            isInSolarSystem = false;
            if (newScene != OWScene.SolarSystem) return;
            isInSolarSystem = true;
            if (previousScene == OWScene.TitleScreen)
            {
                addFramePrompt = true;
                isAsleep = true;
            }
        }

        // events

        private void OnGamePaused()
        {
            isGamePaused = true;
        }

        private void OnGameUnpaused()
        {
            isGamePaused = false;
        }

        private void OnWakeUp()
        {
            isAsleep = false;
        }

        // updates

        public void FixedUpdate()
        {
            if (addFramePrompt && Locator.GetPromptManager() != null)
            {
                framePrompt = new ScreenPrompt("FRAME: 0");
                Locator.GetPromptManager().AddScreenPrompt(framePrompt, PromptPosition.UpperRight, true);
                addFramePrompt = false;
            }
            if (isInSolarSystem && !isGamePaused && Time.time > 0)
            {
                currentTasFrame++;
                if (isReplayingTas)
                {
                    PlayInputs();
                }
            }
        }

        public void Update()
        {
            if (isInSolarSystem)
            {
                Locator.GetPromptManager().UpdateText(framePrompt, "FRAME: " + currentTasFrame);
                framePrompt.SetVisibility(true);
            }
            if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.cancel))
            {
                if (isReplayingTas)
                {
                    isReplayingTas = false;
                }
                else if (isAsleep)
                {
                    fileReader = new("OuterWilds_Data/Managed/OWML._MDJ287.inputs.txt");
                    isReplayingTas = true;
                    remainingFramesOfSameInputs = 0;
                    lastFrameCommands = [];
                    RePressedCommands = [];
                }
            }
            // change to setting later
            if (Keyboard.current.slashKey.wasPressedThisFrame)
            {
                fileReader.Close();
                isReplayingTas = false;
                currentTasFrame = 0;
                forceLoadFromTitle = true;
                LoadManager.ReloadSceneImmediate();
                forceLoadFromTitle = false;
            }
        }

        // tas inputs

        private void PlayInputs()
        {
            lastFrameCommands = inputCommands;
            if (remainingFramesOfSameInputs > 1)
            {
                remainingFramesOfSameInputs--;
            }
            else
            {
                string[] data = [""];
                while (data[0].Length == 0 || data[0][0] == '#')
                {
                    data = fileReader.ReadLine()?.Split('\t');
                    if (data == null)
                    {
                        fileReader.Close();
                        isReplayingTas = false;
                        return;
                    }
                }

                // first 3 columns

                remainingFramesOfSameInputs = int.Parse(data[0]);
                string[] vectorStr = data[1].Split(',');
                walkAxis = new(float.Parse(vectorStr[0]), float.Parse(vectorStr[1]));
                vectorStr = data[2].Split(',');
                lookAxis = new(float.Parse(vectorStr[0]), float.Parse(vectorStr[1]));

                int j = 0;
                int jRePressed = 0;

                inputCommands = new IInputCommands[data.Length-3];
                RePressedCommands = new IInputCommands[data.Length-3];

                // misc buttons

                for (int i = 3; i < data.Length; i++)
                {
                    if (data[i].Equals("jump"))
                    {
                        inputCommands[j] = InputLibrary.jump;
                    }
                    else if (data[i].Equals("+jump"))
                    {
                        inputCommands[j] = InputLibrary.jump;
                        for (int k=0; k<lastFrameCommands.Length; k++)
                        {
                            if (lastFrameCommands[k].Equals(InputLibrary.jump))
                            {
                                RePressedCommands[jRePressed] = InputLibrary.jump;
                                jRePressed++;
                            }
                        }
                    }
                    else if (data[i].Equals("interact"))
                    {
                        inputCommands[j] = InputLibrary.interact;
                    }
                    else
                    {
                        j--;
                    }
                    j++;
                }
            }
        }

        // accessors

        public static bool OverrideControls()
        {
            return Instance.isReplayingTas;
        }
        public static bool AreInputsAllowed()
        {
            return Instance.currentTasFrame > 121 || Instance.currentTasFrame < 1;
        }

        public static int GetFrame()
        {
            return Instance.currentTasFrame;
        }
    }

}
