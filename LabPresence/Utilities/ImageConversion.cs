using System;
using System.IO;
using System.Reflection;

using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using UnityEngine;

namespace LabPresence.Utilities
{
    public static class ImageConversion
    {
        private const string NullTextureError = "The texture cannot be null.";

        private readonly static TextureAndFlagDelegate EncodeToEXRDelegateField = IL2CPP.ResolveICall<TextureAndFlagDelegate>("UnityEngine.ImageConversion::EncodeToEXR");
        private readonly static TextureOnlyDelegate EncodeToTGADelegateField = IL2CPP.ResolveICall<TextureOnlyDelegate>("UnityEngine.ImageConversion::EncodeToTGA");
        private readonly static TextureOnlyDelegate EncodeToPNGDelegateField = IL2CPP.ResolveICall<TextureOnlyDelegate>("UnityEngine.ImageConversion::EncodeToPNG");
        private readonly static TextureAndQualityDelegate EncodeToJPGDelegateField = IL2CPP.ResolveICall<TextureAndQualityDelegate>("UnityEngine.ImageConversion::EncodeToJPG");
        private readonly static LoadImageDelegate LoadImageDelegateField = IL2CPP.ResolveICall<LoadImageDelegate>("UnityEngine.ImageConversion::LoadImage");

        public static Il2CppStructArray<byte> EncodeToTGA(this Texture2D tex)
        {
            if (tex == null)
                throw new ArgumentNullException(nameof(tex), NullTextureError);
            if (EncodeToTGADelegateField == null)
                throw new InvalidOperationException("The EncodeToTGADelegateField cannot be null.");
            Il2CppStructArray<byte> il2CppStructArray;
            IntPtr encodeToTGADelegateField = EncodeToTGADelegateField(IL2CPP.Il2CppObjectBaseToPtr(tex));
            if (encodeToTGADelegateField != IntPtr.Zero)
                il2CppStructArray = new Il2CppStructArray<byte>(encodeToTGADelegateField);
            else
                il2CppStructArray = null;
            return il2CppStructArray;
        }

        public static Il2CppStructArray<byte> EncodeToPNG(this Texture2D tex)
        {
            if (tex == null)
                throw new ArgumentNullException(nameof(tex), NullTextureError);
            if (EncodeToPNGDelegateField == null)
                throw new InvalidOperationException("The EncodeToPNGDelegateField cannot be null.");
            Il2CppStructArray<byte> il2CppStructArray;
            IntPtr encodeToPNGDelegateField = EncodeToPNGDelegateField(IL2CPP.Il2CppObjectBaseToPtr(tex));
            if (encodeToPNGDelegateField != IntPtr.Zero)
                il2CppStructArray = new Il2CppStructArray<byte>(encodeToPNGDelegateField);
            else
                il2CppStructArray = null;
            return il2CppStructArray;
        }

        public static Il2CppStructArray<byte> EncodeToJPG(this Texture2D tex, int quality)
        {
            if (tex == null)
                throw new ArgumentNullException(nameof(tex), NullTextureError);
            if (EncodeToJPGDelegateField == null)
                throw new InvalidOperationException("The EncodeToJPGDelegateField cannot be null.");
            Il2CppStructArray<byte> il2CppStructArray;
            IntPtr encodeToJPGDelegateField = EncodeToJPGDelegateField(IL2CPP.Il2CppObjectBaseToPtr(tex), quality);
            if (encodeToJPGDelegateField != IntPtr.Zero)
                il2CppStructArray = new Il2CppStructArray<byte>(encodeToJPGDelegateField);
            else
                il2CppStructArray = null;
            return il2CppStructArray;
        }

        public static Il2CppStructArray<byte> EncodeToJPG(this Texture2D tex) => EncodeToJPG(tex, 75);

        public static Il2CppStructArray<byte> EncodeToEXR(this Texture2D tex, Texture2D.EXRFlags flags)
        {
            if (tex == null)
                throw new ArgumentNullException(nameof(tex), NullTextureError);
            if (EncodeToEXRDelegateField == null)
                throw new InvalidOperationException("The EncodeToEXRDelegateField cannot be null.");
            Il2CppStructArray<byte> il2CppStructArray;
            IntPtr encodeToEXRDelegateField = EncodeToEXRDelegateField(IL2CPP.Il2CppObjectBaseToPtr(tex), flags);
            if (encodeToEXRDelegateField != IntPtr.Zero)
                il2CppStructArray = new Il2CppStructArray<byte>(encodeToEXRDelegateField);
            else
                il2CppStructArray = null;
            return il2CppStructArray;
        }

        public static Il2CppStructArray<byte> EncodeToEXR(this Texture2D tex) => EncodeToEXR(tex, 0);

        public static bool LoadImage(this Texture2D tex, Il2CppStructArray<byte> data, bool markNonReadable)
        {
            if (tex == null)
                throw new ArgumentNullException(nameof(tex), NullTextureError);
            if (data == null)
                throw new ArgumentNullException(nameof(data), "The data cannot be null.");
            if (LoadImageDelegateField == null)
                throw new InvalidOperationException("The LoadImageDelegateField cannot be null.");
            return LoadImageDelegateField(IL2CPP.Il2CppObjectBaseToPtr(tex), IL2CPP.Il2CppObjectBaseToPtr(data), markNonReadable);
        }

        public static bool LoadImage(this Texture2D tex, Il2CppStructArray<byte> data) => LoadImage(tex, data, false);

        public static Texture2D LoadTexture(string name, byte[] bytes)
        {
            var texture2d = new Texture2D(2, 2);
            texture2d.LoadImage(bytes, false);
            texture2d.name = name;
            texture2d.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return texture2d;
        }

        private delegate IntPtr TextureOnlyDelegate(IntPtr tex);

        private delegate IntPtr TextureAndQualityDelegate(IntPtr tex, int quality);

        private delegate IntPtr TextureAndFlagDelegate(IntPtr tex, Texture2D.EXRFlags flags);

        private delegate bool LoadImageDelegate(IntPtr tex, IntPtr data, bool markNonReadable);
    }
}