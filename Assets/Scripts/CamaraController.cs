using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Referencia (opcional)")]
    public TerrainGenerator terrain;

    [Header("Movimiento")]
    public float moveSpeed = 20f;
    public float fastMultiplier = 2.5f;

    [Header("Zoom")]
    public float zoomSpeed = 6f;
    public float minZoom = 5f;
    public float maxZoom = 64f;

    [Header("Límites del mapa")]
    public Vector2 mapMin = Vector2.zero;
    public Vector2 mapMax = new Vector2(128, 128);

    Camera cam;
    Vector3 lastMousePos;
    bool dragging;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (terrain != null)
        {
            mapMax = new Vector2(terrain.width, terrain.height);
            transform.position = new Vector3(mapMax.x / 2f, mapMax.y / 2f, transform.position.z);
        }
    }

    void Update()
    {
        if (terrain != null) mapMax = new Vector2(terrain.width, terrain.height);

        MoveWithKeys();
        DragWithMouse();
        Zoom();
        KeepInsideMap();
    }

    void MoveWithKeys()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        float speed = moveSpeed * (cam.orthographicSize / 20f);
        if (Input.GetKey(KeyCode.LeftShift)) speed *= fastMultiplier;

        Vector3 move = new Vector3(x, y, 0f).normalized * speed * Time.deltaTime;
        transform.position += move;
    }

    void DragWithMouse()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            dragging = !GenerationMenu.MouseOverMenu;
            lastMousePos = Input.mousePosition;
        }

        if (!Input.GetMouseButton(1) && !Input.GetMouseButton(2)) dragging = false;

        if (dragging)
        {
            Vector3 before = cam.ScreenToWorldPoint(lastMousePos);
            Vector3 now = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 delta = before - now;
            delta.z = 0f;
            transform.position += delta;
            lastMousePos = Input.mousePosition;
        }
    }

    void Zoom()
    {
        if (GenerationMenu.MouseOverMenu) return;

        float scroll = Input.mouseScrollDelta.y;
        if (scroll == 0f) return;

        float limit = terrain != null ? Mathf.Max(maxZoom, terrain.height * 0.5f) : maxZoom;
        float newSize = cam.orthographicSize - scroll * zoomSpeed;
        cam.orthographicSize = Mathf.Clamp(newSize, minZoom, limit);
    }

    void KeepInsideMap()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, mapMin.x, mapMax.x);
        p.y = Mathf.Clamp(p.y, mapMin.y, mapMax.y);
        transform.position = p;
    }
}