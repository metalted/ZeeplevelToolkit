using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeeplevelToolkit
{
    public class ZeeplevelData
    {
        public enum DataType { None, CSV, JSON };

        public LevelScriptableObject level;
        public v15LevelJSON json;
        public v14LevelCSV csv;
        public bool isValid;

        public DataType GetDataType()
        {
            if(json != null)
            {
                return DataType.JSON;
            }
            else if(csv != null)
            {
                return DataType.CSV;
            }

            return DataType.None;
        }
    }
}
