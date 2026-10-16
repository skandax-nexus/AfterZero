using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AfterZero
{
    /// <summary>Ensures the camera renders URP post-processing with high-quality anti-aliasing and HDR.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFXSetup : MonoBehaviour
    {
        public bool useTAA = false;

        void Awake()
        {
            var cam = GetComponent<Camera>();
            cam.allowHDR = true;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = useTAA ? AntialiasingMode.TemporalAntiAliasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
            data.dithering = true;
        }
    }
}
