using Iviz.Core;
using UnityEngine;

namespace Iviz.Resources
{
    public class MaterialsType
    {
        public Info<Material> FontMaterial { get; }
        public Info<Material> FontMaterialZWrite { get; }

        public Info<Material> Lit { get; }
        public Info<Material> TexturedLit { get; }
        public Info<Material> TransparentLit { get; }
        public Info<Material> TransparentTexturedLit { get; }
        public Info<Material> ImagePreview { get; }
        public Info<Material> PointCloud { get; }
        public Info<Material> PointCloudWithColormap { get; }
        public Info<Material> DepthCloud { get; }
        public Info<Material> Grid { get; }
        public Info<Material> GridInterior { get; }
        public Info<Material> GridMap { get; }

        public Info<Material> Line { get; }
        public Info<Material> TransparentLine { get; }
        public Info<Material> LineWithColormap { get; }
        public Info<Material> TransparentLineWithColormap { get; }
        public Info<Material> LineSimple { get; }
        public Info<Material> TransparentLineSimple { get; }
        public Info<Material> LineSimpleWithColormap { get; }
        public Info<Material> TransparentLineSimpleWithColormap { get; }

        public Info<Material> LitOcclusionOnly { get; }

        public Info<Material> MeshList { get; }
        public Info<Material> MeshListWithColormap { get; }
        public Info<Material> MeshListWithColormapScaleY { get; }
        public Info<Material> MeshListOcclusionOnly { get; }
        public Info<Material> MeshListOcclusionOnlyWithScaleY { get; }

        public MaterialsType()
        {
            FontMaterial = new Info<Material>("CoreMaterials/Font Material Normal");
            FontMaterialZWrite = new Info<Material>("CoreMaterials/Font Material ZWrite");

            Lit = Settings.IsHololens
                ? new Info<Material>("Hololens Materials/White")
                : new Info<Material>("CoreMaterials/White");
            TexturedLit = Settings.IsHololens
                ? new Info<Material>("Hololens Materials/Textured Lit")
                : new Info<Material>("CoreMaterials/Textured Lit");
            TransparentLit = Settings.IsHololens
                ? new Info<Material>("Hololens Materials/Transparent Lit")
                : new Info<Material>("CoreMaterials/Transparent Lit");
            TransparentTexturedLit = Settings.IsHololens
                ? new Info<Material>("Hololens Materials/Transparent Textured Lit")
                : new Info<Material>("CoreMaterials/Transparent Textured Lit");
            ImagePreview = new Info<Material>("Materials/ImagePreview");
            Grid = new Info<Material>("Materials/Grid");
            GridInterior = new Info<Material>("Materials/Grid Interior");
            GridMap = new Info<Material>("Materials/GridMap");
            DepthCloud = new Info<Material>("Materials/DepthCloud");

            PointCloud = new Info<Material>("CoreMaterials/PointCloud");
            Line = new Info<Material>("CoreMaterials/Lines/Line");
            TransparentLine = new Info<Material>("CoreMaterials/Lines/Transparent Line");
            LineSimple = new Info<Material>("CoreMaterials/Lines/LineSimple");
            TransparentLineSimple = new Info<Material>("CoreMaterials/Lines/Transparent LineSimple");

            PointCloudWithColormap = new Info<Material>("CoreMaterials/PointCloud with Colormap");
            LineWithColormap = new Info<Material>("CoreMaterials/Lines/Line with Colormap");
            TransparentLineWithColormap = new Info<Material>("CoreMaterials/Lines/Transparent Line with Colormap");
            LineSimpleWithColormap = new Info<Material>("CoreMaterials/Lines/LineSimple with Colormap");
            TransparentLineSimpleWithColormap =
                new Info<Material>("CoreMaterials/Lines/Transparent LineSimple with Colormap");

            MeshList = new Info<Material>("CoreMaterials/MeshList");
            MeshListWithColormap = new Info<Material>("CoreMaterials/MeshList with Colormap");
            MeshListWithColormapScaleY = new Info<Material>("CoreMaterials/MeshList with Colormap ScaleY");
            MeshListWithColormapScaleY.Object.enableInstancing = true;
            MeshListOcclusionOnly = new Info<Material>("CoreMaterials/MeshList OcclusionOnly");
            MeshListOcclusionOnlyWithScaleY = new Info<Material>("CoreMaterials/MeshList OcclusionOnly with ScaleY");

            LitOcclusionOnly = new Info<Material>("CoreMaterials/White OcclusionOnly");
        }
    }
}