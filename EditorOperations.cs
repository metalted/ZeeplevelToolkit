using System.Collections.Generic;
using UnityEngine;

namespace Toolkist
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

    public class BlockDescription
    {
        public int blockID;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public BlockDescription(int blockID, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.blockID = blockID;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
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
            float multiplier = percentage;

            if (mask == Vector3Int.one)
            {
                foreach (BlockProperties block in blockList)
                {
                    if (!inPlace)
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

            if (mask == Vector3Int.one)
            {
                foreach (BlockProperties block in blockList)
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
                foreach (BlockProperties block in blockList)
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

                    if (!inPlace)
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
            if (central == null)
            {
                return;
            }

            if (central.selection.list.Count == 0)
            {
                return;
            }

            Mirror(central, central.selection.list, axis);
        }
        public static void Mirror(LEV_LevelEditorCentral central, List<BlockProperties> blockList, Axis axis)
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

        // --- Creation ---
        public static List<BlockProperties> CreateFromDescriptions(LEV_LevelEditorCentral central, List<BlockDescription> description)
        {
            if (central == null || description == null || description.Count == 0)
            {
                return null;
            }

            central.selection.DeselectAllBlocks(true, "EditorOperations");

            List<BlockProperties> blockList = new List<BlockProperties>();
            foreach (BlockDescription d in description)
            {
                if (d.blockID < 0 || d.blockID >= PlayerManager.Instance.loader.globalBlockList.blocks.Count)
                {
                    continue;
                }

                BlockProperties bp = GameObject.Instantiate<BlockProperties>(central.inspector.globalBlockList.blocks[d.blockID]);
                bp.isBeingCreated = true;
                bp.gameObject.name = central.inspector.globalBlockList.blocks[d.blockID].gameObject.name;
                bp.UID = central.manager.GenerateUniqueIDforBlocks(d.blockID.ToString());
                bp.isEditor = true;
                bp.NewBlockCreatedFromEditorForV15JSON();
                bp.CreateBlock();

                StaticConnectorTracker.AddBlockToTracker(bp, "CreateFromDescription");
                for (int i = 0; i < bp.propertyScripts.Count; i++)
                {
                    bp.propertyScripts[i].CreateBlock(bp);
                }

                bp.DrawDebugUID();

                bp.transform.position = d.position;
                bp.transform.rotation = d.rotation;
                bp.transform.localScale = d.scale;
                bp.SomethingChanged();
                bp.isBeingCreated = false;

                blockList.Add(bp);
            }

            UndoRedoRegistration registration = new UndoRedoRegistration(central);
            registration.SetBefore(blockList.Count);
            registration.blockList.AddRange(blockList);

            registration.GenerateAfter();

            Change_Collection collection = registration.CreateCollection();
            central.validation.BreakLock(collection, "Toolkit");
            central.selection.UndoRedoReselection(registration.blockList);

            return blockList;
        }

        public static GameObject CreateGhostBlock(LEV_LevelEditorCentral central, BlockPropertyJSON block, Material ghostMaterial = null)
        {
            if (block.i < 0 || block.i >= PlayerManager.Instance.loader.globalBlockList.blocks.Count)
            {
                return null;
            }

            BlockProperties bp = GameObject.Instantiate<BlockProperties>(central.inspector.globalBlockList.blocks[block.i]);
            bp.isBeingCreated = true;
            bp.gameObject.name = central.inspector.globalBlockList.blocks[block.i].gameObject.name;
            bp.isEditor = true;
            bp.CreateBlock();
            bp.properties.Clear();
            bp.LoadProperties_v15(block, false);

            GameObject b = bp.gameObject;

            if(ghostMaterial != null)
            {
                Properties_RoadPainter component = bp.gameObject.GetComponent<Properties_RoadPainter>();
                foreach(MeshRenderer ren in component.renderers)
                {
                    Material[] sharedMaterials = ren.sharedMaterials;
                    for (int i = 0; i < sharedMaterials.Length; ++i)
                    {
                        sharedMaterials[i] = ghostMaterial;
                    }
                    ren.sharedMaterials = sharedMaterials;
                }
            }

            ToolkitUtils.CleanGameObject(b);
            return b;
        }

        public static GameObject CreateGhostBlock(LEV_LevelEditorCentral central, int blockID, Material ghostMaterial = null)
        {
            if (blockID < 0 || blockID >= PlayerManager.Instance.loader.globalBlockList.blocks.Count)
            {
                return null;
            }

            BlockProperties bp = GameObject.Instantiate<BlockProperties>(central.inspector.globalBlockList.blocks[blockID]);
            bp.isBeingCreated = true;
            bp.gameObject.name = central.inspector.globalBlockList.blocks[blockID].gameObject.name;
            bp.isEditor = true;
            bp.NewBlockCreatedFromEditorForV15JSON();
            bp.CreateBlock();

            GameObject b = bp.gameObject;

            if (ghostMaterial != null)
            {
                Properties_RoadPainter component = bp.gameObject.GetComponent<Properties_RoadPainter>();
                foreach (MeshRenderer ren in component.renderers)
                {
                    Material[] sharedMaterials = ren.sharedMaterials;
                    for (int i = 0; i < sharedMaterials.Length; ++i)
                    {
                        sharedMaterials[i] = ghostMaterial;
                    }
                    ren.sharedMaterials = sharedMaterials;
                }
            }

            ToolkitUtils.CleanGameObject(b);
            return b;
        }

        //Actions
        public static void DeselectAllBlocks(LEV_LevelEditorCentral central)
        {
            central.selection.DeselectAllBlocks(true, nameof(central.selection.ClickNothing));
        }

        public static bool AnyObjectsSelected(LEV_LevelEditorCentral central)
        {
            return central.selection.list.Count > 0;
        }

        public static List<BlockProperties> GetSelectedBlocks(LEV_LevelEditorCentral central)
        {
            return central.selection.list;
        }

        public static bool InBlockMovementMode(LEV_LevelEditorCentral central)
        {
            if (central.tool.currentTool != 0)
            {
                return false;
            }

            if (central.gizmos.dragButton.isSelected)
            {
                return true;
            }

            return false;
        }

        public static bool InBlockRotationMode(LEV_LevelEditorCentral central)
        {
            if (central.tool.currentTool != 0)
            {
                return false;
            }

            if (central.gizmos.rotateButton.isSelected)
            {
                return true;
            }

            return false;
        }

        public static bool InEditMode(LEV_LevelEditorCentral central)
        {
            return central.tool.currentTool == 0;
        }

        public static bool InPaintMode(LEV_LevelEditorCentral central)
        {
            return central.tool.currentTool == 1;
        }
        public static bool InTreegunMode(LEV_LevelEditorCentral central)
        {
            return central.tool.currentTool == 2;
        }

        public static bool InUIPanelMode(LEV_LevelEditorCentral central)
        {
            return central.tool.currentTool == 3;
        }

        public static bool InSkyboxMode(LEV_LevelEditorCentral central)
        {
            return central.tool.currentTool == 4;
        }

        public static bool InConnectionMode(LEV_LevelEditorCentral central)
        {
            return central.tool.currentTool == 5;
        }

        public static bool InEditPipetteMode(LEV_LevelEditorCentral central)
        {
            return central.tool.currentTool == 6;
        }

        public static bool IsDragging(LEV_LevelEditorCentral central)
        {
            return central.gizmos.isDragging;
        }

        public static bool IsInGMode(LEV_LevelEditorCentral central)
        {
            return central.gizmos.isGrabbing;
        }

        public static bool IsInputBlocked(LEV_LevelEditorCentral central)
        {
            return central.input.inputLocked;
        }

        //Compound blueprint placement
        public static void PlaceBlueprint(
    LEV_LevelEditorCentral central,
    List<BlockProperties> blockList,
    Vector3 surfacePoint,
    Vector3 upVector,
    float scale,
    float liftFactor,
    float axisRotationAngle)
        {
            if (central == null)
                return;

            if (blockList == null || blockList.Count == 0)
                return;

            if (upVector == Vector3.zero)
                return;

            upVector.Normalize();

            UndoRedoRegistration registration = new UndoRedoRegistration(central);
            registration.SetBefore(blockList);

            // ------------------------------------------------------------
            // 1. Scale first.
            // This changes the bounds, so it must happen before placement.
            // ------------------------------------------------------------

            if (!Mathf.Approximately(scale, 1f))
            {
                ScaleBlocksUniformNoRegistration(blockList, scale);
            }

            // ------------------------------------------------------------
            // 2. Move blueprint center to the target point.
            // ------------------------------------------------------------

            Bounds bounds = ToolkitUtils.CalculateBounds(blockList);
            Vector3 moveToPoint = surfacePoint - bounds.center;

            MoveBlocksNoRegistration(blockList, moveToPoint);

            // ------------------------------------------------------------
            // 3. Rotate blueprint world-up to the requested up vector.
            // Pivot is the placement point.
            // ------------------------------------------------------------

            Quaternion upRotation = GetRotationFromUpToNormal(upVector);

            if (upRotation != Quaternion.identity)
            {
                RotateBlocksAroundPivotNoRegistration(
                    blockList,
                    surfacePoint,
                    upRotation
                );
            }

            // ------------------------------------------------------------
            // 4. Rotate around the final up vector.
            // This is your random/tree rotation.
            // ------------------------------------------------------------

            if (!Mathf.Approximately(axisRotationAngle, 0f))
            {
                Quaternion axisRotation = Quaternion.AngleAxis(axisRotationAngle, upVector);

                RotateBlocksAroundPivotNoRegistration(
                    blockList,
                    surfacePoint,
                    axisRotation
                );
            }

            // ------------------------------------------------------------
            // 5. Move bottom toward/onto surface.
            // liftFactor:
            // 1.0 = bottom exactly on surface
            // 0.5 = halfway toward surface
            // 0.45 = slightly lower than halfway
            // ------------------------------------------------------------

            MoveBlueprintBottomToSurfaceNoRegistration(
                blockList,
                surfacePoint,
                upVector,
                liftFactor
            );

            // ------------------------------------------------------------
            // 6. Mark changed once and register as one editor operation.
            // ------------------------------------------------------------

            foreach (BlockProperties block in blockList)
            {
                block.SomethingChanged();
            }

            Bounds finalBounds = ToolkitUtils.CalculateBounds(blockList);

            if (central.gizmos != null && central.gizmos.motherGizmo != null)
            {
                central.gizmos.motherGizmo.position = finalBounds.center;
            }

            registration.GenerateAfter();

            Change_Collection collection = registration.CreateCollection();
            central.validation.BreakLock(collection, "Gizmo1");
        }

        private static void ScaleBlocksUniformNoRegistration(
    List<BlockProperties> blockList,
    float scale)
        {
            Vector3 center = ToolkitUtils.GetCenterPosition(blockList);

            foreach (BlockProperties block in blockList)
            {
                Vector3 pos = block.transform.position;

                pos -= center;
                pos *= scale;
                pos += center;

                block.transform.position = pos;
                block.transform.localScale *= scale;
            }
        }

        private static void MoveBlocksNoRegistration(
            List<BlockProperties> blockList,
            Vector3 move)
        {
            if (move == Vector3.zero)
                return;

            foreach (BlockProperties block in blockList)
            {
                block.transform.position += move;
            }
        }

        private static void RotateBlocksAroundPivotNoRegistration(
            List<BlockProperties> blockList,
            Vector3 pivot,
            Quaternion rotation)
        {
            foreach (BlockProperties block in blockList)
            {
                Vector3 directionFromPivot = block.transform.position - pivot;

                block.transform.position = pivot + rotation * directionFromPivot;
                block.transform.rotation = rotation * block.transform.rotation;
            }
        }

        private static Quaternion GetRotationFromUpToNormal(Vector3 upVector)
        {
            Vector3 from = Vector3.up;
            Vector3 to = upVector.normalized;

            float dot = Vector3.Dot(from, to);

            if (dot > 0.9999f)
                return Quaternion.identity;

            if (dot < -0.9999f)
                return Quaternion.AngleAxis(180f, Vector3.right);

            return Quaternion.FromToRotation(from, to);
        }

        private static void MoveBlueprintBottomToSurfaceNoRegistration(
            List<BlockProperties> blockList,
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            float liftFactor)
        {
            Bounds bounds = ToolkitUtils.CalculateBounds(blockList);

            surfaceNormal.Normalize();

            float surfaceDistance = Vector3.Dot(surfacePoint, surfaceNormal);
            float lowestDistance = GetLowestBoundsDistanceAlongAxis(bounds, surfaceNormal);

            float moveDistance = surfaceDistance - lowestDistance;

            moveDistance *= liftFactor;

            Vector3 move = surfaceNormal * moveDistance;

            MoveBlocksNoRegistration(blockList, move);
        }

        private static float GetLowestBoundsDistanceAlongAxis(Bounds bounds, Vector3 axis)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            float centerDistance = Vector3.Dot(center, axis);

            float projectedExtent =
                Mathf.Abs(axis.x) * extents.x +
                Mathf.Abs(axis.y) * extents.y +
                Mathf.Abs(axis.z) * extents.z;

            return centerDistance - projectedExtent;
        }
    }
}
