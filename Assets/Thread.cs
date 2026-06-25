using UnityEngine;
using UnityEngine.EventSystems;

[ExecuteAlways]
public class Thread : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Hand ThreadOwner;
    public Hand ConnectedThreadOwner;
    public Thread ConnectedThread;

    public Transform anchorTransform;     // 새끼손가락 기준점 Transform
    public Vector2 OrginPosition;
    public LineRenderer lineRenderer;
    public SpriteRenderer endpointSprite; // 드래그 가능한 끝점 이미지
    public Transform innatePoint;         // 혈연 선 클릭 감지용 중간점

    public CircleCollider2D CircleCollider;
    public Collider2D[] colliders = new Collider2D[10];
    public float radius;
    public LayerMask ObjectLayer;

    public bool isInnate;
    public float restLength = 0.5f; // 연결 끊길 때 PinkyTip에서 떨어지는 기본 길이

    [Header("Snap Sprite")]
    [SerializeField] public Sprite snapSprite;
    [SerializeField] public float snapSpriteScale = 0.5f;
    private SpriteRenderer _snapRenderer;

    [Header("Innate Knot Sprite")]
    [SerializeField] public Sprite innateKnotSprite;

    private bool _isDragging;

    private void Awake()
    {
        OrginPosition = anchorTransform != null
            ? (Vector2)anchorTransform.position
            : (Vector2)transform.position;
        lineRenderer = GetComponent<LineRenderer>();
        CreateEndpointDot();
        CreateSnapDot();
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        InitThread(OrginPosition, transform.position);
        RefreshVisual();
        if (_snapRenderer != null) _snapRenderer.enabled = false;
    }

    void CreateEndpointDot()
    {
        var existing = transform.Find("EndpointDot");
        if (existing != null)
        {
            endpointSprite = existing.GetComponent<SpriteRenderer>();
            return;
        }

        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f, dy = y - r + 0.5f;
                tex.SetPixel(x, y, dx * dx + dy * dy <= r * r ? Color.white : Color.clear);
            }
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);

        var dotGo = new GameObject("EndpointDot");
        dotGo.transform.SetParent(transform);
        dotGo.transform.localPosition = Vector3.zero;
        dotGo.transform.localScale = Vector3.one * 0.3f;
        endpointSprite = dotGo.AddComponent<SpriteRenderer>();
        endpointSprite.sprite = sprite;
        endpointSprite.color = new Color(0.7f, 0f, 0f, 1f);
        endpointSprite.sortingOrder = 2;

        var col = dotGo.AddComponent<CircleCollider2D>();
        col.radius = 0.2f;
        CircleCollider = col;

        dotGo.AddComponent<EndpointInput>();
    }

    void CreateSnapDot()
    {
        var existing = transform.Find("SnapDot");
        if (existing != null)
        {
            _snapRenderer = existing.GetComponent<SpriteRenderer>();
        }
        else
        {
            var go = new GameObject("SnapDot");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            _snapRenderer = go.AddComponent<SpriteRenderer>();
            _snapRenderer.sortingOrder = 4;
        }
        _snapRenderer.sprite = snapSprite;
        _snapRenderer.transform.localScale = Vector3.one * snapSpriteScale;
        _snapRenderer.enabled = false;
    }

    void OnValidate()
    {
        if (_snapRenderer == null)
        {
            var t = transform.Find("SnapDot");
            if (t != null) _snapRenderer = t.GetComponent<SpriteRenderer>();
        }
        if (_snapRenderer != null)
        {
            _snapRenderer.sprite = snapSprite;
            _snapRenderer.transform.localScale = Vector3.one * snapSpriteScale;
        }

        if (innatePoint != null)
        {
            var sr = innatePoint.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = innateKnotSprite;
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        // 앵커 위치 추적 (손이 움직일 경우 대비)
        if (anchorTransform != null)
        {
            OrginPosition = anchorTransform.position;
            lineRenderer.SetPosition(0, OrginPosition);
        }

        // 혈연 선의 중간점 위치 갱신
        if (isInnate && innatePoint != null)
        {
            Vector2 mid = ((Vector2)OrginPosition + (Vector2)transform.position) * 0.5f;
            innatePoint.position = mid;
        }
    }

    public void InitThread(Vector3 lineStart, Vector3 lineEnd)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, lineStart);
        lineRenderer.SetPosition(1, lineEnd);
    }

    public void MoveThreadEndPoint(Vector3 pos)
    {
        transform.position = pos;
        lineRenderer.SetPosition(1, pos);
    }

    public void ConectThread(Thread threadValue)
    {
        if (threadValue != null)
        {
            ConnectedThread = threadValue;
            ConnectedThreadOwner = threadValue.ThreadOwner;
        }
    }

    public void DropThread()
    {
        colliders = Physics2D.OverlapCircleAll(transform.position, radius, ObjectLayer);

        Thread found = null;
        foreach (Collider2D hit in colliders)
        {
            Thread t = hit.GetComponentInParent<Thread>();
            if (t != null && t != this && t.ThreadOwner != ThreadOwner)
            {
                found = t;
                break;
            }
        }

        ConectThread(found);
        if (ConnectedThread != null)
        {
            ConnectedThread.ConectThread(this);

            // 두 끝점을 같은 위치(타겟 끝점)로 스냅 → 하나의 점으로 합쳐짐
            MoveThreadEndPoint(ConnectedThread.transform.position);
            if (_snapRenderer != null) _snapRenderer.enabled = snapSprite != null;
            if (ConnectedThread._snapRenderer != null) ConnectedThread._snapRenderer.enabled = ConnectedThread.snapSprite != null;

            Hand myHand = ThreadOwner;
            Hand otherHand = ConnectedThread.ThreadOwner;
            if (myHand != null && otherHand != null)
            {
                if (!myHand.connectedHands.Contains(otherHand))
                    myHand.connectedHands.Add(otherHand);
                if (!otherHand.connectedHands.Contains(myHand))
                    otherHand.connectedHands.Add(myHand);
                myHand.RecalculateWave();
                otherHand.RecalculateWave();
            }
        }
    }

    public void Disconnect()
    {
        if (ConnectedThread == null) return;

        Thread other = ConnectedThread;
        Hand myHand = ThreadOwner;
        Hand otherHand = other.ThreadOwner;

        if (myHand != null && otherHand != null)
        {
            myHand.connectedHands.Remove(otherHand);
            otherHand.connectedHands.Remove(myHand);
        }

        ConnectedThread = null;
        ConnectedThreadOwner = null;
        other.ConnectedThread = null;
        other.ConnectedThreadOwner = null;

        if (_snapRenderer != null) _snapRenderer.enabled = false;
        if (other._snapRenderer != null) other._snapRenderer.enabled = false;

        RetractToRest();
        other.RetractToRest();

        myHand?.RecalculateWave();
        otherHand?.RecalculateWave();
    }

    // 혈연 연결 끊기: 선이 두 끝점으로 분리됨
    public void BreakInnate()
    {
        if (!isInnate) return;
        Thread other = ConnectedThread; // Disconnect() 전에 저장
        isInnate = false;
        if (other != null) other.isInnate = false;
        Disconnect();
        RefreshVisual();
        other?.RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (endpointSprite != null)
            endpointSprite.enabled = !isInnate;
        if (CircleCollider != null)
            CircleCollider.enabled = !isInnate;
        if (innatePoint != null)
            innatePoint.gameObject.SetActive(isInnate);
    }

    void RetractToRest()
    {
        Vector2 dir = (Vector2)transform.position - OrginPosition;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
        else dir = dir.normalized;
        MoveThreadEndPoint((Vector3)OrginPosition + (Vector3)(dir * restLength));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isInnate) return;
        Debug.Log(gameObject.name);
        if (ConnectedThread != null)
        {
            Disconnect();
            return; // 연결 끊기만, 드래그 시작 안 함
        }
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(eventData.position);
        mouseWorld.z = 0f;
        MoveThreadEndPoint(mouseWorld);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;
        DropThread();
        // 연결 안 됐을 때는 드롭한 위치에 그대로 멈춤
    }

    public Hand GetThreadOwner() => ThreadOwner;
    public Hand GetConnectedThreadOwner() => ConnectedThreadOwner;
}
