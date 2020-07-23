﻿using UnityEngine;
using System.Linq;
using Iviz.Resources;

namespace Iviz.Displays
{
    public sealed class MarkerWrapper : MonoBehaviour, ISupportsAROcclusion, ISupportsTint
    {
        MeshRenderer meshRenderer;
        Collider Collider;
        [SerializeField] Color[] originalColors;
        [SerializeField] Material[] materials;
        [SerializeField] Material[] occlusionMaterials;
        [SerializeField] Material[] transparentMaterials;

        public string Name => name;

        public Bounds Bounds => new Bounds(Vector3.zero, 0.1f * Vector3.one);

        public Bounds WorldBounds => Collider.bounds;

        public Pose WorldPose => transform.AsPose();

        public Vector3 WorldScale => transform.lossyScale;

        public int Layer
        {
            get => gameObject.layer;
            set => gameObject.layer = value;
        }
        public Transform Parent
        {
            get => transform.parent;
            set => transform.parent = value;
        }
        public bool ColliderEnabled
        {
            get => Collider.enabled;
            set => Collider.enabled = value;
        }
        public bool Visible
        {
            get => meshRenderer.enabled;
            set => meshRenderer.enabled = value;
        }

        bool occlusionOnly_;
        public bool OcclusionOnly
        {
            get => occlusionOnly_;
            set
            {
                occlusionOnly_ = value;
                UpdateMaterials();
            }
        }

        [SerializeField] Color tint_;
        public Color Tint
        {
            get => tint_;
            set
            {
                tint_ = value;
                UpdateMaterials();
            }
        }

        void UpdateMaterials()
        {
            if (OcclusionOnly)
            {
                if (occlusionMaterials == null)
                {
                    occlusionMaterials = new Material[materials.Length];
                    for (int i = 0; i < occlusionMaterials.Length; i++)
                    {
                        occlusionMaterials[i] = Resource.Materials.LitOcclusionOnly.Object;
                    }
                }
                meshRenderer.sharedMaterials = occlusionMaterials;
            }
            else if (Tint.a <= 254f / 255f)
            {
                if (transparentMaterials == null)
                {
                    transparentMaterials = new Material[materials.Length];
                    for (int i = 0; i < transparentMaterials.Length; i++)
                    {
                        if (materials[i].mainTexture == null)
                        {
                            transparentMaterials[i] = Resource.Materials.TransparentLit.Object;
                        }
                        else
                        {
                            Texture texture = materials[i].mainTexture;
                            Material material = Resource.TexturedMaterials.GetAlpha(texture);
                            transparentMaterials[i] = material;
                        }
                    }
                }
                meshRenderer.sharedMaterials = transparentMaterials;
                for (int i = 0; i < transparentMaterials.Length; i++)
                {
                    Color color = originalColors[i] * Tint;
                    meshRenderer.SetPropertyColor(color, i);
                }
            }
            else
            {
                meshRenderer.sharedMaterials = materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Color color = originalColors[i] * Tint;
                    meshRenderer.SetPropertyColor(color, i);
                }
            }
        }

        void Awake()
        {
            Collider = GetComponent<Collider>();
            meshRenderer = GetComponent<MeshRenderer>();

            materials = meshRenderer.sharedMaterials.ToArray();
            originalColors = new Color[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null)
                {
                    materials[i] = Resource.Materials.Lit.Object;
                    originalColors[i] = Color.white;
                }
                else if (materials[i].mainTexture == null)
                {
                    Color c = materials[i].color;
                    materials[i] = Resource.Materials.Lit.Object;
                    originalColors[i] = c;
                    meshRenderer.SetPropertyColor(c, i);
                }
                else
                {
                    Color c = materials[i].color;
                    Texture tex = materials[i].mainTexture;
                    Material material = Resource.TexturedMaterials.Get(tex);
                    meshRenderer.SetPropertyColor(c, i);
                    meshRenderer.SetPropertyMainTexST(materials[i].mainTextureOffset, materials[i].mainTextureScale, i);
                    materials[i] = material;
                    originalColors[i] = c;
                }
            }
            meshRenderer.sharedMaterials = materials;
        }

        public void Stop()
        {
            Tint = Color.white;
            OcclusionOnly = false;
        }
    }

}