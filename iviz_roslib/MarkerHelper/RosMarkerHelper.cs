using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Iviz.Msgs.GeometryMsgs;
using Iviz.Msgs.StdMsgs;
using Iviz.Msgs.VisualizationMsgs;
using Iviz.Roslib;
using Iviz.XmlRpc;

namespace Iviz.Roslib.MarkerHelper
{
    public sealed class RosMarkerHelper : IDisposable
#if !NETSTANDARD2_0
        , IAsyncDisposable
#endif
    {
        static readonly Marker InvalidMarker = new Marker();

        readonly string ns;
        readonly List<Marker> markers = new List<Marker>();
        readonly RosChannelWriter<MarkerArray> publisher = new RosChannelWriter<MarkerArray> {LatchingEnabled = true};
        bool disposed;

        public string Topic => publisher.Topic;

        public RosMarkerHelper(string ns = "Marker")
        {
            this.ns = ns;
        }

        public RosMarkerHelper(RosClient client, string topic = "markers", string ns = "Marker") : this(ns)
        {
            Start(client, topic);
        }

        public override string ToString()
        {
            return $"[RosMarkerHelper ns={ns}]";
        }

        public void Start(RosClient client, string topic = "markers")
        {
            publisher.Start(client, topic);
        }

        public async Task StartAsync(RosClient client, string topic = "markers")
        {
            await publisher.StartAsync(client, topic);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (publisher.IsAlive)
            {
                Clear();
                ApplyChanges();
            }

            publisher.Dispose();
        }

        public async Task DisposeAsync()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (publisher.IsAlive)
            {
                Clear();
                await ApplyChangesAsync().AwaitNoThrow(this);
            }

            await publisher.DisposeAsync();
        }

#if !NETSTANDARD2_0
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await DisposeAsync();
        }
