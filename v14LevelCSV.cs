using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeeplevelToolkit
{
    public class v14LevelCSV
    {
        public v14LevelCSVHeader Header { get; private set; }
        public List<v14LevelCSVBlock> Blocks { get; private set; }
        public bool IsValid { get; private set; }

        public v14LevelCSV()
        {
            Header = new v14LevelCSVHeader();
            Blocks = new List<v14LevelCSVBlock>();
            IsValid = false;
        }

        public v14LevelCSV(string[] allLines)
        {
            IsValid = false;

            if (allLines == null || allLines.Length < 3)
                return;

            // First 3 lines: header
            string[] headerLines = allLines.Take(3).ToArray();
            Header = new v14LevelCSVHeader(headerLines);
            if (!Header.IsValid)
            {
                return;
            }

            // Remaining lines: blocks
            Blocks = new List<v14LevelCSVBlock>();
            for (int i = 3; i < allLines.Length; i++)
            {
                var block = new v14LevelCSVBlock(allLines[i]);
                if (block.IsValid)
                {
                    Blocks.Add(block);
                }
            }

            IsValid = true;
        }

        public string[] ToCSV()
        {
            List<string> csvLines = new List<string>();

            // Add the header CSV lines
            csvLines.AddRange(Header.ToCSV());

            // Add each block's CSV representation
            foreach (var block in Blocks)
            {
                var blockCsv = block.ToCSV();
                if (!string.IsNullOrWhiteSpace(blockCsv))
                {
                    csvLines.Add(blockCsv);
                }
            }

            // Remove any empty lines
            csvLines = csvLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

            return csvLines.ToArray();
        }
    }
}
