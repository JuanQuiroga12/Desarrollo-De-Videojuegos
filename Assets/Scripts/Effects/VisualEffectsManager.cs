using UnityEngine;
using System.Collections;
using TMPro;

public class VisualEffectsManager : MonoBehaviour
{
    public static VisualEffectsManager Instance { get; private set; }

    [Header("Damage Popup Settings")]
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private float popupDuration = 1.5f;
    [SerializeField] private float popupHeight = 2f;
    [SerializeField] private AnimationCurve popupCurve;

    [Header("Spell Effects")]
    [SerializeField] private GameObject fireEffectPrefab;
    [SerializeField] private GameObject earthEffectPrefab;
    [SerializeField] private GameObject waterEffectPrefab;
    [SerializeField] private GameObject windEffectPrefab;

    [Header("Colors")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private Color buffColor = Color.yellow;
    [SerializeField] private Color debuffColor = new Color(0.5f, 0, 0.5f);

    // ✅ NUEVAS VARIABLES: Prefabs generados en tiempo de ejecución
    private GameObject runtimeFireEffect;
    private GameObject runtimeEarthEffect;
    private GameObject runtimeWaterEffect;
    private GameObject runtimeWindEffect;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // ❌ NO LLAMAR CreateDefaultEffects() aquí
            // CreateDefaultEffects();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ✅ NUEVO: Método llamado bajo demanda
    void EnsureEffectsCreated()
    {
        // ✅ SOLO crear efectos si no están asignados Y se necesitan
        if (damagePopupPrefab == null)
        {
            damagePopupPrefab = CreateDamagePopupPrefab();
        }

        // ✅ NO crear prefabs de spell effects por defecto
        // En su lugar, se crearán SOLO cuando se usen

        // Configurar curva de animación por defecto
        if (popupCurve == null || popupCurve.keys.Length == 0)
        {
            popupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
    }

    GameObject CreateDamagePopupPrefab()
    {
        GameObject popup = new GameObject("DamagePopup");

        // Canvas para renderizar en espacio mundial
        Canvas canvas = popup.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rect = popup.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2, 1);

        // Texto
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(popup.transform);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "0";
        tmp.fontSize = 0.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        // Outline para mejor visibilidad
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;

        // ✅ IMPORTANTE: Desactivar el prefab inmediatamente
        popup.SetActive(false);

        return popup;
    }

    // ✅ CORREGIDO: Crear efecto SOLO cuando se necesita
    GameObject CreateSpellEffectPrefab(Color color)
    {
        GameObject effect = new GameObject("SpellEffect");

        // Partículas básicas
        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 1f;
        main.startSpeed = 3f;
        main.startSize = 0.5f;
        main.startColor = color;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var emission = particles.emission;
        emission.rateOverTime = 30;

        // Renderer
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        // ✅ CRÍTICO: DESACTIVAR inmediatamente para que no aparezca en (0,0,0)
        effect.SetActive(false);

        return effect;
    }

    public void ShowDamage(Vector3 position, int damage)
    {
        EnsureEffectsCreated(); // ✅ Asegurar que los efectos estén creados
        StartCoroutine(ShowPopup(position, damage.ToString(), damageColor));
    }

    public void ShowHeal(Vector3 position, int healing)
    {
        EnsureEffectsCreated();
        StartCoroutine(ShowPopup(position, "+" + healing, healColor));
    }

    public void ShowBuff(Vector3 position, string text)
    {
        EnsureEffectsCreated();
        StartCoroutine(ShowPopup(position, text, buffColor));
    }

    public void ShowDebuff(Vector3 position, string text)
    {
        EnsureEffectsCreated();
        StartCoroutine(ShowPopup(position, text, debuffColor));
    }

    IEnumerator ShowPopup(Vector3 position, string text, Color color)
    {
        if (damagePopupPrefab == null)
        {
            Debug.LogWarning("[VisualEffectsManager] damagePopupPrefab es null");
            yield break;
        }

        // Instanciar popup
        GameObject popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);

        // ✅ ACTIVAR el popup DESPUÉS de instanciarlo en la posición correcta
        popup.SetActive(true);

        TextMeshProUGUI tmp = popup.GetComponentInChildren<TextMeshProUGUI>();

        if (tmp != null)
        {
            tmp.text = text;
            tmp.color = color;
        }

        // Hacer que el popup mire a la cámara
        Canvas canvas = popup.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.worldCamera = Camera.main;
        }

        // Animar el popup
        float elapsed = 0f;
        Vector3 startPos = position;

        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / popupDuration;

