using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ZeeplevelToolkit
{
    public class v14LevelCSVBlock
    {
        public int BlockID { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Rotation { get; private set; }
        public Vector3 Scale { get; private set; }
        public List<float> Properties { get; private set; }
        public bool IsValid { get; private set; }

        public v14LevelCSVBlock()
        {
            BlockID = -1;
            Position = Vector3.zero;
            Rotation = Vector3.zero;
            Scale = Vector3.one;
            Properties = new List<float> { 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            IsValid = false;
        }

        public v14LevelCSVBlock(string csvData)
        {
            Read(csvData);
        }

        public void Read(string csvData)
        {
            string[] values = csvData.Split(',');

            if (values.Length != 38)
            {
                IsValid = false;
                return;
            }

            try
            {
                BlockID = ToolkitUtils.ParseInt(values[0]);
                Position = new Vector3(ToolkitUtils.ParseFloat(values[1]), ToolkitUtils.ParseFloat(values[2]), ToolkitUtils.ParseFloat(values[3]));
                Rotation = new Vector3(ToolkitUtils.ParseFloat(values[4]), ToolkitUtils.ParseFloat(values[5]), ToolkitUtils.ParseFloat(values[6]));
                Scale = new Vector3(ToolkitUtils.ParseFloat(values[7]), ToolkitUtils.ParseFloat(values[8]), ToolkitUtils.ParseFloat(values[9]));

                Properties = new List<float>();
                for (int i = 1; i < values.Length; i++)
                {
                    Properties.Add(ToolkitUtils.ParseFloat(values[i]));
                }

                IsValid = true;
            }
            catch
            {
                IsValid = false;
            }
        }

        public string ToCSV()
        {
            StringBuilder csvBuilder = new StringBuilder();

            // First part: BlockID, Position, Rotation, Scale
            csvBuilder.Append($"{BlockID},{Position.x.ToString(CultureInfo.InvariantCulture)},{Position.y.ToString(CultureInfo.InvariantCulture)},{Position.z.ToString(CultureInfo.InvariantCulture)},");
            csvBuilder.Append($"{Rotation.x.ToString(CultureInfo.InvariantCulture)},{Rotation.y.ToString(CultureInfo.InvariantCulture)},{Rotation.z.ToString(CultureInfo.InvariantCulture)},");
            csvBuilder.Append($"{Scale.x.ToString(CultureInfo.InvariantCulture)},{Scale.y.ToString(CultureInfo.InvariantCulture)},{Scale.z.ToString(CultureInfo.InvariantCulture)},");

            // Properties part
            for (int i = 9; i < Properties.Count; i++)
            {
                csvBuilder.Append(Properties[i].ToString(CultureInfo.InvariantCulture));
                if (i < Properties.Count - 1)
                {
                    csvBuilder.Append(",");
                }
            }

            return csvBuilder.ToString();
        }
    }
}
