using System;
using UnityEngine;
using UnityEngine.VFX;

namespace VoxPointCloudTextureImporter
{
    [RequireComponent(typeof(VisualEffect))]
    public class PointCloudAnimator : MonoBehaviour
    {
        [SerializeField] private float speed;
        [SerializeField] private VoxPointCloud[] pointCloudAnimations;
        
        private VisualEffect _vfx;
        private float _time;
        private int _currentAnimationIndex;
        private int _currentFrameIndex;
        private VoxPointCloud Current => pointCloudAnimations[_currentAnimationIndex];

        private void Awake()
        {
            _vfx = GetComponent<VisualEffect>();
        }

        public void Play(string key)
        {
            _time = 0;
            _currentFrameIndex = 0;
            var index = Mathf.Max(0, Array.FindIndex(pointCloudAnimations, anim => anim.key == key));
            _currentAnimationIndex = index;
        }
        
        private void Update()
        {
            _time += Time.deltaTime * speed;
            var anim = Current;
            var index = Mathf.FloorToInt(_time % anim.frames.Length);
            if (index == _currentFrameIndex) return;
            _currentFrameIndex = index;
            var frame = anim.frames[_currentFrameIndex];
            _vfx.SetTexture("Positions", frame.positions);
            _vfx.SetTexture("Colors", frame.colors);
        }
    }
}