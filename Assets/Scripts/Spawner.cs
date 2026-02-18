// Spawner.cs — Creates Tetromino pieces at top of board.
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Prefabs (7 standard pieces)")]
    public GameObject[] pieces;

    [Header("Materials")]
    public Material   gaborMaterial;
    public Material[] solidMaterials;

    private int _next;
    private Board _board;

    private void Awake()
    {
        _board = FindObjectOfType<Board>();
        _next  = Random.Range(0, pieces.Length);
    }

    public Tetromino SpawnNext()
    {
        if (pieces == null || pieces.Length == 0) return null;

        int idx  = _next;
        _next    = Random.Range(0, pieces.Length);

        var go   = Instantiate(pieces[idx], transform.position, Quaternion.identity);
        ApplyMaterial(go, idx);

        var piece = go.GetComponent<Tetromino>();
        if (!_board.IsValidPosition(piece))
        {
            Destroy(go);
            GameManager.Instance?.TriggerGameOver();
            return null;
        }
        return piece;
    }

    private void ApplyMaterial(GameObject go, int idx)
    {
        bool gabor = AmblyopiaSettings.Instance != null && AmblyopiaSettings.Instance.useGaborMode;
        Material mat = gabor
            ? gaborMaterial
            : (solidMaterials != null && idx < solidMaterials.Length ? solidMaterials[idx] : null);

        if (mat == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            r.material = new Material(mat);
    }
}
