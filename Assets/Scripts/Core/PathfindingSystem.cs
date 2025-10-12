using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PathfindingSystem : MonoBehaviour
{
    public static PathfindingSystem Instance { get; private set; }

    private class Node
    {
        public Vector2Int position;
        public Node parent;
        public float gCost; // Distancia desde el inicio
        public float hCost; // Heurística (distancia al objetivo)
        public float FCost => gCost + hCost;

        public Node(Vector2Int pos)
        {
            position = pos;
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
    {
        if (!MapGenerator.Instance.IsWalkable(target.x, target.y))
            return null;

        List<Node> openList = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Node startNode = new Node(start) { gCost = 0, hCost = GetDistance(start, target) };
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Node currentNode = openList.OrderBy(n => n.FCost).First();

            if (currentNode.position == target)
            {
                return RetracePath(startNode, currentNode);
            }

            openList.Remove(currentNode);
            closedSet.Add(currentNode.position);

            foreach (Vector2Int neighbor in GetNeighbors(currentNode.position))
            {
                if (closedSet.Contains(neighbor) || !MapGenerator.Instance.IsWalkable(neighbor.x, neighbor.y))
                    continue;

                float newGCost = currentNode.gCost + GetDistance(currentNode.position, neighbor);
                Node neighborNode = openList.FirstOrDefault(n => n.position == neighbor);

                if (neighborNode == null)
                {
                    neighborNode = new Node(neighbor)
                    {
                        parent = currentNode,
                        gCost = newGCost,
                        hCost = GetDistance(neighbor, target)
                    };
                    openList.Add(neighborNode);
                }
                else if (newGCost < neighborNode.gCost)
                {
                    neighborNode.parent = currentNode;
                    neighborNode.gCost = newGCost;
                }
            }
        }

        return null; // No se encontró camino
    }

    private List<Vector2Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    public List<Vector2Int> GetReachableTiles(Vector2Int start, int movementPoints)
    {
        List<Vector2Int> reachableTiles = new List<Vector2Int>();
        Queue<(Vector2Int pos, int cost)> queue = new Queue<(Vector2Int, int)>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.cost <= movementPoints)
            {
                reachableTiles.Add(current.pos);

                foreach (Vector2Int neighbor in GetNeighbors(current.pos))
                {
                    if (!visited.Contains(neighbor) && MapGenerator.Instance.IsWalkable(neighbor.x, neighbor.y))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, current.cost + 1));
                    }
                }
            }
        }

        return reachableTiles;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        // 4 direcciones cardinales (no diagonales como en Dofus)
        neighbors.Add(new Vector2Int(pos.x + 1, pos.y));
        neighbors.Add(new Vector2Int(pos.x - 1, pos.y));
        neighbors.Add(new Vector2Int(pos.x, pos.y + 1));
        neighbors.Add(new Vector2Int(pos.x, pos.y - 1));

        return neighbors;
    }

    private float GetDistance(Vector2Int a, Vector2Int b)
    {
        // Distancia Manhattan (sin diagonales)
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public int GetPathDistance(Vector2Int start, Vector2Int target)
    {
        List<Vector2Int> path = FindPath(start, target);
        return path != null ? path.Count : -1;
    }
}