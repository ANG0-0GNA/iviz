﻿using System;
using System.IO;
using System.Threading.Tasks;
using BitMiracle.LibJpeg;
using Iviz.Resources;
using Unity.Collections;
using UnityEngine;
using Logger = Iviz.Controllers.Logger;

namespace Iviz.Displays
{
    public sealed class ImageTexture
    {
        static readonly int PropIntensityCoeff = MarkerResourceWithColormap.PropIntensityCoeff;
        static readonly int PropIntensityAdd = MarkerResourceWithColormap.PropIntensityAdd;
        static readonly int PropIntensity = MarkerResourceWithColormap.PropIntensity;


        byte[] rgbaBuffer;
        byte[] pngBuffer;

        public event Action<Texture2D> TextureChanged;
        public event Action<Texture2D> ColormapChanged;

        Vector2 intensityBounds;
        public Vector2 IntensityBounds
        {
            get => intensityBounds;
            set
            {
                intensityBounds = value;
                float intensitySpan = intensityBounds.y - intensityBounds.x;

                if (intensitySpan == 0)
                {
                    Material.SetFloat(PropIntensityCoeff, 1);
                    Material.SetFloat(PropIntensityAdd, 0);
                }
                else
                {
                    if (!FlipMinMax)
                    {
                        Material.SetFloat(PropIntensityCoeff, 1 / intensitySpan);
                        Material.SetFloat(PropIntensityAdd, -intensityBounds.x / intensitySpan);
                    }
                    else
                    {
                        Material.SetFloat(PropIntensityCoeff, -1 / intensitySpan);
                        Material.SetFloat(PropIntensityAdd, intensityBounds.y / intensitySpan);
                    }
                }
            }
        }

        bool flipMinMax;
        public bool FlipMinMax
        {
            get => flipMinMax;
            set
            {
                flipMinMax = value;
                IntensityBounds = IntensityBounds;
            }
        }

        public Texture2D Texture { get; private set; }
        public Material Material { get; }
        public string Description { get; private set; }
        public bool IsMono { get; private set; }
        public int Width => Texture?.width ?? 0;
        public int Height => Texture?.height ?? 0;

        Resource.ColormapId colormap;
        public Resource.ColormapId Colormap
        {
            get => colormap;
            set
            {
                colormap = value;

                Material.SetTexture(PropIntensity, ColormapTexture);
                ColormapChanged?.Invoke(ColormapTexture);
            }
        }

        public Texture2D ColormapTexture => Resource.Colormaps.Textures[Colormap];

        public ImageTexture()
        {
            Material = Resource.Materials.ImagePreview.Instantiate();
        }

        static int FieldSizeFromEncoding(string encoding)
        {
            switch (encoding)
            {
                case "rgba8":
                case "bgra8":
                case "8SC4":
                    return 4;
                case "rgb8":
                case "bgr8":
                case "8SC3":
                    return 3;
                case "mono16":
                case "16UC1":
                    return 2;
                case "mono8":
                case "8UC1":
                    return 1;
                default:
                    return -1;
            }
        }

        static string EncodingFromPng(BigGustave.Png png)
        {
            switch (png.Header.ColorType)
            {
                case BigGustave.ColorType.None:
                    switch (png.Header.BitDepth)
                    {
                        case 8: return "mono8";
                        case 16: return "mono16";
                    }
                    break;
                case BigGustave.ColorType.ColorUsed:
                    switch (png.Header.BitDepth)
                    {
                        case 8: return "rgb8";
                        case 16: return "rgb16";
                    }
                    break;
                case BigGustave.ColorType.AlphaChannelUsed:
                    switch (png.Header.BitDepth)
                    {
                        case 8: return "rgba8";
                        case 16: return "rgba16";
                    }
                    break;
            }
            return null;
        }

