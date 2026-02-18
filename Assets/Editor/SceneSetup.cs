// SceneSetup.cs
// Menu: Tools > Setup Dichoptic Scene
// Builds the entire scene hierarchy automatically — no manual drag-and-drop needed.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public static class SceneSetup
{
    [MenuItem("Tools/Setup Dichoptic Scene (Run This First!)")]
    public static void Setup()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Layers check ──────────────────────────────────────────────────
        EnsureLayer(8,  "LeftOnly");
        EnsureLayer(9,  "RightOnly");
        EnsureLayer(10, "FusionLock");

        // ── Camera Rig ────────────────────────────────────────────────────
        var rig = new GameObject("DichopticRig");
        var rigMgr = rig.AddComponent<DichopticRigManager>();

        var leftCamGO  = new GameObject("LeftEyeCamera");
        var rightCamGO = new GameObject("RightEyeCamera");
        leftCamGO.transform.SetParent(rig.transform);
        rightCamGO.transform.SetParent(rig.transform);

        var leftCam  = leftCamGO.AddComponent<Camera>();
        var rightCam = rightCamGO.AddComponent<Camera>();
        leftCam.backgroundColor  = new Color(0.05f, 0.05f, 0.05f);
        rightCam.backgroundColor = new Color(0.05f, 0.05f, 0.05f);
        leftCam.tag  = "MainCamera";

        rigMgr.leftEyeCamera  = leftCam;
        rigMgr.rightEyeCamera = rightCam;

        // ── Performance & Settings ────────────────────────────────────────
        var sysGO = new GameObject("Systems");
        sysGO.AddComponent<PerformanceManager>();
        sysGO.AddComponent<AmblyopiaSettings>();

        // ── Board ─────────────────────────────────────────────────────────
        var boardGO = new GameObject("Board");
        var board   = boardGO.AddComponent<Board>();
        boardGO.transform.position = new Vector3(-4.5f, 0f, 5f); // in front of cameras

        // Board frame (FusionLock layer — both eyes see it)
        var frameGO = CreateBoardFrame(board.width, board.height);
        frameGO.transform.SetParent(boardGO.transform, false);
        frameGO.layer = 10; // FusionLock
        board.boardFrame = frameGO.transform;

        // ── Spawner ───────────────────────────────────────────────────────
        var spawnerGO = new GameObject("Spawner");
        spawnerGO.transform.SetParent(boardGO.transform);
        spawnerGO.transform.localPosition = new Vector3(board.width / 2f - 0.5f, board.height + 1f, 0f);
        var spawner = spawnerGO.AddComponent<Spawner>();
        spawner.pieces = CreateTetrominoPrefabs();

        // ── Game Manager ──────────────────────────────────────────────────
        var gmGO = new GameObject("GameManager");
        var gm   = gmGO.AddComponent<GameManager>();
        gm.board   = board;
        gm.spawner = spawner;

        // ── World Space UI Canvas ─────────────────────────────────────────
        var canvas = CreateWorldCanvas(rig.transform, leftCam);
        gm.mainMenuUI = CreateMainMenuUI(canvas, gm);
        gm.gameplayUI = CreateGameplayUI(canvas, gm, out var scoreT, out var levelT, out var linesT);
        gm.gameOverUI = CreateGameOverUI(canvas, gm);
        gm.scoreText  = scoreT;
        gm.levelText  = levelT;
        gm.linesText  = linesT;

        // ── Board lines cleared event ─────────────────────────────────────
        // Wire via UnityEvent in code
        board.onLinesCleared = new UnityEngine.Events.UnityEvent<int>();

        // ── Gaze Interaction ──────────────────────────────────────────────
        var gazeGO = new GameObject("GazeInteraction");
        gazeGO.transform.SetParent(rig.transform);
        gazeGO.AddComponent<GazeInteraction>();

        // ── Save scene ────────────────────────────────────────────────────
        string scenePath = "Assets/MainScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        // Add to Build Settings
        var buildScenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        EditorBuildSettings.scenes = buildScenes;

        Debug.Log("[SceneSetup] Scene created and saved to: " + scenePath);
        EditorUtility.DisplayDialog(
            "Scene Setup Complete",
            "✓ MainScene.unity created\n✓ Added to Build Settings\n✓ Layers configured\n\nYou can now run BUILD.bat!",
            "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static GameObject CreateBoardFrame(int w, int h)
    {
        var frame = new GameObject("BoardFrame");
        frame.layer = 10;

        // Simple line border using thin quads
        CreateBorderQuad(frame, new Vector3(-0.5f, h / 2f - 0.5f, 0), new Vector3(0.1f, h + 1f, 0.1f)); // left
        CreateBorderQuad(frame, new Vector3(w - 0.5f, h / 2f - 0.5f, 0), new Vector3(0.1f, h + 1f, 0.1f)); // right
        CreateBorderQuad(frame, new Vector3(w / 2f - 0.5f, -0.5f, 0), new Vector3(w, 0.1f, 0.1f)); // bottom

        return frame;
    }

    private static void CreateBorderQuad(GameObject parent, Vector3 pos, Vector3 scale)
    {
        var go  = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Border";
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.layer = 10;
        // White high-contrast color for fusion lock
        go.GetComponent<Renderer>().material.color = Color.white;
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
    }

    private static GameObject[] CreateTetrominoPrefabs()
    {
        // Create 7 colored block prefabs (I, O, T, S, Z, J, L)
        Color[] colors =
        {
            new Color(0f,   1f,   1f),   // I — Cyan
            new Color(1f,   1f,   0f),   // O — Yellow
            new Color(0.5f, 0f,   1f),   // T — Purple
            new Color(0f,   1f,   0f),   // S — Green
            new Color(1f,   0f,   0f),   // Z — Red
            new Color(0f,   0f,   1f),   // J — Blue
            new Color(1f,   0.5f, 0f),   // L — Orange
        };

        // Each tetromino shape as (x,y) offsets from pivot
        Vector2Int[][] shapes =
        {
            new[]{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0) }, // I
            new[]{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) }, // O
            new[]{ new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) }, // T
            new[]{ new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1) }, // S
            new[]{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) }, // Z
            new[]{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) }, // J
            new[]{ new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) }, // L
        };

        string prefabDir = "Assets/Resources/Prefabs";
        System.IO.Directory.CreateDirectory(prefabDir);
        AssetDatabase.Refresh();

        var result = new GameObject[7];
        string[] names = { "I","O","T","S","Z","J","L" };

        for (int i = 0; i < 7; i++)
        {
            var root = new GameObject($"Piece_{names[i]}");
            root.AddComponent<Tetromino>();
            root.AddComponent<PlayerInput>(); // New Input System

            foreach (var offset in shapes[i])
            {
                var cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cell.name = "Cell";
                cell.transform.SetParent(root.transform);
                cell.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
                cell.transform.localScale    = Vector3.one * 0.95f;

                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = colors[i];
                mat.SetFloat("_Surface", 1); // Transparent
                cell.GetComponent<Renderer>().material = mat;

                Object.DestroyImmediate(cell.GetComponent<MeshCollider>());
            }

            string prefabPath = $"{prefabDir}/Piece_{names[i]}.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            result[i] = saved;
        }

        return result;
    }

    private static GameObject CreateWorldCanvas(Transform parent, Camera cam)
    {
        var canvasGO = new GameObject("UI_Canvas");
        canvasGO.layer = 10; // FusionLock
        canvasGO.transform.SetParent(parent);
        canvasGO.transform.localPosition = new Vector3(0f, 0f, 5f);
        canvasGO.transform.localScale    = Vector3.one * 0.01f;

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.worldCamera = cam;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 600);

        return canvasGO;
    }

    private static GameObject CreateMainMenuUI(GameObject canvas, GameManager gm)
    {
        var panel = CreatePanel(canvas, "MainMenuPanel");

        var title = CreateText(panel, "Title", "DICHOPTIC\nTETRIS", 60, new Vector2(0, 100));

        var startBtn = CreateButton(panel, "StartButton", "START GAME", new Vector2(0, -50));
        startBtn.GetComponent<Button>().onClick.AddListener(gm.StartGame);

        return panel;
    }

    private static GameObject CreateGameplayUI(GameObject canvas, GameManager gm,
        out TMP_Text score, out TMP_Text level, out TMP_Text lines)
    {
        var panel = CreatePanel(canvas, "GameplayPanel");
        panel.SetActive(false);

        score = CreateText(panel, "Score", "Score\n0",    28, new Vector2(-300, 200));
        level = CreateText(panel, "Level", "Level\n1",    28, new Vector2(-300, 100));
        lines = CreateText(panel, "Lines", "Lines\n0",    28, new Vector2(-300,   0));

        var menuBtn = CreateButton(panel, "MenuButton", "MENU", new Vector2(300, 200));
        menuBtn.GetComponent<Button>().onClick.AddListener(gm.ReturnToMenu);

        return panel;
    }

    private static GameObject CreateGameOverUI(GameObject canvas, GameManager gm)
    {
        var panel = CreatePanel(canvas, "GameOverPanel");
        panel.SetActive(false);

        CreateText(panel, "Title", "GAME OVER", 60, new Vector2(0, 100));

        var retryBtn = CreateButton(panel, "RetryButton", "PLAY AGAIN", new Vector2(0, -50));
        retryBtn.GetComponent<Button>().onClick.AddListener(gm.StartGame);

        var menuBtn = CreateButton(panel, "MenuButton", "MENU", new Vector2(0, -130));
        menuBtn.GetComponent<Button>().onClick.AddListener(gm.ReturnToMenu);

        return panel;
    }

    private static GameObject CreatePanel(GameObject canvas, string name)
    {
        var go = new GameObject(name);
        go.layer = 10;
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.7f);
        return go;
    }

    private static TMP_Text CreateText(GameObject parent, string name, string text, int size, Vector2 pos)
    {
        var go = new GameObject(name);
        go.layer = 10;
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(400, 80);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        return t;
    }

    private static GameObject CreateButton(GameObject parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name);
        go.layer = 10;
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(300, 60);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f);
        var btn = go.AddComponent<Button>();

        var txtGO = new GameObject("Label");
        txtGO.layer = 10;
        txtGO.transform.SetParent(go.transform, false);
        var trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var t = txtGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 28;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;

        return go;
    }

    private static void EnsureLayer(int index, string name)
    {
        var tm = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("layers");
        if (layers == null) return;
        var el = layers.GetArrayElementAtIndex(index);
        if (el.stringValue == name) return;
        el.stringValue = name;
        tm.ApplyModifiedProperties();
    }
}
