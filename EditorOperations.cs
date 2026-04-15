using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static Steamworks.InventoryItem;

namespace ZeeplevelToolkit
{
    public enum ScalingStyle
    {
        Percentage,
        PercentageInPlace,
        Unit,
        UnitInPlace
    }

    public enum Axis
    {
        X,
        Y,
        Z,
        XY,
        YZ,
        XZ,
        XYZ
    }

    public class EditorOperations
    {
        private static readonly Vector3[] directions = { Vector3.right, Vector3.up, Vector3.forward };

        // --- Movement ---
        public static void MoveSelection(LEV_LevelEditorCentral central, Vector3 move)
        {
            if (central == null)
            {
                return;
            }

            if (central.selection.list.Count == 0)
            {
                return;
            }

            if (move == Vector3.zero)
            {
                return;
            }

            Move(central, central.selection.list, move);
        }
        public static void Move(LEV_LevelEditorCentral central, List<BlockProperties> blockList, Vector3 move)
        {
            if (central == null)
            {
                return;
            }

            if (blockList.Count == 0)
            {
                return;
            }

            if (move == Vector3.zero)
            {
                return;
            }

            UndoRedoRegistration registration = new UndoRedoRegistration(central);
            registration.SetBefore(blockList);

            foreach (BlockProperties block in blockList)
            {
                block.transform.position += move;
                block.SomethingChanged();
            }

            //Move the mother gizmo
            central.gizmos.motherGizmo.position += move;

            registration.SetBefore(central.selection.list);
            registration.GenerateAfter();
            Change_Collection collection = registration.CreateCollection();
            central.validation.BreakLock(collection, "Gizmo1");
        }

        // --- Rotation ---
        public static void RotateSelection(LEV_LevelEditorCentral central, Vector3 up, float angle)
        {
            if (central == null)
            {
                return;
            }

            if (central.selection.list.Count == 0)
            {
                return;
            }

            if (up == Vector3.zero)
            {
                return;
            }

            Rotate(central, central.selection.list, up, angle);
        }
        public static void Rotate(LEV_LevelEditorCentral central, List<BlockProperties> blockList, Vector3 up, float angle)
        {
            if (central == null)
            {
                return;
            }

            if (blockList.Count == 0)
            {
                return;
            }

            if (up == Vector3.zero)
            {
                return;
            }

            UndoRedoRegistration registration = new UndoRedoRegistration(central);
            registration.SetBefore(blockList);

            //Rotate the selection
            central.gizmos.DoRotate(up, angle);

            foreach (BlockProperties block in blockList)
            {
                block.SomethingChanged();
            }

            registration.GenerateAfter();
            Change_Collection collection = registration.CreateCollection();
            central.validation.BreakLock(collection, "Gizmo1");
        }

