using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    [Header("Map Settings")]
    public int width = 10;
    public int height = 10;
    public bool guaranteePath = false; // Option, if true, we always have a path from start to end
    private int[,] grid;

    [Header("Prefabs")]
    [SerializeField] private NPC npcPrefab;
    [SerializeField] private GameObject goalPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject pathPrefab;
    [SerializeField] private GameObject foundPathPrefab;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    private NPC npcInstance;

    private void Start()
    {
        GenerateMap();
        SpawnGrid();
        CenterMap();
        FindPath();
    }

    #region Map Generation & Spawn

    // This function will generate map with path that NPC can reach the goal
    private void GenerateMap()
    {
        print("generate new map");

        if (guaranteePath)
        {
            GenerateMapWithPath();
        }
        else
        {
            GenerateRandomMap();
            EnsureStartAndGoalWalkable();
        }
    }

    private void GenerateMapWithPath()
    {
        List<Vector2Int> testPath;

        do
        {
            GenerateRandomMap();
            EnsureStartAndGoalWalkable();

            testPath = CalculatePath(new Vector2Int(0, 0), new Vector2Int(width - 1, height - 1));
        }
        while (testPath == null);
    }

    // Generate random map
    private void GenerateRandomMap()
    {
        grid = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = Random.Range(0, 2);
            }
        }
    }

    // Ensure start point (grid[0,0]) and end point (grid[grid[width - 1, height - 1])
    // always walkable
    private void EnsureStartAndGoalWalkable()
    {
        grid[0, 0] = 0;
        grid[width - 1, height - 1] = 0;
    }

    // Spawn grid
    private void SpawnGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new(x, y, 0);

                // NPC
                if (x == 0 && y == 0)
                {
                    npcInstance = Instantiate(npcPrefab, pos, Quaternion.identity);
                }
                // Goal
                else if (x == width - 1 && y == height - 1)
                {
                    Instantiate(goalPrefab, pos, Quaternion.identity);
                }
                // Wall
                else if (grid[x, y] == 1)
                {
                    Instantiate(wallPrefab, pos, Quaternion.identity);
                }
                // Normnal Path
                else
                {
                    Instantiate(pathPrefab, pos, Quaternion.identity);
                }
            }
        }
    }

    // Make map always in center by modified camera orthorGraphicSize
    private void CenterMap()
    {
        float centerX = (width - 1) / 2f;       // Calculate centerX by width - 1, if width = 10, then centerX = 4.5
        float centerY = (height - 1) / 2f;      // Same as width;
        float aspect = mainCamera.aspect;       // Aspec ratio
        float h = height / 2f;                  // Calculate height
        float w = width / (2f * aspect);        // Calculate width, Ex : If aspect = 9:16 then we have 10 / (2f *  0.5625 ) = 8.89f

        mainCamera.transform.position = new Vector3(centerX, centerY, mainCamera.transform.position.z);
        mainCamera.orthographicSize = Mathf.Max(h, w);
    }

    #endregion

    #region A* Pathfinding

    private void FindPath()
    {
        Vector2Int startNode = new(0, 0);
        Vector2Int goalNode = new(width - 1, height - 1);

        List<Vector2Int> path = CalculatePath(startNode, goalNode);

        if (path != null)
        {
            DisplayPath(path);

            if (npcInstance != null)
            {
                npcInstance.MoveAlongPath(path);
            }
        }
        else
        {
            print($"<color=red> Path not found!!!! </color>");
        }
    }

    private List<Vector2Int> CalculatePath(Vector2Int start, Vector2Int goal)
    {
        Node[,] nodes = new Node[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                nodes[x, y] = new Node(new Vector2Int(x, y), grid[x, y] == 0);
            }
        }

        Node startNode = nodes[start.x, start.y];
        Node goalNode = nodes[goal.x, goal.y];
        List<Node> openList = new() { startNode };
        HashSet<Node> closedHS = new();

        startNode.gCost = 0;
        startNode.hCost = Heuristic(start, goal);

        while (openList.Count > 0)
        {
            Node currentNode = openList[0];

            foreach (var neighbor in openList)
            {
                if (neighbor.FCost < currentNode.FCost || (neighbor.FCost == currentNode.FCost && neighbor.hCost < currentNode.hCost))
                {
                    currentNode = neighbor;
                }
            }
             
            openList.Remove(currentNode);
            closedHS.Add(currentNode);

            if (currentNode == goalNode)
            {
                return RetracePath(startNode, goalNode);
            }

            foreach (var neighbor in GetNeighbors(currentNode, nodes))
            {
                if (!neighbor.walkable || closedHS.Contains(neighbor))
                {
                    continue;
                }

                float tentativeG = currentNode.gCost + 1;

                if (tentativeG < neighbor.gCost)
                {
                    neighbor.gCost = tentativeG;
                    neighbor.hCost = Heuristic(neighbor.pos, goal);
                    neighbor.parent = currentNode;

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }

        return null;
    }

    // Find neighbours from current node
    private List<Node> GetNeighbors(Node currNode, Node[,] nodes)
    {
        Vector2Int[] directions =
        {
            new( 1, 0), // Right
            new(-1, 0), // Left
            new( 0, 1), // Up
            new( 0,-1), // Down
        };

        List<Node> neighbors = new();

        foreach (var dir in directions)
        {
            Vector2Int nodePos = currNode.pos + dir;

            if (nodePos.x >= 0 && nodePos.x < width && nodePos.y >= 0 && nodePos.y < height)
            {
                neighbors.Add(nodes[nodePos.x, nodePos.y]);
            }
        }

        return neighbors;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector2Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new();
        Node currentNode = endNode;

        while (currentNode != null) //(while cur != startNode)
        {
            path.Add(currentNode.pos);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        return path;
    }

    #endregion

    #region Display

    private void DisplayPath(List<Vector2Int> path)
    {
        if (path == null) return;

        foreach (var step in path)
        {
            Vector3 pos = new(step.x, step.y, 0f);

            Instantiate(foundPathPrefab, pos, Quaternion.identity);
        }
    }

    #endregion
}