            // Mover hacia arriba
            float height = popupCurve.Evaluate(progress) * popupHeight;
            popup.transform.position = startPos + Vector3.up * height;

            // Fade out
            if (tmp != null)
            {
                Color c = tmp.color;
                c.a = 1f - progress;
                tmp.color = c;
            }

            // Escalar
            float scale = 1f + (progress * 0.5f);
            popup.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(popup);
    }

    // ✅ CORREGIDO: Crear efectos bajo demanda
    public void PlaySpellEffect(SpellElement element, Vector3 position)
    {
        GameObject effectPrefab = null;

        switch (element)
        {
            case SpellElement.Fire:
                // ✅ Si hay prefab asignado en el Inspector, usarlo
                if (fireEffectPrefab != null)
                {
                    effectPrefab = fireEffectPrefab;
                }
                else
                {
                    // ✅ Si no, crear uno temporal SOLO cuando se necesita
                    if (runtimeFireEffect == null)
                    {
                        runtimeFireEffect = CreateSpellEffectPrefab(new Color(1f, 0.3f, 0f));
                    }
                    effectPrefab = runtimeFireEffect;
                }
                break;

            case SpellElement.Earth:
                if (earthEffectPrefab != null)
                {
                    effectPrefab = earthEffectPrefab;
                }
                else
                {
                    if (runtimeEarthEffect == null)
                    {
                        runtimeEarthEffect = CreateSpellEffectPrefab(new Color(0.6f, 0.4f, 0.2f));
                    }
                    effectPrefab = runtimeEarthEffect;
                }
                break;

            case SpellElement.Water:
                if (waterEffectPrefab != null)
                {
                    effectPrefab = waterEffectPrefab;
                }
                else
                {
                    if (runtimeWaterEffect == null)
                    {
                        runtimeWaterEffect = CreateSpellEffectPrefab(new Color(0f, 0.5f, 1f));
                    }
                    effectPrefab = runtimeWaterEffect;
                }
                break;

            case SpellElement.Wind:
                if (windEffectPrefab != null)
                {
                    effectPrefab = windEffectPrefab;
                }
                else
                {
                    if (runtimeWindEffect == null)
                    {
                        runtimeWindEffect = CreateSpellEffectPrefab(new Color(0.7f, 1f, 0.7f));
                    }
                    effectPrefab = runtimeWindEffect;
                }
                break;
        }

        if (effectPrefab != null)
        {
            // ✅ Instanciar en la posición correcta
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);

            // ✅ ACTIVAR después de posicionar
            effect.SetActive(true);

            // ✅ Reproducir partículas manualmente
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }

            // Destruir después de 2 segundos
            Destroy(effect, 2f);

            Debug.Log($"[VisualEffectsManager] Efecto de {element} reproducido en {position}");
        }
        else
        {
            Debug.LogWarning($"[VisualEffectsManager] No se pudo crear efecto para {element}");
        }
    }

    public void PlayMoveEffect(Vector3 startPos, Vector3 endPos)
    {
        // Crear un trail o línea de movimiento
        GameObject trail = new GameObject("MoveTrail");
        LineRenderer line = trail.AddComponent<LineRenderer>();

        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1f, 1f, 1f, 0.5f);
        line.endColor = new Color(1f, 1f, 1f, 0f);

        line.SetPosition(0, startPos + Vector3.up * 0.1f);
        line.SetPosition(1, endPos + Vector3.up * 0.1f);

        // Fade out y destruir
        StartCoroutine(FadeLineRenderer(line));
    }

    IEnumerator FadeLineRenderer(LineRenderer line)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        Color startColor = line.startColor;
        Color endColor = line.endColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            startColor.a = 0.5f * (1f - progress);
            endColor.a = 0f;

            line.startColor = startColor;
            line.endColor = endColor;

            yield return null;
        }

        Destroy(line.gameObject);
    }

    public void HighlightTile(Vector3 position, Color color, float duration = 0.5f)
    {
        StartCoroutine(FlashTile(position, color, duration));
    }

    IEnumerator FlashTile(Vector3 position, Color color, float duration)
    {
        // Crear un plano temporal para el highlight
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        highlight.transform.position = position + Vector3.up * 0.02f;
        highlight.transform.rotation = Quaternion.Euler(90, 0, 0);

        Renderer renderer = highlight.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Mode", 2);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        renderer.material = mat;

        // Destruir collider
        Destroy(highlight.GetComponent<Collider>());

        // Animar alpha
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            color.a = Mathf.Sin(progress * Mathf.PI);
            mat.color = color;

            yield return null;
        }

        Destroy(highlight);
    }
}