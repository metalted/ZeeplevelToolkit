using BepInEx;
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
            GameObject imager = new GameObject("ZeeplevelImager");
            ZeeplevelImager img = imager.AddComponent<ZeeplevelImager>();
            img.Initialize();
            DontDestroyOnLoad(imager);

            ZeeplevelIO.Initialize();
        }

        private void Update()
        {
            // L = List Levels
            if (Input.GetKeyDown(KeyCode.L))
            {
                var levels = ZeeplevelIO.GetLevels();

                Debug.Log("=== LEVELS ===");
                foreach (var lvl in levels)
                {
                    Debug.Log(lvl.zeeplevel.Name);
                }
            }

            // B = List Blueprints
            if (Input.GetKeyDown(KeyCode.B))
            {
                var blueprints = ZeeplevelIO.GetBlueprints();

                Debug.Log("=== BLUEPRINTS ===");
                foreach (var bp in blueprints)
                {
                    Debug.Log(bp.zeeplevel.Name);
                }
            }

            // S = Search Levels
            if (Input.GetKeyDown(KeyCode.S))
            {
                var results = ZeeplevelIO.SearchLevels("test");

                Debug.Log("=== SEARCH LEVELS (test) ===");
                foreach (var lvl in results)
                {
                    Debug.Log(lvl.zeeplevel.Name);
                }
            }

            // P = Search Blueprints
            if (Input.GetKeyDown(KeyCode.P))
            {
                var results = ZeeplevelIO.SearchBlueprints("test");

                Debug.Log("=== SEARCH BLUEPRINTS (test) ===");
                foreach (var bp in results)
                {
                    Debug.Log(bp.zeeplevel.Name);
                }
            }

            // C = Capture first blueprint
            if (Input.GetKeyDown(KeyCode.C))
            {
                var blueprints = ZeeplevelIO.SearchBlueprints("reset");

                if (blueprints.Count == 0)
                {
                    Debug.Log("No blueprints found");
                    return;
                }

                var first = blueprints[0];

                Debug.Log("Capturing: " + first.zeeplevel.Name);

                var data = ZeeplevelIO.FromFile(first.zeeplevel);

                if (data == null || !data.isValid)
                {
                    Debug.Log("Invalid blueprint");
                    return;
                }

                ZeeplevelImager.Instance.CaptureSubject(256, 4, data, (textures) =>
                {
                    string outputDir = Path.Combine(Application.persistentDataPath, "Captures");

                    if (!Directory.Exists(outputDir))
                        Directory.CreateDirectory(outputDir);

                    for (int i = 0; i < textures.Count; i++)
                    {
                        byte[] png = textures[i].EncodeToPNG();

                        string filePath = Path.Combine(outputDir, $"capture_{i}.png");
                        File.WriteAllBytes(filePath, png);

                        Debug.Log("Saved: " + filePath);
                    }
                });
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                var blueprints = ZeeplevelIO.SearchBlueprints("reset");

                if (blueprints.Count == 0)
                {
                    Debug.Log("No blueprints found");
                    return;
                }

                var first = blueprints[0];                

                ZeeplevelData data = ZeeplevelIO.FromFile(first.zeeplevel);

                if (data == null || !data.isValid)
                {
                    Debug.Log("Invalid blueprint");
                    return;
                }

                LEV_LevelEditorCentral central = GameObject.FindObjectOfType<LEV_LevelEditorCentral>();
                if(central == null)
                {
                    Debug.Log("No central");
                    return;
                }

                List<BlockProperties> blocks = ZeeplevelHandler.LoadIntoEditor(data, central);

                Debug.Log("Spawned blocks: " + blocks.Count);
            }
        }
    }
}
