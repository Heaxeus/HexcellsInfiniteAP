using System;
using System.Runtime.InteropServices;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;
using System.Linq;
using Newtonsoft.Json;
using System.CodeDom;
using System.Reflection.Emit;
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


        // Token: 0x040001B1 RID: 433
	    public int numberOfGems;

        // Token: 0x040001B2 RID: 434
        public int[] levelGemsUnlocked = new int[36];

        // Token: 0x040001B3 RID: 435
        public int[] animationsPlayed = new int[5];
    }




    private void ReloadCellDisplay()
    {

        Logger.LogMessage("Reloading Cell Displays!");

        var gameManagerScript = GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>();

        for (int i = 0; i < 6; i++)
        {
            Logger.LogMessage("Reloading Cell " + i + " Display...");

            if (gameManagerScript.currentSlotNumberOfGems < gameManagerScript.currentPerGameData.worldUnlockThresholds[i])
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
                        if (levelsCleared[((transform3.GetComponent<MenuHexLevel>().levelToLoad) + (i * 6)) - 1])
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


    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        Logger.LogMessage("BEFORE PATCH");
        Harmony.CreateAndPatchAll(typeof(Plugin));
        Logger.LogMessage("AFTER PATCH");

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

        // for (int i = 0; i < levelsCleared.Length; i++)
        // {
        //     levelsCleared[i] = false;
        // }
    }


    KeyboardShortcut key = new KeyboardShortcut(KeyCode.U);


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
            if (SceneManager.GetActiveScene().name == "Level Generator") {
                for (int i = 0; i < 93; i++) {
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

        if (SceneManager.GetActiveScene().name == "Menu - Hexcells Infinite" && switchSceneCheck)
        {
            try
            {
                switchSceneCheck = false;
                GameObject.Find("Blue Hex 1").SetActive(false);
                GameObject.Find("Blue Hex 3").SetActive(false);

                GameObject.Find("Blue Hex 2/Label").GetComponent<TextMeshPro>().text = session.DataStorage.GetSlotData().GetValueSafe("Slot").ToString();
                GameObject.Find("Unlock Amount 1").GetComponent<TextMesh>().text = "0";
                GameObject.Find("Unlock Amount 2").GetComponent<TextMesh>().text = "6";
                GameObject.Find("Unlock Amount 3").GetComponent<TextMesh>().text = "12";
                GameObject.Find("Unlock Amount 4").GetComponent<TextMesh>().text = "18";
                GameObject.Find("Unlock Amount 5").GetComponent<TextMesh>().text = "24";
                GameObject.Find("Unlock Amount 6").GetComponent<TextMesh>().text = "30";

                GameObject.Find("Out of Number").GetComponent<TextMesh>().text = "36";

                //GameObject.Find("Gems Number").GetComponent<TextMesh>().text = itemCount.ToString();

                //GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().LoadGame();
                if (itemCount != GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentSlotNumberOfGems)
                {
                    GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentSlotNumberOfGems = itemCount;
                }
                ReloadCellDisplay();
                //levelEntered.OnMouseExit();
                //Logger.LogMessage("TEST");
                // for (int i = 0; i < levelsCleared.Length; i++) {
                //     Logger.LogMessage(levelsCleared[i]);
                // }

            }
            catch (Exception e)
            {
                Logger.LogMessage(e);
            }
        }
    }


    [HarmonyPatch(typeof(GameManagerScript), "LoadGame")]
    [HarmonyPrefix]
    public static bool Prefix_ModifyGemDisplay_LoadGame(GameManagerScript __instance)
    {
        Logger.LogMessage("LOAD GAME");
        // __instance.SaveGame();
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


        // for (int i = 0; i < levelsCleared.Length; i++)
        // {
        //     Logger.LogMessage(levelsCleared[i]);
        // }
        
        


        

        __instance.SaveGame();

        return false;
    }



    [HarmonyPatch(typeof(GameManagerScript), "LoadSaveSlotsInfo")]
    [HarmonyPrefix]
    public static bool Prefix_LoadAdditionalValues_LoadSaveSlotsInfo(ref int __result, int slot, GameManagerScript __instance)
    {

        // string text = string.Concat(new object[] { __instance.executablePath, "/saves/slotAP.save" });
		// BinaryFormatter binaryFormatter = new BinaryFormatter();
		// SaveAddData saveAddData = new SaveAddData();
		// if (File.Exists(text))
		// {
		// 	Stream stream = File.Open(text, FileMode.Open);
		// 	binaryFormatter = new BinaryFormatter();
		// 	saveAddData = (SaveAddData)binaryFormatter.Deserialize(stream);
		// 	stream.Close();
		// }
		// __result = saveAddData.numberOfGems;
        return false;

    }


    [HarmonyPatch(typeof(GameManagerScript), "CheckIfOldFormatSaveExistsAndConvertToNewSaveFormat")]
    [HarmonyPrefix]
    public static bool Prefix_SaveAdditionalValues_CheckIfOldFormatSaveExistsAndConvertToNewSaveFormat(GameManagerScript __instance)
    {

        return false;

    }




    [HarmonyPatch(typeof(GameManagerScript), "SaveGame")]
    [HarmonyPrefix]
    public static bool Prefix_SaveAdditionalValues_SaveGame(GameManagerScript __instance)
    {
        Logger.LogMessage("SAVED GAME");
        try
        {
            SaveAddData saveData = new SaveAddData();
            saveData.levelsCleared = levelsCleared;
            // saveData.indexOfLastRecievedItem = indexOfLastRecievedItem;
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
                // GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentSlotLevelGemsUnlocked[levelEntered.levelToLoad - 1] = GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentPerGameData.levelRewardAmounts[levelEntered.levelToLoad];
                Logger.LogMessage("Perfect Complete!");
                levelsCleared[levelEntered.levelToLoad - 1] = true;
            }
            else
            {
                Logger.LogMessage("Beat the Level without Mistakes to unlock check!");
                // GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentSlotLevelGemsUnlocked[levelEntered.levelToLoad - 1] = GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentPerGameData.levelRewardAmounts[levelEntered.levelToLoad] - 1;
            }
            Logger.LogMessage(levelEntered.levelToLoad);
            Logger.LogMessage(GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentSlotLevelGemsUnlocked[levelEntered.levelToLoad - 1]);
            Logger.LogMessage(GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().currentPerGameData.levelRewardAmounts[levelEntered.levelToLoad]);
            //     for (int i = 0; i < levelsCleared.Length; i++) {
            //     Logger.LogMessage(levelsCleared[i]);
            // }   
            GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().SaveGame();
            switchSceneCheck = true;
            
            
        }
    }



    [HarmonyPatch(typeof(MenuHexLevel), "OnMouseOver")]
    [HarmonyPrefix]
    public static bool Prefix_Test_MenuHexLevel(MenuHexLevel __instance)
    {
        if (Input.GetMouseButtonDown(0))
        {
            levelEntered = __instance;
            GameObject.Find("Game Manager(Clone)").GetComponent<OptionsManager>().currentOptions.levelGenHardModeActive = false;
            __instance.musicDirector.ChangeTrack(__instance.levelTrack);

            //GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().seedNumber = random.Next(-99999999, 99999999).ToString();
            GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().seedNumber = "95637466";
            GameObject.Find("Fader").GetComponent<FaderScript>().FadeOut(37);
            switchSceneCheck = true;
        }
        return false;
    }
    
    [HarmonyPatch(typeof(StartGeneratorLevel), "OnMouseOver")]
    [HarmonyPrefix]
    public static bool Prefix_StartGeneratorLevel_OnMouseOver(StartGeneratorLevel __instance)
    {
        if (__instance.isActive && Input.GetMouseButtonDown(0))
        {
            int num = int.Parse(__instance.seedText.text) % 9;
            if (num == 0)
            {
                __instance.musicDirector.ChangeTrack(MusicDirector.Track.Aeolian);
            }
            else if (num == 1)
            {
                __instance.musicDirector.ChangeTrack(MusicDirector.Track.Melodia);
            }
            else if (num == 2)
            {
                __instance.musicDirector.ChangeTrack(MusicDirector.Track.Raindrops);
            }
            else if (num == 3)
            {
                __instance.musicDirector.ChangeTrack(MusicDirector.Track.M1);
            }
            else if (num == 4)
            {
                __instance.musicDirector.ChangeTrack(MusicDirector.Track.M2);
            }
            else if (num == 5)
            {
                __instance.musicDirector.ChangeTrack(MusicDirector.Track.M3);
            }
            else if (num == 6)
            {
                __instance.musicDirector.ChangeTrack(MusicDirector.Track.NewTrack1);
            }
            else if (num == 7)
            {
                __instance.musicDirector.ChangeTrack(MusicDirector.Track.NewTrack2);
            }
            else if (num == 8)
            {
                __instance.musicDirector.ChangeTrack(MusicDirector.Track.NewTrack3);
            }
            GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().seedNumber = random.Next(-99999999, 99999999).ToString();
            GameObject.Find("Game Manager(Clone)").GetComponent<GameManagerScript>().isLoadingSavedLevelGenState = false;
            GameObject.Find("Fader").GetComponent<FaderScript>().FadeOut(37);
            GameObject.Find("Loading Text").GetComponent<LoadingText>().FadeIn();
            switchSceneCheck = true;
            
        }

        return false;
    }

}