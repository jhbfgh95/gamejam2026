using NUnit;
using Unity.VisualScripting;
using UnityEngine;

public class Thread : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public Hand ThreadOwner;
    public Hand ConnectedThreadOwner;
    public Thread ConnectedThread;


    public Vector2 OrginPosition;
    public LineRenderer lineRenderer;

    public CircleCollider2D CircleCollider;


    public Collider2D[] colliders = new Collider2D[10];
    public float radius;
    public LayerMask ObjectLayer;


    private void Awake()
    {

        OrginPosition = transform.position;
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
    }

    public void InitThread(Vector3 LineStartPostion, Vector3 LineEndPostion)
    {

        lineRenderer.positionCount = 2;
        // 시작점
        lineRenderer.SetPosition(0, LineStartPostion);
        lineRenderer.SetPosition(1, LineEndPostion);
        // 끝점

    }


    // Update is called once per frame
    void Update()
    {
        //lineRenderer.SetPosition(1, transform.position);
    }


    public void MoveThreadEndPoint(Vector3 vector3)
    {
        float length = Vector3.Distance(OrginPosition, vector3);
        lineRenderer.SetPosition(1, vector3);
    }


    public void ConectThread(Thread threadValue)
    {

        if (threadValue != null)
        {
            ConnectedThread = threadValue;
            ConnectedThreadOwner = ConnectedThread.GetComponent<Hand>();
        }
        
    }


    public void DropThread()
    {
        colliders = Physics2D.OverlapCircleAll(transform.position, radius, ObjectLayer);

        Thread ColliderThread = null;
        foreach (Collider2D hit in colliders)
        {
            if (hit.TryGetComponent<Thread>(out ColliderThread))
            {
                break;
            }
        }
        ConectThread(ColliderThread);
        ConnectedThread.ConectThread(this);
    }

    public Hand GetThreadOwner()
    {
        return ThreadOwner;
    }


    public Hand GetConnectedThreadOwner()
    {
        return ConnectedThreadOwner;
    }
}
