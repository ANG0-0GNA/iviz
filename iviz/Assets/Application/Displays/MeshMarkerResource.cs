﻿using Iviz.Resources;
using UnityEngine;

namespace Iviz.Displays
{
    public class MeshMarkerResource : MarkerResource, ISupportsTintAndAROcclusion
    {
        MeshRenderer MainRenderer { get; set; }
        Material textureMaterial;
        Material textureMaterialAlpha;

        [SerializeField] Texture2D texture;

        public Texture2D Texture
        {
            get => texture;
            set
            {
                if (texture == value)
                {
                    return;
                }

                textureMaterial = null;
                textureMaterialAlpha = null;
                texture = value;
                SetEffectiveColor();
            }
        }

        [SerializeField] Color emissiveColor = Color.black;
        public Color EmissiveColor
        {
            get => emissiveColor;
            set
            {
                emissiveColor = value;
                if (MainRenderer != null)
                {
                    MainRenderer.SetPropertyEmissiveColor(emissiveColor);
                }
            }
        }


        [SerializeField] Color color = Color.white;
        public Color Color
        {
            get => color;
            set
            {
                color = value;
                SetEffectiveColor();
            }
        }

        [SerializeField] bool occlusionOnly;
        public bool OcclusionOnlyActive
        {
            get => occlusionOnly;
            set
            {
                occlusionOnly = value;
                if (value)
                {
                    MainRenderer.sharedMaterial = Resource.Materials.LitOcclusionOnly.Object;
                }
                else
                {
                    SetEffectiveColor();
                }
            }
        }

        [SerializeField] Color tint = Color.white;

        public Color Tint
        {
            get => tint;
            set
            {
                tint = value;
                SetEffectiveColor();
            }
        }

        Color EffectiveColor => Color * Tint;

        void SetEffectiveColor()
        {
            if (MainRenderer == null)
            {
                return;
            }

            if (OcclusionOnlyActive)
            {
                return;
            }

            Color effectiveColor = EffectiveColor;
            if (Texture == null) // do not use 'is' here
            {
                Material material = effectiveColor.a > 254f / 255f
                    ? Resource.Materials.Lit.Object
                    : Resource.Materials.TransparentLit.Object;
                MainRenderer.sharedMaterial = material;
            }
            else if (effectiveColor.a > 254f / 255f)
            {
                if (textureMaterial == null)
                {
                    textureMaterial = Resource.TexturedMaterials.Get(Texture);
                }

                MainRenderer.material = textureMaterial;
            }
            else
            {
                if (textureMaterialAlpha == null)
                {
                    textureMaterialAlpha = Resource.TexturedMaterials.GetAlpha(Texture);
                }

                MainRenderer.sharedMaterial = textureMaterial;
            }

            MainRenderer.SetPropertyColor(effectiveColor);
        }

        protected override void Awake()
        {
            base.Awake();
            MainRenderer = GetComponent<MeshRenderer>();
            Color = color;
            EmissiveColor = emissiveColor;
            Tint = tint;

            MainRenderer.SetPropertyMainTexST(Vector2.zero, Vector2.one, 0);
        }

        public override void Suspend()
        {
            base.Suspend();
            Color = Color.white;
            EmissiveColor = Color.black;
            ColliderEnabled = true;
            OcclusionOnlyActive = false;
        }
    }
}