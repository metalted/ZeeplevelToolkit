using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ZeeplevelToolkit
{
    public static class ToolkitUtils
    {
        public static int ParseInt(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : -1;
        }

        public static float ParseFloat(string value)
        {
            return float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float result) ? result : 0.0f;
        }

        public static Vector3 ClosestGridPosition(Vector3 position)
        {
            return new Vector3(Mathf.FloorToInt(position.x / 16f), Mathf.FloorToInt(position.y / 16f), Mathf.FloorToInt(position.z / 16f)) * 16f;
        }

        public static BlockPropertyJSON DeepCopy(BlockPropertyJSON block)
        {
            return LEV_UndoRedo.GetJSONblock(LEV_UndoRedo.GetJSONstring(block));
        }

        public static void ReUID(List<BlockPropertyJSON> jsonBlocks)
        {
            Dictionary<string, string> uidRemapping = new Dictionary<string, string>();

            // 1) Re-UID all blocks and build remap table
            foreach (BlockPropertyJSON j in jsonBlocks)
            {
                string fileUID = j.u;
                string newUID = PlayerManager.Instance.GenerateUniqueIDforBlocks(j.i.ToString());
                j.u = newUID;
                uidRemapping[fileUID] = newUID;
            }

            // 2) Remap any UID references inside logic dicts (props.t)
            foreach (BlockPropertyJSON j in jsonBlocks)
            {
                PropertyDictionariesJSON props = j.d;
                if (props?.t == null)
                    continue;

                Dictionary<string, string> tNew = new Dictionary<string, string>();

                foreach (KeyValuePair<string, string> kvp in props.t)
                {
                    // Non-connection values just copy through
                    if (string.IsNullOrEmpty(kvp.Value) || !kvp.Value.Contains("{"))
                    {
                        tNew[kvp.Key] = kvp.Value;
                        continue;
                    }

                    try
                    {
                        ConnectionStruct connection = new ConnectionStruct(kvp.Value);

                        // Only remap targets inside the imported blueprint
                        if (!uidRemapping.TryGetValue(connection.targetUID, out string remapped))
                        {
                            connection.targetUID = "";
                        }
                        else
                        {
                            connection.targetUID = remapped;
                        }

                        tNew[kvp.Key] = connection.Serialize();
                    }
                    catch
                    {
                        Debug.LogError("Error parsing connection.");
                        tNew[kvp.Key] = kvp.Value;
                    }
                }

                props.t = tNew;
            }
        }

        public static Bounds CalculateBounds(List<GameObject> objs)
        {
            Vector3 minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxBounds = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (GameObject o in objs)
            {
                MeshRenderer[] renderers = o.GetComponentsInChildren<MeshRenderer>();

                foreach (MeshRenderer r in renderers)
                {
                    if (r != null)
                    {
                        Bounds b = r.bounds;
                        minBounds = Vector3.Min(minBounds, b.min);
                        maxBounds = Vector3.Max(maxBounds, b.max);
                    }
                }
            }

            Vector3 center = (minBounds + maxBounds) * 0.5f;
            Vector3 size = maxBounds - minBounds;
            return new Bounds(center, size);
        }

        public static Bounds CalculateBounds(List<BlockProperties> bps)
        {
            Vector3 minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxBounds = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (BlockProperties bp in bps)
            {
                MeshRenderer[] renderers = bp.gameObject.GetComponentsInChildren<MeshRenderer>();

                foreach (MeshRenderer r in renderers)
                {
                    if (r != null)
                    {
                        Bounds b = r.bounds;
                        minBounds = Vector3.Min(minBounds, b.min);
                        maxBounds = Vector3.Max(maxBounds, b.max);
                    }
                }
            }

            Vector3 center = (minBounds + maxBounds) * 0.5f;
            Vector3 size = maxBounds - minBounds;
            return new Bounds(center, size);
        }
    }
}
