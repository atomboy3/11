// Tetromino.cs — Falling piece on Lazy Eye layer. New Input System.
using UnityEngine;
using UnityEngine.InputSystem;

public class Tetromino : MonoBehaviour
{
    [Header("Speed")]
    public float fallInterval    = 1.0f;
    public float softDropMult    = 5f;
    public float lockDelay       = 0.5f;

    private Board  _board;
    private DichopticRigManager _rig;

    private float _fallTimer;
    private float _lockTimer;
    private bool  _grounded;
    private bool  _softDrop;

    // Input
    private InputAction _move;
    private InputAction _rotate;
    private InputAction _soft;
    private InputAction _hard;

    private void Awake()
    {
        _board = FindObjectOfType<Board>();
        _rig   = DichopticRigManager.Instance ?? FindObjectOfType<DichopticRigManager>();

        // Assign lazy-eye layer — only the amblyopic eye sees falling blocks
        SetLayer(gameObject, _rig?.GetLazyEyeLayer() ?? 8);

        var pi = GetComponent<PlayerInput>();
        if (pi != null)
        {
            _move   = pi.actions["Move"];
            _rotate = pi.actions["Rotate"];
            _soft   = pi.actions["SoftDrop"];
            _hard   = pi.actions["HardDrop"];
        }
    }

    private void Update()
    {
        ProcessInput();
        Fall();
        LockCheck();
    }

    private void ProcessInput()
    {
        if (_move != null && _move.WasPressedThisFrame())
        {
            float x = _move.ReadValue<Vector2>().x;
            if (x != 0) TryMove(new Vector3(Mathf.Sign(x), 0f, 0f));
        }

        if (_rotate != null && _rotate.WasPressedThisFrame())
            TryRotate();

        _softDrop = _soft != null && _soft.IsPressed();

        if (_hard != null && _hard.WasPressedThisFrame())
            HardDrop();
    }

    private void Fall()
    {
        float interval = _softDrop ? fallInterval / softDropMult : fallInterval;
        _fallTimer += Time.deltaTime;
        if (_fallTimer < interval) return;
        _fallTimer = 0f;

        if (!TryMove(Vector3.down))
            _grounded = true;
        else
        {
            _grounded  = false;
            _lockTimer = 0f;
        }
    }

    private void LockCheck()
    {
        if (!_grounded) return;
        _lockTimer += Time.deltaTime;
        if (_lockTimer >= lockDelay)
        {
            _board.LockPiece(this);
            GameManager.Instance?.OnPieceLocked();
        }
    }

    private bool TryMove(Vector3 dir)
    {
        transform.position += dir;
        if (_board.IsValidPosition(this)) return true;
        transform.position -= dir;
        return false;
    }

    private void TryRotate()
    {
        transform.Rotate(0f, 0f, 90f);
        // Wall kicks: try right, left, else revert
        if (!_board.IsValidPosition(this))
        {
            transform.position += Vector3.right;
            if (!_board.IsValidPosition(this))
            {
                transform.position += Vector3.left * 2f;
                if (!_board.IsValidPosition(this))
                {
                    transform.position += Vector3.right;
                    transform.Rotate(0f, 0f, -90f);
                }
            }
        }
        if (_grounded) _lockTimer = 0f;
    }

    private void HardDrop()
    {
        while (TryMove(Vector3.down)) { }
        _board.LockPiece(this);
        GameManager.Instance?.OnPieceLocked();
    }

    private void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayer(t.gameObject, layer);
    }
}
