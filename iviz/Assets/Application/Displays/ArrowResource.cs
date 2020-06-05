﻿using UnityEngine;
using System.Collections;

namespace Iviz.Displays
{
    public class ArrowResource : MeshMarkerResource
    {
        public override string Name => "ArrowResource";

        public void Set(in Vector3 A, in Vector3 B)
        {
            Vector3 diff = A - B; // arrow model is flipped
            float scale = diff.magnitude;

            transform.localScale = scale * Vector3.one;
            transform.localPosition = A;

            if (scale == 0)
            {
                return;
            }
            
            Vector3 X = diff / scale;

            Vector3 up = new Vector3(0, 0, 1);
            if (X == up)
            {
                up = new Vector3(0, 1, 0);
            }

            Vector3 Y = Vector3.Cross(X, up).normalized;
            Vector3 Z = Vector3.Cross(X, Y).normalized;

            Matrix4x4 M = Matrix4x4.identity;
            M.SetColumn(0, X);
            M.SetColumn(1, Y);
            M.SetColumn(2, Z);

            transform.localRotation = M.rotation;
        }
    }
}
