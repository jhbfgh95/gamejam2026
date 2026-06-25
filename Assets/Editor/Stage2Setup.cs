using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class Stage2Setup : EditorWindow
{
    [MenuItem("GameJam/Setup Stage 2")]
    static void Run()
    {
        CreateWaveDataAssets();
        ClearOldNodes();
        CreateGameManager();

        WaveData w2A = Load("Stage2/Wave2A");
        WaveData w2B = Load("Stage2/Wave2B");
        WaveData w2C = Load("Stage2/Wave2C");
        WaveData w2D = Load("Stage2/Wave2D");
        WaveData w2E = Load("Stage2/Wave2E");
        WaveData w2F = Load("Stage2/Wave2F");

        // 6 노드 2열 3행
        // A[t³]    B[t]
        // C[t^⅓]   D[-t³]
        // E[-t]    F[-t^⅓]
        // 혈연: A↔B(수평 상단), A↔F(대각선 좌상→우하)
        // 해답: A↔D, B↔E, C↔F 연결
        Hand handA = CreateHandNode("Hand1", new Vector2(-3f,  2.5f), w2A);
        Hand handB = CreateHandNode("Hand2", new Vector2( 3f,  2.5f), w2B);
        Hand handC = CreateHandNode("Hand3", new Vector2(-3f,  0f),   w2C);
        Hand handD = CreateHandNode("Hand4", new Vector2( 3f,  0f),   w2D);
        Hand handE = CreateHandNode("Hand5", new Vector2(-3f, -2.5f), w2E);
        Hand handF = CreateHandNode("Hand6", new Vector2( 3f, -2.5f), w2F);

        // 각 노드 실 2개씩
        // C/D/E는 자유선 2개가 PinkyTip에서 겹치므로 Thread_N2에 endpointOffset 전달.
        // CreateThread 내부에서 AddComponent 전에 위치를 설정해 EndpointDot이 처음부터 올바른 위치에 생성됨.
        Thread tA1 = CreateThread(handA, "Thread_N1", innate: true);
        Thread tA2 = CreateThread(handA, "Thread_N2", innate: true);
        Thread tB1 = CreateThread(handB, "Thread_N1", innate: true);
        CreateThread(handB, "Thread_N2", innate: false);
        CreateThread(handC, "Thread_N1", innate: false);
        CreateThread(handC, "Thread_N2", innate: false, new Vector3( 0.5f, -0.4f, 0f));
        CreateThread(handD, "Thread_N1", innate: false);
        CreateThread(handD, "Thread_N2", innate: false, new Vector3(-0.5f, -0.4f, 0f));
        CreateThread(handE, "Thread_N1", innate: false);
        CreateThread(handE, "Thread_N2", innate: false, new Vector3( 0.5f, -0.4f, 0f));
        Thread tF1 = CreateThread(handF, "Thread_N1", innate: true);
        CreateThread(handF, "Thread_N2", innate: false);

        // 혈연 연결
        LinkInnate(tA1, tB1, handA, handB);  // A↔B 수평
        LinkInnate(tA2, tF1, handA, handF);  // A↔F 대각선

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Stage2Setup] 완료. 혈연: A↔B, A↔F | 해답: A↔D, B↔E, C↔F");
    }

    // ── 혈연 재연결 (스프라이트/위치 유지, 연결만 리셋) ──────────────────────

    [MenuItem("GameJam/Reconnect Stage 2 Innate")]
    static void ReconnectStage2Innate()
    {
        var hand1 = GameObject.Find("Hand1")?.GetComponent<Hand>();
        var hand2 = GameObject.Find("Hand2")?.GetComponent<Hand>();
        var hand6 = GameObject.Find("Hand6")?.GetComponent<Hand>();
        if (hand1 == null || hand2 == null || hand6 == null)
        {
            Debug.LogError("[Stage2Setup] Hand1/2/6을 씬에서 찾을 수 없어요."); return;
        }

        var tA1 = hand1.transform.Find("Thread_N1")?.GetComponent<Thread>();
        var tA2 = hand1.transform.Find("Thread_N2")?.GetComponent<Thread>();
        var tB1 = hand2.transform.Find("Thread_N1")?.GetComponent<Thread>();
        var tF1 = hand6.transform.Find("Thread_N1")?.GetComponent<Thread>();
        if (tA1 == null || tA2 == null || tB1 == null || tF1 == null)
        {
            Debug.LogError("[Stage2Setup] Thread_N1/N2를 찾을 수 없어요."); return;
        }

        // 기존 혈연 연결 해제
        foreach (var t in new[] { tA1, tA2, tB1, tF1 })
        {
            t.ConnectedThread = null;
            t.ConnectedThreadOwner = null;
        }
        hand1.connectedHands.Clear();
        hand2.connectedHands.Remove(hand1);
        hand6.connectedHands.Remove(hand1);

        // innate 플래그 복원
        tA1.isInnate = true; tA2.isInnate = true;
        tB1.isInnate = true; tF1.isInnate = true;
        if (tA1.innatePoint != null) tA1.innatePoint.gameObject.SetActive(true);
        if (tA2.innatePoint != null) tA2.innatePoint.gameObject.SetActive(true);
        if (tB1.innatePoint != null) tB1.innatePoint.gameObject.SetActive(true);
        if (tF1.innatePoint != null) tF1.innatePoint.gameObject.SetActive(true);

        // 혈연 재연결
        LinkInnate(tA1, tB1, hand1, hand2);
        LinkInnate(tA2, tF1, hand1, hand6);

        foreach (var t in new[] { tA1, tA2, tB1, tF1 })
            EditorUtility.SetDirty(t.gameObject);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Stage2Setup] 혈연 재연결 완료: Hand1(A)↔Hand2(B), Hand1(A)↔Hand6(F)");
    }

    [MenuItem("GameJam/Update Stage 2 Waves")]
    static void UpdateStage2Waves()
    {
        CreateWaveDataAssets();
        Debug.Log("[Stage2Setup] Stage 2 Wave 에셋 갱신 완료.");
    }

    // ── WaveData 에셋 ─────────────────────────────────────────────────────
    // 적분값: A=0.25, B=0.50, C=0.75 / D=-0.25, E=-0.50, F=-0.75
    // 잘못된 쌍의 최소 합산적분 = 0.25 >> 임계값 0.05 → 오작동 없음

    static void CreateWaveDataAssets()
    {
        const string dir = "Assets/WaveData/Stage2";
        if (!AssetDatabase.IsValidFolder("Assets/WaveData"))
            AssetDatabase.CreateFolder("Assets", "WaveData");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/WaveData", "Stage2");

        MakeWave("Wave2A", dir, t => Mathf.Pow(t, 3f));                        // t³: 느린 시작, 빠른 끝
        MakeWave("Wave2B", dir, t => t);                                        // t: 선형 상승
        MakeWave("Wave2C", dir, t => Mathf.Pow(Mathf.Max(t, 0f), 1f / 3f));   // t^⅓: 빠른 시작, 느린 끝
        MakeWave("Wave2D", dir, t => -Mathf.Pow(t, 3f));                       // -t³ (A 상쇄)
        MakeWave("Wave2E", dir, t => -t);                                       // -t  (B 상쇄)
        MakeWave("Wave2F", dir, t => -Mathf.Pow(Mathf.Max(t, 0f), 1f / 3f));  // -t^⅓ (C 상쇄)

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void MakeWave(string name, string dir, System.Func<float, float> f)
    {
        string path = $"{dir}/{name}.asset";
        var data = AssetDatabase.LoadAssetAtPath<WaveData>(path);
        bool isNew = data == null;
        if (isNew) data = ScriptableObject.CreateInstance<WaveData>();

        int n = 10;
        var keys = new Keyframe[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / (n - 1);
            keys[i] = new Keyframe(t, f(t));
        }
        data.curve = new AnimationCurve(keys);
        for (int i = 0; i < data.curve.keys.Length; i++)
            data.curve.SmoothTangents(i, 0f);

        if (isNew) AssetDatabase.CreateAsset(data, path);
        else EditorUtility.SetDirty(data);
    }

    static WaveData Load(string name) =>
        AssetDatabase.LoadAssetAtPath<WaveData>($"Assets/WaveData/{name}.asset");

    // ── 씬 오브젝트 ──────────────────────────────────────────────────────

    static void ClearOldNodes()
    {
        foreach (var n in new[] { "Hand1","Hand2","Hand3","Hand4","Hand5","Hand6","GameManager" })
        {
            var go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    static void CreateGameManager()
    {
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    static Hand CreateHandNode(string nodeName, Vector2 pos, WaveData wave)
    {
        var go = new GameObject(nodeName);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        go.AddComponent<SpriteRenderer>().sortingOrder = 0;

        var hand = go.AddComponent<Hand>();
        hand.waves.Add(wave);

        var graphMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        graphMat.color = Color.white;
        var graphLr = go.AddComponent<LineRenderer>();
        graphLr.startWidth = 0.02f;
        graphLr.endWidth = 0.02f;
        graphLr.sortingOrder = 3;
        graphLr.material = graphMat;
        go.AddComponent<WaveGraphRenderer>();

        var pinky = new GameObject("PinkyTip");
        pinky.transform.SetParent(go.transform);
        pinky.transform.localPosition = new Vector3(-0.3f, -0.5f, 0f);

        return hand;
    }

    static Thread CreateThread(Hand owner, string threadName, bool innate, Vector3 endpointOffset = default)
    {
        Transform pinky = owner.transform.Find("PinkyTip");

        var go = new GameObject(threadName);
        go.transform.SetParent(owner.transform);
        Vector3 basePos = pinky != null ? pinky.position : owner.transform.position;
        go.transform.position = basePos + endpointOffset; // Awake 전에 위치 확정 → EndpointDot이 여기에 생성됨

        var thread = go.AddComponent<Thread>();
        thread.ThreadOwner = owner;
        thread.anchorTransform = pinky;
        thread.isInnate = innate;
        thread.radius = 0.25f;
        thread.ObjectLayer = ~0;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.color = Color.red;
        var lr = go.AddComponent<LineRenderer>();
        lr.startWidth = 0.15f;
        lr.endWidth = 0.15f;
        lr.startColor = Color.red;
        lr.endColor = Color.red;
        lr.sortingOrder = 1;
        lr.useWorldSpace = true;
        lr.material = mat;
        thread.lineRenderer = lr;

        // 에디터에서 InitThread()가 호출되지 않으므로 여기서 직접 초기화
        lr.positionCount = 2;
        lr.SetPosition(0, basePos);
        lr.SetPosition(1, go.transform.position);

        var midGo = new GameObject("InnatePoint");
        midGo.transform.SetParent(go.transform);
        midGo.transform.localPosition = Vector3.zero;
        midGo.AddComponent<CircleCollider2D>().radius = 0.2f;
        midGo.AddComponent<SpriteRenderer>().sortingOrder = 2;
        midGo.AddComponent<InnateBreakPoint>();
        midGo.SetActive(innate);
        thread.innatePoint = midGo.transform;

        return thread;
    }

    static void LinkInnate(Thread tA, Thread tB, Hand handA, Hand handB)
    {
        Transform pinkyA = handA.transform.Find("PinkyTip");
        Transform pinkyB = handB.transform.Find("PinkyTip");

        tA.transform.position = pinkyB != null ? pinkyB.position : handB.transform.position;
        tB.transform.position = pinkyA != null ? pinkyA.position : handA.transform.position;

        // 에디터에서 LineRenderer 위치를 혈연 연결 후 갱신
        Vector3 originA = pinkyA != null ? pinkyA.position : handA.transform.position;
        Vector3 originB = pinkyB != null ? pinkyB.position : handB.transform.position;
        if (tA.lineRenderer != null) { tA.lineRenderer.SetPosition(0, originA); tA.lineRenderer.SetPosition(1, tA.transform.position); }
        if (tB.lineRenderer != null) { tB.lineRenderer.SetPosition(0, originB); tB.lineRenderer.SetPosition(1, tB.transform.position); }

        tA.ConnectedThread = tB;
        tA.ConnectedThreadOwner = handB;
        tB.ConnectedThread = tA;
        tB.ConnectedThreadOwner = handA;

        if (!handA.connectedHands.Contains(handB)) handA.connectedHands.Add(handB);
        if (!handB.connectedHands.Contains(handA)) handB.connectedHands.Add(handA);
    }
}
