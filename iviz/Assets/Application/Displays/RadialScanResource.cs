﻿using UnityEngine;
using System.Collections;
using Iviz.Resources;
using System.Collections.Generic;
using Iviz.App;
using System;

namespace Iviz.Displays
{
    public sealed class RadialScanResource : MonoBehaviour, IDisplay, IRecyclable
    {
        PointListResource pointCloud;
        LineResource lines;

        readonly List<PointWithColor> pointBuffer = new List<PointWithColor>();
        readonly List<LineWithColor> lineBuffer = new List<LineWithColor>();

        public string Name => "RadialScanResource";
        public Bounds Bounds => UseLines ? lines.Bounds : pointCloud.Bounds;
        public Bounds WorldBounds => UseLines ? lines.WorldBounds : pointCloud.WorldBounds;
        public Pose WorldPose => UseLines ? lines.WorldPose : pointCloud.WorldPose;
        public Vector3 WorldScale => UseLines ? lines.WorldScale : pointCloud.WorldScale;

        int layer;
        public int Layer
        {
            get => layer;
            set
            {
                layer = value;
                pointCloud.Layer = layer;
                lines.Layer = layer;
            }
        }
        public Transform Parent
        {
            get => transform.parent;
            set => transform.parent = value;
        }

        bool colliderEnabled;
        public bool ColliderEnabled
        {
            get => colliderEnabled;
            set
            {
                colliderEnabled = value;
                lines.ColliderEnabled = value;
                pointCloud.ColliderEnabled = value;
            }
        }

        public int Size { get; private set; }

        public Vector2 MeasuredIntensityBounds { get; private set; }

