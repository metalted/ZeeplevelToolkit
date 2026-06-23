using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Toolkist
{
    public static class ToolkitUtils
    {
        public static string GetPlayerName(string defaultName = "Bouwerman")
        {
            string playerName = defaultName;
            try
            {
                playerName = PlayerManager.Instance.steamAchiever.GetPlayerNameNoTag(true);
                return playerName;
            }
            catch
            {
                return playerName;
            }
        }

        public static Vector3 EditorCameraPosition(LEV_LevelEditorCentral central)
        {
            return central.cam.transform.position;
        }

        public static Vector3 BlocksAtCameraGridMovement(LEV_LevelEditorCentral central, List<BlockProperties> blocks)
        {
            Bounds bounds = CalculateBounds(blocks);
            Vector3 cameraGridPosition = ClosestGridPosition(EditorCameraPosition(central));
            Vector3 blocksGridPosition = ClosestGridPosition(bounds.center);
            Vector3 move = cameraGridPosition - blocksGridPosition;
            return move;
        }

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

        /*public static void ReUID(List<BlockPropertyJSON> jsonBlocks)
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
        }*/

        public static void ReUID(List<BlockPropertyJSON> jsonBlocks)
        {
            Dictionary<string, string> uidRemapping = new Dictionary<string, string>();

            // 1) Re-UID all blocks and build remap table
            foreach (BlockPropertyJSON j in jsonBlocks)
            {
                string oldUID = j.u;
                string newUID = PlayerManager.Instance.GenerateUniqueIDforBlocks(j.i.ToString());

                j.u = newUID;

                if (!string.IsNullOrEmpty(oldUID))
                {
                    if (!uidRemapping.ContainsKey(oldUID))
                    {
                        uidRemapping.Add(oldUID, newUID);
                    }
                    else
                    {
                        Debug.LogWarning("ReUID: duplicate old UID found: " + oldUID);
                    }
                }
                else
                {
                    Debug.LogWarning("ReUID: block had empty old UID. New UID: " + newUID);
                }
            }

            // 2) Remap connections and repair connection counters
            foreach (BlockPropertyJSON j in jsonBlocks)
            {
                PropertyDictionariesJSON props = j.d;

                if (props == null || props.t == null)
                    continue;

                Dictionary<string, string> tNew = new Dictionary<string, string>();

                // pin id -> list of surviving serialized connections
                Dictionary<string, List<KeyValuePair<int, string>>> connectionsByPin =
                    new Dictionary<string, List<KeyValuePair<int, string>>>();

                // Pins that had connection-style keys before cleanup.
                HashSet<string> touchedPins = new HashSet<string>();

                foreach (KeyValuePair<string, string> kvp in props.t)
                {
                    string pinKey;
                    int connectionIndex;

                    // Not a connection entry, for example "oi2": "0"
                    if (!TryParseConnectionKey(kvp.Key, out pinKey, out connectionIndex))
                    {
                        tNew[kvp.Key] = kvp.Value;
                        continue;
                    }

                    touchedPins.Add(pinKey);

                    if (string.IsNullOrEmpty(kvp.Value))
                    {
                        Debug.LogWarning("ReUID: removed empty connection value. Key: " + kvp.Key);
                        continue;
                    }

                    try
                    {
                        ConnectionStruct connection = new ConnectionStruct(kvp.Value);

                        if (string.IsNullOrEmpty(connection.targetUID))
                        {
                            Debug.LogWarning("ReUID: removed connection with empty target UID. Key: " + kvp.Key);
                            continue;
                        }

                        string remappedUID;

                        if (!uidRemapping.TryGetValue(connection.targetUID, out remappedUID))
                        {
                            Debug.LogWarning(
                                "ReUID: removed orphaned connection. " +
                                "Key: " + kvp.Key +
                                " | Missing target UID: " + connection.targetUID
                            );

                            continue;
                        }

                        connection.targetUID = remappedUID;

                        if (!connectionsByPin.ContainsKey(pinKey))
                        {
                            connectionsByPin.Add(pinKey, new List<KeyValuePair<int, string>>());
                        }

                        connectionsByPin[pinKey].Add(
                            new KeyValuePair<int, string>(connectionIndex, connection.Serialize())
                        );
                    }
                    catch
                    {
                        Debug.LogWarning("ReUID: removed invalid connection entry. Key: " + kvp.Key);
                    }
                }

                // 3) Rebuild connection keys compactly: id3-0, id3-1, id3-2, ...
                foreach (KeyValuePair<string, List<KeyValuePair<int, string>>> group in connectionsByPin)
                {
                    string pinKey = group.Key;
                    List<KeyValuePair<int, string>> connections = group.Value;

                    connections.Sort((a, b) => a.Key.CompareTo(b.Key));

                    for (int i = 0; i < connections.Count; i++)
                    {
                        string newConnectionKey = pinKey + "-" + i;
                        tNew[newConnectionKey] = connections[i].Value;
                    }
                }

                // 4) Repair d.n counters for every pin we touched
                if (props.n != null)
                {
                    foreach (string pinKey in touchedPins)
                    {
                        int count = 0;

                        if (connectionsByPin.ContainsKey(pinKey))
                        {
                            count = connectionsByPin[pinKey].Count;
                        }

                        props.n[pinKey] = count;
                    }
                }

                props.t = tNew;
            }
        }

        private static bool TryParseConnectionKey(string key, out string pinKey, out int connectionIndex)
        {
            pinKey = null;
            connectionIndex = -1;

            if (string.IsNullOrEmpty(key))
                return false;

            int dashIndex = key.LastIndexOf('-');

            if (dashIndex <= 0 || dashIndex >= key.Length - 1)
                return false;

            string left = key.Substring(0, dashIndex);
            string right = key.Substring(dashIndex + 1);

            int parsedIndex;

            if (!int.TryParse(right, out parsedIndex))
                return false;

            pinKey = left;
            connectionIndex = parsedIndex;
            return true;
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

        public static void CleanGameObject(GameObject obj, bool keepCollisions = false)
        {
            Component[] objectComponents = obj.GetComponents(typeof(Component));
            Component[] childComponents = obj.GetComponentsInChildren(typeof(Component));
            
            objectComponents = objectComponents.Concat(childComponents).ToArray();

            foreach (Component c in objectComponents)
            {
                if (c == null) { continue; }

                if (c.GetType() == typeof(Rigidbody))
                {
                    ((Rigidbody)c).isKinematic = true;
                }

                bool keep = (c.GetType() == typeof(Transform)) || (c.GetType() == typeof(MeshFilter)) || (c.GetType() == typeof(MeshRenderer)) || (c.GetType() == typeof(RectTransform)) || (c.GetType() == typeof(Rigidbody)) || (c.GetType() == typeof(Light)) || (c.GetType() == typeof(HxVolumetricLight)) || (c.GetType() == typeof(TextMeshPro));

                if(c is Collider collider)
                {
                    if(keepCollisions)
                    {
                        continue;
                    }

                    collider.enabled = false;
                    continue;
                }

                if (!keep)
                {
                    GameObject.DestroyImmediate(c);
                }
            }
        }
    }
}
