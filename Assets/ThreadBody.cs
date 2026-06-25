using UnityEngine;

public class ThreadBody : MonoBehaviour
{
    public BoxCollider2D boxCollider;
    private Thread _thread;

    void Start()
    {
        _thread = GetComponentInParent<Thread>();
        if (boxCollider != null)
            boxCollider.enabled = false; // 기본 비활성 — 필요시 외부에서 활성화
    }
}
