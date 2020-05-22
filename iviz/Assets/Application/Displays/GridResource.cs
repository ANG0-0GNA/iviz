﻿using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Iviz.App.Listeners;
using Iviz.Resources;
using Iviz.App;

namespace Iviz.Displays
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GridOrientation
    {
        XY, YZ, XZ
    }

    public class GridResource : MarkerResource
    {
        Mesh mesh;
        MeshRenderer meshRenderer;
        GameObject interiorObject;
        MeshRenderer interiorRenderer;

        public static readonly List<string> OrientationNames = new List<string> { "XY", "YZ", "XZ" };

        static readonly Dictionary<GridOrientation, Quaternion> RotationByOrientation = new Dictionary<GridOrientation, Quaternion>
        {
            { GridOrientation.XZ, Quaternion.identity },
            { GridOrientation.XY, Quaternion.Euler(90, 0, 0) },
            { GridOrientation.YZ, Quaternion.Euler(0, 90, 0) }
        };

        GridOrientation orientation;
        public GridOrientation Orientation
        {
            get => orientation;
            set
            {
                orientation = value;
                transform.localRotation = RotationByOrientation[value];
            }
        }

        Color gridColor;
        public Color GridColor
        {
            get => gridColor;
            set
            {
                gridColor = value;
                meshRenderer.SetPropertyColor(value);
            }
        }

        Color interiorColor;
        public Color InteriorColor
        {
            get => interiorColor;
            set
            {
                interiorColor = value;
                interiorRenderer.SetPropertyColor(value);
            }
        }

        float gridLineWidth;
        public float GridLineWidth
        {
            get => gridLineWidth;
            set
            {
                gridLineWidth = value;
                UpdateMesh();
            }
        }

        float gridCellSize;
        public float GridCellSize
        {
            get => gridCellSize;
            set
            {
                gridCellSize = value;
                UpdateMesh();
            }
        }

        int numberOfGridCells;
        public int NumberOfGridCells
        {
            get => numberOfGridCells;
            set
            {
                numberOfGridCells = value;
                UpdateMesh();
            }
        }

        bool showInterior;
        public bool ShowInterior
        {
            get => showInterior;
            set
            {
                showInterior = value;
                interiorObject.SetActive(value);
            }
        }

        public override string Name => "Grid";

        protected override void Awake()
        {
            base.Awake();

            mesh = new Mesh();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = Resource.Materials.Grid;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            //meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            interiorObject = Resource.Markers.Cube.Instantiate(transform);
            interiorObject.name = "Grid Interior";
            interiorObject.transform.localPosition = new Vector3(0, 0, 0.01f);
            interiorRenderer = interiorObject.GetComponent<MeshRenderer>();
            interiorRenderer.sharedMaterial = meshRenderer.sharedMaterial;
            interiorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            interiorRenderer.receiveShadows = true;
            //interiorRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            Orientation = GridOrientation.XY;
            GridColor = Color.white * 0.25f;
            InteriorColor = Color.white * 0.5f;
            GridLineWidth = 0.02f;
            GridCellSize = 1;
            NumberOfGridCells = 20;
            ShowInterior = true;

            gameObject.layer = Resource.ClickableLayer;
        }

        void UpdateMesh()
        {
            Mesh squareMesh = Resource.Markers.Square.GameObject.GetComponent<MeshFilter>().sharedMesh;
            IEnumerable<int> squareIndices = squareMesh.GetIndices(0);

            float totalSize = GridCellSize * NumberOfGridCells + GridLineWidth;
            int gridVertSize;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> indices = new List<int>();

            List<Vector3> squareVertices =
                squareMesh.vertices.
                Select(x => new Vector3(x.x, x.z, -x.y) * 0.1f + new Vector3(0.5f, 0.5f, -0.5f)).
                ToList();
            List<Vector3> squareNormals =
                squareMesh.normals.
                Select(x => new Vector3(x.x, x.z, -x.y)).
                ToList();

            float offsetX = -totalSize / 2;
            float offsetY = -totalSize / 2;
            Vector3 offset;

            Vector3 scaleHorizontal = new Vector3(GridLineWidth, totalSize, GridLineWidth / 8);
            List<Vector3> verticesHorizontal = squareVertices.Select(x => x.CwiseProduct(scaleHorizontal)).ToList();

            for (int i = 0; i <= NumberOfGridCells; i++)
            {
                gridVertSize = vertices.Count();
                offset = new Vector3(offsetX + i * GridCellSize, offsetY, 0);
                indices.AddRange(squareIndices.Select(x => x + gridVertSize));
                vertices.AddRange(verticesHorizontal.Select(x => x + offset));
                normals.AddRange(squareNormals);
            }


            Vector3 scaleVertical = new Vector3(totalSize, GridLineWidth, GridLineWidth / 8);
            List<Vector3> verticesVertical = squareVertices.Select(x => x.CwiseProduct(scaleVertical)).ToList();

            for (int i = 0; i <= NumberOfGridCells; i++)
            {
                gridVertSize = vertices.Count();
                offset = new Vector3(offsetX, offsetY + i * GridCellSize, 0);
                indices.AddRange(squareIndices.Select(x => x + gridVertSize));
                vertices.AddRange(verticesVertical.Select(x => x + offset));
                normals.AddRange(squareNormals);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices.ToArray(), squareMesh.GetTopology(0), 0);
            mesh.Optimize();

            interiorObject.transform.localScale = new Vector3(totalSize, totalSize, GridLineWidth / 8.1f);

            GetComponent<BoxCollider>().size = new Vector3(totalSize, totalSize, GridLineWidth / 8);
        }

        public override void Stop()
        {
            Destroy(interiorObject);
            interiorObject = null;
        }
    }
}