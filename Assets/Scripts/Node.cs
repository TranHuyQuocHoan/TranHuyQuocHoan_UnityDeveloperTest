using UnityEngine;

public class Node
{
    public Vector2Int pos;
    public bool walkable;                   // If grid[x,y] = 0 return true, 1 return false
    public float gCost = Mathf.Infinity;
    public float hCost;
    public float FCost => gCost + hCost;
    public Node nodeCameFrom;

    public Node(Vector2Int pos, bool walkable)
    {
        this.pos = pos;
        this.walkable = walkable;
    }
}
