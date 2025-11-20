using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class SpellCastingSystem : MonoBehaviour
{
    private PlayerController playerController;
    private SpellData selectedSpell = null;
    private int selectedSpellIndex = -1;
    private bool isWaitingForTarget = false;

    [Header("Colores de Rango")]
    [SerializeField] private Color spellRangeColor = new Color(1f, 0.5f, 0f, 0.6f);
    [SerializeField] private Color targetableColor = new Color(1f, 0f, 0f, 0.8f);

    void Awake()
    {
        // Habilitar el sistema de touch mejorado
        EnhancedTouchSupport.Enable();
    }

    void OnDestroy()
    {
        // Deshabilitar cuando se destruye
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    public void SelectSpell(int spellIndex)
    {
        if (!playerController.GetPlayerData().isMyTurn || !playerController.isLocalPlayer)
            return;

        if (spellIndex >= playerController.GetPlayerData().spells.Count)
            return;

        SpellData spell = playerController.GetPlayerData().spells[spellIndex];

        if (!playerController.GetPlayerData().CanCastSpell(spell))
        {
            Debug.Log($"[SpellCasting] No hay suficiente PA para {spell.spellName}");
            return;
        }

        selectedSpell = spell;
        selectedSpellIndex = spellIndex;
        isWaitingForTarget = true;

        ShowSpellRange(spell);
    }

    void Update()
    {
        if (!isWaitingForTarget || selectedSpell == null)
            return;

        // Detectar tap/click para seleccionar objetivo
        bool inputDetected = false;
        Vector2 inputPosition = Vector2.zero;

        // Detectar input táctil usando el nuevo Input System
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                // Verificar que no estamos sobre UI
                if (!EventSystem.current.IsPointerOverGameObject(touch.touchId))
                {
                    inputDetected = true;
                    inputPosition = touch.screenPosition;
                }
            }

            // Cancelar con segundo dedo
            if (Touch.activeTouches.Count > 1)
            {
                CancelSpellCasting();
                return;
            }
        }
        // Detectar click del mouse usando el nuevo Input System
        else if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Verificar que no estamos sobre UI
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    inputDetected = true;
                    inputPosition = Mouse.current.position.ReadValue();
                }
            }

            // Cancelar con click derecho
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CancelSpellCasting();
                return;
            }
        }

        // Cancelar con tecla Escape
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelSpellCasting();
            return;
        }

        if (inputDetected)
        {
            ProcessSpellTarget(inputPosition);
        }
    }

    void ProcessSpellTarget(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            Vector2Int targetPos = MapGenerator.Instance.GetGridPosition(hit.point);

            if (IsInSpellRange(targetPos))
            {
                CastSpell(targetPos);
            }
            else
            {
                Debug.Log($"[SpellCasting] Objetivo fuera de rango");
            }
        }
    }

    bool IsInSpellRange(Vector2Int targetPos)
    {
        Vector2Int casterPos = playerController.GetPlayerData().gridPosition;
        int distance = Mathf.Abs(casterPos.x - targetPos.x) + Mathf.Abs(casterPos.y - targetPos.y);
        return distance <= selectedSpell.range;
    }

    void CastSpell(Vector2Int targetPos)
    {
        Debug.Log($"[SpellCasting] Casteando {selectedSpell.spellName} en {targetPos}");

        // Usar el PlayerController para castear el hechizo
        StartCoroutine(ExecuteSpellCast(targetPos));
    }

    IEnumerator ExecuteSpellCast(Vector2Int targetPos)
    {
        // Primero, cancelar el modo de selección
        CancelSpellCasting();

        // Esperar un frame
        yield return null;

        // Ahora castear el hechizo a través del PlayerController
        playerController.TryCastSpell(selectedSpellIndex);

        // Sincronizar en red
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsOnlineMode())
        {
            yield return SyncSpellCast(targetPos);
        }
    }

    IEnumerator SyncSpellCast(Vector2Int targetPos)
    {
        yield return null;

        if (GameManager.Instance != null)
        {
            var gameState = GameManager.Instance.GetGameState();
            if (NetworkManager.Instance != null && NetworkManager.Instance.currentRoomRef != null)
            {
                _ = NetworkManager.Instance.SendGameState(gameState);
            }
        }
    }

    void ShowSpellRange(SpellData spell)
    {
        if (GridVisualizer.Instance == null) return;

        GridVisualizer.Instance.ResetGridColors();
        Vector2Int casterPos = playerController.GetPlayerData().gridPosition;

        // Mostrar rango del hechizo
        for (int x = -spell.range; x <= spell.range; x++)
        {
            for (int y = -spell.range; y <= spell.range; y++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) <= spell.range)
                {
                    Vector2Int tile = new Vector2Int(casterPos.x + x, casterPos.y + y);

                    if (MapGenerator.Instance.IsWalkable(tile.x, tile.y))
                    {
                        // Verificar si hay un enemigo en esta posición
                        PlayerController enemy = GameManager.Instance.GetPlayerAtPosition(tile);

                        if (enemy != null && enemy != playerController)
                        {
                            GridVisualizer.Instance.SetTileColor(tile, targetableColor);
                        }
                        else
                        {
                            GridVisualizer.Instance.SetTileColor(tile, spellRangeColor);
                        }
                    }
                }
            }
        }
    }

    void CancelSpellCasting()
    {
        selectedSpell = null;
        selectedSpellIndex = -1;
        isWaitingForTarget = false;

        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ResetGridColors();
        }
    }
}