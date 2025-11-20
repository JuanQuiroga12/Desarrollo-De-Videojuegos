using UnityEngine;

/// <summary>
/// Controlador de cámara isométrica estática.
/// La cámara mantiene una vista de 45° fija y permite ajustar solo la distancia al objetivo.
/// NO tiene controles de movimiento - es completamente estática.
/// </summary>
public class IsometricCameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Punto central que la cámara observará (centro del mapa)")]
    public Transform target;

    [Header("Isometric Settings")]
    [Tooltip("Distancia desde el target (en unidades del mundo). Controla el zoom sin cambiar el ángulo.")]
    [Range(5f, 50f)]
    public float distance = 20f;

    [Tooltip("Ángulo de inclinación de la cámara (45° para vista isométrica perfecta)")]
    [Range(30f, 60f)]
    public float pitch = 45f;

    [Tooltip("Rotación horizontal de la cámara (0° = norte, 45° = isométrico clásico)")]
    [Range(0f, 360f)]
    public float yaw = 45f;

    [Header("Camera Settings")]
    [Tooltip("Si está activo, usa proyección ortográfica en lugar de perspectiva")]
    public bool useOrthographic = false;

    [Tooltip("Tamaño de la vista ortográfica (solo si useOrthographic = true)")]
    [Range(1f, 30f)]
    public float orthographicSize = 15f;

    [Header("Debug")]
    [Tooltip("Mostrar gizmos de debug en el editor")]
    public bool showDebugGizmos = true;

    private Camera cam;
    private Vector3 targetOffset = Vector3.zero; // Offset opcional del centro

    void Awake()
    {
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            cam = gameObject.AddComponent<Camera>();
            Debug.LogWarning("[IsometricCameraController] No había Camera, se agregó automáticamente");
        }
    }

    void Start()
    {
        // Si no hay target, buscar el centro del mapa
        if (target == null)
        {
            target = FindMapCenter();
        }

        // Configurar la cámara inicial
        UpdateCameraPosition();

        Debug.Log($"[IsometricCameraController] Inicializado - Target: {(target != null ? target.name : "null")}, Distance: {distance}, Pitch: {pitch}°, Yaw: {yaw}°");
    }

    void LateUpdate()
    {
        // Actualizar posición cada frame (por si cambia el target o los parámetros en el editor)
        if (target != null)
        {
            UpdateCameraPosition();
        }
    }

    /// <summary>
    /// Actualiza la posición y rotación de la cámara según los parámetros configurados.
    /// La altura se calcula automáticamente basándose en el pitch y la distancia.
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (target == null)
        {
            Debug.LogWarning("[IsometricCameraController] No hay target asignado");
            return;
        }

        // Convertir ángulos a radianes
        float pitchRad = pitch * Mathf.Deg2Rad;
        float yawRad = yaw * Mathf.Deg2Rad;

        // Calcular la posición de la cámara en coordenadas esféricas
        // La altura (Y) se calcula automáticamente desde el pitch y la distancia
        float height = distance * Mathf.Sin(pitchRad);
        float horizontalDistance = distance * Mathf.Cos(pitchRad);

        // Calcular posición X y Z basándose en el yaw (rotación horizontal)
        float x = horizontalDistance * Mathf.Sin(yawRad);
        float z = horizontalDistance * Mathf.Cos(yawRad);

        // Posición final de la cámara
        Vector3 targetPosition = target.position + targetOffset;
        Vector3 cameraPosition = targetPosition + new Vector3(x, height, z);

        // Aplicar posición
        transform.position = cameraPosition;

        // Hacer que la cámara mire al target
        transform.LookAt(targetPosition);

        // Configurar proyección
        if (useOrthographic)
        {
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }
        else
        {
            cam.orthographic = false;
            // El FOV se puede ajustar manualmente en el Inspector si es necesario
        }
    }

    /// <summary>
    /// Encuentra el centro del mapa generado por MapGenerator.
    /// </summary>
    private Transform FindMapCenter()
    {
        if (MapGenerator.Instance != null)
        {
            int centerX = MapGenerator.Instance.GetMapWidth() / 2;
            int centerY = MapGenerator.Instance.GetMapHeight() / 2;
            Vector3 centerPos = MapGenerator.Instance.GetWorldPosition(centerX, centerY);

            // Crear un GameObject vacío en el centro del mapa
            GameObject centerObj = new GameObject("MapCenter");
            centerObj.transform.position = centerPos;

            Debug.Log($"[IsometricCameraController] Centro del mapa creado en: {centerPos}");
            return centerObj.transform;
        }

        Debug.LogWarning("[IsometricCameraController] No se encontró MapGenerator, usando posición (0,0,0)");

        // Fallback: crear target en el origen
        GameObject fallbackCenter = new GameObject("MapCenter_Fallback");
        fallbackCenter.transform.position = Vector3.zero;
        return fallbackCenter.transform;
    }

    /// <summary>
    /// Establece un nuevo target para la cámara.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        UpdateCameraPosition();
        Debug.Log($"[IsometricCameraController] Nuevo target: {newTarget.name}");
    }

    /// <summary>
    /// Cambia la distancia de la cámara (zoom in/out).
    /// </summary>
    public void SetDistance(float newDistance)
    {
        distance = Mathf.Clamp(newDistance, 5f, 50f);
        UpdateCameraPosition();
    }

    /// <summary>
    /// Cambia el ángulo de inclinación de la cámara.
    /// </summary>
    public void SetPitch(float newPitch)
    {
        pitch = Mathf.Clamp(newPitch, 30f, 60f);
        UpdateCameraPosition();
    }

    /// <summary>
    /// Cambia la rotación horizontal de la cámara.
    /// </summary>
    public void SetYaw(float newYaw)
    {
        yaw = newYaw % 360f;
        UpdateCameraPosition();
    }

    /// <summary>
    /// Ajusta el offset del centro (útil para seguir a un jugador específico).
    /// </summary>
    public void SetTargetOffset(Vector3 offset)
    {
        targetOffset = offset;
        UpdateCameraPosition();
    }

    /// <summary>
    /// Resetea a vista isométrica clásica (45°, 45° yaw).
    /// </summary>
    public void ResetToIsometric()
    {
        pitch = 45f;
        yaw = 45f;
        distance = 20f;
        UpdateCameraPosition();
        Debug.Log("[IsometricCameraController] Vista reseteada a isométrica clásica");
    }

    /// <summary>
    /// Gizmos para visualizar la configuración en el editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || target == null) return;

        // Dibujar línea desde la cámara al target
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position + targetOffset);

        // Dibujar esfera en el target
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(target.position + targetOffset, 0.5f);

        // Dibujar círculo de distancia
        Gizmos.color = Color.yellow;
        DrawCircle(target.position + targetOffset, distance, 32);

        // Dibujar indicador de dirección
        Gizmos.color = Color.red;
        Vector3 forward = transform.forward * 3f;
        Gizmos.DrawRay(transform.position, forward);
    }

    /// <summary>
    /// Dibuja un círculo en el plano XZ para visualizar la distancia.
    /// </summary>
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    #region Editor Helpers

#if UNITY_EDITOR
    /// <summary>
    /// Botones de ayuda en el Inspector (solo en Editor).
    /// </summary>
    [ContextMenu("Reset to Classic Isometric (45°, 45°)")]
    private void EditorResetIsometric()
    {
        ResetToIsometric();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Set Top-Down View (90°, 0°)")]
    private void EditorSetTopDown()
    {
        pitch = 90f;
        yaw = 0f;
        UpdateCameraPosition();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Set Side View (0°, 0°)")]
    private void EditorSetSideView()
    {
        pitch = 0f;
        yaw = 0f;
        UpdateCameraPosition();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Find Map Center")]
    private void EditorFindMapCenter()
    {
        target = FindMapCenter();
        UpdateCameraPosition();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    #endregion
}