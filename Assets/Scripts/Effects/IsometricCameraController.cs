using UnityEngine;
using UnityEngine.InputSystem; // Agregar este using

public class IsometricCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float cameraDistance = 15f;
    [SerializeField] private float cameraHeight = 10f;
    [SerializeField] private float cameraAngle = 45f;
    [SerializeField] private float rotationSpeed = 50f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 25f;

    [Header("Boundaries")]
    [SerializeField] private bool useBoundaries = true;
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 15f;
    [SerializeField] private float minZ = -5f;
    [SerializeField] private float maxZ = 15f;

    [Header("Edge Scrolling")]
    [SerializeField] private bool useEdgeScrolling = true;
    [SerializeField] private float edgeScrollSize = 20f;

    private Vector3 targetPosition;
    private float currentRotation = 45f;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        // Configurar posición inicial
        SetupInitialPosition();
    }

    void SetupInitialPosition()
    {
        // Centrar la cámara en el mapa
        if (MapGenerator.Instance != null)
        {
            float centerX = MapGenerator.Instance.GetMapWidth() / 2f;
            float centerZ = MapGenerator.Instance.GetMapHeight() / 2f;
            targetPosition = new Vector3(centerX, 0, centerZ);
        }
        else
        {
            targetPosition = new Vector3(5, 0, 5);
        }

        UpdateCameraPosition();
    }

    void Update()
    {
        HandleKeyboardInput();
        HandleMouseInput();
        HandleEdgeScrolling();
        UpdateCameraPosition();
    }

    void HandleKeyboardInput()
    {
        // Usar el nuevo Input System
        Vector2 movement = Vector2.zero;

        // Leer input del teclado usando el nuevo sistema
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                movement.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                movement.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                movement.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                movement.x += 1;

            if (movement != Vector2.zero)
            {
                Vector3 forward = new Vector3(1, 0, 1).normalized;
                Vector3 right = new Vector3(1, 0, -1).normalized;

                Vector3 finalMovement = (forward * movement.y + right * movement.x) * moveSpeed * Time.deltaTime;
                targetPosition += finalMovement;

                // Aplicar límites
                if (useBoundaries)
                {
                    targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
                    targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
                }
            }

            // Rotación con Q y E
            if (Keyboard.current.qKey.isPressed)
            {
                currentRotation -= rotationSpeed * Time.deltaTime;
            }
            if (Keyboard.current.eKey.isPressed)
            {
                currentRotation += rotationSpeed * Time.deltaTime;
            }
        }
    }

    void HandleMouseInput()
    {
        // Zoom con rueda del mouse usando el nuevo sistema
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y / 120f; // Normalizar el scroll
            if (scroll != 0)
            {
                cameraDistance -= scroll * zoomSpeed;
                cameraDistance = Mathf.Clamp(cameraDistance, minZoom, maxZoom);
                cameraHeight = cameraDistance * 0.66f;
            }

            // Movimiento con click medio
            if (Mouse.current.middleButton.isPressed)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                float mouseX = mouseDelta.x * 0.01f;
                float mouseY = mouseDelta.y * 0.01f;

                Vector3 forward = new Vector3(1, 0, 1).normalized;
                Vector3 right = new Vector3(1, 0, -1).normalized;

                Vector3 movement = (-forward * mouseY - right * mouseX) * moveSpeed * 0.5f;
                targetPosition += movement;

                if (useBoundaries)
                {
                    targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
                    targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
                }
            }
        }
    }

    void HandleEdgeScrolling()
    {
        if (!useEdgeScrolling) return;

        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 movement = Vector3.zero;

        // Revisar bordes de la pantalla
        if (mousePos.x < edgeScrollSize)
            movement.x = -1;
        else if (mousePos.x > Screen.width - edgeScrollSize)
            movement.x = 1;

        if (mousePos.y < edgeScrollSize)
            movement.z = -1;
        else if (mousePos.y > Screen.height - edgeScrollSize)
            movement.z = 1;

        if (movement != Vector3.zero)
        {
            Vector3 forward = new Vector3(1, 0, 1).normalized;
            Vector3 right = new Vector3(1, 0, -1).normalized;

            Vector3 finalMovement = (forward * movement.z + right * movement.x) * moveSpeed * Time.deltaTime;
            targetPosition += finalMovement;

            if (useBoundaries)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
                targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
            }
        }
    }

    void UpdateCameraPosition()
    {
        // Calcular posición de la cámara basada en el objetivo
        Vector3 offset = Quaternion.Euler(0, currentRotation, 0) * new Vector3(-cameraDistance * 0.7f, cameraHeight, -cameraDistance * 0.7f);
        transform.position = targetPosition + offset;

        // Mirar hacia el objetivo
        transform.LookAt(targetPosition);
    }

    public void FocusOnPosition(Vector3 position)
    {
        targetPosition = position;
        targetPosition.y = 0;
    }

    public void FocusOnPlayer(PlayerController player)
    {
        if (player != null)
        {
            Vector2Int gridPos = player.GetPlayerData().gridPosition;
            Vector3 worldPos = MapGenerator.Instance.GetWorldPosition(gridPos.x, gridPos.y);
            FocusOnPosition(worldPos);
        }
    }

    public void SetZoomLevel(float zoom)
    {
        cameraDistance = Mathf.Clamp(zoom, minZoom, maxZoom);
        cameraHeight = cameraDistance * 0.66f;
    }
}