        public void SetPng(byte[] data)
        {
            Task.Run(() =>
            {
                try
                {
                    byte[] newData;
                    BigGustave.Png png = BigGustave.Png.Open(data);

                    if (png.RowOffset != 0)
                    {
                        int reqSize = png.Height * png.RowSize;
                        if (pngBuffer == null || pngBuffer.Length < reqSize)
                        {
                            pngBuffer = new byte[reqSize];
                        }

                        int srcOffset = png.RowOffset;
                        int dstOffset = 0;
                        int rowSize = png.RowSize;
                        for (int i = png.Height; i != 0; i--, srcOffset += png.RowStep, dstOffset += rowSize)
                        {
                            Buffer.BlockCopy(png.Data, srcOffset, pngBuffer, dstOffset, rowSize);
                        }
                        newData = pngBuffer;
                    }
                    else
                    {
                        newData = png.Data;
                    }
                    GameThread.RunOnce(() => Set(png.Width, png.Height, EncodingFromPng(png), newData));
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            });
        }

        public void SetJpg(byte[] data)
        {
            Task.Run(() =>
            {
                try
                {
                    Stream inStream = new MemoryStream(data);
                    var image = new JpegImage(inStream);

                    string encoding = null; ;
                    int reqSize = image.Height * image.Width;
                    if (image.Colorspace == Colorspace.RGB && image.BitsPerComponent == 8)
                    {
                        if (image.Width % 4 != 0)
                        {
                            Logger.Debug("ImageListener: Row padding not implemented");
                            return;
                        }
                        encoding = "rgb";
                        reqSize *= 3;
                    }
                    else if (image.Colorspace == Colorspace.Grayscale && image.BitsPerComponent == 8)
                    {
                        if (image.Width % 4 != 0)
                        {
                            Logger.Debug("ImageListener: Row padding not implemented");
                            return;
                        }
                        encoding = "mono8";
                    }
                    else if (image.Colorspace == Colorspace.Grayscale && image.BitsPerComponent == 16)
                    {
                        if (image.Width % 2 != 0)
                        {
                            Logger.Debug("ImageListener: Row padding not implemented");
                            return;
                        }
                        encoding = "mono16";
                        reqSize *= 2;
                    }
                    if (encoding == null)
                    {
                        Logger.Debug("ImageListener: Unsupported encoding '" + image.Colorspace + "' with size " + image.BitsPerComponent);
                        return;
                    }

                    const int bmpHeaderLength = 54;
                    reqSize += bmpHeaderLength;

                    if (pngBuffer == null || pngBuffer.Length < reqSize)
                    {
                        pngBuffer = new byte[reqSize];
                    }

                    Stream outStream = new MemoryStream(pngBuffer);
                    image.WriteBitmap(outStream);
                    GameThread.RunOnce(() => Set(image.Width, image.Height, encoding, pngBuffer, bmpHeaderLength));
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            });
        }


        public void Set(int width, int height, string encoding, byte[] data, int dataStart = 0)
        {
            int size = width * height;
            int bpp = FieldSizeFromEncoding(encoding);

            if (bpp == -1)
            {
                Logger.Debug("ImageListener: Unsupported encoding '" + encoding + "'");
                return;
            }
            else if (data.Length < size * bpp)
            {
                Logger.Debug($"ImageListener: Invalid image! Expected at least {size * bpp} bytes, received {data.Length}");
                return;
            }

            Description = $"<b>Desc:</b>: {width}x{height} {encoding}";

            switch (encoding)
            {
                case "rgba8":
                case "8SC4":
                    IsMono = false;
                    Material.DisableKeyword("USE_INTENSITY");
                    Material.DisableKeyword("FLIP_RB");
                    ApplyTexture(width, height, data, encoding, size * 4, dataStart);
                    break;
                case "bgra8":
                    IsMono = false;
                    Material.DisableKeyword("USE_INTENSITY");
                    Material.EnableKeyword("FLIP_RB");
                    ApplyTexture(width, height, data, encoding, size * 4, dataStart);
                    break;
                case "rgb8":
                case "8SC3":
                    IsMono = false;
                    Material.DisableKeyword("USE_INTENSITY");
                    Material.DisableKeyword("FLIP_RB");
                    ApplyTexture(width, height, data, encoding, size * 3, dataStart);
                    /*
                    Task.Run(() =>
                    {
                        FillRGBABuffer(width, height, data);
                        GameThread.RunOnce(() =>
                        {
                            ApplyTexture(width, height, rgbaBuffer, encoding, size * 4);
                            IsMono = false;
                            Material.DisableKeyword("USE_INTENSITY");
                            Material.DisableKeyword("FLIP_RB");
                        });
                    });
                    */
                    break;
                case "bgr8":
                    IsMono = false;
                    Material.DisableKeyword("USE_INTENSITY");
                    Material.EnableKeyword("FLIP_RB");
                    ApplyTexture(width, height, data, encoding, size * 3, dataStart);
                    /*
                    Task.Run(() =>
                    {
                        FillRGBABuffer(width, height, data);
                        GameThread.RunOnce(() =>
                        {
                            ApplyTexture(width, height, rgbaBuffer, encoding, size * 4);
                            IsMono = false;
                            Material.DisableKeyword("USE_INTENSITY");
                            Material.EnableKeyword("FLIP_RB");
                        });
                    });
                    */
                    break;
                case "mono16":
                case "16UC1":
                    IsMono = true;
                    Material.EnableKeyword("USE_INTENSITY");
                    ApplyTexture(width, height, data, encoding, size * 2, dataStart);
                    break;
                case "mono8":
                case "8UC1":
                    IsMono = true;
                    Material.EnableKeyword("USE_INTENSITY");
                    ApplyTexture(width, height, data, encoding, size, dataStart);
                    break;
            }
        }


        void FillRGBABuffer(int width, int height, byte[] data)
        {
            int size = width * height;
            if (rgbaBuffer == null || rgbaBuffer.Length < size * 4)
            {
                rgbaBuffer = new byte[size * 4];
            }
            unsafe
            {
                fixed (byte* tmpBufferPtr = rgbaBuffer, dataPtr = data)
                {
                    byte* tmpOff = tmpBufferPtr, dataOff = dataPtr;
                    for (int i = size; i > 0; i--)
                    {
                        *tmpOff++ = *dataOff++;
                        *tmpOff++ = *dataOff++;
                        *tmpOff++ = *dataOff++;
                        *tmpOff++ = 255;
                    }
                }
            }
        }

        void ApplyTexture(int width, int height, byte[] data, string type, int length, int dataStart)
        {
            switch (type)
            {
                case "rgb8":
                case "bgr8":
                case "8SC3":
                    EnsureSize(width, height, TextureFormat.RGB24);
                    break;
                case "rgba8":
                case "bgra8":
                case "8SC4":
                    EnsureSize(width, height, TextureFormat.RGBA32);
                    break;
                case "mono16":
                case "16UC1":
                    EnsureSize(width, height, TextureFormat.R16);
                    break;
                case "mono8":
                case "8UC1":
                    EnsureSize(width, height, TextureFormat.R8);
                    break;
                default:
                    return;
            }
            NativeArray<byte>.Copy(data, dataStart, Texture.GetRawTextureData<byte>(), 0, length);
            Texture.Apply(false, false);
        }

        void EnsureSize(int width, int height, TextureFormat format)
        {
            if (Texture == null ||
                Texture.width != width ||
                Texture.height != height ||
                Texture.format != format)
            {
                if (Texture != null)
                {
                    UnityEngine.Object.Destroy(Texture);
                }
                Texture = new Texture2D(width, height, format, false);
                Material.SetTexture("_MainTex", Texture);
                TextureChanged?.Invoke(Texture);
            }
        }

        public void Stop()
        {
            TextureChanged?.Invoke(null);
            TextureChanged = null;
            ColormapChanged?.Invoke(null);
            ColormapChanged = null;
        }

        public void Destroy()
        {
            if (Texture != null) UnityEngine.Object.Destroy(Texture);
            if (Material != null) UnityEngine.Object.Destroy(Material);
        }
    }

}