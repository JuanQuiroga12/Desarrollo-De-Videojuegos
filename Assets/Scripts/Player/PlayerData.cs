using System;
using UnityEngine;
using System.Collections.Generic;

// Enumeraciones
public enum SpellType
{
    SingleTarget,
    Self,
    Area,
    Line
}

public enum SpellElement
{
    Fire,
    Earth,
    Water,
    Wind
}

// Clase para los datos del hechizo
[Serializable]
public class SpellData
{
    public string spellName;
    public SpellElement element;
    public SpellType spellType;
    public int apCost; // Costo en puntos de ataque
    public int damage;
    public int healing;
    public int movementBonus;
    public int movementPenalty;
    public int apBonus;
    public int apPenalty;
    public int range;
    public int areaSize;

    public SpellData(string name, SpellElement elem, SpellType type, int ap, int dmg = 0,
                     int heal = 0, int moveBonus = 0, int movePenalty = 0,
                     int apBon = 0, int apPen = 0, int rng = 3, int area = 0)
    {
        spellName = name;
        element = elem;
        spellType = type;
        apCost = ap;
        damage = dmg;
        healing = heal;
        movementBonus = moveBonus;
        movementPenalty = movePenalty;
        apBonus = apBon;
        apPenalty = apPen;
        range = rng;
        areaSize = area;
    }
}

// Clase principal de datos del jugador
[Serializable]
public class PlayerData
{
    public string username;
    public int currentHealth;
    public int maxHealth;
    public int currentMovementPoints;
    public int baseMovementPoints;
    public int currentAttackPoints;
    public int baseAttackPoints;
    public Vector2Int gridPosition;
    public List<SpellData> spells;
    public bool isMyTurn;

    // Constructor
    public PlayerData()
    {
        maxHealth = 200;
        currentHealth = maxHealth;
        baseMovementPoints = 4;
        currentMovementPoints = baseMovementPoints;
        baseAttackPoints = 6;
        currentAttackPoints = baseAttackPoints;
        gridPosition = new Vector2Int(0, 0);
        spells = new List<SpellData>();
        InitializeSpells();
    }

    void InitializeSpells()
    {
        // Fuego: 3 PA, 40 daño
        spells.Add(new SpellData("Fuego", SpellElement.Fire, SpellType.SingleTarget, 3, 40, 0, 0, 0, 0, 0, 4));

        // Tierra: 2 PA, 30 daño
        spells.Add(new SpellData("Tierra", SpellElement.Earth, SpellType.SingleTarget, 2, 30, 0, 0, 0, 0, 0, 3));

        // Agua: 3 PA, 20 cura, +1 PM (solo a sí mismo)
        spells.Add(new SpellData("Agua", SpellElement.Water, SpellType.Self, 3, 0, 20, 1, 0, 0, 0, 0));

        // Viento: 2 PA, efectos variables en área
        spells.Add(new SpellData("Viento", SpellElement.Wind, SpellType.Area, 2, 0, 0, 0, 0, 0, 0, 3, 1));
    }

    public void StartNewTurn()
    {
        currentMovementPoints = baseMovementPoints;
        currentAttackPoints = baseAttackPoints;
        isMyTurn = true;  // ✅ AGREGAR ESTA LÍNEA
    }

    public bool CanCastSpell(SpellData spell)
    {
        return currentAttackPoints >= spell.apCost;
    }

    public void CastSpell(SpellData spell)
    {
        if (CanCastSpell(spell))
        {
            currentAttackPoints -= spell.apCost;
        }
    }

    public bool CanMove(int distance)
    {
        return currentMovementPoints >= distance;
    }

    public void Move(int distance)
    {
        currentMovementPoints = Mathf.Max(0, currentMovementPoints - distance);
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void ModifyMovementPoints(int amount)
    {
        currentMovementPoints = Mathf.Max(0, currentMovementPoints + amount);
    }

    public void ModifyAttackPoints(int amount)
    {
        currentAttackPoints = Mathf.Max(0, currentAttackPoints + amount);
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    // Serialización JSON
    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    public static PlayerData FromJson(string json)
    {
        return JsonUtility.FromJson<PlayerData>(json);
    }
}

// Clase para manejar el estado del juego
[Serializable]
public class GameStateData
{
    public PlayerData player1;
    public PlayerData player2;
    public int currentTurn; // 1 o 2
    public float turnTimeRemaining;
    public string gameId;
    public bool gameStarted;
    public bool gameEnded;
    public string winner;

    public GameStateData()
    {
        player1 = new PlayerData();
        player2 = new PlayerData();
        currentTurn = 1;
        turnTimeRemaining = 60f; // 60 segundos por turno
        gameId = System.Guid.NewGuid().ToString();
        gameStarted = false;
        gameEnded = false;
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    public static GameStateData FromJson(string json)
    {
        return JsonUtility.FromJson<GameStateData>(json);
    }
}