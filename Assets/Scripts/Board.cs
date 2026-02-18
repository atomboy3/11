// Board.cs — Tetris grid. Locked blocks switch to Good Eye layer.
using UnityEngine;
using UnityEngine.Events;

public class Board : MonoBehaviour
{
    [Header("Grid")]
    public int width  = 10;
    public int height = 20;
    public int buffer = 4;

    [Header("References")]
    public Transform boardFrame;

    public UnityEvent<int> onLinesCleared;

    private Transform[,] _grid;
    private DichopticRigManager _rig;

    private void Awake()
    {
        _rig  = DichopticRigManager.Instance != null
              ? DichopticRigManager.Instance
              : FindObjectOfType<DichopticRigManager>();
        _grid = new Transform[width, height + buffer];
        SetLayerAll(boardFrame?.gameObject, _rig?.GetFusionLayer() ?? 10);
    }

    public bool IsValidPosition(Tetromino p)
    {
        foreach (Transform c in p.transform)
        {
            var pos = ToGrid(c.position);
            if (!InBounds(pos) || IsOccupied(pos)) return false;
        }
        return true;
    }

    public void LockPiece(Tetromino p)
    {
        int goodLayer = _rig?.GetGoodEyeLayer() ?? 9;
        foreach (Transform c in p.transform)
        {
            var pos = ToGrid(c.position);
            c.position = ToWorld(pos);
            c.SetParent(transform);
            SetLayerAll(c.gameObject, goodLayer);
            AmblyopiaSettings.Instance?.RegisterRenderer(c.GetComponent<Renderer>());
            if (pos.y < height + buffer) _grid[pos.x, pos.y] = c;
        }
        Destroy(p.gameObject);
        int cleared = ClearLines();
        if (cleared > 0) onLinesCleared?.Invoke(cleared);
    }

    public void ClearAll()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height + buffer; y++)
                if (_grid[x, y] != null)
                {
                    AmblyopiaSettings.Instance?.UnregisterRenderer(
                        _grid[x, y].GetComponent<Renderer>());
                    Destroy(_grid[x, y].gameObject);
                    _grid[x, y] = null;
                }
    }

    private int ClearLines()
    {
        int count = 0;
        for (int y = height - 1; y >= 0; y--)
        {
            if (!IsFull(y)) continue;
            EraseLine(y); DropAbove(y); count++; y++;
        }
        return count;
    }

    private bool IsFull(int y)
    {
        for (int x = 0; x < width; x++) if (_grid[x, y] == null) return false;
        return true;
    }

    private void EraseLine(int y)
    {
        for (int x = 0; x < width; x++)
        {
            AmblyopiaSettings.Instance?.UnregisterRenderer(
                _grid[x, y]?.GetComponent<Renderer>());
            if (_grid[x, y] != null) Destroy(_grid[x, y].gameObject);
            _grid[x, y] = null;
        }
    }

    private void DropAbove(int cy)
    {
        for (int y = cy + 1; y < height + buffer; y++)
            for (int x = 0; x < width; x++)
            {
                _grid[x, y - 1] = _grid[x, y];
                _grid[x, y]     = null;
                if (_grid[x, y - 1] != null)
                    _grid[x, y - 1].position = ToWorld(new Vector2Int(x, y - 1));
            }
    }

    private bool IsOccupied(Vector2Int p) => !InBounds(p) || _grid[p.x, p.y] != null;
    private bool InBounds(Vector2Int p)   => p.x >= 0 && p.x < width && p.y >= 0 && p.y < height + buffer;

    private Vector2Int ToGrid(Vector3 w) => new Vector2Int(
        Mathf.RoundToInt(w.x - transform.position.x),
        Mathf.RoundToInt(w.y - transform.position.y));

    private Vector3 ToWorld(Vector2Int g) => new Vector3(
        g.x + transform.position.x, g.y + transform.position.y, 0f);

    private void SetLayerAll(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayerAll(t.gameObject, layer);
    }
}