        // --- Scaling ---
        public static void ScaleSelection(LEV_LevelEditorCentral central, Axis axis, float amount, ScalingStyle scalingStyle)
        {
            if (central == null)
            {
                return;
            }

            if (central.selection.list.Count == 0)
            {
                return;
            }

            switch (scalingStyle)
            {
                case ScalingStyle.Percentage:
                    ScaleByPercentage(central, central.selection.list, axis, amount, false);
                    break;
                case ScalingStyle.PercentageInPlace:
                    ScaleByPercentage(central, central.selection.list, axis, amount, true);
                    break;
                case ScalingStyle.Unit:
                    ScaleByUnit(central, central.selection.list, axis, amount, false);
                    break;
                case ScalingStyle.UnitInPlace:
                    ScaleByUnit(central, central.selection.list, axis, amount, true);
                    break;
            }
        }
        public static void ScaleByPercentage(LEV_LevelEditorCentral central, List<BlockProperties> blockList, Axis axis, float percentage, bool inPlace)
        {
            if (central == null)
            {
                return;
            }

            if (blockList.Count == 0)
            {
                return;
            }

            Vector3Int mask = ToolkitUtils.ToBinaryMask(axis);

            UndoRedoRegistration registration = new UndoRedoRegistration(central);
            registration.SetBefore(blockList);

            Vector3 center = ToolkitUtils.GetCenterPosition(blockList);
            float multiplier = 1f + (percentage / 100f);

            if (mask == Vector3Int.one)
            {
                foreach (BlockProperties block in blockList)
                {
                    if(!inPlace)
                    {
                        Vector3 pos = block.transform.position;
                        pos -= center;
                        pos *= multiplier;
                        pos += center;
                        block.transform.position = pos;
                    }
                   
                    block.transform.localScale *= multiplier;
                    block.SomethingChanged();

                }
            }
            else
            {
                foreach (BlockProperties block in blockList)
                {
                    if (!inPlace)
                    {
                        Vector3 pos = block.transform.position;
                        pos -= center;
                        pos.x = mask.x > 0 ? pos.x * multiplier : pos.x;
                        pos.y = mask.y > 0 ? pos.y * multiplier : pos.y;
                        pos.z = mask.z > 0 ? pos.z * multiplier : pos.z;
                        pos += center;
                        block.transform.position = pos;
                    }

                    Vector3[] convertedVectors = ToolkitUtils.ConvertLocalToWorldVectors(block.transform);

                    for (int i = 0; i < 3; i++)
                    {
                        if (mask[i] <= 0)
                        {
                            continue;
                        }

                        Vector3 scaledAxis = Vector3.zero;

                        if (convertedVectors[0] == directions[i])
                        {
                            scaledAxis = Vector3.right;
                        }
                        else if (convertedVectors[1] == directions[i])
                        {
                            scaledAxis = Vector3.up;
                        }
                        else if (convertedVectors[2] == directions[i])
                        {
                            scaledAxis = Vector3.forward;
                        }

                        Vector3 scaleAddition = Vector3.Scale((block.transform.localScale * multiplier - block.transform.localScale), scaledAxis);
                        block.transform.localScale += scaleAddition;
                    }

                    block.SomethingChanged();
                }
            }

            registration.GenerateAfter();

            Change_Collection collection = registration.CreateCollection();
            central.validation.BreakLock(collection, "Gizmo1");
        }
        public static void ScaleByUnit(LEV_LevelEditorCentral central, List<BlockProperties> blockList, Axis axis, float units, bool inPlace)
        {
            if(central == null)
            {
                return;
            }

            if(blockList.Count == 0)
            {
                return;
            }

            Vector3Int mask = ToolkitUtils.ToBinaryMask(axis);

            UndoRedoRegistration registration = new UndoRedoRegistration(central);
            registration.SetBefore(blockList);

            Vector3 center = ToolkitUtils.GetCenterPosition(blockList);

            if(mask == Vector3Int.one)
            {
                foreach(BlockProperties block in blockList)
                {
                    Vector3 pos = block.transform.position - center;
                    Vector3 currentScale = block.transform.localScale;

                    Vector3 newScale = new Vector3(
                        ApplyUnitStep(currentScale.x, units),
                        ApplyUnitStep(currentScale.y, units),
                        ApplyUnitStep(currentScale.z, units)
                    );

                    Vector3 ratios = new Vector3(
                        currentScale.x == 0f ? 1f : newScale.x / currentScale.x,
                        currentScale.y == 0f ? 1f : newScale.y / currentScale.y,
                        currentScale.z == 0f ? 1f : newScale.z / currentScale.z
                    );

                    pos = Vector3.Scale(pos, ratios);

                    if (!inPlace)
                    {
                        block.transform.position = pos + center;
                    }

                    block.transform.localScale = newScale;
                    block.SomethingChanged();
                }
            }
            else
            {
                foreach(BlockProperties block in blockList)
                {
                    Vector3 pos = block.transform.position - center;
                    Vector3 currentScale = block.transform.localScale;
                    Vector3 newScale = currentScale;

                    Vector3[] convertedVectors = ToolkitUtils.ConvertLocalToWorldVectors(block.transform);

                    for (int i = 0; i < 3; i++)
                    {
                        if (mask[i] <= 0f)
                            continue;

                        if (convertedVectors[0] == directions[i])
                        {
                            float old = currentScale.x;
                            float ns = ApplyUnitStep(old, units);
                            newScale.x = ns;

                            float ratio = old == 0f ? 1f : ns / old;
                            pos[i] *= ratio;
                        }
                        else if (convertedVectors[1] == directions[i])
                        {
                            float old = currentScale.y;
                            float ns = ApplyUnitStep(old, units);
                            newScale.y = ns;

                            float ratio = old == 0f ? 1f : ns / old;
                            pos[i] *= ratio;
                        }
                        else if (convertedVectors[2] == directions[i])
                        {
                            float old = currentScale.z;
                            float ns = ApplyUnitStep(old, units);
                            newScale.z = ns;

                            float ratio = old == 0f ? 1f : ns / old;
                            pos[i] *= ratio;
                        }
                    }

                    if(!inPlace)
                    {
                        block.transform.position = pos + center;
                    }
                    
                    block.transform.localScale = newScale;
                    block.SomethingChanged();
                }
            }

            registration.GenerateAfter();
            Change_Collection collection = registration.CreateCollection();
            central.validation.BreakLock(collection, "Gizmo1");
        }
        private static float ApplyUnitStep(float current, float amount)
        {
            float sign = current < 0f ? -1f : 1f;

            float absCurrent = Mathf.Abs(current);
            float absStep = Mathf.Abs(amount);

            // Growing
            if (amount > 0f)
            {
                float result = (absCurrent + absStep) * sign;
                return SnapToUnit(result, absStep);
            }

            // Shrinking
            float absNew = absCurrent - absStep;

            if (absNew < absStep)
                absNew = absStep;

            float final = absNew * sign;

            return SnapToUnit(final, absStep);
        }
        private static float SnapToUnit(float value, float unit)
        {
            float absUnit = Mathf.Abs(unit);

            if (absUnit == 0f)
                return value;

            float sign = value < 0f ? -1f : 1f;

            float absValue = Mathf.Abs(value);

            float snapped = Mathf.Round(absValue / absUnit) * absUnit;

            return snapped * sign;
        }

