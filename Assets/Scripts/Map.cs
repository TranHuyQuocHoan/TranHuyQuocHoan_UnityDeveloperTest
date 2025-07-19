using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Map : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
   
    [Header("Prefabs")]
    [SerializeField] private NPC npcPrefab;
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private GameObject nodePrefab;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    private int[,] grid;
    private NPC npcInstance;
    private Tile[,] tiles;

    private Vector2Int npcPos;
    private Vector2Int goalPos;

    #region Unity Function
    private void Start()
    {
        GenerateMap();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var currentScene = SceneManager.GetActiveScene();

            SceneManager.LoadScene(currentScene.buildIndex);
        }
    }

    #endregion

    #region Helpers
    private Vector2Int[] GetFourDiections()
    {
        return new Vector2Int[] 
        {   
            new( 1, 0),
            new(-1, 0),
            new( 0, 1),
            new( 0,-1),
        };
    }
    #endregion

    #region Map Generation & Spawn
    private void GenerateMap()
    {
        print("generate new map");

        ChooseNpcPosAndGoal();

        List<Vector2Int> testPath;

        do
        {
            GenerateRandomData();

            grid[npcPos.x, npcPos.y] = 0;
            grid[goalPos.x, goalPos.y] = 0;

            testPath = CalculatePath(npcPos, goalPos);
        }
        while (testPath == null);

        SpawnPrefabs();
        CenterMap();
    }

    private void ChooseNpcPosAndGoal()
    {
        List<Vector2Int> allCells = new();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                allCells.Add(new Vector2Int(x, y));
            }
        }

        // Shuffle 
        for (int i = 0; i < allCells.Count; i++)
        {
            int j = Random.Range(i, allCells.Count);

            Vector2Int temp = allCells[i];

            allCells[i] = allCells[j];
            allCells[j] = temp;
        }

        npcPos = allCells[0];
        goalPos = allCells[1];
    }


    // Generate random map
    private void GenerateRandomData()
    {
        tiles = new Tile[width, height];
        grid = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = Random.Range(0, 2);
            }
        }
    }

    // Spawn grid
    private void SpawnPrefabs()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new(x, y, 0);

                Instantiate(nodePrefab, pos, Quaternion.identity);

                Tile tile = Instantiate(tilePrefab, pos, Quaternion.identity);

                // NPC
                if (x == npcPos.x && y == npcPos.y)
                {
                    npcInstance = Instantiate(npcPrefab, pos, Quaternion.identity);

                    tile = npcInstance.GetComponent<Tile>();
                    tile.ChooseColor(ColorType.NPC);
                }
                // Goal
                else if (x == goalPos.x && y == goalPos.y)
                {
                    tile.ChooseColor(ColorType.Goal);
                }
                // Wall
                else if (grid[x, y] == 1)
                {
                    tile.ChooseColor(ColorType.Wall);
                }
                // Normnal Path
                else
                {
                    tile.ChooseColor(ColorType.Path);
                }

                tiles[x, y] = tile;
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

    // Find path ( button listener )
    public void DisplayPathAndMoveNpcAStar()
    {
        List<Vector2Int> path = CalculatePath(npcPos, goalPos);

        if (path != null)
        {
            StartCoroutine(DisplayPath(path));
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

            // Check if we reach the goal, then we retrace the path
            if (currentNode == goalNode)
            {
                return RetracePathAStar(startNode, goalNode);
            }

            foreach (var neighbor in GetNeighbors(currentNode, nodes))
            {
                // Check if neighbor is not walkable or already contains neighbor
                if (!neighbor.walkable || closedHS.Contains(neighbor))
                {
                    continue;
                }

                // Neighbor gCost = Current gCost + 1 step
                float tentativeG = currentNode.gCost + 1;

                if (tentativeG < neighbor.gCost)
                {
                    neighbor.gCost = tentativeG;
                    neighbor.hCost = Heuristic(neighbor.pos, goal);
                    neighbor.nodeCameFrom = currentNode;

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
        List<Node> neighbors = new();

        foreach (var dir in GetFourDiections())
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

    private List<Vector2Int> RetracePathAStar(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.pos);
            currentNode = currentNode.nodeCameFrom;
        }

        path.Reverse();

        return path;
    }

    #endregion

    #region BFS Pathfinding

    public void DisplayPathAndMoveNpcBFS()
    {
        List<Vector2Int> path = FindPathBFS(npcPos, goalPos);

        if (path != null)
        {
            StartCoroutine(DisplayPath(path));
        }
        else
        {
            print($"<color=red> Path not found!!!! </color>");
        }
    }

    public List<Vector2Int> FindPathBFS(Vector2Int start, Vector2Int goal)
    {
        bool[,] visited = new bool[width, height];

        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Queue<Vector2Int> queue = new();

        queue.Enqueue(start);
        visited[start.x, start.y] = true;
        cameFrom[start] = start;

        // BFS loop
        while (queue.Count > 0)
        {
            Vector2Int currentNode = queue.Dequeue();

            // Check if we reach the goal, then we retrace the path
            if (currentNode == goal)
            {
                return RetracePathBFS(cameFrom, start, goal);
            }

            // Loop through directions
            foreach (var dir in GetFourDiections())
            {
                Vector2Int next = currentNode + dir;

                // Check out of bounds
                if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height)
                {
                    continue;
                }
                // Check if not walkable or already visited
                if (grid[next.x, next.y] == 1 || visited[next.x, next.y])
                {
                    continue;
                }

                visited[next.x, next.y] = true;
                cameFrom[next] = currentNode;
                queue.Enqueue(next);
            }
        }

        return null;
    }

    private List<Vector2Int> RetracePathBFS(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int goal)
    {
        var path = new List<Vector2Int>();
        var currentNode = goal;

        while (currentNode != start)
        {
            path.Add(currentNode);
            currentNode = cameFrom[currentNode];
        }

        //path.Add(start);
        path.Reverse();

        return path;
    }

    #endregion

    #region Display

    private IEnumerator DisplayPath(List<Vector2Int> path)
    {
        if (path == null)
        {
            yield break;
        }

        foreach (var step in path)
        {
            Tile tile = tiles[step.x, step.y];
            tile.ChooseColor(ColorType.FoundPath);

            yield return new WaitForSeconds(0.1f);
        }

        npcInstance.MoveAlongPath(path);
    }

    #endregion
}
