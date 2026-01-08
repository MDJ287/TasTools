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

        StreamReader inputsFileReader;

        public bool isReplayingTas = false;
        public int currentTasFrame = 0;
        private bool isAsleep = true;
        public bool isInSolarSystem = false;
        public bool isGamePaused = true;
        public bool forceLoadFromTitle = false;

        private bool addFramePrompt = true;

        public int remainingFramesOfSameInputs = 0;

        public static Vector2 walkAxis = new(0,0);
        public static Vector2 lookAxis = new(0,0);
        public static IInputCommands[] inputCommands = [];
        public static IInputCommands[] lastFrameCommands = [];

        public static int chumpFrame = -1;
        public static Vector2 chumpVelocity;

        private bool forceRngSeed = false;
        private int rngSeed = 123;

        public ScreenPrompt tasStartPrompt;
        private ScreenPrompt framePrompt;
        private ScreenPrompt tasExitPrompt;

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

        public override void Configure(IModConfig config)
        {
            chumpVelocity = new(config.GetSettingsValue<float>("chumpSpeedX"), config.GetSettingsValue<float>("chumpSpeedZ"));
            forceRngSeed = config.GetSettingsValue<bool>("doRngManip");
            rngSeed = config.GetSettingsValue<int>("rngSeed");
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
                Random.InitState(123);
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
                Locator.GetPromptManager().AddScreenPrompt(framePrompt, PromptPosition.BottomCenter, true);
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
            if (chumpFrame < currentTasFrame)
            {
                chumpFrame = -1;
            }
            if (isInSolarSystem)
            {
                Locator.GetPromptManager().UpdateText(framePrompt, "FRAME: " + currentTasFrame);
                framePrompt.SetVisibility(true);
            }
            // change key to setting later?
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                if (isReplayingTas)
                {
                    StopTasReplay();
                }
                else if (isAsleep)
                {
                    inputsFileReader = new("OuterWilds_Data/Managed/OWML._MDJ287.inputs.txt");
                    isReplayingTas = true;
                    remainingFramesOfSameInputs = 0;
                    lastFrameCommands = [];
                    tasExitPrompt = Prompts.CreatePrompt("Stop TAS Playback", KeyCode.Q);
                    Locator.GetPromptManager().AddScreenPrompt(tasExitPrompt, PromptPosition.BottomCenter, true);
                }
            }
            if (Keyboard.current.slashKey.wasPressedThisFrame)
            {
                if (isReplayingTas) StopTasReplay();
                currentTasFrame = 0;
                forceLoadFromTitle = true;
                LoadManager.ReloadSceneImmediate();
                forceLoadFromTitle = false;
            }
            if (Keyboard.current.commaKey.wasPressedThisFrame)
            {
                chumpFrame = currentTasFrame + 1;
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
                    data = inputsFileReader.ReadLine()?.Split('\t');
                    if (data == null)
                    {
                        StopTasReplay();
                        return;
                    }
                }

                // first 3 columns

                remainingFramesOfSameInputs = int.Parse(data[0]);
                string[] vectorStr;
                if (!data[1].Equals("."))
                {
                    vectorStr = data[1].Split(',');
                    walkAxis = new(float.Parse(vectorStr[0]), float.Parse(vectorStr[1]));
                }
                if (!data[2].Equals("."))
                {
                    vectorStr = data[2].Split(',');
                    lookAxis = new(float.Parse(vectorStr[0]), float.Parse(vectorStr[1]));
                }

                int j = 0;

                inputCommands = new IInputCommands[data.Length-3];

                // misc buttons

                for (int i = 3; i < data.Length; i++)
                {
                    if (data[i].Equals("jump"))
                    {
                        inputCommands[j] = InputLibrary.jump;
                    }
                    else if (data[i].Equals("interact"))
                    {
                        inputCommands[j] = InputLibrary.interact;
                    }
                    else if (data[i].Equals("cancel"))
                    {
                        inputCommands[j] = InputLibrary.cancel;
                    }
                    else if (data[i].Equals("launch"))
                    {
                        inputCommands[j] = InputLibrary.probeLaunch;
                    }
                    else if (data[i].Equals("recall"))
                    {
                        inputCommands[j] = InputLibrary.probeRetrieve;
                    }
                    else
                    {
                        j--;
                    }
                    j++;
                }
            }
        }

        private static void StopTasReplay()
        {
            Instance.isReplayingTas = false;
            Locator.GetPromptManager().RemoveScreenPrompt(Instance.tasExitPrompt);
            Instance.inputsFileReader.Close();
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
