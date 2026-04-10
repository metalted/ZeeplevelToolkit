using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeeplevelToolkit
{
    public static class ZeeplevelHelpers
    {
        public static int GetBlockCount(ZeeplevelData data)
        {
            if(!data.isValid)
            {
                return 0;
            }

            ZeeplevelData.DataType dataType = data.GetDataType();

            switch(dataType)
            {
                default:
                case ZeeplevelData.DataType.None:
                    return 0;
                case ZeeplevelData.DataType.CSV:
                    return data.csv.Blocks.Count;
                case ZeeplevelData.DataType.JSON:
                    return data.json.blox.Count;
            }
        }

        public static void SetFallbackTimes(ZeeplevelData data)
        {
            var level = data.level;
            bool wasMissing = false;

            if (level.TimeAuthor <= 0f)
            {
                level.TimeAuthor = 3540f;
                level.IsValidated = false;
                wasMissing = true;
            }
            else
            {
                level.IsValidated = true;
            }

            if (level.TimeGold <= 0f)
                level.TimeGold = wasMissing ? 3546f : level.TimeAuthor * 1.1f;
            if (level.TimeSilver <= 0f)
                level.TimeSilver = wasMissing ? 3552f : level.TimeAuthor * 1.2f;
            if (level.TimeBronze <= 0f)
                level.TimeBronze = wasMissing ? 3558f : level.TimeAuthor * 1.35f;
        }
        public static string GenerateUniqueID(string playerName)
        {
            string guid = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")
                .Substring(0, 15);

            return guid + "_" + playerName.Replace(",", "");
        }

        public static string GenerateUUID(string playerName, int objectCount)
        {
            // Get the current date and time
            string date = DateTime.Now.ToString("ddMMyyyy");
            string time = DateTime.Now.ToString("HHmmssfff");

            // Generate a 12-digit random number that does not start with 0
            Random random = new Random();
            string randomNumber = (random.Next(1, 10).ToString() + random.Next(0, 1000000000).ToString("D9"));

            // Combine all parts to form the UUID
            string uuid = $"{date}-{time}-{playerName}-{randomNumber}-{objectCount}";

            return uuid;
        }

        public static bool WillSuccesfullyLoadIntoEditor(ZeeplevelData data)
        {
            if (data == null || !data.isValid)
            {
                return false;
            }

            int count = ZeeplevelHelpers.GetBlockCount(data);

            if (count == 0)
            {
                return false;
            }

            if(data.GetDataType() == ZeeplevelData.DataType.None)
            {
                return false;
            }

            return true;
        }
    }
}
