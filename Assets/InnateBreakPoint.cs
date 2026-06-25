using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CircleCollider2D))]
public class InnateBreakPoint : MonoBehaviour, IPointerClickHandler
{
    private Thread _thread;

    void Start()
    {
        _thread = GetComponentInParent<Thread>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _thread?.BreakInnate();
    }
}
