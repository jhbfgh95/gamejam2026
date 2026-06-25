using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Hand[] allHands;
    [SerializeField] private float comfortThreshold = 0.05f;

    public bool IsCleared { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (allHands == null || allHands.Length == 0)
            allHands = FindObjectsByType<Hand>(FindObjectsSortMode.None);
    }

    public void CheckWinCondition()
    {
        if (IsCleared) return;
        if (allHands == null || allHands.Length == 0) return;

        foreach (var hand in allHands)
        {
            if (hand == null) continue;
            if (Mathf.Abs(hand.integralValue) >= comfortThreshold) return;
        }

        IsCleared = true;
        OnPuzzleCleared();
    }

    private void OnPuzzleCleared()
    {
        Debug.Log("[GameManager] All nodes at rest — puzzle cleared!");
        // TODO: 클리어 연출 (UI, 이펙트 등)
    }
}
