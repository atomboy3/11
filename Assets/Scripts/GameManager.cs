// GameManager.cs — State machine: MainMenu / Playing / GameOver
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public Spawner spawner;
    public Board   board;

    [Header("UI (World Space Canvas)")]
    public GameObject mainMenuUI;
    public GameObject gameplayUI;
    public GameObject gameOverUI;
    public TMP_Text   scoreText;
    public TMP_Text   levelText;
    public TMP_Text   linesText;

    private enum State { Menu, Playing, Over }
    private State _state;

    private int _score, _level, _lines;
    private Tetromino _active;

    // Score table: 0,1,2,3,4 lines
    private readonly int[] _pts = { 0, 100, 300, 500, 800 };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        board.onLinesCleared.AddListener(OnLines);
        SetState(State.Menu);
    }

    public void StartGame()
    {
        _score = 0; _level = 1; _lines = 0;
        UpdateHUD();
        board.ClearAll();
        SetState(State.Playing);
        SpawnNext();
    }

    public void TriggerGameOver()
    {
        _active = null;
        SetState(State.Over);
    }

    public void OnPieceLocked()
    {
        if (_state != State.Playing) return;
        SpawnNext();
    }

    public void ReturnToMenu()
    {
        if (_active != null) Destroy(_active.gameObject);
        board.ClearAll();
        SetState(State.Menu);
    }

    private void SpawnNext() => _active = spawner.SpawnNext();

    private void OnLines(int n)
    {
        _lines  += n;
        _score  += (n < _pts.Length ? _pts[n] : 800) * _level;
        _level   = _lines / 10 + 1;
        if (_active != null)
            _active.fallInterval = Mathf.Max(0.1f, 1f - (_level - 1) * 0.08f);
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (scoreText) scoreText.text = $"Score\n{_score:N0}";
        if (levelText) levelText.text = $"Level\n{_level}";
        if (linesText) linesText.text = $"Lines\n{_lines}";
    }

    private void SetState(State s)
    {
        _state = s;
        mainMenuUI?.SetActive(s == State.Menu);
        gameplayUI?.SetActive(s == State.Playing);
        gameOverUI?.SetActive(s == State.Over);
    }
}