        // --- Mirroring ---
        public static void MirrorSelection(LEV_LevelEditorCentral central, Axis axis)
        {
            if(central == null)
            {
                return;
            }

            if(central.selection.list.Count == 0)
            {
                return;
            }

            Mirror(central, central.selection.list, axis);
        }
        public static void Mirror(LEV_LevelEditorCentral central, List<BlockProperties> blockList, Axis axis)
        {
            if(central == null)
            {
                return;
            }

            if(blockList.Count == 0)
            {
                return;
            }

            Vector3Int mask = ToolkitUtils.ToBinaryMask(axis);

            UndoRedoRegistration registration = new UndoRedoRegistration(central);
            registration.SetBefore(blockList);

            Vector3 center = ToolkitUtils.GetCenterPosition(blockList);

            Transform tempParent = new GameObject("Temp Mirror Parent").transform;
            tempParent.position = center;

            foreach (BlockProperties block in blockList)
            {
                // Set the temporary parent as the parent of each block
                block.transform.parent = tempParent;
            }

            // Apply mirroring based on the specified axis
            if (mask.x > 0)
            {
                tempParent.transform.localScale = new Vector3(-1, 1, 1);
            }
            else if (mask.y > 0)
            {
                tempParent.transform.localScale = new Vector3(1, -1, 1);
            }
            else if (mask.z > 0)
            {
                tempParent.transform.localScale = new Vector3(1, 1, -1);
            }

            foreach (BlockProperties block in blockList)
            {
                // Remove the temporary parent by setting each block's parent to null
                block.transform.parent = null;
                block.SomethingChanged();
            }

            // Destroy the temporary parent object
            GameObject.Destroy(tempParent.gameObject);

            registration.GenerateAfter();

            Change_Collection collection = registration.CreateCollection();
            central.validation.BreakLock(collection, "Gizmo1");
        }
    }
}
