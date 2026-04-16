using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace ZeeplevelToolkit
{
    public class ZeeplevelImager : MonoBehaviour
    {
        private bool init = false;
        private Transform cameraTransform;
        private Camera captureCamera;
        private RenderTexture renderTexture;
        private Transform subjectPivot;
        private Transform subjectHolder;
        private List<GameObject> objects = new List<GameObject>();
        public static ZeeplevelImager Instance;

        public void Initialize()
        {
            if (init) { return; }

            Instance = this;

            cameraTransform = new GameObject("Camera Container").transform;
            cameraTransform.parent = transform;

            captureCamera = cameraTransform.gameObject.AddComponent<Camera>();
            captureCamera.orthographic = true;

            renderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            captureCamera.targetTexture = renderTexture;

            Transform background = GameObject.CreatePrimitive(PrimitiveType.Plane).transform;
            background.parent = cameraTransform;
            background.localPosition = new Vector3(0, 0, 128);
            background.Rotate(-90, 0, 0);

            Material backgroundMaterial = new Material(Shader.Find("Unlit/Color"));
            backgroundMaterial.color = Color.black;
            background.gameObject.GetComponent<Renderer>().material = backgroundMaterial;
            background.localScale = new Vector3(75f, 75f, 75f);

            subjectPivot = new GameObject("Subject Pivot").transform;
            subjectPivot.parent = transform;
            subjectPivot.localPosition = Vector3.zero;

            subjectHolder = new GameObject("Subject Holder").transform;
            subjectHolder.parent = subjectPivot;
            subjectHolder.localPosition = Vector3.zero;

            transform.position = new Vector3(0, 20000, 0);

            cameraTransform.localPosition = new Vector3(20, 20, -20);
            cameraTransform.LookAt(subjectPivot);

            Disable();

            init = true;
        }

        public void CaptureSubject(int imageSize, int angleCount, ZeeplevelData zeeplevelData, UnityAction<List<Texture2D>> callback)
        {
            Enable();

            renderTexture.Release();
            renderTexture.width = imageSize;
            renderTexture.height = imageSize;
            renderTexture.Create();

            StartCoroutine(CaptureRoutine(zeeplevelData, angleCount, callback));
        }

        private IEnumerator CaptureRoutine(ZeeplevelData zeeplevelData, int angleCount, UnityAction<List<Texture2D>> callback)
        {
            Reset();

            LoadSubject(zeeplevelData);

            yield return new WaitForEndOfFrame();

            List<Texture2D> captures = Capture(angleCount);

            RemoveSubject();

            callback?.Invoke(captures);

            Disable();
        }

        private void Reset()
        {
            if (objects.Count > 0)
            {
                foreach (GameObject o in objects)
                {
                    if (o != null)
                    {
                        GameObject.Destroy(o);
                    }
                }
            }
            objects.Clear();

            subjectHolder.localPosition = Vector3.zero;
            subjectPivot.localRotation = Quaternion.identity;
        }

        private void Enable()
        {
            transform.gameObject.SetActive(true);
        }

        private void Disable()
        {
            transform.gameObject.SetActive(false);
        }

        private void LoadSubject(ZeeplevelData data)
        {
            objects.Clear();

            List<BlockProperties> spawnedBlocks = new List<BlockProperties>();

            ZeeplevelData.DataType dataType = data.GetDataType();
            switch (dataType)
            {
                default:
                case ZeeplevelData.DataType.None:
                    return;
                case ZeeplevelData.DataType.CSV:
                    spawnedBlocks = ZeeplevelHandler.LoadBlocks(data.csv, true);
                    break;
                case ZeeplevelData.DataType.JSON:
                    spawnedBlocks = ZeeplevelHandler.LoadBlocks(data.json, true);
                    break;
            }

            if(spawnedBlocks.Count == 0)
            {
                return;
            }

            foreach (BlockProperties bp in spawnedBlocks)
            {
                GameObject go = bp.gameObject;
                go.transform.SetParent(subjectHolder, false);
                objects.Add(go);
            }

            if (objects.Count == 0)
            {
                return;
            }

            ToolkitUtils.CleanGameObject(subjectHolder.gameObject, true);
            Bounds bounds = ToolkitUtils.CalculateBounds(objects);

            float scaleFactor = 64f / bounds.size.magnitude;
            subjectHolder.transform.localScale = Vector3.one * scaleFactor;

            Vector3 move = (subjectPivot.position - bounds.center) * scaleFactor;
            subjectHolder.localPosition += move;

            captureCamera.orthographicSize = bounds.size.magnitude * 0.5f * scaleFactor;
        }


        public List<Texture2D> Capture(int angleCount)
        {
            List<Texture2D> captures = new List<Texture2D>();

            float step = 360f / angleCount;

            subjectPivot.transform.localEulerAngles = new Vector3(0, -90f, 0);

            RenderTexture.active = renderTexture;

            for (int i = 0; i < angleCount; i++)
            {
                captureCamera.Render();

                Texture2D capture = new Texture2D(renderTexture.width, renderTexture.height);
                capture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                capture.Apply();

                captures.Add(capture);

                subjectPivot.Rotate(0, step, 0);
            }

            RenderTexture.active = null;
            return captures;
        }

        public void RemoveSubject()
        {
            GameObject.Destroy(subjectHolder.gameObject);

            subjectHolder = new GameObject("Subject Holder").transform;
            subjectHolder.parent = subjectPivot;
            subjectHolder.localPosition = Vector3.zero;
        }
    }
}
