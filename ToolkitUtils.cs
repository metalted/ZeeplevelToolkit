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

        public static Vector3 GetCenterPosition(List<BlockProperties> list)
        {
            Vector3 total = Vector3.zero;

            foreach (BlockProperties bp in list)
            {
                total += bp.gameObject.transform.position;
            }

            return new Vector3(total.x / list.Count, total.y / list.Count, total.z / list.Count);
        }

        public static Vector3Int ToBinaryMask(Vector3 v)
        {
            return new Vector3Int(
                Mathf.Approximately(v.x, 0f) ? 0 : 1,
                Mathf.Approximately(v.y, 0f) ? 0 : 1,
                Mathf.Approximately(v.z, 0f) ? 0 : 1
            );
        }

        public static Vector3Int ToBinaryMask(Axis axis)
        {
            switch(axis)
            {
                case Axis.X: return Vector3Int.right;
                case Axis.Y: return Vector3Int.up;
                case Axis.Z: return Vector3Int.forward;
                case Axis.XY: return new Vector3Int(1, 1, 0);
                case Axis.YZ: return new Vector3Int(0, 1, 1);
                case Axis.XZ: return new Vector3Int(1, 0, 1);
                default:
                case Axis.XYZ: return Vector3Int.one;
            }
        }

        public static Vector3[] ConvertLocalToWorldVectors(Transform local)
        {
            Vector3[] xDirections = GetSortedDirections(local.right);
            Vector3[] yDirections = GetSortedDirections(local.up);
            Vector3[] zDirections = GetSortedDirections(local.forward);

            List<string> taken = new List<string>();

            Vector3 chosenXDirection = xDirections[0];
            Vector3 chosenYDirection = Vector3.zero;
            Vector3 chosenZDirection = Vector3.zero;

            taken.Add(GetAxisFromDirection(chosenXDirection));

            foreach (Vector3 vy in yDirections)
            {
                string axis = GetAxisFromDirection(vy);

                if (!taken.Contains(axis))
                {
                    chosenYDirection = vy;
                    taken.Add(axis);
                    break;
                }
            }

            foreach (Vector3 vz in zDirections)
            {
                string axis = GetAxisFromDirection(vz);

                if (!taken.Contains(axis))
                {
                    chosenZDirection = vz;
                    taken.Add(axis);
                    break;
                }
            }

            return new Vector3[] { GetAbsoluteVector(chosenXDirection), GetAbsoluteVector(chosenYDirection), GetAbsoluteVector(chosenZDirection) };
        }

        public static Vector3 GetAbsoluteVector(Vector3 inputVector)
        {
            return new Vector3(Mathf.Abs(inputVector.x), Mathf.Abs(inputVector.y), Mathf.Abs(inputVector.z));
        }

        public static string GetAxisFromDirection(Vector3 direction)
        {
            // Get the absolute values of the direction components
            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);
            float absZ = Mathf.Abs(direction.z);

            // Check which axis has the largest absolute value
            if (absX > absY && absX > absZ)
            {
                return "X";
            }
            else if (absY > absX && absY > absZ)
            {
                return "Y";
            }
            else
            {
                return "Z";
            }
        }

        public static Vector3[] GetSortedDirections(Vector3 inputVector)
        {
            // Convert input vector to world space
            Vector3 inputVectorWorld = inputVector.normalized;

            // Define the six directions
            Vector3[] directions = {
                Vector3.up,
                Vector3.down,
                Vector3.left,
                Vector3.right,
                Vector3.forward,
                Vector3.back
            };

            // Calculate the angles between input vector and the six directions
            float[] angles = new float[directions.Length];
            for (int i = 0; i < directions.Length; i++)
            {
                angles[i] = Mathf.RoundToInt(Vector3.Angle(inputVectorWorld, directions[i]));
            }

            // Sort the directions based on the angles
            System.Array.Sort(angles, directions);

            return directions;
        }
    }
}
