﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Iviz.Msgs;
using Iviz.Msgs.StdMsgs;
using System.Runtime.Serialization;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Iviz.Controllers;
using Unity.Collections;
using Unity.Mathematics;

namespace Iviz
{
    [DataContract]
    public struct SerializableColor
    {
        [DataMember] public float R { get; set; }
        [DataMember] public float G { get; set; }
        [DataMember] public float B { get; set; }
        [DataMember] public float A { get; set; }

        public SerializableColor(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static implicit operator Color(in SerializableColor i)
        {
            return new Color(i.R, i.G, i.B, i.A);
        }

        public static implicit operator SerializableColor(in Color color)
        {
            return new SerializableColor(
                r: color.r,
                g: color.g,
                b: color.b,
                a: color.a
            );
        }
    }

    [DataContract]
    public struct SerializableVector3
    {
        [DataMember] public float X { get; set; }
        [DataMember] public float Y { get; set; }
        [DataMember] public float Z { get; set; }

        public SerializableVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static implicit operator Vector3(in SerializableVector3 i)
        {
            return new Vector3(i.X, i.Y, i.Z);
        }

        public static implicit operator SerializableVector3(in Vector3 v)
        {
            return new SerializableVector3(
                x: v.x,
                y: v.y,
                z: v.z
            );
        }
    }

