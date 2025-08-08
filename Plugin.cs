using System;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;



namespace HexcellsInfiniteRandomizer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Hexcells Infinite.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public static ArchipelagoSession session;

    public static System.Random random = new System.Random();

    public static MenuHexLevel levelEntered = new MenuHexLevel();

    public static int itemCount = 0;

    public static bool[] levelsCleared = new bool[36];

    public static long indexOfLastRecievedItem = 0;

    public static bool switchSceneCheck = true;


    //Custom version of SaveData, used to include levelsCleared
    [Serializable]
    public class SaveAddData
    {
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("LevelsCleared", levelsCleared);
            info.AddValue("NumberOfGems", numberOfGems);
            info.AddValue("LevelGemsUnlocked", levelGemsUnlocked);
            info.AddValue("AnimationsPlayed", animationsPlayed);
        }


        public bool[] levelsCleared = new bool[36];

        public int numberOfGems;

        public int[] levelGemsUnlocked = new int[36];

        public int[] animationsPlayed = new int[5];
    }



    //Used to ensure UI on Level Select Screen is accurate/correct
    private void ReloadCellDisplay()
    {

        Logger.LogMessage("Reloading Cell Displays!");

        var gameManagerScript = GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>();


        //removes Save slots 1 and 3
        GameObject.Find("Blue Hex 1").SetActive(false);
        GameObject.Find("Blue Hex 3").SetActive(false);
        GameObject.Find("Go To Generator").SetActive(false);
        GameObject.Find("User Levels Button").SetActive(false);

        //sets save slot 2's name to slot name
        GameObject.Find("Blue Hex 2/Label").GetComponent<TextMeshPro>().text = session.DataStorage.GetSlotData().GetValueSafe("Slot").ToString();

        //sets UI unlock amounts to correct amounts
        GameObject.Find("Unlock Amount 1").GetComponent<TextMesh>().text = "0";
        GameObject.Find("Unlock Amount 2").GetComponent<TextMesh>().text = "6";
        GameObject.Find("Unlock Amount 3").GetComponent<TextMesh>().text = "12";
        GameObject.Find("Unlock Amount 4").GetComponent<TextMesh>().text = "18";
        GameObject.Find("Unlock Amount 5").GetComponent<TextMesh>().text = "24";
        GameObject.Find("Unlock Amount 6").GetComponent<TextMesh>().text = "30";

        //changes UI of center gem bottom number
        GameObject.Find("Out of Number").GetComponent<TextMesh>().text = "36";

        //ensures that the amount of items we have recieved is also the number shown in center/current gem count
        if (itemCount != GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentSlotNumberOfGems)
        {
            GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentSlotNumberOfGems = itemCount;
            GameObject.Find("Gems Number").GetComponent<TextMesh>().text = itemCount.ToString();
        }

        Logger.LogMessage(itemCount);
        Logger.LogMessage(GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentSlotNumberOfGems);


        //Logic for handling display proper state of levels (locked, incomplete, perfect)
        for (int i = 0; i < 6; i++)
        {
            Logger.LogMessage("Reloading Cell Group" + i + "...");

            if (itemCount < gameManagerScript.currentPerGameData.worldUnlockThresholds[i])
            {
                IEnumerator enumerator = GameObject.Find((i + 1).ToString()).transform.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        object obj = enumerator.Current;
                        Transform transform2 = (Transform)obj;
                        transform2.GetComponent<MenuHexLevel>().SetState(MenuHexLevel.State.Locked);
                    }
                }
                finally
                {
                    IDisposable disposable;
                    if ((disposable = enumerator as IDisposable) != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
            else
            {
                Transform transform = GameObject.Find((i + 1).ToString()).transform;
                IEnumerator enumerator2 = transform.GetEnumerator();
                try
                {
                    while (enumerator2.MoveNext())
                    {
                        
                        object obj2 = enumerator2.Current;
                        Transform transform3 = (Transform)obj2;
                        Logger.LogMessage(transform3.GetComponent<MenuHexLevel>().levelToLoad - 1);
                        if (levelsCleared[transform3.GetComponent<MenuHexLevel>().levelToLoad - 1])
                        {
                            transform3.GetComponent<MenuHexLevel>().SetState(MenuHexLevel.State.Perfect);
                        }
                        else
                        {
                            transform3.GetComponent<MenuHexLevel>().SetState(MenuHexLevel.State.Notplayed);
                        }
                    }
                }
                finally
                {
                    IDisposable disposable2;
                    if ((disposable2 = enumerator2 as IDisposable) != null)
                    {
                        disposable2.Dispose();
                    }
                }
            }
        }
    }




    //Initial method that establishes AP session
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        Harmony.CreateAndPatchAll(typeof(Plugin));

        session = ArchipelagoSessionFactory.CreateSession("localhost", 38281);
        LoginResult result;

        try
        {
            result = session.TryConnectAndLogin("Hexcells Infinite", "Ex", ItemsHandlingFlags.AllItems);
        }
        catch (Exception e)
        {
            result = new LoginFailure(e.GetBaseException().Message);
        }

        if (!result.Successful)
        {
            LoginFailure failure = (LoginFailure)result;
            string errorMessage = $"Failed to Connect to Hexcells Infinite as Ex:";
            foreach (string error in failure.Errors)
            {
                errorMessage += $"\n    {error}";
            }
            foreach (ConnectionRefusedError error in failure.ErrorCodes)
            {
                errorMessage += $"\n    {error}";
            }


            Logger.LogMessage(result + "\n\n\n\n\n\n");

            Logger.LogMessage(errorMessage);

            return; // Did not connect, show the user the contents of `errorMessage`
        }


        foreach (var stuff in session.DataStorage.GetSlotData())
        {
            Logger.LogMessage(stuff.Key + ": " + stuff.Value);
        }


        foreach (var stuff in session.DataStorage.GetLocationNameGroups())
        {
            Logger.LogMessage(stuff.Key + ": " + stuff.Value);
        }

    }

    //DEBUG key to brute solve puzzles
    KeyboardShortcut key = new KeyboardShortcut(KeyCode.U);

    //runs every frame. used to check for AP items coming in, check goal completion, use brute solver for debug, and ensure level select screen is accurate using ReloadCellDisplay
    private void Update()
    {
        if (session.Items.Any())
        {
            Logger.LogMessage(session.Items.DequeueItem());
            itemCount++;
        }


        if (levelsCleared.All(x => x))
        {
            session.SetGoalAchieved();
        }

        if (key.IsPressed())
        {
            Logger.LogMessage(SceneManager.GetActiveScene().name);
            if (SceneManager.GetActiveScene().name == "Level Generator")
            {
                for (int i = 0; i < 93; i++)
                {
                    var cell = GameObject.Find("Orange Hex(Clone)").GetComponent<HexBehaviour>();
                    if (!cell.containsShapeBlock)
                    {
                        cell.DestroyClick();
                    }
                    else
                    {
                        cell.HighlightClick();
                    }

                }

            }
        }
        //toggled by switchSceneCheck, which is true whenever we switch from any scene back to the level select scene
        if (SceneManager.GetActiveScene().name == "Menu - Hexcells Infinite" && switchSceneCheck)
        {
            try
            {
                switchSceneCheck = false;
                ReloadCellDisplay();

            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
            }
        }
    }


    //custom implementation of LoadGame, which also handles setting unlock amounts, as well as showing correct level states when returning to game
    [HarmonyPatch(typeof(GameManagerScript), "LoadGame")]
    [HarmonyPrefix]
    public static bool Prefix_ModifyGemDisplay_LoadGame(GameManagerScript __instance)
    {
        if (!File.Exists(__instance.executablePath + "/saves/slotAP.save"))
        {
            __instance.SaveGame();
        }
        Logger.LogMessage("LOAD GAME");
        try
        {
            SaveAddData saveData = new SaveAddData();
            string text = string.Concat(new object[] { __instance.executablePath, "/saves/slotAP.save" });
            Stream stream = File.Open(text, FileMode.Open);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            saveData = (SaveAddData)binaryFormatter.Deserialize(stream);
            stream.Close();
            levelsCleared = saveData.levelsCleared;
            __instance.currentSlotNumberOfGems = itemCount;
            __instance.currentSlotLevelGemsUnlocked = saveData.levelGemsUnlocked;
            __instance.currentSlotAnimationsPlayed = saveData.animationsPlayed;

        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }


        __instance.currentPerGameData.worldUnlockThresholds = new int[] { 0, 6, 12, 18, 24, 30 };

        __instance.currentPerGameData.levelRewardAmounts = new int[] {
            0,2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2
        };



        __instance.currentPerGameData.totalGemsInGame = 36;

        for (int i = 0; i < GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentSlotLevelGemsUnlocked.Length; i++)
        {
            if (levelsCleared[i])
            {
                GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentSlotLevelGemsUnlocked[i] = 2;
            }

        }

        return false;
    }


    //custom implementation of SaveGame, used to include levelsCleared
    [HarmonyPatch(typeof(GameManagerScript), "SaveGame")]
    [HarmonyPrefix]
    public static bool Prefix_SaveAdditionalValues_SaveGame(GameManagerScript __instance)
    {
        Logger.LogMessage("SAVED GAME");
        try
        {
            SaveAddData saveData = new SaveAddData();
            saveData.levelsCleared = levelsCleared;
            saveData.animationsPlayed = __instance.currentSlotAnimationsPlayed;
            string text = string.Concat(new object[] { __instance.executablePath, "/saves/slotAP.save" });
            Stream stream = File.Open(text, FileMode.Create);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(stream, saveData);
            stream.Close();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        return false;

    }



    //LoadNextLevel is called whenever a level is completed, perfected or not. This is where locations are sent out, and levelsCleared is updated to reflect if an level has already sent out it's location (i.e. perfected level)
    [HarmonyPatch(typeof(HexScoring), "LoadNextLevel")]
    [HarmonyPrefix]
    public static void Prefix_LocationCheck_LoadNextLevel(HexScoring __instance)
    {
        if (!levelsCleared[levelEntered.levelToLoad - 1])
        {
            Logger.LogMessage("Beat Level");
            if (__instance.numberOfMistakesMade == 0)
            {
                session.Locations.CompleteLocationChecks(75000 + levelEntered.levelToLoad);
                Logger.LogMessage("Perfect Complete!");
                levelsCleared[levelEntered.levelToLoad - 1] = true;
            }
            else
            {
                Logger.LogMessage("Beat the Level without Mistakes to unlock check!");
            }

            GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().SaveGame();
            switchSceneCheck = true;
        }
    }


    //Overwrites what level is loaded when selected what level to play, ensuring that a random level is loaded.
    [HarmonyPatch(typeof(MenuHexLevel), "OnMouseOver")]
    [HarmonyPrefix]
    public static bool Prefix_LoadRandomLevel_MenuHexLevel(MenuHexLevel __instance)
    {
        if (Input.GetMouseButtonDown(0))
        {
            levelEntered = __instance;
            GameObject.Find("Game Manager(Clone)").GetComponent<OptionsManager>().currentOptions.levelGenHardModeActive = false;
            __instance.musicDirector.ChangeTrack(__instance.levelTrack);

            //GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().seedNumber = random.Next(-99999999, 99999999).ToString();
            GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().seedNumber = "95637466";
            GameObject.Find("Fader").GetComponent<FaderScript>().FadeOut(37);
        }
        return false;
    }


    [HarmonyPatch(typeof(MenuExitButton), "OnMouseOver")]
    [HarmonyPrefix]
    public static bool Prefix_CustomExitLevel_MenuExitButton(MenuExitButton __instance)
    {
        if (Input.GetMouseButtonDown(0))
		{
			if (GameObject.Find("Game Manager(Clone)") != null)
			{
				GameManagerScript component = GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>();
				component.SaveCurrentLevelState();
			}
            switchSceneCheck = true;
			GameObject.Find("Fader").GetComponent<FaderScript>().FadeOut(0);
		}
        return false;
    }

    
}