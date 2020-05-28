﻿using System;
using System.Collections.Generic;
using System.Linq;
using Iviz.Resources;
using UnityEngine;

namespace Iviz.Displays
{
    public class MeshTrianglesResource : MarkerResource
    {
        public Mesh Mesh { get; private set; }

        Color color;
        public Color Color
        {
            get => color;
            set
            {
                color = value;
                mainRenderer.material = (color.a > 0.99f) ?
                    Resource.Materials.Lit.Object :
                    Resource.Materials.TransparentLit.Object;
                mainRenderer.SetPropertyColor(color);
            }
        }

        public override string Name => "MeshTriangles";

        MeshRenderer mainRenderer;
        protected override void Awake()
        {
            base.Awake();
            mainRenderer = GetComponent<MeshRenderer>();
            Mesh = new Mesh();
            Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            GetComponent<MeshFilter>().mesh = Mesh;
        }

        void SetVertices(IList<Vector3> points)
        {
            if (points is List<Vector3> points_v)
            {
                Mesh.SetVertices(points_v);
            }
            else if (points is Vector3[] points_a)
            {
                Mesh.vertices = points_a;
            }
            else
            {
                Mesh.vertices = points.ToArray();
            }
        }

        void SetTriangles(IList<int> indices)
        {
            if (indices is List<int> indices_v)
            {
                Mesh.SetTriangles(indices_v, 0);
            }
            else if (indices is int[] indices_a)
            {
                Mesh.SetTriangles(indices_a, 0);
            }
            else
            {
                Mesh.SetTriangles(indices.ToArray(), 0);
            }
        }

        public void Set(IList<Vector3> points)
        {
            if (points.Count % 3 != 0)
            {
                throw new ArgumentException("Invalid triangle list " + points.Count, nameof(points));
            }
            int[] triangles = new int[points.Count];
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = i;
            }

            Mesh.Clear();
            SetVertices(points);
            Mesh.SetTriangles(triangles, 0);
            Mesh.RecalculateNormals();

            Collider.center = Mesh.bounds.center;
            Collider.size = Mesh.bounds.size;
            //Bounds = Mesh.bounds;
        }

        public void Set(IList<Vector3> points, IList<int> triangles)
        {
            if (triangles.Count % 3 != 0)
            {
                throw new ArgumentException("Invalid triangle list " + triangles.Count, nameof(triangles));
            }

            Mesh.Clear();
            SetVertices(points);
            SetTriangles(triangles);
            Mesh.RecalculateBounds();
            Mesh.RecalculateNormals();
        }
    }
}