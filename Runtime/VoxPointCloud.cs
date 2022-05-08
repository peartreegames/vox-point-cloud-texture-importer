using System;
using UnityEngine;

namespace VoxPointCloudTextureImporter
{
    public class VoxPointCloud : ScriptableObject
    {
        [Serializable]
        public class Frame
        {
            public Texture2D positions;
            public Texture2D colors;
        }

        public string key;
        public Frame[] frames;
    }
}