﻿using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using Iviz.Resources;
using Iviz.App.Listeners;
using Unity.Mathematics;

namespace Iviz.Displays
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct LineWithColor
    {
        readonly float4x2 f;

        public Vector3 A => new Vector3(f.c0.x, f.c0.y, f.c0.z);
        public Color32 ColorA
        {
            get
            {
                unsafe
                {
                    float w = f.c0.w;
                    return *(Color32*)&w;
                }
            }
        }
        public Vector3 B => new Vector3(f.c1.x, f.c1.y, f.c1.z);
        public Color32 ColorB
        {
            get
            {
                unsafe
                {
                    float w = f.c1.w;
                    return *(Color32*)&w;
                }
            }
        }

        public LineWithColor(Vector3 a, Color32 colorA, Vector3 b, Color32 colorB)
        {
            f.c0.x = a.x;
            f.c0.y = a.y;
            f.c0.z = a.z;

            f.c1.x = b.x;
            f.c1.y = b.y;
            f.c1.z = b.z;

            unsafe
            {
                f.c0.w = *(float*)&colorA;
                f.c1.w = *(float*)&colorB;
            }
        }

        public static implicit operator float4x2(LineWithColor c) => c.f;
    };

    public class LineResource : MarkerResource
    {
        Material material;

        LineWithColor[] lineBuffer = Array.Empty<LineWithColor>();
        ComputeBuffer lineComputeBuffer;
        ComputeBuffer quadComputeBuffer;

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
                Reserve(size_);
            }
        }

        public void Reserve(int size_)
        {
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

        public void Set(int size, Action<LineWithColor[]> func)
        {
            Size = size;
            func(lineBuffer);
            lineComputeBuffer.SetData(lineBuffer, 0, 0, Size);
            Bounds bounds = CalculateBounds();
            Collider.center = bounds.center;
            Collider.size = bounds.size;
        }

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

        bool useIntensityTexture;
        public bool UseIntensityTexture
        {
            get => useIntensityTexture;
            set
            {
                if (useIntensityTexture == value)
                {
                    return;
                }
                useIntensityTexture = value;
                if (useIntensityTexture)
                {
                    material.EnableKeyword("USE_TEXTURE");
                }
                else
                {
                    material.DisableKeyword("USE_TEXTURE");
                }
            }
        }

        static readonly int PropIntensity = Shader.PropertyToID("_IntensityTexture");

        Resource.ColormapId colormap;
        public Resource.ColormapId Colormap
        {
            get => colormap;
            set
            {
                colormap = value;

                Texture2D texture = Resource.Colormaps.Textures[Colormap];
                material.SetTexture(PropIntensity, texture);
            }
        }

        static readonly int PropIntensityCoeff = Shader.PropertyToID("_IntensityCoeff");
        static readonly int PropIntensityAdd = Shader.PropertyToID("_IntensityAdd");

        Vector2 intensityBounds;
        public Vector2 IntensityBounds
        {
            get => intensityBounds;
            set
            {
                intensityBounds = value;
                float intensitySpan = intensityBounds.y - intensityBounds.x;

                if (intensitySpan == 0)
                {
                    material.SetFloat(PropIntensityCoeff, 1);
                    material.SetFloat(PropIntensityAdd, 0);
                }
                else
                {
                    material.SetFloat(PropIntensityCoeff, 1 / intensitySpan);
                    material.SetFloat(PropIntensityAdd, -intensityBounds.x / intensitySpan);
                }
            }
        }

        public override string Name => "LineResource";

        protected override void Awake()
        {
            base.Awake();

            Scale = 0.1f;

            material = Resource.Materials.Line.Instantiate();
            material.DisableKeyword("USE_TEXTURE");

            UseIntensityTexture = false;
            IntensityBounds = new Vector2(0, 1);
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

            Camera camera = TFListener.MainCamera;
            material.SetVector("_Front", transform.InverseTransformDirection(camera.transform.forward));

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

            IntensityBounds = IntensityBounds;
            Colormap = Colormap;

            //Debug.Log("LineResource: Rebuilding compute buffers");
        }

        public override void Stop()
        {
            base.Stop();
            Size = 0;
        }
    }
}