using UnityEngine;
using System.Collections;

/// <summary>
/// Indicador visual que muestra qué jugador tiene el turno activo.
/// Se renderiza como un anillo brillante debajo del personaje.
/// </summary>
public class PlayerTurnIndicator : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("Color del indicador cuando el jugador está activo")]
    [SerializeField] private Color activeColor = new Color(1f, 0.843f, 0f, 0.8f); // Dorado brillante

    [Tooltip("Color secundario para animación de pulso")]
    [SerializeField] private Color pulseColor = new Color(1f, 1f, 0.3f, 0.9f); // Amarillo claro

    [Tooltip("Radio del círculo indicador")]
    [SerializeField] private float indicatorRadius = 0.6f;

    [Tooltip("Grosor del anillo")]
    [SerializeField] private float ringThickness = 0.08f;

    [Tooltip("Altura sobre el suelo")]
    [SerializeField] private float heightOffset = 0.02f;

    [Header("Animation Settings")]
    [Tooltip("Velocidad de rotación del indicador (grados/segundo)")]
    [SerializeField] private float rotationSpeed = 45f;

    [Tooltip("Velocidad de pulso (ciclos por segundo)")]
    [SerializeField] private float pulseSpeed = 1.5f;

    [Tooltip("Intensidad del efecto de pulso")]
    [SerializeField] private float pulseIntensity = 0.3f;

    [Tooltip("Usar animación de escalado")]
    [SerializeField] private bool useScalePulse = true;

    private GameObject indicatorObject;
    private Renderer indicatorRenderer;
    private Material indicatorMaterial;
    private bool isActive = false;
    private float pulseTimer = 0f;

    void Awake()
    {
        CreateIndicatorVisual();
    }

    // ❌ ELIMINADO: Ya no desactivamos en Start()
    // El estado lo controla GameManager.StartTurn()

    void Update()
    {
        if (!isActive || indicatorObject == null) return;

        // Animación de rotación
        indicatorObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // Animación de pulso
        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulseFactor = (Mathf.Sin(pulseTimer * Mathf.PI * 2f) + 1f) * 0.5f; // 0 a 1

        if (indicatorMaterial != null)
        {
            // Interpolar color entre activeColor y pulseColor
            Color currentColor = Color.Lerp(activeColor, pulseColor, pulseFactor * pulseIntensity);
            indicatorMaterial.color = currentColor;
        }

        // Animación de escala (opcional)
        if (useScalePulse && indicatorObject != null)
        {
            float scaleMultiplier = 1f + (pulseFactor * 0.1f); // Variación de ±10%
            indicatorObject.transform.localScale = Vector3.one * scaleMultiplier;
        }
    }

    /// <summary>
    /// Crea el objeto visual del indicador.
    /// </summary>
    void CreateIndicatorVisual()
    {
        // Crear un anillo como hijo del jugador
        indicatorObject = new GameObject("TurnIndicator");
        indicatorObject.transform.SetParent(transform);
        indicatorObject.transform.localPosition = new Vector3(0f, heightOffset, 0f);
        indicatorObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Rotar para estar horizontal

        // Crear geometría del anillo usando un Quad con shader especial
        // O usar un sprite circular con transparencia

        // Opción 1: Usar un Quad simple con textura
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(indicatorObject.transform);
        quad.transform.localPosition = Vector3.zero;
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(indicatorRadius * 2f, indicatorRadius * 2f, 1f);

        // Eliminar el collider (no necesitamos física)
        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        // Configurar material con transparencia
        indicatorRenderer = quad.GetComponent<Renderer>();

        // Intentar usar shader URP/Standard transparente
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        indicatorMaterial = new Material(shader);

        // Configurar modo transparente
        indicatorMaterial.SetFloat("_Mode", 3); // Transparent
        indicatorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        indicatorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        indicatorMaterial.SetInt("_ZWrite", 0);
        indicatorMaterial.DisableKeyword("_ALPHATEST_ON");
        indicatorMaterial.DisableKeyword("_ALPHABLEND_ON");
        indicatorMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        indicatorMaterial.renderQueue = 3000;

        indicatorMaterial.color = activeColor;

        // Activar emisión para efecto brillante
        indicatorMaterial.EnableKeyword("_EMISSION");
        indicatorMaterial.SetColor("_EmissionColor", activeColor * 0.5f);

        indicatorRenderer.material = indicatorMaterial;
        indicatorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        indicatorRenderer.receiveShadows = false;

        // ✅ IMPORTANTE: Por defecto DESACTIVADO hasta que GameManager lo active
        indicatorObject.SetActive(false);

        Debug.Log($"[PlayerTurnIndicator] Indicador creado para {gameObject.name}");
    }

    /// <summary>
    /// Activa o desactiva el indicador visual.
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;

        if (indicatorObject != null)
        {
            indicatorObject.SetActive(active);
        }

        if (active)
        {
            pulseTimer = 0f; // Resetear animación
            Debug.Log($"[PlayerTurnIndicator] ✅ Indicador ACTIVADO para {gameObject.name}");
        }
        else
        {
            Debug.Log($"[PlayerTurnIndicator] ⏸️ Indicador DESACTIVADO para {gameObject.name}");
        }
    }

    /// <summary>
    /// Cambia el color del indicador.
    /// </summary>
    public void SetColor(Color color)
    {
        activeColor = color;

        if (indicatorMaterial != null)
        {
            indicatorMaterial.color = color;
            indicatorMaterial.SetColor("_EmissionColor", color * 0.5f);
        }
    }

    /// <summary>
    /// Cambia el radio del indicador.
    /// </summary>
    public void SetRadius(float radius)
    {
        indicatorRadius = radius;

        if (indicatorObject != null)
        {
            Transform quad = indicatorObject.transform.GetChild(0);
            if (quad != null)
            {
                quad.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            }
        }
    }

    void OnDestroy()
    {
        // Limpiar material al destruir
        if (indicatorMaterial != null)
        {
            Destroy(indicatorMaterial);
        }
    }

    void OnValidate()
    {
        // Actualizar en el editor cuando cambien valores
        if (indicatorObject != null && Application.isPlaying)
        {
            Transform quad = indicatorObject.transform.GetChild(0);
            if (quad != null)
            {
                quad.localScale = new Vector3(indicatorRadius * 2f, indicatorRadius * 2f, 1f);
            }

            if (indicatorMaterial != null)
            {
                indicatorMaterial.color = activeColor;
                indicatorMaterial.SetColor("_EmissionColor", activeColor * 0.5f);
            }
        }
    }
}