using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;

// GameJam 메뉴 → Setup Stage 1 실행 시 스테이지 1 씬을 자동 구성.
// 실행 후 각 오브젝트의 SpriteRenderer > Sprite 필드에 이미지 파일만 할당하면 됨.
public class Stage1Setup : EditorWindow
{
    [MenuItem("GameJam/Setup Camera and Input")]
    static void SetupCameraAndInput()
    {
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<Physics2DRaycaster>() == null)
            cam.gameObject.AddComponent<Physics2DRaycaster>();

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        Debug.Log("[GameJam] Camera & Input 설정 완료.");
    }

    [MenuItem("GameJam/Setup Stage 1")]
    static void Run()
    {
        CreateWaveDataAssets();
        ClearOldNodes();
        CreateGameManager();

        WaveData waveA = Load("WaveA");
        WaveData waveB = Load("WaveB");
        WaveData waveC = Load("WaveC");
        WaveData waveD = Load("WaveD");

        // 노드 4개 생성
        // A[t²]  ←혈연→  B[-파라볼라]   (초기 연결 = 잘못된 쌍)
        // 해답: A↔C 연결(t²+-t²=0), B↔D 연결(-파라볼라+파라볼라=0)
        Hand hand1 = CreateHandNode("Hand1", new Vector2(-3f,  1.5f), waveA); // A = sin
        Hand hand2 = CreateHandNode("Hand2", new Vector2( 3f,  1.5f), waveB); // B = -parabola
        Hand hand3 = CreateHandNode("Hand3", new Vector2(-3f, -1.5f), waveC); // C = -sin
        Hand hand4 = CreateHandNode("Hand4", new Vector2( 3f, -1.5f), waveD); // D = +parabola

        Thread t1 = CreateThread(hand1, "Thread_N1", innate: true);
        Thread t2 = CreateThread(hand2, "Thread_N2", innate: true);
        Thread t3 = CreateThread(hand3, "Thread_N3", innate: false);
        Thread t4 = CreateThread(hand4, "Thread_N4", innate: false);

        // 혈연 연결: Hand1(A) ↔ Hand2(B)
        LinkInnate(t1, t2, hand1, hand2);

        Debug.Log("[Stage1Setup] 완료. 각 오브젝트의 SpriteRenderer > Sprite 에 이미지를 할당하세요.");
    }

    // ── WaveData 에셋 ─────────────────────────────────────────────────

    static void CreateWaveDataAssets()
    {
        const string dir = "Assets/WaveData";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "WaveData");

        MakeWave("WaveA", dir, t => t * t);                                // A: t² 가속 상승 0→1
        MakeWave("WaveB", dir, t => -4f * t * (1f - t));                // B: -파라볼라 아래볼록 0→-1→0
        MakeWave("WaveC", dir, t => -(t * t));                          // C: -t² 가속 하강 0→-1 (A 상쇄)
        MakeWave("WaveD", dir, t => 4f * t * (1f - t));                 // D: +파라볼라 위볼록 0→+1→0 (B 상쇄)

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

    [MenuItem("GameJam/Update Stage 1 Waves")]
    static void UpdateWaves()
    {
        const string dir = "Assets/WaveData";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "WaveData");

        MakeWave("WaveA", dir, t => t * t);
        MakeWave("WaveB", dir, t => -4f * t * (1f - t));
        MakeWave("WaveC", dir, t => -(t * t));
        MakeWave("WaveD", dir, t => 4f * t * (1f - t));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GameJam] Wave 업데이트 완료. A=t², B=-파라볼라, C=-t², D=+파라볼라");
    }

    [MenuItem("GameJam/Reconnect Stage 1 Innate")]
    static void ReconnectStage1Innate()
    {
        var hand1 = GameObject.Find("Hand1")?.GetComponent<Hand>();
        var hand2 = GameObject.Find("Hand2")?.GetComponent<Hand>();
        var hand3 = GameObject.Find("Hand3")?.GetComponent<Hand>();
        if (hand1 == null || hand2 == null || hand3 == null)
        {
            Debug.LogError("Hand1/2/3을 씬에서 찾을 수 없어요."); return;
        }

        var t1 = hand1.transform.Find("Thread_N1")?.GetComponent<Thread>();
        var t2 = hand2.transform.Find("Thread_N2")?.GetComponent<Thread>();
        var t3 = hand3.transform.Find("Thread_N3")?.GetComponent<Thread>();
        if (t1 == null || t2 == null || t3 == null)
        {
            Debug.LogError("Thread_N1/2/3을 찾을 수 없어요."); return;
        }

        // 기존 혈연 해제
        t1.ConnectedThread = null; t1.ConnectedThreadOwner = null;
        t3.ConnectedThread = null; t3.ConnectedThreadOwner = null;
        hand1.connectedHands.Remove(hand3);
        hand3.connectedHands.Remove(hand1);

        // innate 플래그 변경
        t2.isInnate = true;
        t3.isInnate = false;
        if (t2.innatePoint != null) t2.innatePoint.gameObject.SetActive(true);
        if (t3.innatePoint != null) t3.innatePoint.gameObject.SetActive(false);

        // Hand1 ↔ Hand2 혈연 연결
        LinkInnate(t1, t2, hand1, hand2);

        EditorUtility.SetDirty(t1.gameObject);
        EditorUtility.SetDirty(t2.gameObject);
        EditorUtility.SetDirty(t3.gameObject);
        Debug.Log("[GameJam] 혈연 재연결 완료: Hand1(A) ↔ Hand2(B)");
    }

    static WaveData Load(string name) =>
        AssetDatabase.LoadAssetAtPath<WaveData>($"Assets/WaveData/{name}.asset");

    // ── 씬 오브젝트 ───────────────────────────────────────────────────

    static void ClearOldNodes()
    {
        foreach (var name in new[] { "Hand1", "Hand2", "Hand3", "Hand4",
                                     "Node1", "Node2", "Node3", "Node4", "GameManager" })
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    static void CreateGameManager()
    {
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    // Hand 노드 구조:
    //   Hand (SpriteRenderer ← 손 이미지 여기에)
    //     PinkyTip (위치 기준점 — 손 이미지 맞춰서 localPosition 조정)
    //     WaveGraphRenderer + LineRenderer (파동 그래프 시각화)
    static Hand CreateHandNode(string nodeName, Vector2 pos, WaveData wave)
    {
        var go = new GameObject(nodeName);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        // ★ 손 이미지: Sprite 필드에 이미지 파일 할당
        var handSprite = go.AddComponent<SpriteRenderer>();
        handSprite.sortingOrder = 0;

        var hand = go.AddComponent<Hand>();
        hand.waves.Add(wave);

        // 파동 그래프 LineRenderer (WaveGraphRenderer가 사용)
        var graphMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        graphMat.color = Color.white;
        var graphLr = go.AddComponent<LineRenderer>();
        graphLr.startWidth = 0.02f;
        graphLr.endWidth = 0.02f;
        graphLr.sortingOrder = 3;
        graphLr.material = graphMat;
        go.AddComponent<WaveGraphRenderer>();

        // 새끼손가락 기준점 (손 이미지 적용 후 localPosition을 손가락 끝에 맞게 조정)
        var pinky = new GameObject("PinkyTip");
        pinky.transform.SetParent(go.transform);
        pinky.transform.localPosition = new Vector3(-0.3f, -0.5f, 0f);

        return hand;
    }

    // Thread 구조:
    //   Thread_Hx (SpriteRenderer ← 끝점 이미지 여기에 / CircleCollider2D)
    //     InnatePoint (SpriteRenderer ← 혈연 중간점 이미지 여기에 / CircleCollider2D)
    static Thread CreateThread(Hand owner, string threadName, bool innate)
    {
        Transform pinky = owner.transform.Find("PinkyTip");

        var go = new GameObject(threadName);
        go.transform.SetParent(owner.transform);
        go.transform.position = pinky != null ? pinky.position : owner.transform.position;

        var thread = go.AddComponent<Thread>();
        thread.ThreadOwner = owner;
        thread.anchorTransform = pinky;
        thread.isInnate = innate;
        thread.radius = 0.25f;
        thread.ObjectLayer = ~0; // 모든 레이어에서 끝점 탐지

        // 실 선 LineRenderer — 색상만 빨간색, 텍스처 없음
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

        // 혈연 중간점 (선 클릭 감지, 매 프레임 선의 중간으로 이동)
        var midGo = new GameObject("InnatePoint");
        midGo.transform.SetParent(go.transform);
        midGo.transform.localPosition = Vector3.zero;
        midGo.AddComponent<CircleCollider2D>().radius = 0.2f;

        // ★ 혈연 중간점 이미지: Sprite 필드에 이미지 파일 할당 (매듭 표시용)
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

        tA.ConnectedThread = tB;
        tA.ConnectedThreadOwner = handB;
        tB.ConnectedThread = tA;
        tB.ConnectedThreadOwner = handA;

        if (!handA.connectedHands.Contains(handB)) handA.connectedHands.Add(handB);
        if (!handB.connectedHands.Contains(handA)) handB.connectedHands.Add(handA);
    }
}
