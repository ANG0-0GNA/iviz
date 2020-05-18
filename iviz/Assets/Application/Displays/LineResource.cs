﻿using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using Iviz.Resources;
using Iviz.App.Listeners;

namespace Iviz.Displays
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LineWithColor
    {
        public Vector3 A;
        public Vector3 B;
        public Color32 colorA;
        public Color32 colorB;

        public LineWithColor(Vector3 a, Vector3 b, Color32 color)
        {
            A = a;
            B = b;
            colorA = color;
            colorB = color;
        }

        public LineWithColor(Vector3 a, Vector3 b, Color32 colorA, Color32 colorB)
        {
            A = a;
            B = b;
            this.colorA = colorA;
            this.colorB = colorB;
        }
    };

    public class LineResource : MarkerResource
    {
        Material material;

        LineWithColor[] lineBuffer = Array.Empty<LineWithColor>();
        ComputeBuffer lineComputeBuffer;
        ComputeBuffer quadComputeBuffer;

        public Camera MainCamera { get; set; }
        public Color Color { get; set; } = Color.white;

        static readonly int PropLines = Shader.PropertyToID("_Lines");

        int size_;
        public int Size
        {
            get => size_;
            private set
            {
                if (value == size_)
                {
                    return;
                }
                size_ = value;
                int reqDataSize = (int)(size_ * 1.1f);
                if (lineBuffer == null || lineBuffer.Length < reqDataSize)
                {
                    lineBuffer = new LineWithColor[reqDataSize];

                    if (lineComputeBuffer != null)
                    {
                        lineComputeBuffer.Release();
                    }
                    lineComputeBuffer = new ComputeBuffer(lineBuffer.Length, Marshal.SizeOf<LineWithColor>());
                    material.SetBuffer(PropLines, lineComputeBuffer);
                }
            }
        }

        public IList<LineWithColor> LinesWithColor
        {
            get => lineBuffer;
            set
            {
                Size = value.Count;
                for (int i = 0; i < Size; i++)
                {
                    lineBuffer[i] = value[i];
                }
                lineComputeBuffer.SetData(lineBuffer, 0, 0, Size);
                Bounds bounds = CalculateBounds();
                Collider.center = bounds.center;
                Collider.size = bounds.size;
            }
        }

        /*
        public void Set(IList<LineWithColor> points)
        {
            Size = points.Count;
            for (int i = 0; i < Size; i++)
            {
                lineBuffer[i] = points[i];
            }
            lineComputeBuffer.SetData(lineBuffer, 0, 0, Size);
            Bounds bounds = CalculateBounds();
            Collider.center = bounds.center;
            Collider.size = bounds.size;
        }
        */

        float scale;
        public float Scale
        {
            get => scale;
            set
            {
                if (scale != value)
                {
                    scale = value;
                    UpdateQuadComputeBuffer();
                }
            }
        }

        public override string Name => "LineResource";

        protected override void Awake()
        {
            base.Awake();

            material = Instantiate(Resource.Materials.Line);
            Scale = 0.1f;
        }

        static readonly int PropQuad = Shader.PropertyToID("_Quad");

        void UpdateQuadComputeBuffer()
        {
            Vector3[] quad = {
                    new Vector3( 0.5f * Scale,  0.5f * Scale, 1),
                    new Vector3( 0.5f * Scale, -0.5f * Scale, 1),
                    new Vector3(-0.5f * Scale, -0.5f * Scale, 0),
                    new Vector3(-0.5f * Scale,  0.5f * Scale, 0),
            };
            if (quadComputeBuffer == null)
            {
                quadComputeBuffer = new ComputeBuffer(4, Marshal.SizeOf<Vector3>());
                material.SetBuffer(PropQuad, quadComputeBuffer);
            }
            quadComputeBuffer.SetData(quad, 0, 0, 4);
        }

        static readonly int PropLocalToWorld = Shader.PropertyToID("_LocalToWorld");
        static readonly int PropWorldToLocal = Shader.PropertyToID("_WorldToLocal");

        void Update()
        {
            material.SetMatrix(PropLocalToWorld, transform.localToWorldMatrix);
            material.SetMatrix(PropWorldToLocal, transform.worldToLocalMatrix);

            Camera camera = MainCamera ?? TFListener.MainCamera;
            material.SetVector("_Front", camera.transform.forward);

            Bounds worldBounds = Collider.bounds;
            Graphics.DrawProcedural(material, worldBounds, MeshTopology.Quads, 4, Size);
        }

        Bounds CalculateBounds()
        {
            Vector3 positionMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 positionMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < Size; i++)
            {
                Vector3 pA = lineBuffer[i].A;
                if (float.IsNaN(pA.x) ||
                    float.IsNaN(pA.y) ||
                    float.IsNaN(pA.z))
                {
                    continue;
                }
                positionMin = Vector3.Min(positionMin, pA);
                positionMax = Vector3.Max(positionMax, pA);

                Vector3 pB = lineBuffer[i].B;
                if (float.IsNaN(pB.x) ||
                    float.IsNaN(pB.y) ||
                    float.IsNaN(pB.z))
                {
                    continue;
                }
                positionMin = Vector3.Min(positionMin, pB);
                positionMax = Vector3.Max(positionMax, pB);

            }
            return new Bounds((positionMax + positionMin) / 2, positionMax - positionMin);
        }

        void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
            if (lineComputeBuffer != null)
            {
                lineComputeBuffer.Release();
                lineComputeBuffer = null;
            }
            if (quadComputeBuffer != null)
            {
                quadComputeBuffer.Release();
                quadComputeBuffer = null;
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                return;
            }
            // unity bug causes all compute buffers to disappear when focus is lost
            if (lineComputeBuffer != null)
            {
                lineComputeBuffer.Release();
                lineComputeBuffer = null;
            }
            if (lineBuffer.Length != 0)
            {
                lineComputeBuffer = new ComputeBuffer(lineBuffer.Length, Marshal.SizeOf<LineWithColor>());
                lineComputeBuffer.SetData(lineBuffer, 0, 0, Size);
                material.SetBuffer(PropLines, lineComputeBuffer);
            }

            if (quadComputeBuffer != null)
            {
                quadComputeBuffer.Release();
                quadComputeBuffer = null;
            }
            UpdateQuadComputeBuffer();

            //Debug.Log("LineResource: Rebuilding compute buffers");
        }
    }
}