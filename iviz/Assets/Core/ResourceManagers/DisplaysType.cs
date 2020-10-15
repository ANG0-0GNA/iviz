using System;
using System.Collections.Generic;
using System.Reflection;
using Iviz.Displays;
using UnityEngine;

namespace Iviz.Resources
{
    public sealed class DisplaysType
    {
        readonly Dictionary<Type, Info<GameObject>> resourceByType;

        public Info<GameObject> Cube { get; }
        public Info<GameObject> Cylinder { get; }
        public Info<GameObject> Sphere { get; }
        public Info<GameObject> Text { get; }
        public Info<GameObject> LineConnector { get; }
        public Info<GameObject> NamedBoundary { get; }
        public Info<GameObject> Arrow { get; }
        public Info<GameObject> SphereSimple { get; }
        public Info<GameObject> MeshList { get; }
        public Info<GameObject> PointList { get; }
        public Info<GameObject> MeshTriangles { get; }
        public Info<GameObject> TFFrame { get; }
        public Info<GameObject> Image { get; }
        public Info<GameObject> Square { get; }
        public Info<GameObject> Line { get; }
        public Info<GameObject> Grid { get; }
        public Info<GameObject> DepthImageResource { get; }
        public Info<GameObject> OccupancyGridResource { get; }
        public Info<GameObject> RadialScanResource { get; }
        public Info<GameObject> ARMarkerResource { get; }
        public Info<GameObject> AxisFrame { get; }
        public Info<GameObject> AngleAxis { get; }
        public Info<GameObject> Trail { get; }
        public Info<GameObject> InteractiveControl { get; }
        public Info<GameObject> GridMap { get; }
        
        public DisplaysType()
        {
            Cube = new Info<GameObject>("Displays/Cube");
            Cylinder = new Info<GameObject>("Displays/Cylinder");
            Sphere = new Info<GameObject>("Displays/Sphere");
            Text = new Info<GameObject>("Displays/Text");
            LineConnector = new Info<GameObject>("Displays/LineConnector");
            NamedBoundary = new Info<GameObject>("Displays/NamedBoundary");
            Arrow = new Info<GameObject>("Displays/Arrow");
            SphereSimple = new Info<GameObject>("Spheres/sphere-LOD1");
            MeshList = new Info<GameObject>("Displays/MeshList");
            PointList = new Info<GameObject>("Displays/PointList");
            MeshTriangles = new Info<GameObject>("Displays/MeshTriangles");
            TFFrame = new Info<GameObject>("Displays/TFFrame");
            Image = new Info<GameObject>("Displays/ImageResource");
            Square = new Info<GameObject>("Displays/Plane");
            Line = new Info<GameObject>("Displays/Line");
            Grid = new Info<GameObject>("Displays/Grid");
            DepthImageResource = new Info<GameObject>("Displays/DepthImageResource");
            OccupancyGridResource = new Info<GameObject>("Displays/OccupancyGridResource");
            RadialScanResource = new Info<GameObject>("Displays/RadialScanResource");
            ARMarkerResource = new Info<GameObject>("Displays/ARMarkerResource");
            AxisFrame = new Info<GameObject>("Displays/AxisFrameResource");
            AngleAxis = new Info<GameObject>("Displays/AngleAxis");
            Trail = new Info<GameObject>("Displays/Trail");
            InteractiveControl = new Info<GameObject>("Displays/InteractiveControl");
            GridMap = new Info<GameObject>("Displays/GridMap");

            resourceByType = CreateTypeDictionary();
        }

        Dictionary<Type, Info<GameObject>> CreateTypeDictionary()
        {
            Dictionary<Type, Info<GameObject>> tmpResourceByType = new Dictionary<Type, Info<GameObject>>();
            PropertyInfo[] properties = GetType().GetProperties();
            foreach (var property in properties)
            {
                if (!typeof(Info<GameObject>).IsAssignableFrom(property.PropertyType))
                {
                    continue;
                }

                Info<GameObject> info = (Info<GameObject>) property.GetValue(this);
                IDisplay display = info.Object.GetComponent<IDisplay>();
                Type type = display?.GetType();
                string name = type?.FullName;
                if (name is null)
                {
                    continue;
                }

                if (tmpResourceByType.ContainsKey(type))
                {
                    tmpResourceByType[type] = null; // not unique! invalidate
                    continue;
                }

                tmpResourceByType[type] = info;
            }

            return tmpResourceByType;
        }

        public bool TryGetResource(Type type, out Info<GameObject> info)
        {
            return resourceByType.TryGetValue(type, out info) && info != null;
        }
    }
}