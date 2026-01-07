using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Draggable2D : MonoBehaviour
{
    Camera cam;
    Rigidbody2D rb;
    Vector3 offset;
    bool dragging;
    float originalGravity;
    int originalLayer;

    void Awake()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;
        originalLayer = gameObject.layer;
    }

    void OnMouseDown()
    {
        dragging = true;
        rb.velocity = Vector2.zero;
        rb.gravityScale = 0f;                    // float while dragging
        Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - new Vector3(m.x, m.y, transform.position.z);
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    void OnMouseDrag()
    {
        if (!dragging) return;
        Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 target = new Vector3(m.x, m.y, transform.position.z) + offset;
        rb.MovePosition(target);
    }

    void OnMouseUp()
    {
        dragging = false;
        rb.gravityScale = originalGravity;
        gameObject.layer = originalLayer;
    }
}
