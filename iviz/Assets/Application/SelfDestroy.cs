﻿using UnityEngine;

namespace Iviz.App
{
    public class SelfDestroy : MonoBehaviour
    {
        float time;

        void Start()
        {
            time = Time.realtimeSinceStartup;
        }

        void Update()
        {
            float actualTime = Time.realtimeSinceStartup;
            if (actualTime - time > 1)
            {
                Destroy(gameObject);
            }
        }
    }
}
