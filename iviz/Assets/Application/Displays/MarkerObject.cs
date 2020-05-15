﻿using UnityEngine;
using System.Linq;
using System;
using UnityEngine.EventSystems;
using Iviz.Msgs.visualization_msgs;
using Iviz.App.Displays;
using System.Collections.Generic;

namespace Iviz.App
{
    enum MarkerType
    {
        ARROW = Marker.ARROW,
        CUBE = Marker.CUBE,
        SPHERE = Marker.SPHERE,
        CYLINDER = Marker.CYLINDER,
        LINE_STRIP = Marker.LINE_STRIP,
        LINE_LIST = Marker.LINE_LIST,
        CUBE_LIST = Marker.CUBE_LIST,
        SPHERE_LIST = Marker.SPHERE_LIST,
        POINTS = Marker.POINTS,
        TEXT_VIEW_FACING = Marker.TEXT_VIEW_FACING,
        MESH_RESOURCE = Marker.MESH_RESOURCE,
        TRIANGLE_LIST = Marker.TRIANGLE_LIST,
    }

    public class MarkerObject : ClickableDisplayNode
    {
        const string packagePrefix = "package://ibis/";

        public event Action<Vector3, int> Clicked;

        public string Id { get; private set; }

        MarkerResource resource;
        Resource.Info resourceType;
        Mesh cacheCube, cacheSphere;

        public override Bounds Bounds => resource?.Bounds ?? new Bounds();
        public override Bounds WorldBounds => resource?.WorldBounds ?? new Bounds();

        public DateTime ExpirationTime { get; private set; }

        void Awake()
        {
            cacheCube = Resource.Markers.Cube.GameObject.GetComponent<MeshFilter>().sharedMesh;
            cacheSphere = Resource.Markers.SphereSimple.GameObject.GetComponent<MeshFilter>().sharedMesh;
        }

        public void Set(Marker msg)
        {
            Id = MarkerListener.IdFromMessage(msg);
            name = Id;

            ExpirationTime = msg.lifetime.IsZero() ?
                DateTime.MaxValue :
                DateTime.Now + msg.lifetime.ToTimeSpan();

            Resource.Info newResourceType = GetRequestedResource(msg);
            if (newResourceType != resourceType)
            {
                if (resource != null)
                {
                    ResourcePool.Dispose(resourceType, resource.gameObject);
                    resource = null;
                }
                resourceType = newResourceType;
                if (resourceType == null)
                {
                    if (msg.Type() == MarkerType.MESH_RESOURCE)
                    {
                        Logger.Error($"MarkerObject: Unknown mesh resource '{msg.mesh_resource}'");
                    }
                    else
                    {
                        Logger.Error($"MarkerObject: Marker type '{msg.Type()}'");
                    }
                    return;
                }
                resource = ResourcePool.GetOrCreate(resourceType, transform).GetComponent<MarkerResource>();
                if (resource == null)
                {
                    Debug.LogError("Resource " + resourceType + " has no MarkerResource!");
                }
            }
            if (resource == null)
            {
                return;
            }

            UpdateTransform(msg);

            resource.gameObject.layer = Resource.ClickableLayer;

            switch (msg.Type())
            {
                case MarkerType.CUBE:
                case MarkerType.SPHERE:
                case MarkerType.CYLINDER:
                    MeshMarkerResource meshMarker = resource as MeshMarkerResource;
                    meshMarker.Color = msg.color.Sanitize().ToUnityColor();
                    transform.localScale = msg.scale.Ros2Unity().Abs();
                    break;
                case MarkerType.TEXT_VIEW_FACING:
                    TextMarkerResource textResource = resource as TextMarkerResource;
                    textResource.Text = msg.text;
                    textResource.Color = msg.color.Sanitize().ToUnityColor();
                    transform.localScale = (float)msg.scale.z * Vector3.one;
                    break;
                case MarkerType.CUBE_LIST:
                case MarkerType.SPHERE_LIST:
                    MeshListResource meshList = resource as MeshListResource;
                    meshList.Mesh = (msg.Type() == MarkerType.CUBE_LIST) ? cacheCube : cacheSphere;
                    meshList.SetSize(msg.points.Length);
                    meshList.Scale = msg.scale.Ros2Unity().Abs();
                    meshList.Color = msg.color.Sanitize().ToUnityColor();
                    meshList.Colors = (msg.colors.Length == 0) ? null : msg.colors.Select(x => x.ToUnityColor32());
                    meshList.Points = msg.points.Select(x => x.Ros2Unity());
                    break;
                case MarkerType.LINE_LIST:
                    {
                        LineResource lineResource = resource as LineResource;
                        lineResource.Scale = (float)msg.scale.x;
                        LineWithColor[] lines = new LineWithColor[msg.points.Length / 2];
                        if (msg.colors.Length == 0)
                        {
                            Color32 color = msg.color.Sanitize().ToUnityColor32();
                            for (int i = 0; i < lines.Length; i++)
                            {
                                lines[i].A = msg.points[2 * i + 0].Ros2Unity();
                                lines[i].B = msg.points[2 * i + 1].Ros2Unity();
                                lines[i].colorA = color;
                                lines[i].colorB = color;
                            }
                        }
                        else
                        {
                            Color color = msg.color.Sanitize().ToUnityColor();
                            for (int i = 0; i < lines.Length; i++)
                            {
                                lines[i].A = msg.points[2 * i + 0].Ros2Unity();
                                lines[i].B = msg.points[2 * i + 1].Ros2Unity();
                                lines[i].colorA = color * msg.colors[2 * i + 0].ToUnityColor();
                                lines[i].colorB = color * msg.colors[2 * i + 1].ToUnityColor();
                            }
                        }
                        lineResource.Set(lines);
                        break;
                    }
                case MarkerType.LINE_STRIP:
                    {
                        LineResource lineResource = resource as LineResource;
                        lineResource.Scale = (float)msg.scale.x;
                        LineWithColor[] lines = new LineWithColor[msg.points.Length - 1];
                        if (msg.colors.Length == 0)
                        {
                            Color32 color = msg.color.Sanitize().ToUnityColor32();
                            for (int i = 0; i < lines.Length; i++)
                            {
                                lines[i].A = msg.points[i + 0].Ros2Unity();
                                lines[i].B = msg.points[i + 1].Ros2Unity();
                                lines[i].colorA = color;
                                lines[i].colorB = color;
                            }
                        }
                        else
                        {
                            Color color = msg.color.Sanitize().ToUnityColor();
                            for (int i = 0; i < lines.Length; i++)
                            {
                                lines[i].A = msg.points[i + 0].Ros2Unity();
                                lines[i].B = msg.points[i + 1].Ros2Unity();
                                lines[i].colorA = color * msg.colors[i + 0].ToUnityColor();
                                lines[i].colorB = color * msg.colors[i + 1].ToUnityColor();
                            }
                        }
                        lineResource.Set(lines);
                        break;
                    }
                case MarkerType.POINTS:
                    PointListResource pointList = resource as PointListResource;
                    pointList.Scale = msg.scale.Ros2Unity().Abs();
                    PointWithColor[] points = new PointWithColor[msg.points.Length];
                    if (msg.colors.Length == 0)
                    {
                        Color32 color = msg.color.Sanitize().ToUnityColor32();
                        for (int i = 0; i < points.Length; i++)
                        {
                            points[i].position = msg.points[i].Ros2Unity();
                            points[i].color = color;
                        }
                    }
                    else
                    {
                        Color color = msg.color.Sanitize().ToUnityColor();
                        for (int i = 0; i < points.Length; i++)
                        {
                            points[i].position = msg.points[i].Ros2Unity();
                            points[i].color = color * msg.colors[i].ToUnityColor();
                        }
                    }
                    pointList.UseIntensityTexture = false;
                    break;
                case MarkerType.TRIANGLE_LIST:
                    MeshTrianglesResource meshTriangles = resource as MeshTrianglesResource;
                    meshTriangles.Color = msg.color.Sanitize().ToUnityColor();
                    meshTriangles.Set(msg.points.Select(x => x.Ros2Unity()).ToArray());
                    break;
            }
        }

