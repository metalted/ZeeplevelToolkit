using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZeeplevelToolkit
{
    public class ZeeplevelFile
    {
        public FileInfo zeeplevel;
        public FileInfo zeepIndex;
        public FileInfo thumbnail;
    }

    public class ZeeplevelBrowseResult
    {
        public List<ZeeplevelFile> files = new List<ZeeplevelFile>();
        public List<DirectoryInfo> directories = new List<DirectoryInfo>();
    }

    public static class ZeeplevelIO
    {
        private static bool init = false;

        public static string PluginBasePath;
        public static string BlueprintBasePath;
        public static string LevelBasePath;

        public static void Initialize()
        {
            if(init)
            {
                return;
            }

            PluginBasePath = AppDomain.CurrentDomain.BaseDirectory + @"\BepInEx\plugins";
            BlueprintBasePath = Path.Combine(PluginBasePath, "Blueprints");

            if (!Directory.Exists(BlueprintBasePath))
            {
                Directory.CreateDirectory(BlueprintBasePath);
            }

            LevelBasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Zeepkist\\Levels";
        }

        public static bool FileExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                return File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

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
                ? ZeeplevelHandler.FromJSON(content, file)
                : ZeeplevelHandler.FromCSV(Regex.Split(content, @"\r\n|\r|\n"), file);
        }

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

        public static List<ZeeplevelFile> GetBlueprints()
        {
            List<ZeeplevelFile> result = new List<ZeeplevelFile>();

            if (!Directory.Exists(BlueprintBasePath))
                return result;

            string[] files = Directory.GetFiles(BlueprintBasePath, "*.zeeplevel", SearchOption.AllDirectories);

            foreach (string path in files)
            {
                result.Add(new ZeeplevelFile
                {
                    zeeplevel = new FileInfo(path),
                    zeepIndex = null,
                    thumbnail = null
                });
            }

            return result;
        }

        public static List<ZeeplevelFile> SearchBlueprints(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return GetBlueprints();

            searchTerm = searchTerm.ToLowerInvariant();

            return GetBlueprints()
                .Where(f => f.zeeplevel.Name.ToLowerInvariant().Contains(searchTerm))
                .ToList();
        }

        public static ZeeplevelBrowseResult BrowseBlueprints(string subDirectory)
        {
            ZeeplevelBrowseResult result = new ZeeplevelBrowseResult();

            string root = BlueprintBasePath;
            string targetPath = Path.Combine(root, subDirectory ?? "");

            if (!Directory.Exists(targetPath))
                return result;

            DirectoryInfo dir = new DirectoryInfo(targetPath);

            // --- FILES (non-recursive)
            FileInfo[] levelFiles = dir.GetFiles("*.zeeplevel", SearchOption.TopDirectoryOnly);

            foreach (var file in levelFiles)
            {
                result.files.Add(new ZeeplevelFile
                {
                    zeeplevel = file,
                    zeepIndex = null,
                    thumbnail = null
                });
            }

            // --- DIRECTORIES (recursive check)
            DirectoryInfo[] subDirs = dir.GetDirectories();

            foreach (var sub in subDirs)
            {
                bool containsLevel = Directory
                    .GetFiles(sub.FullName, "*.zeeplevel", SearchOption.AllDirectories)
                    .Length > 0;

                if (containsLevel)
                {
                    result.directories.Add(sub);
                }
            }

            return result;
        }

        public static List<ZeeplevelFile> GetLevels()
        {
            List<ZeeplevelFile> result = new List<ZeeplevelFile>();

            if (!Directory.Exists(LevelBasePath))
                return result;

            string[] files = Directory.GetFiles(LevelBasePath, "*.zeeplevel", SearchOption.AllDirectories);

            foreach (string path in files)
            {
                FileInfo zeeplevelFile = new FileInfo(path);
                DirectoryInfo dir = zeeplevelFile.Directory;

                FileInfo thumbnail = null;
                FileInfo zeepIndex = null;

                if (dir != null)
                {
                    // find first jpg in same folder
                    string jpgPath = Directory.GetFiles(dir.FullName, "*.jpg", SearchOption.TopDirectoryOnly)
                                              .FirstOrDefault();

                    if (jpgPath != null)
                        thumbnail = new FileInfo(jpgPath);

                    // find first zeepindex in same folder
                    string indexPath = Directory.GetFiles(dir.FullName, "*.zeepindex", SearchOption.TopDirectoryOnly)
                                                .FirstOrDefault();

                    if (indexPath != null)
                        zeepIndex = new FileInfo(indexPath);
                }

                result.Add(new ZeeplevelFile
                {
                    zeeplevel = zeeplevelFile,
                    zeepIndex = zeepIndex,
                    thumbnail = thumbnail
                });
            }

            return result;
        }

        public static List<ZeeplevelFile> SearchLevels(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return GetLevels();

            searchTerm = searchTerm.ToLowerInvariant();

            return GetLevels()
                .Where(f => f.zeeplevel.Name.ToLowerInvariant().Contains(searchTerm))
                .ToList();
        }

        public static ZeeplevelBrowseResult BrowseLevels(string subDirectory)
        {
            ZeeplevelBrowseResult result = new ZeeplevelBrowseResult();

            string root = LevelBasePath;
            string targetPath = Path.Combine(root, subDirectory ?? "");

            if (!Directory.Exists(targetPath))
                return result;

            DirectoryInfo dir = new DirectoryInfo(targetPath);

            // --- FILES (non-recursive)
            FileInfo[] levelFiles = dir.GetFiles("*.zeeplevel", SearchOption.TopDirectoryOnly);

            foreach (var file in levelFiles)
            {
                DirectoryInfo parent = file.Directory;

                FileInfo thumbnail = null;
                FileInfo zeepIndex = null;

                if (parent != null)
                {
                    string jpg = Directory.GetFiles(parent.FullName, "*.jpg", SearchOption.TopDirectoryOnly)
                                          .FirstOrDefault();

                    if (jpg != null)
                        thumbnail = new FileInfo(jpg);

                    string idx = Directory.GetFiles(parent.FullName, "*.zeepindex", SearchOption.TopDirectoryOnly)
                                          .FirstOrDefault();

                    if (idx != null)
                        zeepIndex = new FileInfo(idx);
                }

                result.files.Add(new ZeeplevelFile
                {
                    zeeplevel = file,
                    zeepIndex = zeepIndex,
                    thumbnail = thumbnail
                });
            }

            // --- DIRECTORIES (recursive check)
            DirectoryInfo[] subDirs = dir.GetDirectories();

            foreach (var sub in subDirs)
            {
                bool containsLevel = Directory
                    .GetFiles(sub.FullName, "*.zeeplevel", SearchOption.AllDirectories)
                    .Length > 0;

                if (containsLevel)
                {
                    result.directories.Add(sub);
                }
            }

            return result;
        }
    }
}
