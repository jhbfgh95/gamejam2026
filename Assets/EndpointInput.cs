using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CircleCollider2D))]
public class EndpointInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private Thread _thread;

    void Start()
    {
        _thread = GetComponentInParent<Thread>();
    }

    public void OnPointerDown(PointerEventData eventData) => _thread?.OnPointerDown(eventData);
    public void OnDrag(PointerEventData eventData)       => _thread?.OnDrag(eventData);
    public void OnPointerUp(PointerEventData eventData)  => _thread?.OnPointerUp(eventData);

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.7f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
