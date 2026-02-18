// PerformanceManager.cs — Forces 120Hz on OnePlus 12, thermal governor.
using UnityEngine;

public class PerformanceManager : MonoBehaviour
{
    [Range(60, 120)] public int targetFPS       = 120;
    [Range(30,  60)] public int thermalFallback = 60;
    public bool thermalGovernor = true;

    private float _thermalTimer;

    private void Awake()
    {
        QualitySettings.vSyncCount  = 0;
        Application.targetFrameRate = targetFPS;
        Screen.sleepTimeout         = SleepTimeout.NeverSleep;
        Debug.Log($"[Perf] Target: {targetFPS}Hz");
    }

    private void Update()
    {
        if (!thermalGovernor) return;
        _thermalTimer += Time.unscaledDeltaTime;
        if (_thermalTimer < 5f) return;
        _thermalTimer = 0f;

#if UNITY_ANDROID && !UNITY_EDITOR
        float fps = 1f / Time.unscaledDeltaTime;
        if (fps < targetFPS * 0.75f && Application.targetFrameRate == targetFPS)
        {
            Debug.LogWarning($"[Perf] Thermal throttle detected ({fps:F0}fps). Reducing to {thermalFallback}Hz.");
            Application.targetFrameRate = thermalFallback;
        }
        else if (fps > thermalFallback * 0.9f && Application.targetFrameRate == thermalFallback)
        {
            Debug.Log("[Perf] Thermal OK. Restoring 120Hz.");
            Application.targetFrameRate = targetFPS;
        }
#endif
    }
}