    public static class RosUtils
    {
        //----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Unity2Ros(this Vector3 vector3) => new Vector3(vector3.z, -vector3.x, vector3.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Ros2Unity(this Vector3 vector3) => new Vector3(-vector3.y, vector3.z, vector3.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Ros2Unity(this Quaternion quaternion) =>
            new Quaternion(quaternion.y, -quaternion.z, -quaternion.x, quaternion.w);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Quaternion Unity2Ros(this Quaternion quaternion) =>
            new Quaternion(-quaternion.z, quaternion.x, -quaternion.y, quaternion.w);
        //----

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 Ros2Unity(this float4 v) => new float4(-v.y, v.z, v.x, v.w);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion RosRpy2Unity(this Vector3 v) => Quaternion.Euler(v.Ros2Unity() * -Mathf.Rad2Deg);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3 ToUnity(this Msgs.GeometryMsgs.Vector3 p)
        {
            return new Vector3((float) p.X, (float) p.Y, (float) p.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3 ToUnity(this Msgs.GeometryMsgs.Point32 p)
        {
            return new Vector3(p.X, p.Y, p.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Ros2Unity(this Msgs.GeometryMsgs.Vector3 p)
        {
            return p.ToUnity().Ros2Unity();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Ros2Unity(this Msgs.GeometryMsgs.Point32 p)
        {
            return p.ToUnity().Ros2Unity();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Abs(this Vector3 p)
        {
            return new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 CwiseProduct(this Vector3 p, Vector3 o)
        {
            return Vector3.Scale(p, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Msgs.GeometryMsgs.Vector3 ToRosVector3(this Vector3 p)
        {
            return new Msgs.GeometryMsgs.Vector3(p.x, p.y, p.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Msgs.GeometryMsgs.Vector3 Unity2RosVector3(this Vector3 p)
        {
            return ToRosVector3(p.Unity2Ros());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3 ToUnity(this Msgs.GeometryMsgs.Point p)
        {
            return new Vector3((float) p.X, (float) p.Y, (float) p.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Ros2Unity(this Msgs.GeometryMsgs.Point p)
        {
            return p.ToUnity().Ros2Unity();
        }

        public static Color ToUnityColor(this ColorRGBA p)
        {
            return new Color(p.R, p.G, p.B, p.A);
        }

        public static ColorRGBA Sanitize(this ColorRGBA p)
        {
            return new ColorRGBA
            (
                R: SanitizeColor(p.R),
                G: SanitizeColor(p.G),
                B: SanitizeColor(p.B),
                A: SanitizeColor(p.A)
            );
        }

        static float SanitizeColor(float f)
        {
            return float.IsNaN(f) ? 0 : Mathf.Max(Mathf.Min(f, 1), 0);
        }

        public static Color32 ToUnityColor32(this ColorRGBA p)
        {
            return p.ToUnityColor(); // ColorRGBA -> Color -> Color32
            // note: Color -> Color32 sanitizes implicitly 
        }

        public static ColorRGBA ToRos(this Color p)
        {
            return new ColorRGBA(p.r, p.g, p.b, p.a);
        }

        public static ColorRGBA ToRos(this Color32 p)
        {
            return new ColorRGBA
            (
                R: p.r / 255f,
                G: p.g / 255f,
                B: p.b / 255f,
                A: p.a / 255f
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Msgs.GeometryMsgs.Point ToRosPoint(this Vector3 p)
        {
            return new Msgs.GeometryMsgs.Point
            (
                X: p.x,
                Y: p.y,
                Z: p.z
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Msgs.GeometryMsgs.Point Unity2RosPoint(this Vector3 p)
        {
            return ToRosPoint(p.Unity2Ros());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Quaternion ToUnity(this Msgs.GeometryMsgs.Quaternion p)
        {
            return new Quaternion((float) p.X, (float) p.Y, (float) p.Z, (float) p.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Ros2Unity(this Msgs.GeometryMsgs.Quaternion p)
        {
            return p.ToUnity().Ros2Unity();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Msgs.GeometryMsgs.Quaternion ToRos(this Quaternion p)
        {
            return new Msgs.GeometryMsgs.Quaternion
            (
                X: p.x,
                Y: p.y,
                Z: p.z,
                W: p.w
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Msgs.GeometryMsgs.Quaternion Unity2RosQuaternion(this Quaternion p)
        {
            return ToRos(p.Unity2Ros());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Pose ToUnity(this Msgs.GeometryMsgs.Transform pose)
        {
            return new Pose(pose.Translation.ToUnity(), pose.Rotation.ToUnity());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose Ros2Unity(this Msgs.GeometryMsgs.Transform pose)
        {
            return new Pose(pose.Translation.Ros2Unity(), pose.Rotation.Ros2Unity());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Pose ToUnity(this Msgs.GeometryMsgs.Pose pose)
        {
            return new Pose(pose.Position.ToUnity(), pose.Orientation.ToUnity());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose Ros2Unity(this Msgs.GeometryMsgs.Pose pose)
        {
            return new Pose(pose.Position.Ros2Unity(), pose.Orientation.Ros2Unity());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Msgs.GeometryMsgs.Transform Unity2RosTransform(this Pose p)
        {
            return new Msgs.GeometryMsgs.Transform(p.position.Unity2RosVector3(), p.rotation.Unity2RosQuaternion());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Msgs.GeometryMsgs.Pose Unity2RosPose(this Pose p)
        {
            return new Msgs.GeometryMsgs.Pose(p.position.Unity2RosPoint(), p.rotation.Unity2RosQuaternion());
        }

        public static Header CreateHeader(uint seq = 0, string frameId = null)
        {
            return new Header(seq, new time(DateTime.Now), frameId ?? TFListener.BaseFrameId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNaN(this float4 v) => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNaN(this Vector3 v) => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNaN(this Msgs.GeometryMsgs.Vector3 v)
        {
            return double.IsNaN(v.X) || double.IsNaN(v.Y) || double.IsNaN(v.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNaN(this Msgs.GeometryMsgs.Point v)
        {
            return double.IsNaN(v.X) || double.IsNaN(v.Y) || double.IsNaN(v.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNaN(this Msgs.GeometryMsgs.Point32 v)
        {
            return float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNaN(this Msgs.GeometryMsgs.Quaternion v)
        {
            return double.IsNaN(v.X) || double.IsNaN(v.Y) || double.IsNaN(v.Z) || double.IsNaN(v.W);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNaN(this Msgs.GeometryMsgs.Transform transform)
        {
            return HasNaN(transform.Rotation) || HasNaN(transform.Translation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNaN(this Msgs.GeometryMsgs.Pose pose)
        {
            return HasNaN(pose.Orientation) || HasNaN(pose.Position);
        }
    }

    public static class UnityUtils
    {
        public static CultureInfo Culture { get; } = BuiltIns.Culture;

        public static bool TryParse(string s, out float f)
        {
            if (float.TryParse(s, NumberStyles.Any, Culture, out f))
            {
                return true;
            }

            f = 0;
            return false;
        }

        public static Pose AsPose(this Transform t)
        {
            return new Pose(t.position, t.rotation);
        }

        public static Pose AsLocalPose(this Transform t)
        {
            return new Pose(t.localPosition, t.localRotation);
        }

        public static Vector3 Multiply(this Pose p, in Vector3 v)
        {
            return p.rotation * v + p.position;
        }

        public static Pose Multiply(this Pose p, in Pose o)
        {
            return new Pose
            (
                rotation: p.rotation * o.rotation,
                position: p.rotation * o.position + p.position
            );
        }

        public static void SetPose(this Transform t, in Pose p)
        {
            t.SetPositionAndRotation(p.position, p.rotation);
        }

        public static void SetParentLocal(this Transform t, Transform parent)
        {
            t.SetParent(parent, false);
        }

        public static void SetLocalPose(this Transform t, in Pose p)
        {
            t.localPosition = p.position;
            t.localRotation = p.rotation;
        }


        public static Pose Inverse(this Pose p)
        {
            Quaternion q = Quaternion.Inverse(p.rotation);
            return new Pose(
                rotation: q,
                position: q * -p.position
            );
        }

        public static Pose Lerp(this Pose p, in Pose o, float t)
        {
            return new Pose(
                Vector3.Lerp(p.position, o.position, t),
                Quaternion.Lerp(p.rotation, o.rotation, t)
            );
        }

        public static Pose Lerp(this Transform p, in Pose o, float t)
        {
            return new Pose(
                Vector3.Lerp(p.position, o.position, t),
                Quaternion.Lerp(p.rotation, o.rotation, t)
            );
        }

        public static Pose LocalLerp(this Transform p, in Pose o, float t)
        {
            return new Pose(
                Vector3.Lerp(p.localPosition, o.position, t),
                Quaternion.Lerp(p.localRotation, o.rotation, t)
            );
        }

        public static void ForEach<T>(this IEnumerable<T> col, Action<T> action)
        {
            foreach (var item in col)
            {
                action(item);
            }
        }

        public static void ForEach<T>(this T[] col, Action<T> action)
        {
            foreach (var t in col)
            {
                action(t);
            }
        }

        public static void ForEach<T>(this IList<T> col, Action<T> action)
        {
            foreach (var t in col)
            {
                action(t);
            }
        }

        public static ArraySegment<T> AsSlice<T>(this T[] ts)
        {
            return new ArraySegment<T>(ts);
        }

        public static ArraySegment<T> AsSlice<T>(this T[] ts, int offset)
        {
            return new ArraySegment<T>(ts, offset, ts.Length - offset);
        }        
        
        public static ArraySegment<T> AsSlice<T>(this T[] ts, int offset, int count)
        {
            return new ArraySegment<T>(ts, offset, count);
        }        

        static MaterialPropertyBlock propBlock;
        static readonly int ColorPropId = Shader.PropertyToID("_Color");

        public static void SetPropertyColor(this MeshRenderer meshRenderer, Color color, int id = 0)
        {
            if (propBlock == null)
            {
                propBlock = new MaterialPropertyBlock();
            }

            meshRenderer.GetPropertyBlock(propBlock, id);
            propBlock.SetColor(ColorPropId, color);
            meshRenderer.SetPropertyBlock(propBlock, id);
        }

        static readonly int EmissiveColorPropId = Shader.PropertyToID("_EmissiveColor");

        public static void SetPropertyEmissiveColor(this MeshRenderer meshRenderer, Color color, int id = 0)
        {
            if (propBlock == null)
            {
                propBlock = new MaterialPropertyBlock();
            }

            meshRenderer.GetPropertyBlock(propBlock, id);
            propBlock.SetColor(EmissiveColorPropId, color);
            meshRenderer.SetPropertyBlock(propBlock, id);
        }

        static readonly int MainTexStPropId = Shader.PropertyToID("_MainTex_ST_");

        public static void SetPropertyMainTexST(
            this MeshRenderer meshRenderer, 
            in Vector2 xy, 
            in Vector2 wh,
            int id = 0)
        {
            if (propBlock == null)
            {
                propBlock = new MaterialPropertyBlock();
            }

            meshRenderer.GetPropertyBlock(propBlock, id);
            propBlock.SetVector(MainTexStPropId, new Vector4(wh.x, wh.y, xy.x, xy.y));
            meshRenderer.SetPropertyBlock(propBlock, id);
        }
        
        struct NativeArrayHelper<T> : IList<T>, IReadOnlyList<T> where T : struct
        {
            NativeArray<T> nArray;
            
            public NativeArrayHelper(in NativeArray<T> array) => nArray = array;
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => nArray.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => nArray.GetEnumerator();
            public NativeArray<T>.Enumerator GetEnumerator() => nArray.GetEnumerator();
            public void Add(T item) => throw new InvalidOperationException();
            public void Clear() => throw new InvalidOperationException();
            public bool Contains(T item) => nArray.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => array.CopyTo(array, arrayIndex);
            public bool Remove(T item) => throw new InvalidOperationException();
            public int Count => nArray.Length;
            public bool IsReadOnly => false;
            public int IndexOf(T item) => throw new InvalidOperationException();
            public void Insert(int index, T item) => throw new InvalidOperationException();
            public void RemoveAt(int index) => throw new InvalidOperationException();
            
            public T this[int index] {
                get => nArray[index];
                set => nArray[index] = value;
            }
        }        

        public static IList<T> AsList<T>(this NativeArray<T> array) where T : struct => 
            new NativeArrayHelper<T>(array);

        public static IReadOnlyList<T> AsReadOnlyList<T>(this NativeArray<T> array) where T : struct => 
            new NativeArrayHelper<T>(array);
    }
    
    public static class UrdfUtils
    {
        public static Vector3 ToVector3(this Urdf.Vector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z).Ros2Unity();
        }

        static Quaternion ToQuaternion(this Urdf.Vector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z).RosRpy2Unity();
        }

        public static Pose ToPose(this Urdf.Origin v)
        {
            return new Pose(v.Xyz.ToVector3(), v.Rpy.ToQuaternion());
        }

        public static Color ToColor(this Urdf.Color v)
        {
            return new Color(v.Rgba.R, v.Rgba.G, v.Rgba.B, v.Rgba.A);
        }

        public static bool IsReference(this Urdf.Material material)
        {
            return material.Color is null && material.Texture is null && !(material.Name is null);
        }
    }
    
    public static class SdfUtils
    {
        public static Vector3 ToVector3(this Sdf.Vector3 v)
        {
            return new Vector3((float)v.X, (float)v.Y, (float)v.Z).Ros2Unity();
        }

        static Quaternion ToQuaternion(this Sdf.Vector3 v)
        {
            return new Vector3((float)v.X, (float)v.Y, (float)v.Z).RosRpy2Unity();
        }

        public static Pose ToPose(this Sdf.Pose v)
        {
            return new Pose(v.Position.ToVector3(), v.Orientation.ToQuaternion());
        }

        public static Color ToColor(this Sdf.Color v)
        {
            return new Color((float)v.R, (float)v.G, (float)v.B, (float)v.A);
        }
    }
}