using I2.Loc;
using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace ProLib.Utility
{
    public static class Utility
    {
        public static TMP_FontAsset GetFont(String path)
        {
            foreach (LanguageSourceData source in LocalizationManager.Sources)
            {
                foreach (UnityEngine.Object asset in source.Assets)
                {
                    if (path == asset.name && asset is TMP_FontAsset font)
                    {
                        return font;
                    }
                }
            }
            return null;
        }

        public static byte[] ReadToEnd(this Stream stream)
        {
            long originalPosition = stream.Position;
            stream.Position = 0;

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        public static Sprite LoadSprite(this Assembly assembly, String filePath, float pixelsPerUnit = 16f)
        {
            String path = $"{assembly.GetName().Name}.{filePath}";
            Texture2D texture = assembly.LoadTexture(path);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        public static Sprite LoadSprite(this Assembly assembly, String filePath, float width, float height, float pixelsPerUnit = 16f)
        {
            String path = $"{assembly.GetName().Name}.{filePath}";
            Texture2D texture = assembly.LoadTexture(path);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        public static Texture2D LoadTexture(this Assembly assembly, string filePath)
        {
            Plugin.Log.LogMessage(filePath);
            Texture2D texture;
            Stream stream = assembly.GetManifestResourceStream(filePath);
            texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.LoadImage(stream.ReadToEnd());
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.wrapModeU = TextureWrapMode.Clamp;
            texture.wrapModeV = TextureWrapMode.Clamp;
            texture.wrapModeW = TextureWrapMode.Clamp;
            return texture;
        }
    }
}
