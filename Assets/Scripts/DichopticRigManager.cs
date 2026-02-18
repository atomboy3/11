// DichopticRigManager.cs
// Split-screen dichoptic camera rig for amblyopia VR therapy.
// Layer 8 = LeftOnly | Layer 9 = RightOnly | Layer 10 = FusionLock

using UnityEngine;

public class DichopticRigManager : MonoBehaviour
{
    public static DichopticRigManager Instance { get; private set; }

    [Header("Cameras")]
    public Camera leftEyeCamera;
    public Camera rightEyeCamera;

    [Header("Settings")]
    public bool isLeftEyeLazy = true;
    [Range(0.04f, 0.08f)] public float ipd = 0.064f;

    private const int LAYER_LEFT   = 8;
    private const int LAYER_RIGHT  = 9;
    private const int LAYER_FUSION = 10;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ConfigureViewports();
        ApplyIPD();
        SetLazyEye(isLeftEyeLazy);
    }

    public void SetLazyEye(bool isLeft)
    {
        isLeftEyeLazy = isLeft;
        int lazyLayer = isLeft ? LAYER_LEFT : LAYER_RIGHT;
        int goodLayer  = isLeft ? LAYER_RIGHT : LAYER_LEFT;

        // Lazy eye: sees falling blocks (lazy layer) + fusion frame
        Camera lazyCam = isLeft ? leftEyeCamera : rightEyeCamera;
        lazyCam.cullingMask = (1 << lazyLayer) | (1 << LAYER_FUSION);

        // Good eye: sees locked stack (good layer, contrast-reduced) + fusion frame
        Camera goodCam = isLeft ? rightEyeCamera : leftEyeCamera;
        goodCam.cullingMask = (1 << goodLayer) | (1 << LAYER_FUSION);

        AmblyopiaSettings.Instance?.SetGoodEyeLayer(goodLayer);
    }

    public int GetLazyEyeLayer() => isLeftEyeLazy ? LAYER_LEFT : LAYER_RIGHT;
    public int GetGoodEyeLayer()  => isLeftEyeLazy ? LAYER_RIGHT : LAYER_LEFT;
    public int GetFusionLayer()   => LAYER_FUSION;

    private void ConfigureViewports()
    {
        leftEyeCamera.rect  = new Rect(0f,   0f, 0.5f, 1f);
        rightEyeCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
        rightEyeCamera.fieldOfView   = leftEyeCamera.fieldOfView;
        rightEyeCamera.nearClipPlane = leftEyeCamera.nearClipPlane;
        rightEyeCamera.farClipPlane  = leftEyeCamera.farClipPlane;
        leftEyeCamera.depth  = 0;
        rightEyeCamera.depth = 1;
    }

    private void ApplyIPD()
    {
        float half = ipd * 0.5f;
        leftEyeCamera.transform.localPosition  = new Vector3(-half, 0f, 0f);
        rightEyeCamera.transform.localPosition = new Vector3( half, 0f, 0f);
    }

    private void OnValidate()
    {
        if (leftEyeCamera == null || rightEyeCamera == null) return;
        ApplyIPD();
        SetLazyEye(isLeftEyeLazy);
    }
}
