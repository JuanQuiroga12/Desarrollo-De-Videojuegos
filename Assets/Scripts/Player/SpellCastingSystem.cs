using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Sistema de casting de hechizos para jugadores locales.
/// Visualiza el rango de hechizos y permite seleccionar objetivos.
/// ✅ CON LOGS EXHAUSTIVOS PARA DEBUGGING
/// </summary>
public class SpellCastingSystem : MonoBehaviour
{
    [Header("Colores de Rango")]
    [SerializeField] private Color spellRangeColor = new Color(0f, 0.5f, 1f, 0.8f);
    [SerializeField] private Color targetableColor = new Color(1f, 0.5f, 0f, 0.8f);

    // Estado
    private PlayerController playerController;
    private PlayerData playerData;
    private SpellData selectedSpell = null;
    private bool isSelectingTarget = false;

    void OnEnable()
    {
        Debug.Log("[SpellCastingSystem] 🟢 OnEnable - Habilitando Enhanced Touch");
        TouchSimulation.Enable();
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        Debug.Log("[SpellCastingSystem] 🔴 OnDisable - Deshabilitando Enhanced Touch");
        TouchSimulation.Disable();
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        playerController = GetComponent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("[SpellCastingSystem] ❌ No se encontró PlayerController");
            enabled = false;
            return;
        }

