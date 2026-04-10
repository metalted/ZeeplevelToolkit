using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace ZeeplevelToolkit
{
    public static class ZeeplevelHandler
    {
        public static ZeeplevelData FromPath(string path)
        {
            return FromFile(new FileInfo(path));
        }
        public static ZeeplevelData FromFile(FileInfo file)
        {
            if (file == null || !file.Exists)
            {
                return null;
            }

            string content = File.ReadAllText(ZeepkistFolders.ConvertToLongPath(file.FullName));
            return GeneralLevelLoadStatic.IsThisLevelDataStringV15(content)
                ? FromJSON(content, file)
                : FromCSV(Regex.Split(content, @"\r\n|\r|\n"), file);
        }
        public static ZeeplevelData FromJSON(string json, FileInfo file = null)
        {
            var data = new ZeeplevelData
            {
                json = JsonConvert.DeserializeObject<v15LevelJSON>(json),
                level = ScriptableObject.CreateInstance<LevelScriptableObject>(),
                isValid = true
            };

            var level = data.level;
            var j = data.json;

            level.Author = j.author.name;
            level.Collaborators = j.author.collaborators;
            level.OverrideAuthorName = j.author.nameOverride;
            level.UID = j.level.UID;
            level.IsValidated = j.medals.isLegit;
            level.TimeAuthor = j.medals.author;
            level.TimeGold = j.medals.gold;
            level.TimeSilver = j.medals.silver;
            level.TimeBronze = j.medals.bronze;
            level.useLevelV15Data = true;
            level.LevelDataV15 = json;

            if (file != null)
            {
                level.Name = Path.GetFileNameWithoutExtension(file.Name);
                level.Path = file.FullName;
            }

            ZeeplevelHelpers.SetFallbackTimes(data);
            return data;
        }
        public static ZeeplevelData FromCSV(string[] lines, FileInfo file = null)
        {
            var csv = new v14LevelCSV(lines);
            if (!csv.IsValid)
                return new ZeeplevelData { isValid = false };

            var data = new ZeeplevelData
            {
                csv = csv,
                level = ScriptableObject.CreateInstance<LevelScriptableObject>(),
                isValid = true
            };

            var level = data.level;
            var h = csv.Header;

            level.Author = h.PlayerName;
            level.UID = h.UUID;
            level.TimeAuthor = h.AuthorTime;
            level.TimeGold = h.GoldTime;
            level.TimeSilver = h.SilverTime;
            level.TimeBronze = h.BronzeTime;
            level.useLevelV15Data = false;
            level.LevelData = lines;

            if (file != null)
            {
                level.Name = Path.GetFileNameWithoutExtension(file.Name);
                level.Path = file.FullName;
            }

            ZeeplevelHelpers.SetFallbackTimes(data);
            return data;
        }
        public static ZeeplevelData FromEditor(List<BlockProperties> blocks, string levelName, LEV_LevelEditorCentral central, SkyboxManager skybox)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return new ZeeplevelData { isValid = false };
            }

            v15LevelJSON zeepLevel = new v15LevelJSON();

            // Level
            zeepLevel.level.name = levelName;
            zeepLevel.level.UID = ZeeplevelHelpers.GenerateUniqueID(central.manager.steamAchiever.GetPlayerName(false));

            // Author
            zeepLevel.author.name = central.manager.steamAchiever.GetPlayerName(false);
            zeepLevel.author.StmID = central.manager.steamAchiever.GetPlayerSteamID();

            // Medals
            if (central.manager.validated)
            {
                zeepLevel.medals.isLegit = true;
                zeepLevel.medals.author = central.manager.validationTime;

                if (central.medalTimes.allGood)
                {
                    zeepLevel.medals.gold = central.medalTimes.goldTime;
                    zeepLevel.medals.silver = central.medalTimes.silverTime;
                    zeepLevel.medals.bronze = central.medalTimes.bronzeTime;
                }
                else
                {
                    float t = central.manager.validationTime;
                    zeepLevel.medals.gold = t * 1.1f;
                    zeepLevel.medals.silver = t * 1.2f;
                    zeepLevel.medals.bronze = t * 1.35f;
                }
            }
            else
            {
                zeepLevel.medals.isLegit = false;
                zeepLevel.medals.author = 0f;
                zeepLevel.medals.gold = 0f;
                zeepLevel.medals.silver = 0f;
                zeepLevel.medals.bronze = 0f;
            }

            // Environment
            zeepLevel.enviro.skybox = skybox.current;
            zeepLevel.enviro.groundMat = central.painter.currentGroundMaterial;
            zeepLevel.enviro.overrideFog_b = skybox.overrideFogBool;
            zeepLevel.enviro.overrideFog_f = skybox.overrideFogFloat;
            if (skybox.GetCurrentCustomSkybox() != null)
                zeepLevel.enviro.skyboxOverride = skybox.GetCurrentCustomSkybox();
            else if (GetSettings.Get().hidden_saveLevelsWithCustomSkyboxTemplate || PlayerManager.Instance.version.saveLevelWithSkyboxTemplate)
                zeepLevel.enviro.skyboxOverride = new SkyboxCreator_DataObject();

            // Camera
            zeepLevel.editcam.pos = new CV3(central.cam.transform.position);
            zeepLevel.editcam.euler = new CV3(central.cam.cameraTransform.eulerAngles);
            zeepLevel.editcam.rotXY = new CV2(new Vector2(central.cam.rotationX, central.cam.rotationY));

            // Blocks
            zeepLevel.blox = blocks.Select(b => b.ConvertBlockToJSON_v15()).ToList();

            // Hash
            zeepLevel.level.zeepHash = GeneralLevelLoadStatic.HashLevel(zeepLevel, blocks);

            // Wrap into ZeeplevelData
            var data = new ZeeplevelData
            {
                json = zeepLevel,
                level = ScriptableObject.CreateInstance<LevelScriptableObject>(),
                isValid = true
            };

            data.level.Name = levelName;
            data.level.Author = zeepLevel.author.name;
            data.level.Collaborators = zeepLevel.author.collaborators;
            data.level.OverrideAuthorName = zeepLevel.author.nameOverride;
            data.level.UID = zeepLevel.level.UID;
            data.level.IsValidated = zeepLevel.medals.isLegit;
            data.level.TimeAuthor = zeepLevel.medals.author;
            data.level.TimeGold = zeepLevel.medals.gold;
            data.level.TimeSilver = zeepLevel.medals.silver;
            data.level.TimeBronze = zeepLevel.medals.bronze;
            data.level.useLevelV15Data = true;
            data.level.LevelDataV15 = JsonConvert.SerializeObject(zeepLevel, Formatting.Indented);

            return data;
        }
        
        public static void LoadIntoEditor(ZeeplevelData data, LEV_LevelEditorCentral central, Vector3? position)
        {
            if(!ZeeplevelHelpers.WillSuccesfullyLoadIntoEditor(data))
            {
                return;
            }

            central.selection.DeselectAllBlocks(true, nameof(central.selection.ClickNothing));



            List<BlockProperties> blockList = data.GetDataType() == LoadBlocks(data);
            

        }

        private static List<BlockProperties> LoadBlocks()

        public static void SaveToFile(ZeeplevelData data, string path)
        {
            if (data == null || !data.isValid)
            {
                return;
            }

            if (data.json != null)
            {
                string jsonString = JsonConvert.SerializeObject(data.json, Formatting.Indented);
                File.WriteAllText(path, jsonString);
            }
            else if (data.csv != null)
            {
                string[] csvLines = data.csv.ToCSV();
                File.WriteAllLines(path, csvLines);
            }
            else
            {
                return;
            }
        }
        public static void ToCSVFile(ZeeplevelData data, string path)
        {
            if (data.csv == null || !data.isValid)
            {
                return;
            }

            File.WriteAllLines(path, data.csv.ToCSV());
        }
        public static void ToJSONFile(ZeeplevelData data, string path)
        {
            if (data.json == null || !data.isValid)
            {
                return;
            }

            string json = JsonConvert.SerializeObject(data.json, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        
        public static ZeeplevelData Copy(ZeeplevelData original)
        {
            if (original == null || !original.isValid)
                return new ZeeplevelData { isValid = false };

            ZeeplevelData copy = new ZeeplevelData();
            copy.level = ScriptableObject.CreateInstance<LevelScriptableObject>();
            copy.isValid = original.isValid;

            if (original.json != null)
            {
                // Deep copy v15 JSON
                string jsonString = JsonConvert.SerializeObject(original.json);
                copy.json = JsonConvert.DeserializeObject<v15LevelJSON>(jsonString);
                copy.level.useLevelV15Data = true;
                copy.level.LevelDataV15 = jsonString;
            }
            else if (original.csv != null)
            {
                // Deep copy v14 CSV
                string[] csvLines = original.csv.ToCSV();
                copy.csv = new v14LevelCSV(csvLines);
                copy.level.useLevelV15Data = false;
                copy.level.LevelData = csvLines;
            }
            else
            {
                return new ZeeplevelData { isValid = false };
            }

            // Copy level metadata
            copy.level.Name = original.level.Name;
            copy.level.Author = original.level.Author;
            copy.level.Collaborators = original.level.Collaborators;
            copy.level.OverrideAuthorName = original.level.OverrideAuthorName;
            copy.level.UID = original.level.UID;
            copy.level.IsValidated = original.level.IsValidated;
            copy.level.TimeAuthor = original.level.TimeAuthor;
            copy.level.TimeGold = original.level.TimeGold;
            copy.level.TimeSilver = original.level.TimeSilver;
            copy.level.TimeBronze = original.level.TimeBronze;
            copy.level.IsTestLevel = original.level.IsTestLevel;
            copy.level.IsAdventureLevel = original.level.IsAdventureLevel;
            copy.level.UseAvonturenLevel = original.level.UseAvonturenLevel;
            copy.level.Path = original.level.Path;

            return copy;
        }

        
    }
}
