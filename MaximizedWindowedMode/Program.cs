using BepInEx;
using BepInEx.Configuration;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.SceneManagement;
using SettingsMenu.Components;
using System.Linq;
using HarmonyLib;
using Logger = BepInEx.Logging.Logger;
using System.Reflection;
using TMPro;
using SettingsMenu.Models;
using System.Runtime.CompilerServices;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using Unity.Collections;
using System.Runtime.InteropServices;
using GameConsole.pcon;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib.Tools;
using static UnityEngine.Random;
using System.Collections;

namespace MWM;

[BepInPlugin("dolfelive.Ultrakill.MaximizedWindowedMode", "MaximizedWindowedMode", "1.0.0")]
public class MaximizedWindowedMode : BaseUnityPlugin
{
    public static MaximizedWindowedMode Instance;
    void Awake()
    {
        Instance = this;
        logger = Logger;

        //Application.runInBackground = true;

        SceneManager.sceneLoaded += OnSceneLoaded;

        new Harmony("dolfelive.Ultrakill.MaximizedWindowedMode").PatchAll();
    }
    public static ManualLogSource logger;
    public void OnSceneLoaded(Scene scene, LoadSceneMode lsm)
    {
        Logger.LogInfo($"[MWM] Scene loaded: {scene.name}");

        if (scene.name == "b3e7f2f8052488a45b35549efb98d902" || SceneHelper.CurrentScene == "Main Menu") // Main Menu
        {
            Logger.LogInfo($"[MWM] Main menu loaded");
            SetWindowState();
            StartCoroutine(setLater());
        }
    }
    IEnumerator setLater()
    {
        yield return new WaitForSeconds(0.1f);
        SetWindowState();

    }
    public static void PrefsManagerExists()
    {
        if (PrefsManager.Instance.localPrefMap.ContainsKey("maxWindowedMode") == false)
            PrefsManager.Instance.localPrefMap.Add("maxWindowedMode", true);

        MaximizedWindowedMode.SetWindowState();
    }

    public static GameObject InstanciateUtil(GameObject go, Transform parent)
    {
        return Instantiate(go, parent);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            SetWindowState();
        }
    }
    public static void SetWindowState()
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        IntPtr hWnd = GetActiveWindow();

        int width = PrefsManager.Instance.GetIntLocal("resolutionWidth");
        int height = PrefsManager.Instance.GetIntLocal("resolutionHeight");

        ShowWindow(hWnd, PrefsManager.Instance.GetBoolLocal("maxWindowedMode") == true ? 3 : 1);
    }

}


[HarmonyPatch(typeof(SettingsItemBuilder), "ConfigureFrom")]
[HarmonyPatch(MethodType.Normal)]
public class SettingsItemBuilderPatch
{
    [HarmonyPrefix]
    public static void ConfigFromPatch(SettingsItemBuilder __instance, SettingsItem item, SettingsCategory category, SettingsPageBuilder pageBuilder)
    {

        if (item.GetLabel() == "FULLSCREEN" && __instance.gameObject.name != "maxWindowedMode")
        {
            GameObject maxWindowedMode = MaximizedWindowedMode.InstanciateUtil(__instance.gameObject, __instance.transform.parent);
            maxWindowedMode.name = "maxWindowedMode";

            SettingsItem modifiedItem = item.Clone();
            modifiedItem.label = "Max Window Mode";
            modifiedItem.preferenceKey.key = "maxWindowedMode";
                        

            maxWindowedMode.GetComponent<SettingsItemBuilder>().ConfigureFrom(modifiedItem, category, pageBuilder);
        }
        if (__instance.gameObject.name == "maxWindowedMode")
        {
            PrefsManager.onPrefChanged += (str, obj) =>
            {
                Debug.Log($"Pref changed: {str}, {obj}");
                if (str == "maxWindowedMode" && SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
                {
                    MaximizedWindowedMode.SetWindowState();
                }
            };
        }
    }
}


[HarmonyPatch(typeof(PrefsManager), "Initialize")]
[HarmonyPatch(MethodType.Normal)]
public class PrefsManagerStarted
{
    [HarmonyPostfix]
    public static void PrefsManagerPostfix()
    {
        MaximizedWindowedMode.PrefsManagerExists();
    }
}

public static class ObjectExtentions
{
    public static T Clone<T>(this T obj)
    {
        if (obj == null) return default;

        var type = obj.GetType();
        var clone = Activator.CreateInstance(type);

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            field.SetValue(clone, field.GetValue(obj));
        }

        return (T)clone;
    }
}