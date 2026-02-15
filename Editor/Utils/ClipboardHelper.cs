using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Weppy.AIProvider.Chat.Editor
{
    public static class ClipboardHelper
    {
        public static bool HasImage()
        {
#if UNITY_EDITOR_WIN
            return HasImageWindows();
#elif UNITY_EDITOR_OSX
            return HasImageMac();
#else
            return false;
#endif
        }

        public static Texture2D GetImage()
        {
#if UNITY_EDITOR_WIN
            return GetImageWindows();
#elif UNITY_EDITOR_OSX
            return GetImageMac();
#else
            return null;
#endif
        }

        public static bool HasFilePaths()
        {
#if UNITY_EDITOR_WIN
            return HasFilePathsWindows();
#elif UNITY_EDITOR_OSX
            return HasFilePathsMac();
#else
            return false;
#endif
        }

        public static List<string> GetFilePaths()
        {
#if UNITY_EDITOR_WIN
            return GetFilePathsWindows();
#elif UNITY_EDITOR_OSX
            return GetFilePathsMac();
#else
            return new List<string>();
#endif
        }

#if UNITY_EDITOR_WIN
        private const uint CF_BITMAP = 2;
        private const uint CF_DIB = 8;
        private const uint CF_HDROP = 15;

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern int GlobalSize(IntPtr hMem);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, System.Text.StringBuilder lpszFile, uint cch);

        private static bool HasFilePathsWindows()
        {
            return IsClipboardFormatAvailable(CF_HDROP);
        }

        private static List<string> GetFilePathsWindows()
        {
            List<string> filePaths = new List<string>();

            if (!OpenClipboard(IntPtr.Zero))
                return filePaths;

            try
            {
                if (!IsClipboardFormatAvailable(CF_HDROP))
                    return filePaths;

                IntPtr hDrop = GetClipboardData(CF_HDROP);
                if (hDrop == IntPtr.Zero)
                    return filePaths;

                uint fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                for (uint i = 0; i < fileCount; i++)
                {
                    uint pathLen = DragQueryFile(hDrop, i, null, 0);
                    System.Text.StringBuilder sb = new System.Text.StringBuilder((int)pathLen + 1);
                    DragQueryFile(hDrop, i, sb, pathLen + 1);
                    filePaths.Add(sb.ToString());
                }
            }
            finally
            {
                CloseClipboard();
            }

            return filePaths;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        private static bool HasImageWindows()
        {
            return IsClipboardFormatAvailable(CF_DIB) || IsClipboardFormatAvailable(CF_BITMAP);
        }

        private static Texture2D GetImageWindows()
        {
            if (!OpenClipboard(IntPtr.Zero))
                return null;

            try
            {
                if (!IsClipboardFormatAvailable(CF_DIB))
                    return null;

                IntPtr hData = GetClipboardData(CF_DIB);
                if (hData == IntPtr.Zero)
                    return null;

                IntPtr pData = GlobalLock(hData);
                if (pData == IntPtr.Zero)
                    return null;

                try
                {
                    BITMAPINFOHEADER header = Marshal.PtrToStructure<BITMAPINFOHEADER>(pData);
                    int width = header.biWidth;
                    int height = Math.Abs(header.biHeight);
                    bool bottomUp = header.biHeight > 0;
                    int bitsPerPixel = header.biBitCount;

                    if (bitsPerPixel != 24 && bitsPerPixel != 32)
                        return null;

                    int bytesPerPixel = bitsPerPixel / 8;
                    int stride = ((width * bytesPerPixel + 3) / 4) * 4;
                    int dataOffset = (int)header.biSize;

                    byte[] pixelData = new byte[stride * height];
                    Marshal.Copy(IntPtr.Add(pData, dataOffset), pixelData, 0, pixelData.Length);

                    Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    Color32[] colors = new Color32[width * height];

                    for (int y = 0; y < height; y++)
                    {
                        int srcY = bottomUp ? (height - 1 - y) : y;
                        for (int x = 0; x < width; x++)
                        {
                            int srcIndex = srcY * stride + x * bytesPerPixel;
                            byte b = pixelData[srcIndex];
                            byte g = pixelData[srcIndex + 1];
                            byte r = pixelData[srcIndex + 2];
                            byte a = bytesPerPixel == 4 ? pixelData[srcIndex + 3] : (byte)255;

                            colors[y * width + x] = new Color32(r, g, b, a);
                        }
                    }

                    texture.SetPixels32(colors);
                    texture.Apply();
                    return texture;
                }
                finally
                {
                    GlobalUnlock(hData);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
#endif

#if UNITY_EDITOR_OSX
        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        private static extern IntPtr objc_getClass(string className);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
        private static extern IntPtr sel_registerName(string selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern long objc_msgSend_long(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, long arg1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, long arg1, IntPtr arg2);

        private static bool HasImageMac()
        {
            try
            {
                IntPtr nsImage = GetNSImageFromPasteboard();
                return nsImage != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        private static Texture2D GetImageMac()
        {
            try
            {
                byte[] pngData = GetPngDataFromPasteboard();
                if (pngData != null && pngData.Length > 0)
                {
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(pngData))
                        return texture;
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                IntPtr nsImage = GetNSImageFromPasteboard();
                if (nsImage == IntPtr.Zero)
                    return null;

                byte[] imageData = GetPngDataFromNSImage(nsImage);
                if (imageData != null && imageData.Length > 0)
                {
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(imageData))
                        return texture;
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get image from clipboard: {ex.Message}");
                return null;
            }
        }

        private static byte[] GetPngDataFromPasteboard()
        {
            try
            {
                IntPtr nsPasteboardClass = objc_getClass("NSPasteboard");
                IntPtr generalPasteboard = objc_msgSend(nsPasteboardClass, sel_registerName("generalPasteboard"));
                if (generalPasteboard == IntPtr.Zero)
                    return null;

                IntPtr pngType = CreateNSString("public.png");
                IntPtr pngData = objc_msgSend(generalPasteboard, sel_registerName("dataForType:"), pngType);
                if (pngData != IntPtr.Zero)
                {
                    long length = objc_msgSend_long(pngData, sel_registerName("length"));
                    if (length > 0)
                    {
                        IntPtr bytesPtr = objc_msgSend(pngData, sel_registerName("bytes"));
                        if (bytesPtr != IntPtr.Zero)
                        {
                            byte[] bytes = new byte[length];
                            Marshal.Copy(bytesPtr, bytes, 0, (int)length);
                            return bytes;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static byte[] GetPngDataFromNSImage(IntPtr nsImage_)
        {
            try
            {
                IntPtr cgImageSelector = sel_registerName("CGImageForProposedRect:context:hints:");
                IntPtr cgImage = objc_msgSend(nsImage_, cgImageSelector, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                if (cgImage == IntPtr.Zero)
                {
                    IntPtr tiffData = objc_msgSend(nsImage_, sel_registerName("TIFFRepresentation"));
                    if (tiffData != IntPtr.Zero)
                    {
                        IntPtr bitmapRepClass = objc_getClass("NSBitmapImageRep");
                        IntPtr bitmapRep = objc_msgSend(
                            objc_msgSend(bitmapRepClass, sel_registerName("alloc")),
                            sel_registerName("initWithData:"),
                            tiffData
                        );

                        if (bitmapRep != IntPtr.Zero)
                        {
                            IntPtr pngData = objc_msgSend(bitmapRep, sel_registerName("representationUsingType:properties:"), (long)4, IntPtr.Zero);
                            if (pngData != IntPtr.Zero)
                            {
                                long length = objc_msgSend_long(pngData, sel_registerName("length"));
                                if (length > 0)
                                {
                                    IntPtr bytesPtr = objc_msgSend(pngData, sel_registerName("bytes"));
                                    if (bytesPtr != IntPtr.Zero)
                                    {
                                        byte[] bytes = new byte[length];
                                        Marshal.Copy(bytesPtr, bytes, 0, (int)length);
                                        return bytes;
                                    }
                                }
                            }
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static IntPtr GetNSImageFromPasteboard()
        {
            IntPtr nsPasteboardClass = objc_getClass("NSPasteboard");
            IntPtr generalPasteboard = objc_msgSend(nsPasteboardClass, sel_registerName("generalPasteboard"));

            if (generalPasteboard == IntPtr.Zero)
                return IntPtr.Zero;

            IntPtr nsImageClass = objc_getClass("NSImage");
            IntPtr nsArray = objc_msgSend(nsImageClass, sel_registerName("imageTypes"));

            IntPtr availableType = objc_msgSend(generalPasteboard, sel_registerName("availableTypeFromArray:"), nsArray);
            if (availableType == IntPtr.Zero)
                return IntPtr.Zero;

            IntPtr nsImage = objc_msgSend(
                objc_msgSend(nsImageClass, sel_registerName("alloc")),
                sel_registerName("initWithPasteboard:"),
                generalPasteboard
            );

            return nsImage;
        }

        private static bool HasFilePathsMac()
        {
            try
            {
                IntPtr nsPasteboardClass = objc_getClass("NSPasteboard");
                IntPtr generalPasteboard = objc_msgSend(nsPasteboardClass, sel_registerName("generalPasteboard"));

                if (generalPasteboard == IntPtr.Zero)
                    return false;

                IntPtr filenameType = CreateNSString("NSFilenamesPboardType");
                IntPtr propertyList = objc_msgSend(generalPasteboard, sel_registerName("propertyListForType:"), filenameType);

                if (propertyList != IntPtr.Zero)
                    return true;

                IntPtr fileUrlType = CreateNSString("public.file-url");
                IntPtr types = objc_msgSend(generalPasteboard, sel_registerName("types"));
                if (types != IntPtr.Zero)
                {
                    bool containsFileUrl = objc_msgSend(types, sel_registerName("containsObject:"), fileUrlType) != IntPtr.Zero;
                    if (containsFileUrl)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static List<string> GetFilePathsMac()
        {
            List<string> filePaths = new List<string>();

            try
            {
                IntPtr nsPasteboardClass = objc_getClass("NSPasteboard");
                IntPtr generalPasteboard = objc_msgSend(nsPasteboardClass, sel_registerName("generalPasteboard"));

                if (generalPasteboard == IntPtr.Zero)
                    return filePaths;

                IntPtr filenameType = CreateNSString("NSFilenamesPboardType");
                IntPtr propertyList = objc_msgSend(generalPasteboard, sel_registerName("propertyListForType:"), filenameType);

                if (propertyList != IntPtr.Zero)
                {
                    long count = objc_msgSend_long(propertyList, sel_registerName("count"));
                    for (long i = 0; i < count; i++)
                    {
                        IntPtr pathObj = objc_msgSend(propertyList, sel_registerName("objectAtIndex:"), i);
                        if (pathObj != IntPtr.Zero)
                        {
                            IntPtr utf8Ptr = objc_msgSend(pathObj, sel_registerName("UTF8String"));
                            if (utf8Ptr != IntPtr.Zero)
                            {
                                string path = Marshal.PtrToStringAnsi(utf8Ptr);
                                if (!string.IsNullOrEmpty(path))
                                {
                                    filePaths.Add(path);
                                }
                            }
                        }
                    }

                    if (filePaths.Count > 0)
                        return filePaths;
                }

                IntPtr pasteboardItems = objc_msgSend(generalPasteboard, sel_registerName("pasteboardItems"));
                if (pasteboardItems == IntPtr.Zero)
                    return filePaths;

                IntPtr fileUrlType = CreateNSString("public.file-url");
                IntPtr nsUrlClass = objc_getClass("NSURL");

                long itemCount = objc_msgSend_long(pasteboardItems, sel_registerName("count"));
                for (long i = 0; i < itemCount; i++)
                {
                    IntPtr item = objc_msgSend(pasteboardItems, sel_registerName("objectAtIndex:"), i);
                    if (item == IntPtr.Zero)
                        continue;

                    IntPtr urlString = objc_msgSend(item, sel_registerName("stringForType:"), fileUrlType);
                    if (urlString == IntPtr.Zero)
                        continue;

                    IntPtr nsUrl = objc_msgSend(nsUrlClass, sel_registerName("URLWithString:"), urlString);
                    if (nsUrl == IntPtr.Zero)
                        continue;

                    IntPtr pathPtr = objc_msgSend(nsUrl, sel_registerName("path"));
                    if (pathPtr == IntPtr.Zero)
                        continue;

                    IntPtr utf8Ptr = objc_msgSend(pathPtr, sel_registerName("UTF8String"));
                    if (utf8Ptr != IntPtr.Zero)
                    {
                        string path = Marshal.PtrToStringAnsi(utf8Ptr);
                        if (!string.IsNullOrEmpty(path))
                        {
                            filePaths.Add(path);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get file paths from clipboard: {ex.Message}");
            }

            return filePaths;
        }

        private static IntPtr CreateNSString(string str_)
        {
            IntPtr nsStringClass = objc_getClass("NSString");
            IntPtr utf8Ptr = Marshal.StringToHGlobalAnsi(str_);
            IntPtr nsString = objc_msgSend(nsStringClass, sel_registerName("stringWithUTF8String:"), utf8Ptr);
            Marshal.FreeHGlobal(utf8Ptr);
            return nsString;
        }
#endif
    }
}
