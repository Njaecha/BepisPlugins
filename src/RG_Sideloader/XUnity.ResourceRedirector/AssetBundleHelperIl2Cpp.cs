﻿using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using XUnity.Common.Logging;
using XUnity.Common.Utilities;

namespace XUnity.ResourceRedirector
{
    /// <summary>
    /// Utility methods for AssetBundles.
    /// </summary>
    public static class AssetBundleHelperIl2Cpp
    {
        private static readonly PropertyInfo PropertiesResourcesEmptyProperty = typeof(AssetBundleHelper).Assembly.GetType($"{nameof(XUnity)}.{nameof(ResourceRedirector)}.{nameof(Properties)}.Resources").GetProperty("empty", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo PathForLoadedInMemoryBundleField = typeof(AssetBundleHelper).GetField("PathForLoadedInMemoryBundle", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        private static byte[] Properties_Resources_empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PropertiesResourcesEmptyProperty.GetValue(null) as byte[];
        }

        private static string PathForLoadedInMemoryBundle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PathForLoadedInMemoryBundleField.GetValue(null) as string;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => PathForLoadedInMemoryBundleField.SetValue(null, value);
        }

        /// <summary>
        /// Creates an empty AssetBundle with a randomly generated CAB identifier.
        /// </summary>
        /// <returns>The empty asset bundle with a random CAB identifier.</returns>
        public static AssetBundle CreateEmptyAssetBundle()
        {
            var buffer = Properties_Resources_empty;
            CabHelper.RandomizeCab(buffer);
            return AssetBundle.LoadFromMemory(buffer);
        }

        /// <summary>
        /// Creates an empty AssetBundle request with a randomly generated CAB identifier.
        /// </summary>
        /// <returns>The asset bundle request with a random CAB identifier.</returns>
        public static AssetBundleCreateRequest CreateEmptyAssetBundleRequest()
        {
            var buffer = Properties_Resources_empty;
            CabHelper.RandomizeCab(buffer);
            return AssetBundle.LoadFromMemoryAsync(buffer);
        }

        /// <summary>
        /// Convenience method to maintain a name of an asset bundle being loaded through
        /// memory for logging purposes.
        /// </summary>
        /// <param name="path">Path to the asset bundle being loaded. Only used for logging.</param>
        /// <param name="binary">Binary data of the asset bundle being loaded.</param>
        /// <param name="crc">Crc of the asset bundle.</param>
        /// <returns>The loaded asset bundle.</returns>
        public static AssetBundle LoadFromMemory(string path, byte[] binary, uint crc)
        {
            try
            {
                PathForLoadedInMemoryBundle = path;

                return AssetBundle.LoadFromMemory(binary, crc);
            }
            finally
            {
                PathForLoadedInMemoryBundle = null;
            }
        }

        /// <summary>
        /// Convenience method to maintain a name of an asset bundle being loaded through
        /// memory for logging purposes.
        /// </summary>
        /// <param name="path">Path to the asset bundle being loaded. Only used for logging.</param>
        /// <param name="binary">Binary data of the asset bundle being loaded.</param>
        /// <param name="crc">Crc of the asset bundle.</param>
        /// <returns>The request.</returns>
        public static AssetBundleCreateRequest LoadFromMemoryAsync(string path, byte[] binary, uint crc)
        {
            try
            {
                PathForLoadedInMemoryBundle = path;

                return AssetBundle.LoadFromMemoryAsync(binary, crc);
            }
            finally
            {
                PathForLoadedInMemoryBundle = null;
            }
        }

        /// <summary>
        /// Loads an asset bundle from a file. If loading fails, randomize the CAB and try again from memory.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="crc"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static AssetBundle LoadFromFileWithRandomizedCabIfRequired(string path, uint crc, ulong offset)
        {
            return LoadFromFileWithRandomizedCabIfRequired(path, crc, offset, true);
        }

        internal static AssetBundle LoadFromFileWithRandomizedCabIfRequired(string path, uint crc, ulong offset, bool confirmFileExists)
        {
            var bundle = AssetBundle.LoadFromFile(path, crc, offset);
            if (bundle == null && (!confirmFileExists || File.Exists(path)))
            {
                byte[] buffer;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var fullLength = stream.Length;
                    var longOffset = (long)offset;
                    var lengthToRead = fullLength - longOffset;
                    stream.Seek(longOffset, SeekOrigin.Begin);
                    buffer = Common.Extensions.StreamExtensions.ReadFully(stream, (int)lengthToRead);
                }

                CabHelper.RandomizeCabWithAnyLength(buffer);

                XuaLogger.ResourceRedirector.Warn($"Randomized CAB for '{path}' in order to load it because another asset bundle already uses its CAB-string. You can ignore the previous error message, but this is likely caused by two mods incorrectly using the same CAB-string.");

                return AssetBundle.LoadFromMemory(buffer);
            }
            else
            {
                return bundle;
            }
        }
    }
}
