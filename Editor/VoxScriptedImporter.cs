using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace VoxPointCloudTextureImporter.Editor
{
    [ScriptedImporter(1, "vox")]
    public class VoxScriptedImporter : ScriptedImporter
    {
        public string key;
        
        public override void OnImportAsset(AssetImportContext ctx)
        {
            using var reader = new BinaryReader(File.Open(ctx.assetPath, FileMode.Open));
            if (!Seek("VOX ", reader, 4)) return;
            if (!Seek("MAIN", reader)) return;

            var framesData = new List<(int size, List<VoxPoint> points)>();
            while (Seek("SIZE", reader))
            {
                var width = reader.ReadInt32();
                var length = reader.ReadInt32();
                var height = reader.ReadInt32();
                var dimensions = new Vector3Int(width, height, length); // need to switch z<>y
                if (!Seek("XYZI", reader)) break;
                var size = reader.ReadInt32();
                var textureSize = Mathf.CeilToInt(Mathf.Sqrt(size));
                var frameData = (size: textureSize, points: new List<VoxPoint>());
                framesData.Add(frameData);
                for (var i = 0; i < size; i++)
                {
                    var x = dimensions.x - 1 - reader.ReadByte();
                    var z = dimensions.z - 1 - reader.ReadByte();
                    var y = reader.ReadByte();
                    var index = reader.ReadByte() - 1;
                    if (index <= 0) continue;
                    var point = new VoxPoint(x, y, z, index);
                    frameData.points.Add(point);
                }
            }

            if (!Seek("RGBA", reader)) return;
            var palette = new byte[256][];
            for (var i = 0; i < 256; i++)
            {
                palette[i] = new[]
                {
                    reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()
                };
            }
            
            reader.Dispose();
            var voxPointCloud = ScriptableObject.CreateInstance<VoxPointCloud>();
            voxPointCloud.name = "VoxPointCloud";
            voxPointCloud.frames = new VoxPointCloud.Frame[framesData.Count];
            voxPointCloud.key = key;
            ctx.AddObjectToAsset("main", voxPointCloud);
            ctx.SetMainObject(voxPointCloud);
            var frames = CreateFrames(framesData, palette);
            for (var i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                ctx.AddObjectToAsset(frame.positions.name, frame.positions);
                ctx.AddObjectToAsset(frame.colors.name, frame.colors);
                voxPointCloud.frames[i] = frame;
            }
        }

        private static List<VoxPointCloud.Frame> CreateFrames(IReadOnlyList<(int size, List<VoxPoint> points)> framesData, IReadOnlyList<byte[]> palette)
        {
            var frames = new List<VoxPointCloud.Frame>();
            for (var i = 0; i < framesData.Count; i++)
            {
                var (size, points) = framesData[i];
                var positions = CreateTexture(size, $"{i}_Positions", TextureFormat.RGBAHalf);
                var colors = CreateTexture(size, $"{i}_Colors", TextureFormat.RGBA32);
                var indexRandomizer = 0U;
                for (var x = 0; x < size; x++)
                {
                    for (var y = 0; y < size; y++)
                    {
                        // Due to square texture some points may not size
                        // set at random distributions throughout
                        // Taken from keijiro PCX
                        var tempI = x * size + y;
                        var index = tempI < points.Count ? tempI : (int) (indexRandomizer % points.Count);
                        var point = points[index];
                        positions.SetPixel(x, y, new Color(point.x / 10f, point.y / 10f, point.z / 10f));
                        var col = palette[point.i];
                        colors.SetPixel(x, y, new Color32(col[0], col[1], col[2], col[3]));
                        indexRandomizer += 123049U; 
                    }
                }
                positions.Apply(false, true);
                colors.Apply(false, true);
                frames.Add(new VoxPointCloud.Frame()
                {
                    positions = positions,
                    colors = colors
                });
            }
            return frames;
        }

        private static Texture2D CreateTexture(int size, string name, TextureFormat format) =>
            new(size, size, format, false)
            {
                name = name,
                filterMode = FilterMode.Point
            };

        private static bool Seek(string seekName, BinaryReader reader, int offset = 8)
        {
            var startPosition = reader.BaseStream.Position;
            while (reader.BaseStream.Length - reader.BaseStream.Position >= seekName.Length)
            {
                var data = Encoding.ASCII.GetString(reader.ReadBytes(seekName.Length));
                if (seekName == data)
                {
                    reader.BaseStream.Seek(offset, SeekOrigin.Current);
                    return true;
                }

                var chunks = reader.ReadInt32();
                var children = reader.ReadInt32();
                reader.BaseStream.Seek(chunks + children, SeekOrigin.Current);
            }

            reader.BaseStream.Position = startPosition;
            return false;
        }
    }
}