        bool visible = true;
        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                pointCloud.Visible = value && !UseLines;
                lines.Visible = value && UseLines;
            }
        }

        float pointSize;
        public float PointSize
        {
            get => pointSize;
            set
            {
                pointSize = value;
                pointCloud.Scale = value * Vector2.one;
                lines.Scale = value;
            }
        }

        Resource.ColormapId colormap;
        public Resource.ColormapId Colormap
        {
            get => colormap;
            set
            {
                colormap = value;
                pointCloud.Colormap = value;
                lines.Colormap = value;
            }
        }

        public bool UseIntensityNoRange { get; set; }

        bool forceMinMax;
        public bool ForceMinMax
        {
            get => forceMinMax;
            set
            {
                forceMinMax = value;
                if (value)
                {
                    pointCloud.IntensityBounds = new Vector2(MinIntensity, MaxIntensity);
                    lines.IntensityBounds = new Vector2(MinIntensity, MaxIntensity);
                }
                else
                {
                    pointCloud.IntensityBounds = MeasuredIntensityBounds;
                    lines.IntensityBounds = MeasuredIntensityBounds;
                }
            }
        }

        bool flipMinMax;
        public bool FlipMinMax
        {
            get => flipMinMax;
            set
            {
                flipMinMax = value;
                pointCloud.FlipMinMax = value;
                lines.FlipMinMax = value;
            }
        }

        float minIntensity;
        public float MinIntensity
        {
            get => minIntensity;
            set
            {
                minIntensity = value;
                if (ForceMinMax)
                {
                    pointCloud.IntensityBounds = new Vector2(MinIntensity, MaxIntensity);
                    lines.IntensityBounds = new Vector2(MinIntensity, MaxIntensity);
                }
            }
        }

        float maxIntensity;
        public float MaxIntensity
        {
            get => maxIntensity;
            set
            {
                maxIntensity = value;
                if (ForceMinMax)
                {
                    pointCloud.IntensityBounds = new Vector2(MinIntensity, MaxIntensity);
                    lines.IntensityBounds = new Vector2(MinIntensity, MaxIntensity);
                }
            }
        }

        bool useLines;
        public bool UseLines
        {
            get => useLines;
            set
            {
                useLines = value;
                if (useLines)
                {
                    SetLines();
                    lines.Visible = Visible;
                }
                else
                {
                    SetPoints();
                    pointCloud.Visible = Visible;
                }
            }
        }

        float maxLineDistance;
        public float MaxLineDistance
        {
            get => maxLineDistance;
            set
            {
                bool changed = value != maxLineDistance;
                maxLineDistance = value;
                if (changed)
                {
                    SetLines();
                }
            }
        }

        void Awake()
        {
            pointCloud = ResourcePool.GetOrCreate<PointListResource>(Resource.Markers.PointList, transform);
            lines = ResourcePool.GetOrCreate<LineResource>(Resource.Markers.Line, transform);

            pointCloud.UseIntensityTexture = true;
            lines.UseIntensityTexture = true;

            MinIntensity = 0;
            MaxIntensity = 1;
            UseLines = true;
            Colormap = Resource.ColormapId.hsv;
            PointSize = 0.01f;
        }

        public void Set(float angleMin, float angleIncrement, float rangeMin, float rangeMax, float[] ranges, float[] intensities)
        {
            if (rangeMin > rangeMax)
            {
                throw new ArgumentException(nameof(rangeMin));
            }
            if (intensities.Length != 0 && intensities.Length != ranges.Length)
            {
                throw new ArgumentException(nameof(ranges));
            }

            float x = Mathf.Cos(angleMin);
            float y = Mathf.Sin(angleMin);

            float dx = Mathf.Cos(angleIncrement);
            float dy = Mathf.Sin(angleIncrement);

            pointBuffer.Clear();
            if (!UseIntensityNoRange)
            {
                for (int i = 0; i < ranges.Length; i++)
                {
                    float range = ranges[i];
                    if (float.IsNaN(range) || range > rangeMax || range < rangeMin)
                    {
                        continue;
                    }
                    pointBuffer.Add(new PointWithColor(new Unity.Mathematics.float4(-y, 0, x, 1) * range));
                    x = dx * x - dy * y;
                    y = dy * x + dx * y;
                }
            }
            else
            {
                for (int i = 0; i < ranges.Length; i++)
                {
                    float range = ranges[i];
                    if (float.IsNaN(range) || range > rangeMax || range < rangeMin)
                    {
                        continue;
                    }
                    pointBuffer.Add(new PointWithColor(-y * range, 0, x * range, intensities[i]));
                    x = dx * x - dy * y;
                    y = dy * x + dx * y;
                }
            }

            Size = pointBuffer.Count;

            if (!UseLines)
            {
                SetPoints();
            }
            else
            {
                SetLines();
            }
        }

        void SetPoints()
        {
            pointCloud.PointsWithColor = pointBuffer;
            MeasuredIntensityBounds = pointCloud.IntensityBounds;
            if (ForceMinMax)
            {
                pointCloud.IntensityBounds = new Vector2(MinIntensity, MaxIntensity);
            }
        }

        void SetLines()
        {
            int n = pointBuffer.Count;
            float maxLineDistanceSq = maxLineDistance * maxLineDistance;

            lineBuffer.Clear();
            for (int i = 0; i < pointBuffer.Count; i++)
            {
                PointWithColor pA = pointBuffer[i];
                PointWithColor pB = pointBuffer[(i + 1) % n];
                if ((pB.Position - pA.Position).sqrMagnitude < maxLineDistanceSq)
                {
                    lineBuffer.Add(new LineWithColor(pA, pB));
                }
                else
                {
                    lineBuffer.Add(new LineWithColor(pA, pA));
                }
            }
            lines.LinesWithColor = lineBuffer;
            MeasuredIntensityBounds = lines.IntensityBounds;
            if (ForceMinMax)
            {
                lines.IntensityBounds = new Vector2(MinIntensity, MaxIntensity);
            }
        }

        public void Stop()
        {
            pointCloud.Stop();
            lines.Stop();
        }

        public void Recycle()
        {
            ResourcePool.Dispose(Resource.Markers.PointList, pointCloud.gameObject);
            ResourcePool.Dispose(Resource.Markers.Line, lines.gameObject);
        }
    }
}
