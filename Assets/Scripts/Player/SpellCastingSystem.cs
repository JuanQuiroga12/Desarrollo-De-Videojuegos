using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // ✅ AGREGADO
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Sistema de casting de hechizos para jugadores locales.
/// ✅ CON DETECCIÓN DE UI PARA EVITAR CONFLICTOS CON BOTONES
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
    private bool isSystemReady = false;

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

        StartCoroutine(WaitForGameInitialization());
    }

    private IEnumerator WaitForGameInitialization()
    {
        while (GameInitializationManager.Instance == null || !GameInitializationManager.Instance.IsInitialized())
        {
            yield return new WaitForSeconds(0.1f);
        }

        isSystemReady = true;
        Debug.Log($"[SpellCastingSystem] ✅ Sistema listo para {gameObject.name}");
    }


    void Update()
    {
        if (!isSystemReady)
        {
            return;
        }

        PlayerData playerData = playerController.GetPlayerData();

        if (playerData == null || !playerData.isMyTurn || !playerController.isLocalPlayer)
        {
            // ✅ LOG DE DEBUGGING (comentar en producción)
            if (Time.frameCount % 300 == 0)
            {
                Debug.Log($"[SpellCastingSystem] Estado de {gameObject.name}:");
                Debug.Log($"    - playerData: {(playerData != null ? "OK" : "NULL")}");
                Debug.Log($"    - isMyTurn: {playerData?.isMyTurn}");
                Debug.Log($"    - isLocalPlayer: {playerController.isLocalPlayer}");
            }
            return;
        }

        if (selectedSpell == null)
        {
            return;
        }

        HandleSpellTargeting();
    }

    public void SelectSpell(int spellIndex)
    {
        Debug.Log($"[SpellCastingSystem] 📌 SelectSpell llamado - Index: {spellIndex}");

        if (playerData == null || playerData.spells == null || spellIndex < 0 || spellIndex >= playerData.spells.Count)
        {
            Debug.LogWarning($"[SpellCastingSystem] ❌ Índice de hechizo inválido: {spellIndex}");
            return;
        }

        SpellData spell = playerData.spells[spellIndex];

        if (!playerData.CanCastSpell(spell))
        {
            Debug.Log($"[SpellCastingSystem] ❌ No se puede castear {spell.spellName}. PA insuficientes.");
            return;
        }

        selectedSpell = spell;
        isSelectingTarget = true;

        Debug.Log($"[SpellCastingSystem] ✅ Hechizo seleccionado: {spell.spellName}");
        VisualizeSpellRange();

        PlayerUI playerUI = Object.FindFirstObjectByType<PlayerUI>();
        if (playerUI != null)
        {
            playerUI.AddActionToLog($"Hechizo {spell.spellName} seleccionado. Selecciona objetivo.");
        }
    }

    public void CancelSpellSelection()
    {
        if (!isSelectingTarget)
        {
            return;
        }

        Debug.Log("[SpellCastingSystem] ❌ Selección de hechizo cancelada");

        selectedSpell = null;
        isSelectingTarget = false;

        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ResetGridColors();
        }
    }

    void VisualizeSpellRange()
    {
        if (selectedSpell == null || GridVisualizer.Instance == null)
        {
            return;
        }

        GridVisualizer.Instance.ShowSpellRange(playerData.gridPosition, selectedSpell);
        Debug.Log($"[SpellCastingSystem] 🎯 Rango visualizado - Rango: {selectedSpell.range}");
    }

    void HandleSpellTargeting()
    {
        Vector2Int? targetTile = null;

        // 📱 TOUCH INPUT
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                // ✅ VERIFICAR SI EL TOQUE ESTÁ SOBRE UI
                if (IsPointerOverUI(touch.screenPosition))
                {
                    Debug.Log("[SpellCastingSystem] 🛑 Toque sobre UI - ignorando");
                    return;
                }

                Debug.Log($"[SpellCastingSystem] 📱 Toque en: {touch.screenPosition}");
                targetTile = GetTileFromScreenPosition(touch.screenPosition);
            }
        }
        // 🖱️ MOUSE INPUT
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // ✅ VERIFICAR SI EL CLICK ESTÁ SOBRE UI
            if (IsPointerOverUI(mousePos))
            {
                Debug.Log("[SpellCastingSystem] 🛑 Click sobre UI - ignorando");
                return;
            }

            Debug.Log($"[SpellCastingSystem] 🖱️ Click en: {mousePos}");
            targetTile = GetTileFromScreenPosition(mousePos);
        }

        if (targetTile.HasValue)
        {
            Debug.Log($"[SpellCastingSystem] 🎯 Intentando castear en: {targetTile.Value}");
            TryCastSpellAt(targetTile.Value);
        }
    }

    /// <summary>
    /// ✅ NUEVO: Verifica si el pointer está sobre un elemento de UI
    /// </summary>
    bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count > 0)
        {
            Debug.Log($"[SpellCastingSystem] 📍 UI detectada: {results[0].gameObject.name}");
            return true;
        }

        return false;
    }

    Vector2Int? GetTileFromScreenPosition(Vector2 screenPos)
    {
        if (Camera.main == null)
        {
            Debug.LogError("[SpellCastingSystem] ❌ Camera.main es null");
            return null;
        }

        // ✅ MÉTODO PARA CÁMARA ORTHOGRAPHIC
        if (Camera.main.orthographic)
        {
            Plane gridPlane = new Plane(Vector3.up, new Vector3(0, 0.55f, 0));
            Ray ray = Camera.main.ScreenPointToRay(screenPos);

            if (gridPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                if (MapGenerator.Instance != null)
                {
                    Vector2Int gridPos = MapGenerator.Instance.GetGridPosition(hitPoint);
                    Debug.Log($"[SpellCastingSystem] ✅ Grid Position: {gridPos}");
                    return gridPos;
                }
            }
        }
        // ✅ MÉTODO PARA CÁMARA PERSPECTIVE
        else
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (MapGenerator.Instance != null)
                {
                    Vector2Int gridPos = MapGenerator.Instance.GetGridPosition(hit.point);
                    return gridPos;
                }
            }
        }

        return null;
    }

    void TryCastSpellAt(Vector2Int targetPos)
    {
        if (selectedSpell == null)
        {
            return;
        }

        Vector2Int casterPos = playerData.gridPosition;

        if (!IsInSpellRange(casterPos, targetPos, selectedSpell))
        {
            Debug.Log($"[SpellCastingSystem] ❌ Objetivo fuera de rango");
            return;
        }

        Debug.Log($"[SpellCastingSystem] ✅ Objetivo en rango - Casteando");
        StartCoroutine(CastSpellCoroutine(selectedSpell, targetPos));
    }

    IEnumerator CastSpellCoroutine(SpellData spell, Vector2Int targetPos)
    {
        Debug.Log($"[SpellCastingSystem] 🔮 CASTING: {spell.spellName} en {targetPos}");

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

        Debug.Log($"[SpellCastingSystem] ✅ CASTEO COMPLETADO");
    }

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

    void ApplySingleTargetSpell(SpellData spell, Vector2Int targetPos)
    {
        PlayerController target = GameManager.Instance.GetPlayerAtPosition(targetPos);

        if (target != null)
        {
            if (spell.damage > 0)
            {
                target.TakeDamage(spell.damage);
            }

            if (spell.healing > 0)
            {
                target.Heal(spell.healing);
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
    }

    void ApplyLineSpell(SpellData spell, Vector2Int targetPos)
    {
        ApplySingleTargetSpell(spell, targetPos);
    }

    void ApplyAreaSpell(SpellData spell, Vector2Int targetPos)
    {
        var playersInArea = GameManager.Instance.GetPlayersInArea(targetPos, spell.areaSize);

        foreach (var target in playersInArea)
        {
            if (spell.damage > 0)
            {
                target.TakeDamage(spell.damage);
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

    bool IsInSpellRange(Vector2Int casterPos, Vector2Int targetPos, SpellData spell)
    {
        int distance = Mathf.Abs(targetPos.x - casterPos.x) + Mathf.Abs(targetPos.y - casterPos.y);
        return distance <= spell.range;
    }

    public bool IsSelectingTarget() => isSelectingTarget;
}