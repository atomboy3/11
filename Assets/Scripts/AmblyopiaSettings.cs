// AmblyopiaSettings.cs
// Singleton — manages contrast calibration and Gabor mode for vision therapy.

using System.Collections.Generic;
using UnityEngine;

public class AmblyopiaSettings : MonoBehaviour
{
    public static AmblyopiaSettings Instance { get; private set; }

    [Header("Contrast (Good Eye)")]
    [Range(0.1f, 1.0f)] public float goodEyeContrast = 0.5f;

    [Header("Gabor Mode")]
    public bool  useGaborMode    = false;
    [Range(1f, 16f)]  public float gaborFrequency   = 4f;
    [Range(0f, 180f)] public float gaborOrientation = 45f;
    [Range(0.1f, 1f)] public float gaborContrast    = 0.8f;
    [Range(0.05f,2f)] public float gaborSigma       = 0.3f;

    private int _goodEyeLayer = 9;
    private readonly List<Material> _goodMats = new List<Material>();

    private static readonly int _colID   = Shader.PropertyToID("_Color");
    private static readonly int _contID  = Shader.PropertyToID("_Contrast");
    private static readonly int _freqID  = Shader.PropertyToID("_Frequency");
    private static readonly int _orienID = Shader.PropertyToID("_Orientation");
    private static readonly int _gcID    = Shader.PropertyToID("_GaborContrast");
    private static readonly int _sigID   = Shader.PropertyToID("_Sigma");

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        ApplyContrast();
        if (useGaborMode) ApplyGabor();
    }

    public void SetGoodEyeLayer(int layer)
    {
        _goodEyeLayer = layer;
        RefreshRegistry();
    }

    public void RegisterRenderer(Renderer r)
    {
        if (r == null) return;
        foreach (var m in r.materials)
            if (!_goodMats.Contains(m)) _goodMats.Add(m);
        ApplyContrast();
    }

    public void UnregisterRenderer(Renderer r)
    {
        if (r == null) return;
        foreach (var m in r.materials) _goodMats.Remove(m);
    }

    public void OnContrastChanged(float v)
    {
        goodEyeContrast = Mathf.Clamp(v, 0.1f, 1f);
        ApplyContrast();
    }

    public void SetGaborMode(bool on)
    {
        useGaborMode = on;
        foreach (var m in _goodMats)
        {
            if (on) m.EnableKeyword("GABOR_ON");
            else    m.DisableKeyword("GABOR_ON");
        }
    }

    private void ApplyContrast()
    {
        foreach (var m in _goodMats)
        {
            if (m == null) continue;
            if (m.HasProperty(_colID))
            {
                Color c = m.GetColor(_colID); c.a = goodEyeContrast;
                m.SetColor(_colID, c);
            }
            if (m.HasProperty(_contID))
                m.SetFloat(_contID, goodEyeContrast);
        }
    }

    private void ApplyGabor()
    {
        float orientRad = gaborOrientation * Mathf.Deg2Rad;
        foreach (var m in _goodMats)
        {
            if (m == null) continue;
            if (m.HasProperty(_freqID))  m.SetFloat(_freqID,  gaborFrequency);
            if (m.HasProperty(_orienID)) m.SetFloat(_orienID, orientRad);
            if (m.HasProperty(_gcID))    m.SetFloat(_gcID,    gaborContrast);
            if (m.HasProperty(_sigID))   m.SetFloat(_sigID,   gaborSigma);
        }
    }

    private void RefreshRegistry()
    {
        _goodMats.Clear();
        foreach (var r in FindObjectsOfType<Renderer>())
            if (r.gameObject.layer == _goodEyeLayer) RegisterRenderer(r);
    }
}
