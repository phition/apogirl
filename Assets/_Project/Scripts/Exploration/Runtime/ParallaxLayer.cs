using UnityEngine;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 바깥 길 전경의 먼 겹: 카메라가 길 가운데에서 벗어난 만큼의 Factor 배만 따라가 멀리 있는 것처럼 보인다.
    /// 화소 격자에 맞춰 옮긴다 (도트가 흔들리지 않게).
    /// </summary>
    public sealed class ParallaxLayer : MonoBehaviour
    {
        /// <summary>탐험 카메라 (ExpeditionDirector가 만들 때 채운다). 없으면 Camera.main.</summary>
        public static Camera ViewCamera;

        public float Factor;
        public float CenterX;
        private float _baseX;

        private void Awake() => _baseX = transform.localPosition.x;

        private void LateUpdate()
        {
            var cam = ViewCamera != null ? ViewCamera : Camera.main;
            if (cam == null || transform.parent == null)
            {
                return;
            }

            var camX = transform.parent.InverseTransformPoint(cam.transform.position).x;
            var shift = (camX - CenterX) * Factor;
            shift = Mathf.Round(shift * ExplorationArt.PixelsPerUnit) / ExplorationArt.PixelsPerUnit;
            var p = transform.localPosition;
            transform.localPosition = new Vector3(_baseX + shift, p.y, p.z);
        }
    }
}
