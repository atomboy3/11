// GazeInteraction.cs — Reticle dwell-time UI for VR menus.
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GazeInteraction : MonoBehaviour
{
    public Transform reticle;
    public float reticleDistance = 3f;
    public float dwellTime       = 1.5f;
    public Image progressRing;

    private GazeButton _current;
    private float      _timer;
    private Camera     _cam;

    private void Awake()
    {
        _cam = Camera.main ?? FindObjectOfType<Camera>();
    }

    private void Update()
    {
        if (reticle != null)
        {
            reticle.position = _cam.transform.position + _cam.transform.forward * reticleDistance;
            reticle.rotation = _cam.transform.rotation;
        }

        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 10f))
        {
            var btn = hit.collider.GetComponent<GazeButton>();
            if (btn != null)
            {
                if (btn != _current) { _current?.OnGazeExit(); _current = btn; _timer = 0f; btn.OnGazeEnter(); }
                _timer += Time.deltaTime;
                if (progressRing) progressRing.fillAmount = _timer / dwellTime;
                if (_timer >= dwellTime) { _current.OnGazeActivate(); _current = null; _timer = 0f; }
                return;
            }
        }

        _current?.OnGazeExit(); _current = null; _timer = 0f;
        if (progressRing) progressRing.fillAmount = 0f;
    }
}

public class GazeButton : MonoBehaviour
{
    public UnityEvent onActivated;
    public Color hoverColor   = Color.yellow;
    public Color defaultColor = Color.white;
    private Renderer _r;
    private void Awake() => _r = GetComponent<Renderer>();
    public void OnGazeEnter()    { if (_r) _r.material.color = hoverColor; }
    public void OnGazeExit()     { if (_r) _r.material.color = defaultColor; }
    public void OnGazeActivate() { OnGazeExit(); onActivated?.Invoke(); }
}
