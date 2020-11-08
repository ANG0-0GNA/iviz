﻿using System;
using System.Runtime.Serialization;
using Iviz.Core;
using Iviz.Displays;
using Iviz.Msgs.GeometryMsgs;
using Iviz.Msgs.SensorMsgs;
using Iviz.Resources;
using Iviz.Ros;
using Iviz.Roslib;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Iviz.Controllers
{
    [DataContract]
    public sealed class JoystickConfiguration : JsonToString, IConfiguration
    {
        [DataMember] public string JoyTopic { get; set; } = "";
        [DataMember] public bool PublishJoy { get; set; }
        [DataMember] public string TwistTopic { get; set; } = "";
        [DataMember] public bool PublishTwist { get; set; } = true;
        [DataMember] public bool UseTwistStamped { get; set; }
        [DataMember] public SerializableVector3 MaxSpeed { get; set; } = Vector3.one * 0.25f;
        [DataMember] public string AttachToFrame { get; set; } = "map";
        [DataMember] public bool XIsFront { get; set; } = true;
        [DataMember] public string Id { get; set; } = Guid.NewGuid().ToString();
        [DataMember] public Resource.Module Module => Resource.Module.Joystick;
        [DataMember] public bool Visible { get; set; } = true;
    }


    public sealed class JoystickController : IController
    {
        readonly JoystickConfiguration config = new JoystickConfiguration();
        uint joySeq;
        uint twistSeq;

        Joystick joystick;
        
        public JoystickController(IModuleData moduleData)
        {
            ModuleData = moduleData;
            Config = new JoystickConfiguration();
            GameThread.EveryFrame += PublishData;
        }

        public JoystickConfiguration Config
        {
            get => config;
            set
            {
                Visible = value.Visible;
                JoyTopic = value.JoyTopic;
                PublishJoy = value.PublishJoy;
                TwistTopic = value.TwistTopic;
                PublishTwist = value.PublishTwist;
                MaxSpeed = value.MaxSpeed;
                AttachToFrame = value.AttachToFrame;
                XIsFront = value.XIsFront;
            }
        }

        public string JoyTopic
        {
            get => config.JoyTopic;
            set
            {
                config.JoyTopic = string.IsNullOrEmpty(value) ? "joy" : value;
                RebuildJoy();
            }
        }

        public bool UseTwistStamped
        {
            get => config.UseTwistStamped;
            set
            {
                config.UseTwistStamped = value;
                RebuildTwist();
            }
        }

        public string TwistTopic
        {
            get => config.TwistTopic;
            set
            {
                config.TwistTopic = string.IsNullOrEmpty(value) ? "twist" : value;
                RebuildTwist();
            }
        }

        public Joystick Joystick
        {
            get => joystick;
            set
            {
                joystick = value;
                Joystick.Visible = Visible;
            }
        }

        public Sender<Joy> SenderJoy { get; private set; }
        public ISender SenderTwist { get; private set; }

        public bool Visible
        {
            get => config.Visible;
            set
            {
                config.Visible = value;
                if (!(Joystick is null))
                {
                    Joystick.Visible = value;
                }
            }
        }

        public bool PublishJoy
        {
            get => config.PublishJoy;
            set
            {
                config.PublishJoy = value;
                if (value && SenderJoy == null)
                {
                    RebuildJoy();
                }
                else if (!value && SenderJoy != null)
                {
                    SenderJoy.Stop();
                    SenderJoy = null;
                }
            }
        }

        public bool PublishTwist
        {
            get => config.PublishTwist;
            set
            {
                config.PublishTwist = value;
                if (value && SenderTwist == null)
                {
                    RebuildTwist();
                }
                else if (!value && SenderTwist != null)
                {
                    SenderTwist.Stop();
                    SenderTwist = null;
                }
            }
        }

        public string AttachToFrame
        {
            get => config.AttachToFrame;
            set => config.AttachToFrame = value;
        }

        public bool XIsFront
        {
            get => config.XIsFront;
            set => config.XIsFront = value;
        }

        public Vector3 MaxSpeed
        {
            get => config.MaxSpeed;
            set => config.MaxSpeed = value.HasNaN() ? Vector3.zero : value;
        }

        public IModuleData ModuleData { get; }

        public void StopController()
        {
            GameThread.EveryFrame -= PublishData;
            SenderJoy?.Stop();
            SenderTwist?.Stop();
            Visible = false;
        }

        public void ResetController()
        {
        }

        void RebuildJoy()
        {
            if (SenderJoy != null && SenderJoy.Topic != config.JoyTopic)
            {
                SenderJoy.Stop();
                SenderJoy = null;
            }

            if (SenderJoy is null)
            {
                SenderJoy = new Sender<Joy>(JoyTopic);
            }
        }

        void RebuildTwist()
        {
            string twistType = UseTwistStamped
                ? Msgs.GeometryMsgs.TwistStamped.RosMessageType
                : Twist.RosMessageType;

            if (SenderTwist != null &&
                (SenderTwist.Topic != config.TwistTopic || SenderTwist.Type != twistType))
            {
                SenderTwist.Stop();
                SenderTwist = null;
            }

            if (SenderTwist == null)
            {
                SenderTwist = UseTwistStamped
                    ? (ISender) new Sender<TwistStamped>(TwistTopic)
                    : new Sender<Twist>(TwistTopic);
            }
        }

        void PublishData()
        {
            if (Joystick is null)
            {
                return;
            }

            if (SenderTwist != null && Visible)
            {
                Vector2 leftDir = Joystick.Left;
                Vector2 rightDir = Joystick.Right;

                string frame = string.IsNullOrWhiteSpace(AttachToFrame) ? TfListener.BaseFrameId : AttachToFrame;

                Vector2 linear = XIsFront ? new Vector2(leftDir.y, -leftDir.x) : new Vector2(leftDir.x, leftDir.y);

                Twist twist = new Twist(
                    new Msgs.GeometryMsgs.Vector3(linear.x * MaxSpeed.x, linear.y * MaxSpeed.y, 0),
                    new Msgs.GeometryMsgs.Vector3(0, 0, -rightDir.x * MaxSpeed.z)
                );

                if (UseTwistStamped)
                {
                    TwistStamped twistStamped = new TwistStamped(
                        RosUtils.CreateHeader(twistSeq++, frame),
                        twist
                    );
                    SenderTwist.Publish(twistStamped);
                }
                else
                {
                    SenderTwist.Publish(twist);
                }
            }

            if (SenderJoy != null && Visible)
            {
                Vector2 leftDir = Joystick.Left;
                Vector2 rightDir = Joystick.Right;

                string frame = string.IsNullOrWhiteSpace(AttachToFrame) ? TfListener.BaseFrameId : AttachToFrame;

                Joy joy = new Joy(
                    RosUtils.CreateHeader(joySeq++, frame),
                    new[] {leftDir.x, leftDir.y, rightDir.x, rightDir.y},
                    Array.Empty<int>()
                );
                SenderJoy.Publish(joy);
            }
        }
    }
}