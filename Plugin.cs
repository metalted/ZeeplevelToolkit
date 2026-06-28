using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Toolkist
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.metalted.zeepkist.toolkist";
        public const string PLUGIN_NAME = "Toolkist";
        public const string PLUGIN_VERSION = "1.0";

        public void Awake()
        {
            Harmony harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();

            GameObject imager = new GameObject("ZeeplevelImager");
            ZeeplevelImager img = imager.AddComponent<ZeeplevelImager>();
            img.Initialize();
            DontDestroyOnLoad(imager);

            ZeeplevelIO.Initialize();
        }        
    }
}
