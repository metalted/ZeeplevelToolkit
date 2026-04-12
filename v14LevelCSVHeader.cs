using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeeplevelToolkit
{
    public class v14LevelCSVHeader
    {
        public string SceneName { get; private set; }
        public string PlayerName { get; private set; }
        public string UUID { get; private set; }
        public float[] CameraProperties { get; private set; }
        public float AuthorTime { get; private set; }
        public string AuthorTimeString { get; private set; }
        public float GoldTime { get; private set; }
        public float SilverTime { get; private set; }
        public float BronzeTime { get; private set; }
        public int Skybox { get; private set; }
        public int Floor { get; private set; }
        public bool IsValid { get; private set; }

        public v14LevelCSVHeader()
        {
            SceneName = "LevelEditor2";
            PlayerName = "Bouwerman";
            UUID = ZeeplevelHelpers.GenerateUUID(PlayerName, 0);
            CameraProperties = new float[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            AuthorTime = 0;
            AuthorTimeString = "invalid track";
            GoldTime = 0;
            SilverTime = 0;
            BronzeTime = 0;
            Skybox = 0;
            Floor = -1;
            IsValid = true;
        }

        public v14LevelCSVHeader(string[] csvData)
        {
            CameraProperties = new float[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            Read(csvData);
        }

        public string[] ToCSV()
        {
            // First line: SceneName, PlayerName, UUID
            string firstLine = $"{SceneName},{PlayerName},{UUID}";

            // Second line: CameraProperties
            string secondLine = string.Join(",", CameraProperties);

            // Third line: AuthorTime (or AuthorTimeString), GoldTime, SilverTime, BronzeTime, Skybox, Ground
            string authorTimeValue = AuthorTimeString == "invalid track" ? AuthorTimeString : AuthorTime.ToString(CultureInfo.InvariantCulture);
            string thirdLine = $"{authorTimeValue},{GoldTime},{SilverTime},{BronzeTime},{Skybox},{Floor}";

            return new string[] { firstLine, secondLine, thirdLine };
        }

        public void Read(string[] csvData)
        {
            IsValid = true;

            if (csvData.Length != 3)
            {
                IsValid = false;
                return;
            }

            for (int i = 0; i < csvData.Length; i++)
            {
                string[] values = csvData[i].Split(",");
                if (i == 0)
                {
                    if (values.Length != 3)
                    {
                        IsValid = false;
                        break;
                    }

                    SceneName = values[0];
                    PlayerName = values[1];
                    UUID = values[2];
                }
                else if (i == 1)
                {
                    if (values.Length != 8)
                    {
                        IsValid = false;
                        break;
                    }

                    for (int j = 0; j < 8; j++)
                    {
                        CameraProperties[j] = ToolkitUtils.ParseFloat(values[j]);
                    }
                }
                else if (i == 2)
                {
                    if (values.Length != 6)
                    {
                        IsValid = false;
                        break;
                    }

                    AuthorTime = ToolkitUtils.ParseFloat(values[0]);
                    AuthorTimeString = AuthorTime == 0 ? "invalid track" : "";

                    GoldTime = ToolkitUtils.ParseFloat(values[1]);
                    SilverTime = ToolkitUtils.ParseFloat(values[2]);
                    BronzeTime = ToolkitUtils.ParseFloat(values[3]);
                    Skybox = ToolkitUtils.ParseInt(values[4]);
                    if (Skybox == -1) 
                    { 
                        Skybox = 0; 
                    }
                    Floor = ToolkitUtils.ParseInt(values[5]);
                }
            }
        }
    }
}
