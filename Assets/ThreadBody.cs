
using UnityEngine;
using UnityEngine.UIElements;

public class ThreadBody : MonoBehaviour
{

    public BoxCollider2D boxCollider;
    Vector2 OrginPosition;
    Thread ThreadEndPoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ThreadEndPoint = transform.GetComponentInParent<Thread>();
    }


    public void SetBodySize()
    {
        float length = Vector3.Distance(ThreadEndPoint.OrginPosition, ThreadEndPoint.transform.position);
        boxCollider.size = new Vector3(length, 0.2f, 0.2f);
        boxCollider.offset = new Vector3(length * 0.5f, 0f, 0f);
    }
}