        playerData = playerController.GetPlayerData();
        Debug.Log($"[SpellCastingSystem] ✅ Inicializado - PlayerData: {playerData.username}");
    }

    void Update()
    {
        // ✅ LOG 1: Verificar estado de selección de objetivo
        if (!isSelectingTarget || selectedSpell == null)
        {
            return;
        }

        // ✅ LOG 2: Verificar condiciones antes de procesamiento
        if (playerData == null || !playerData.isMyTurn || !playerController.isLocalPlayer)
        {
            Debug.LogWarning($"[SpellCastingSystem] ⚠️ Condición fallida - isMyTurn: {playerData?.isMyTurn}, isLocalPlayer: {playerController.isLocalPlayer}");
            CancelSpellSelection();
            return;
        }

        HandleSpellTargeting();
    }

    /// <summary>
    /// Selecciona un hechizo para castear.
    /// </summary>
    public void SelectSpell(int spellIndex)
    {
        Debug.Log($"[SpellCastingSystem] 📌 SelectSpell llamado - Index: {spellIndex}");

        if (playerData == null || playerData.spells == null || spellIndex < 0 || spellIndex >= playerData.spells.Count)
        {
            Debug.LogWarning($"[SpellCastingSystem] ❌ Índice de hechizo inválido: {spellIndex}");
            return;
        }

        SpellData spell = playerData.spells[spellIndex];

        // Verificar si puede castear el hechizo
        if (!playerData.CanCastSpell(spell))
        {
            Debug.Log($"[SpellCastingSystem] ❌ No se puede castear {spell.spellName}. PA insuficientes.");
            return;
        }

        // Seleccionar hechizo
        selectedSpell = spell;
        isSelectingTarget = true;

        Debug.Log($"[SpellCastingSystem] ✅ Hechizo seleccionado: {spell.spellName}. Esperando objetivo...");

        // Visualizar rango del hechizo
        VisualizeSpellRange();

        // Notificar UI
        PlayerUI playerUI = Object.FindFirstObjectByType<PlayerUI>();
        if (playerUI != null)
        {
            playerUI.AddActionToLog($"Hechizo {spell.spellName} seleccionado. Selecciona objetivo.");
        }
    }

    /// <summary>
    /// Cancela la selección de hechizo actual.
    /// </summary>
    public void CancelSpellSelection()
    {
        if (!isSelectingTarget)
        {
            return;
        }

        Debug.Log("[SpellCastingSystem] ❌ Selección de hechizo cancelada");

        selectedSpell = null;
        isSelectingTarget = false;

        // Limpiar visualización
        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ResetGridColors();
        }
    }

    /// <summary>
    /// Visualiza el rango de un hechizo.
    /// </summary>
    void VisualizeSpellRange()
    {
        if (selectedSpell == null || GridVisualizer.Instance == null)
        {
            Debug.LogWarning("[SpellCastingSystem] ⚠️ No se puede visualizar rango - GridVisualizer null");
            return;
        }

        GridVisualizer.Instance.ShowSpellRange(playerData.gridPosition, selectedSpell);
        Debug.Log($"[SpellCastingSystem] 🎯 Rango de hechizo visualizado - Rango: {selectedSpell.range}");
    }

    /// <summary>
    /// Maneja la selección de objetivo para el hechizo.
    /// ✅ CON LOGS EXHAUSTIVOS
    /// </summary>
    void HandleSpellTargeting()
    {
        Vector2Int? targetTile = null;

        // 📱 LOG 1: Verificar toques
        if (Touch.activeTouches.Count > 0)
        {
            Debug.Log($"[SpellCastingSystem] 📱 TOQUE DETECTADO - Cantidad de toques: {Touch.activeTouches.Count}");
            var touch = Touch.activeTouches[0];

            Debug.Log($"[SpellCastingSystem] 📱 Touch info - Phase: {touch.phase}, Position: {touch.screenPosition}");

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Debug.Log($"[SpellCastingSystem] 📱 Touch BEGAN - Obteniendo tile desde posición: {touch.screenPosition}");
                targetTile = GetTileFromScreenPosition(touch.screenPosition);
                if (targetTile.HasValue)
                {
                    Debug.Log($"[SpellCastingSystem] ✅ Tile obtenido del toque: {targetTile.Value}");
                }
                else
                {
                    Debug.Log("[SpellCastingSystem] ❌ No se pudo obtener tile del toque (raycast falló)");
                }
            }
            else
            {
                Debug.Log($"[SpellCastingSystem] ⏸️ Touch detectado pero phase no es BEGAN: {touch.phase}");
            }
        }
        // 🖱️ LOG 2: Verificar clicks de mouse
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Debug.Log($"[SpellCastingSystem] 🖱️ CLICK DEL MOUSE DETECTADO - Posición: {mousePos}");
            targetTile = GetTileFromScreenPosition(mousePos);
            if (targetTile.HasValue)
            {
                Debug.Log($"[SpellCastingSystem] ✅ Tile obtenido del click: {targetTile.Value}");
            }
            else
            {
                Debug.Log("[SpellCastingSystem] ❌ No se pudo obtener tile del click (raycast falló)");
            }
        }
        else
        {
            Debug.Log($"[SpellCastingSystem] ⏳ Esperando input - Touch.Count: {Touch.activeTouches.Count}, Mouse: {Mouse.current != null}");
        }

        // 📍 LOG 3: Procesar resultado
        if (targetTile.HasValue)
        {
            Debug.Log($"[SpellCastingSystem] 🎯 Intentando castear en: {targetTile.Value}");
            TryCastSpellAt(targetTile.Value);
        }
    }

    /// <summary>
    /// Convierte una posición de pantalla a coordenadas de grid.
    /// ✅ CON LOGS PARA RAYCAST
    /// </summary>
    Vector2Int? GetTileFromScreenPosition(Vector2 screenPos)
    {
        Debug.Log($"[SpellCastingSystem] 🔍 GetTileFromScreenPosition - screenPos: {screenPos}");

        if (Camera.main == null)
        {
            Debug.LogError("[SpellCastingSystem] ❌ Camera.main es null");
            return null;
        }

        // ✅ MÉTODO CORRECTO PARA CÁMARA ORTHOGRAPHIC
        if (Camera.main.orthographic)
        {
            Debug.Log("[SpellCastingSystem] 📐 Usando raycast para cámara ORTHOGRAPHIC");

            // Crear un plano en Y = 0.55 (altura del grid)
            Plane gridPlane = new Plane(Vector3.up, new Vector3(0, 0.55f, 0));

            // Crear rayo desde la cámara
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            Debug.Log($"[SpellCastingSystem] 🔍 Ray - Origin: {ray.origin}, Direction: {ray.direction}");

            // Verificar intersección con el plano
            if (gridPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Debug.Log($"[SpellCastingSystem] ✅ Plano intersectado en: {hitPoint}");

                // Convertir a coordenadas de grid
                if (MapGenerator.Instance != null)
                {
                    Vector2Int gridPos = MapGenerator.Instance.GetGridPosition(hitPoint);
                    Debug.Log($"[SpellCastingSystem] ✅ Grid Position: {gridPos}");
                    return gridPos;
                }
            }
            else
            {
                Debug.LogWarning("[SpellCastingSystem] ⚠️ Rayo no intersecta el plano del grid");
            }
        }
        else
        {
            // Método original para cámara en perspectiva
            Debug.Log("[SpellCastingSystem] 📐 Usando raycast para cámara PERSPECTIVE");
            Ray ray = Camera.main.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Debug.Log($"[SpellCastingSystem] ✅ Raycast HIT - Objeto: {hit.collider.gameObject.name}");

                if (MapGenerator.Instance != null)
                {
                    Vector2Int gridPos = MapGenerator.Instance.GetGridPosition(hit.point);
                    return gridPos;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Intenta castear un hechizo en una posición objetivo.
    /// </summary>
    void TryCastSpellAt(Vector2Int targetPos)
    {
        Debug.Log($"[SpellCastingSystem] 🎯 TryCastSpellAt llamado - targetPos: {targetPos}");

        if (selectedSpell == null)
        {
            Debug.LogError("[SpellCastingSystem] ❌ selectedSpell es null");
            return;
        }

        Vector2Int casterPos = playerData.gridPosition;

        // Verificar si el objetivo está en rango
        if (!IsInSpellRange(casterPos, targetPos, selectedSpell))
        {
            Debug.Log($"[SpellCastingSystem] ❌ Objetivo {targetPos} fuera de rango ({selectedSpell.range})");
            return;
        }

        Debug.Log($"[SpellCastingSystem] ✅ Objetivo en rango - Iniciando casteo");
        StartCoroutine(CastSpellCoroutine(selectedSpell, targetPos));
    }

    /// <summary>
    /// Corutina que ejecuta el casting del hechizo.
    /// </summary>
    IEnumerator CastSpellCoroutine(SpellData spell, Vector2Int targetPos)
    {
        Debug.Log($"[SpellCastingSystem] 🔮 CASTING INICIADO - Hechizo: {spell.spellName}, Target: {targetPos}");

        isSelectingTarget = false;
        playerController.SetCastingSpell(true);

        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ResetGridColors();
        }

        Vector3 targetWorldPos = MapGenerator.Instance.GetWorldPosition(targetPos.x, targetPos.y);
        Vector3 direction = (targetWorldPos - transform.position).normalized;

        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float rotationTime = 0.3f;
            float elapsed = 0f;

            while (elapsed < rotationTime)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, elapsed / rotationTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.rotation = targetRotation;
        }

        playerController.PlayAnimation("Magic Spell Cast");
        yield return new WaitForSeconds(1f);

        playerData.CastSpell(spell);
        ApplySpellEffects(spell, targetPos);

        PlayerUI playerUI = Object.FindFirstObjectByType<PlayerUI>();
        if (playerUI != null)
        {
            playerUI.UpdatePlayerStats(playerData);
            playerUI.AddActionToLog($"{playerData.username} casteó {spell.spellName}");
        }

        playerController.PlayAnimation("Idle");
        selectedSpell = null;
        playerController.SetCastingSpell(false);

        Debug.Log($"[SpellCastingSystem] ✅ CASTEO COMPLETADO - PA restantes: {playerData.currentAttackPoints}");
    }

    /// <summary>
    /// Aplica los efectos de un hechizo.
    /// </summary>
    void ApplySpellEffects(SpellData spell, Vector2Int targetPos)
    {
        switch (spell.spellType)
        {
            case SpellType.SingleTarget:
                ApplySingleTargetSpell(spell, targetPos);
                break;

            case SpellType.Line:
                ApplyLineSpell(spell, targetPos);
                break;

            case SpellType.Area:
                ApplyAreaSpell(spell, targetPos);
                break;
        }
    }

    /// <summary>
    /// Aplica un hechizo de objetivo único.
    /// </summary>
    void ApplySingleTargetSpell(SpellData spell, Vector2Int targetPos)
    {
        PlayerController target = GameManager.Instance.GetPlayerAtPosition(targetPos);

        if (target != null)
        {
            if (spell.damage > 0)
            {
                target.TakeDamage(spell.damage);
                Debug.Log($"[SpellCastingSystem] {spell.spellName} inflige {spell.damage} de daño a {target.GetPlayerData().username}");
            }

            if (spell.healing > 0)
            {
                target.Heal(spell.healing);
                Debug.Log($"[SpellCastingSystem] {spell.spellName} cura {spell.healing} a {target.GetPlayerData().username}");
            }

            if (VisualEffectsManager.Instance != null)
            {
                Vector3 worldPos = MapGenerator.Instance.GetWorldPosition(targetPos.x, targetPos.y);
                worldPos.y = 1f;

                if (spell.damage > 0)
                {
                    VisualEffectsManager.Instance.ShowDamage(worldPos, spell.damage);
                }
                else if (spell.healing > 0)
                {
                    VisualEffectsManager.Instance.ShowHeal(worldPos, spell.healing);
                }
            }
        }
        else
        {
            Debug.Log($"[SpellCastingSystem] ⚠️ No hay objetivo en {targetPos}");
        }
    }

    /// <summary>
    /// Aplica un hechizo de línea.
    /// </summary>
    void ApplyLineSpell(SpellData spell, Vector2Int targetPos)
    {
        ApplySingleTargetSpell(spell, targetPos);
    }

    /// <summary>
    /// Aplica un hechizo de área.
    /// </summary>
    void ApplyAreaSpell(SpellData spell, Vector2Int targetPos)
    {
        var playersInArea = GameManager.Instance.GetPlayersInArea(targetPos, spell.areaSize);

        foreach (var target in playersInArea)
        {
            if (spell.damage > 0)
            {
                target.TakeDamage(spell.damage);
                Debug.Log($"[SpellCastingSystem] {spell.spellName} inflige {spell.damage} de daño a {target.GetPlayerData().username}");
            }

            if (spell.healing > 0)
            {
                target.Heal(spell.healing);
            }

            if (VisualEffectsManager.Instance != null)
            {
                Vector3 worldPos = target.transform.position;
                worldPos.y = 1f;

                if (spell.damage > 0)
                {
                    VisualEffectsManager.Instance.ShowDamage(worldPos, spell.damage);
                }
                else if (spell.healing > 0)
                {
                    VisualEffectsManager.Instance.ShowHeal(worldPos, spell.healing);
                }
            }
        }
    }

    /// <summary>
    /// Verifica si un objetivo está en rango de un hechizo.
    /// </summary>
    bool IsInSpellRange(Vector2Int casterPos, Vector2Int targetPos, SpellData spell)
    {
        int distance = Mathf.Abs(targetPos.x - casterPos.x) + Mathf.Abs(targetPos.y - casterPos.y);
        bool inRange = distance <= spell.range;
        Debug.Log($"[SpellCastingSystem] 📏 Verificando rango - Distancia: {distance}, Rango del hechizo: {spell.range}, En rango: {inRange}");
        return inRange;
    }

    public bool IsSelectingTarget() => isSelectingTarget;
}