        void UpdateTransform(Marker msg)
        {
            if (msg.frame_locked)
            {
                SetParent(msg.header.frame_id);
            }
            else if (msg.header.frame_id == "")
            {
                Parent = TFListener.ListenersFrame;
            }
            else
            {
                Pose pose = TFListener.GetOrCreateFrame(msg.header.frame_id).transform.AsPose();
                Parent = TFListener.BaseFrame;
                transform.SetLocalPose(pose);
            }

            transform.SetLocalPose(msg.pose.Ros2Unity());
        }

        Resource.Info GetRequestedResource(Marker msg)
        {
            switch (msg.Type())
            {
                case MarkerType.ARROW: return Resource.Markers.Arrow;
                case MarkerType.CYLINDER: return Resource.Markers.Cylinder;
                case MarkerType.CUBE: return Resource.Markers.Cube;
                case MarkerType.SPHERE: return Resource.Markers.Sphere;
                case MarkerType.TEXT_VIEW_FACING: return Resource.Markers.Text;
                case MarkerType.LINE_STRIP:
                case MarkerType.LINE_LIST:
                    return Resource.Markers.Line;
                case MarkerType.MESH_RESOURCE:
                    if (!Uri.IsWellFormedUriString(msg.mesh_resource, UriKind.Absolute))
                    {
                        return null;
                    }
                    if (msg.mesh_resource.StartsWith(packagePrefix))
                    {
                        string resourcePath = msg.mesh_resource.Substring(packagePrefix.Length);
                        return Resource.Markers.Generic.TryGetValue(resourcePath, out Resource.Info info) ? info : null;
                    }
                    return null;
                case MarkerType.CUBE_LIST:
                case MarkerType.SPHERE_LIST:
                    return Resource.Markers.MeshList;
                case MarkerType.POINTS:
                    return Resource.Markers.PointList;
                case MarkerType.TRIANGLE_LIST:
                    return Resource.Markers.MeshTriangles;
                default:
                    return null;
            }
        }

        public override void Stop()
        {
            base.Stop();
            if (resource == null)
            {
                return;
            }
            resource.ColliderEnabled = false;
            ResourcePool.Dispose(resourceType, resource.gameObject);
            resource = null;
            resourceType = null;
            Clicked = null;
        }

        /*
        public void EnableColliders(bool b)
        {
            resource.ColliderEnabled = b;
        }
        */

        public bool ColliderEnabled
        {
            get => resource?.ColliderEnabled ?? false;
            set
            {
                if (resource != null)
                {
                    resource.ColliderEnabled = value;
                }
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.IsPointerMoving())
            {
                if (eventData.clickCount == 1)
                {
                    Clicked?.Invoke(eventData.pointerCurrentRaycast.worldPosition, 0);
                }
                else if (eventData.clickCount == 2)
                {
                    Clicked?.Invoke(eventData.pointerCurrentRaycast.worldPosition, 1);
                }
            }
            base.OnPointerClick(eventData);
        }
    }

    static class MarkerTypeHelper
    {
        public static MarkerType Type(this Marker marker)
        {
            return (MarkerType)marker.type;
        }
    }
}
