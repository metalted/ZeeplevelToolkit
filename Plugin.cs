using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZeeplevelToolkit
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.metalted.zeepkist.zeepleveltoolkit";
        public const string PLUGIN_NAME = "ZeeplevelToolkit";
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

        private void DoCreationTest()
        {
            if(Input.GetKeyDown(KeyCode.L))
            {
                LEV_LevelEditorCentral central = GameObject.FindObjectOfType<LEV_LevelEditorCentral>();
                if(central == null)
                {
                    return;
                }

                List<BlockDescription> descriptions = new List<BlockDescription>();

                int columns = 16; // set your column count
                float spacingX = 32f;
                float spacingZ = 32f;
                float amount = PlayerManager.Instance.loader.globalBlockList.blocks.Count;

                for (int i = 0; i < amount; i++)
                {
                    int col = i % columns;
                    int row = i / columns;

                    Vector3 position = new Vector3(
                        col * spacingX,
                        0f,
                        row * spacingZ
                    );

                    descriptions.Add(new BlockDescription(i, position, Quaternion.identity));
                }

                List<BlockProperties> created = EditorOperations.CreateFromDescriptions(central, descriptions);
            }
        }       
    }

    /*[HarmonyPatch(typeof(BlockProperties), "ConvertBlockToJSON_v15")]
    public static class Patch_ConvertBlockToJSON
    {
        static void Postfix(BlockPropertyJSON __result)
        {
            if (__result.d == null)
            {
                __result.d = new PropertyDictionariesJSON();
                Debug.LogWarning("[PATCH] Prevented d=null in JSON");
            }
        }
    }*/
}