#endif

        int GetFreeId()
        {
            int index = markers.FindIndex(marker => marker == InvalidMarker);
            if (index != -1)
            {
                return index;
            }

            markers.Add(InvalidMarker);
            return markers.Count - 1;
        }

        public int CreateArrow(in Pose pose, in ColorRGBA color, in Vector3 scale, string frameId = "",
            int replaceId = -1)
        {
            int id = replaceId != -1 ? replaceId : GetFreeId();
            markers[id] = CreateArrow(ns, id, pose, color, scale, frameId);
            return id;
        }

        public static Marker CreateArrow(string? ns, int id, in Pose pose, in ColorRGBA color, in Vector3 scale,
            string? frameId)
        {
            return new Marker
            {
                Header = {FrameId = frameId ?? ""},
                Ns = ns ?? "",
                Id = id,
                Type = Marker.ARROW,
                Action = Marker.ADD,
                Pose = pose,
                Scale = scale,
                Color = color,
                FrameLocked = true,
            };
        }

        public int CreateArrow(in Point a, in Point b, in ColorRGBA color, string frameId = "",
            int replaceId = -1)
        {
            int id = replaceId != -1 ? replaceId : GetFreeId();
            markers[id] = CreateArrow(ns, id, a, b, color, frameId);
            return id;
        }

        public static Marker CreateArrow(string? ns, int id, in Point a, in Point b, in ColorRGBA color,
            string? frameId)
        {
            return new Marker
            {
                Header = {FrameId = frameId ?? ""},
                Ns = ns ?? "",
                Id = id,
                Type = Marker.ARROW,
                Action = Marker.ADD,
                Pose = Pose.Identity,
                Scale = Vector3.One,
                Color = color,
                FrameLocked = true,
                Points = new[] {a, b}
            };
        }

        public int CreateCube(Pose? pose = null, ColorRGBA? color = null, Vector3? scale = null, string frameId = "",
            int replaceId = -1)
        {
            int id = replaceId != -1 ? replaceId : GetFreeId();
            markers[id] = CreateCube(ns, id, pose, scale, color, frameId);
            return id;
        }

        public static Marker CreateCube(string? ns = null, int id = 0, Pose? pose = null, Vector3? scale = null,
            ColorRGBA? color = null, string? frameId = "")
        {
            return new Marker
            {
                Header = {FrameId = frameId ?? ""},
                Ns = ns ?? "",
                Id = id,
                Type = Marker.CUBE,
                Action = Marker.ADD,
                Pose = pose ?? Pose.Identity,
                Scale = scale ?? Vector3.One,
                Color = color ?? ColorRGBA.White,
                FrameLocked = true
            };
        }

        public int CreateSphere(Pose? pose = null, ColorRGBA? color = null, Vector3? scale = null, string frameId = "",
            int replaceId = -1)
        {
            int id = replaceId != -1 ? replaceId : GetFreeId();
            markers[id] = CreateSphere(ns, id, pose, scale, color, frameId);
            return id;
        }

        public static Marker CreateSphere(string? ns = "", int id = 0, Pose? pose = null, Vector3? scale = null,
            ColorRGBA? color = null, string? frameId = "")
        {
            return new Marker
            {
                Header = {FrameId = frameId ?? ""},
                Ns = ns ?? "",
                Id = id,
                Type = Marker.SPHERE,
                Action = Marker.ADD,
                Pose = pose ?? Pose.Identity,
                Scale = scale ?? Vector3.One,
                Color = color ?? ColorRGBA.White,
                FrameLocked = true
            };
        }

        public int CreateCylinder(in Point position, in ColorRGBA color, in Vector3 scale, string frameId = "",
            int replaceId = -1)
        {
            return CreateCylinder(new Pose(position, Quaternion.Identity), color, scale, frameId, replaceId);
        }

        public int CreateCylinder(in Pose pose, in ColorRGBA color, in Vector3 scale, string frameId = "",
            int replaceId = -1)
        {
            int id = replaceId != -1 ? replaceId : GetFreeId();
            markers[id] = CreateCylinder(ns, id, pose, scale, color, frameId);
            return id;
        }

        public static Marker CreateCylinder(string? ns, int id, in Pose pose, in Vector3 scale, in ColorRGBA color,
            string? frameId)
        {
            return new Marker
            {
                Header = {FrameId = frameId ?? ""},
                Ns = ns ?? "",
                Id = id,
                Type = Marker.CYLINDER,
                Action = Marker.ADD,
                Pose = pose,
                Scale = scale,
                Color = color,
                FrameLocked = true
            };
        }

        public int CreateTextViewFacing(string text, in Point position, in ColorRGBA color, double scale,
            string frameId = "",
            int replaceId = -1)
        {
            int id = replaceId != -1 ? replaceId : GetFreeId();
            Marker marker = new Marker
            {
                Header = {FrameId = frameId ?? ""},
                Ns = ns,
                Id = id,
                Type = Marker.TEXT_VIEW_FACING,
                Action = Marker.ADD,
                Pose = new Pose(position, Quaternion.Identity),
                Scale = scale * Vector3.One,
                Color = color,
                FrameLocked = true,
                Text = text
            };

            markers[id] = marker;
            return id;
        }

        public int CreateLines(Point[] lines, in Pose? pose = null, in ColorRGBA? color = null, double scale = 1,
            string frameId = "",
            int replaceId = -1)
        {
            int id = replaceId != -1 ? replaceId : GetFreeId();
            markers[id] = CreateLines(ns, id, lines, null, color, pose, scale, frameId);
            return id;
        }

        public int CreateLines(Point[] lines, ColorRGBA[] colors, in Pose? pose = null, double scale = 1,
            string frameId = "", int replaceId = -1)
        {
            if (colors == null)
            {
                throw new ArgumentNullException(nameof(colors));
            }

            int id = replaceId != -1 ? replaceId : GetFreeId();
            markers[id] = CreateLines(ns, id, lines, colors, ColorRGBA.White, pose, scale, frameId);
            return id;
        }

        public static Marker CreateLines(string? ns, int id, Point[] lines, ColorRGBA[]? colors = null,
            in ColorRGBA? color = null,
            in Pose? pose = null, double scale = 1, string frameId = "")
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }

            if (lines.Length % 2 != 0)
            {
                throw new ArgumentException("Number of points must be even", nameof(lines));
            }

            if (colors != null && colors.Length != lines.Length)
            {
                throw new ArgumentException("Number of points and colors must be equal", nameof(colors));
            }

            return new Marker
            {
                Header = {FrameId = frameId},
                Ns = ns ?? "",
                Id = id,
                Type = Marker.LINE_LIST,
                Action = Marker.ADD,
                Pose = pose ?? Pose.Identity,
                Scale = scale * Vector3.One,
                Color = color ?? ColorRGBA.White,
                Points = lines,
                Colors = colors ?? Array.Empty<ColorRGBA>(),
                FrameLocked = true
            };
        }

        public int CreateLineStrip(Point[] lines, Pose? pose = null, ColorRGBA? color = null, double scale = 1,
            string frameId = "",
            int replaceId = -1)
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }

            int id = replaceId != -1 ? replaceId : GetFreeId();
            markers[id] = CreateLineStrip(ns, id, lines, null, color, pose, scale, frameId);
            return id;
        }

        public int CreateLineStrip(Point[] lines, ColorRGBA[] colors, Pose? pose = null, double scale = 1,
            string frameId = "", int replaceId = -1)
        {
            if (colors == null)
            {
                throw new ArgumentNullException(nameof(colors));
            }

            int id = replaceId != -1 ? replaceId : GetFreeId();
            markers[id] = CreateLineStrip(ns, id, lines, colors, ColorRGBA.White, pose, scale, frameId);
            return id;
        }

        public static Marker CreateLineStrip(string? ns, int id, Point[] lines, ColorRGBA[]? colors = null,
            ColorRGBA? color = null,
            Pose? pose = null, double scale = 1, string? frameId = "")
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }

            if (colors != null && colors.Length != lines.Length)
            {
                throw new ArgumentException("Number of points and colors must be equal", nameof(colors));
            }

            return new Marker
            {
                Header = {FrameId = frameId ?? ""},
                Ns = ns ?? "",
                Id = id,
                Type = Marker.LINE_STRIP,
                Action = Marker.ADD,
                Pose = pose ?? Pose.Identity,
                Scale = scale * Vector3.One,
                Color = color ?? ColorRGBA.White,
                Points = lines,
                Colors = colors ?? Array.Empty<ColorRGBA>(),
                FrameLocked = true
            };
        }


        public void Erase(int id)
        {
            if (id < 0 || id >= markers.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            markers[id] = new Marker
            {
                Ns = ns,
                Id = id,
                Action = Marker.DELETE,
            };
        }

        public void Clear()
        {
            for (int id = 0; id < markers.Count; id++)
            {
                if (markers[id] == InvalidMarker)
                {
                    continue;
                }

                markers[id] = new Marker
                {
                    Ns = ns,
                    Id = id,
                    Action = Marker.DELETE,
                };
            }
        }

        public void SetPose(int id, in Pose pose, Header? header = null)
        {
            if (id < 0 || id >= markers.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            markers[id].Pose = pose;
            if (header != null)
            {
                markers[id].Header = header;
            }
        }

        public int Size => markers.Count(marker => marker != InvalidMarker);

        public void ApplyChanges()
        {
            if (!publisher.IsAlive)
            {
                throw new InvalidOperationException("Start has not been called!");
            }

            Marker[] toSend = markers.Where(marker => marker != InvalidMarker).ToArray();
            MarkerArray array = new MarkerArray(toSend);

            for (int id = 0; id < markers.Count; id++)
            {
                if (markers[id] != InvalidMarker && markers[id].Action == Marker.DELETE)
                {
                    markers[id] = InvalidMarker;
                }
            }

            publisher.Write(array);
        }

        public async Task ApplyChangesAsync()
        {
            if (!publisher.IsAlive)
            {
                throw new InvalidOperationException("Start has not been called!");
            }

            Marker[] toSend = markers.Where(marker => marker != InvalidMarker).ToArray();
            MarkerArray array = new MarkerArray(toSend);

            for (int id = 0; id < markers.Count; id++)
            {
                if (markers[id] != InvalidMarker && markers[id].Action == Marker.DELETE)
                {
                    markers[id] = InvalidMarker;
                }
            }

            await publisher.WriteAsync(array, RosPublishPolicy.WaitUntilSent);
        }
    }

    public enum RosInteractionMode
    {
        /// NONE: This control is only meant for visualization; no context menu.
        None = InteractiveMarkerControl.NONE,
        /// MENU: Like NONE, but right-click menu is active.
        Menu = InteractiveMarkerControl.MENU,
        /// BUTTON: Element can be left-clicked.
        Button = InteractiveMarkerControl.BUTTON,
        /// MOVE_AXIS: Translate along local x-axis.
        MoveAxis = InteractiveMarkerControl.MOVE_AXIS,
        /// MOVE_PLANE: Translate in local y-z plane.
        MovePlane = InteractiveMarkerControl.MOVE_PLANE,
        /// ROTATE_AXIS: Rotate around local x-axis.
        RotateAxis = InteractiveMarkerControl.ROTATE_AXIS,
        /// MOVE_ROTATE: Combines MOVE_PLANE and ROTATE_AXIS.
        MoveRotate = InteractiveMarkerControl.MOVE_ROTATE,
        /// MOVE_3D: Translate freely in 3D space.
        Move3D = InteractiveMarkerControl.MOVE_3D,
        /// ROTATE_3D: Rotate freely in 3D space about the origin of parent frame.
        Rotate3D = InteractiveMarkerControl.ROTATE_3D,
        /// MOVE_ROTATE_3D: Full 6-DOF freedom of translation and rotation about the cursor origin.
        MoveRotate3D = InteractiveMarkerControl.MOVE_ROTATE_3D
    }

    public enum RosEventType
    {
        /// KEEP_ALIVE: sent while dragging to keep up control of the marker
        KeepAlive = InteractiveMarkerFeedback.KEEP_ALIVE,
        /// POSE_UPDATE: the pose has been changed using one of the controls
        PoseUpdate = InteractiveMarkerFeedback.POSE_UPDATE,
        /// MENU_SELECT: a menu entry has been selected
        MenuSelect = InteractiveMarkerFeedback.MENU_SELECT,
        /// BUTTON_CLICK: a button control has been clicked
        ButtonClick = InteractiveMarkerFeedback.BUTTON_CLICK,
        MouseDown = InteractiveMarkerFeedback.MOUSE_DOWN,
        MouseUp = InteractiveMarkerFeedback.MOUSE_UP
    }


    public class RosInteractiveMarkerHelper
    {
        public static InteractiveMarker Create(string name, Pose? pose = null, string description = "", float scale = 1,
            string frameId = "", params InteractiveMarkerControl[] controls)
        {
            return new InteractiveMarker
            {
                Header = {FrameId = frameId},
                Name = name,
                Description = description,
                Pose = pose ?? Pose.Identity,
                Scale = scale,
                Controls = controls
            };
        }

        public static InteractiveMarkerControl CreateControl(string name = "", Quaternion? orientation = null,
            RosInteractionMode mode = RosInteractionMode.None, params Marker[] markers)
        {
            return new InteractiveMarkerControl
            {
                Name = name,
                Orientation = orientation ?? Quaternion.Identity,
                InteractionMode = (byte) mode,
                Markers = markers
            };
        }

        public static InteractiveMarkerUpdate CreateMarkerUpdate(params InteractiveMarker[] args)
        {
            return new InteractiveMarkerUpdate
            {
                Type = InteractiveMarkerUpdate.UPDATE,
                Markers = args
            };
        }

        public static InteractiveMarkerUpdate CreatePoseUpdate(params (string name, Pose pose)[] args)
        {
            return new InteractiveMarkerUpdate
            {
                Type = InteractiveMarkerUpdate.UPDATE,
                Poses = args.Select(tuple => new InteractiveMarkerPose {Name = tuple.name, Pose = tuple.pose}).ToArray()
            };
        }

        public static InteractiveMarkerInit CreateInit(params InteractiveMarker[] markers)
        {
            return new InteractiveMarkerInit
            {
                Markers = markers
            };
        }
    }
}