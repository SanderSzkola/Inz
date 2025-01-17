using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

public class NodeMapGenerator : MonoBehaviour
{
    public static NodeMapGenerator Instance { get; private set; }

    public List<List<MapNode>> Map;

    public readonly int NumFloors = 10;
    public readonly int FloorWidth = 5;
    private readonly int MaxNodesPerFloor = 5;
    private readonly int MaxLengthOfStraigthPath = 3;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void GenerateMap()
    {
        Map = new List<List<MapNode>>();
        int startY1 = Mathf.FloorToInt(FloorWidth / 3);
        int startY2 = Mathf.CeilToInt(2 * FloorWidth / 3);

        // Initialize the first floor with 2 starting nodes
        List<MapNode> floor0 = new List<MapNode>
        {
            new MapNode(0, startY1),
            new MapNode(0, startY2)
        };
        Map.Add(floor0);

        // Generate next floors
        for (int floorIndex = 1; floorIndex < NumFloors; floorIndex++)
        {
            List<MapNode> currentFloor = new List<MapNode>();
            List<MapNode> previousFloor = Map[floorIndex - 1];
            MapNode[] alreadyGenerated = new MapNode[MaxNodesPerFloor];

            // Generate 1 new node for each old node
            foreach (MapNode baseNode in previousFloor)
            {
                bool ignoreStraightPathCheck = false;
                while (baseNode.NextNodes.Count < 1)
                {
                    GenerateNode(baseNode, alreadyGenerated, currentFloor, floorIndex, ignoreStraightPathCheck);
                    ignoreStraightPathCheck = true;
                }
            }

            // Fill the remaining nodes randomly
            for (int i = currentFloor.Count; i < MaxNodesPerFloor; i++)
            {
                MapNode baseNode = previousFloor[UnityEngine.Random.Range(0, previousFloor.Count)];
                GenerateNode(baseNode, alreadyGenerated, currentFloor, floorIndex);
            }

            CorrectPaths(Map[floorIndex - 1]);
            Map.Add(currentFloor);
        }
    }

    private void GenerateNode(MapNode baseNode, MapNode[] alreadyGenerated, List<MapNode> currentFloor, int floorIndex, bool ignoreStraightPathCheck = false)
    {
        int newX = baseNode.X + 1;
        int newY;
        int maxAttempts = 5;
        int attempts = 0;
        do
        {
            attempts++;
            if (attempts > maxAttempts)
            {
                // Debug.LogWarning($"Reached maxAttempts limit, MapNode not created");
                return;
            }
            newY = Mathf.Clamp(baseNode.Y + UnityEngine.Random.Range(-1, 2), 0, FloorWidth - 1);
        } while (!ignoreStraightPathCheck && IsStraightPath(Map, newY, floorIndex, MaxLengthOfStraigthPath));

        MapNode newNode;
        if (alreadyGenerated[newY] == null)
        {
            newNode = new MapNode(newX, newY);
            alreadyGenerated[newY] = newNode;
            currentFloor.Add(newNode);
        }
        else
        {
            newNode = alreadyGenerated[newY];
        }
        baseNode.Connect(newNode);
    }

    private bool IsStraightPath(List<List<MapNode>> floors, int yValue, int currentFloorIndex, int lookback)
    {
        if (currentFloorIndex < lookback)
        {
            return false;
        }

        for (int i = currentFloorIndex - lookback; i < currentFloorIndex; i++)
        {
            if (!floors[i].Exists(node => node.Y == yValue))
            {
                return false;
            }
        }
        return true;
    }

    private void CorrectPaths(List<MapNode> previousFloor)
    {
        List<(MapNode nodeA, MapNode nodeB, MapNode nextA, MapNode nextB)> swaps = new List<(MapNode, MapNode, MapNode, MapNode)>();

        foreach (MapNode nodeA in previousFloor)
        {
            foreach (MapNode nodeB in previousFloor)
            {
                if (nodeA == nodeB || Mathf.Abs(nodeA.Y - nodeB.Y) > 1)
                {
                    continue;
                }

                foreach (MapNode nextA in nodeA.NextNodes)
                {
                    foreach (MapNode nextB in nodeB.NextNodes)
                    {
                        if (nextA.Y == nodeB.Y && nextB.Y == nodeA.Y)
                        {
                            swaps.Add((nodeA, nodeB, nextA, nextB));
                        }
                    }
                }
            }
        }

        foreach (var (nodeA, nodeB, nextA, nextB) in swaps)
        {
            nodeA.Disconnect(nextA);
            nodeA.Connect(nextB);
            nodeB.Disconnect(nextB);
            nodeB.Connect(nextA);
        }
    }

    public MapData GetMapData()
    {
        if (Map == null)
        {
            Debug.LogError("Tried to get MapData before it was initialized");
            return null;
        }

        MapData mapData = new MapData();
        foreach (var floor in Map)
        {
            FloorData floorData = new FloorData();
            foreach (var node in floor)
            {
                MapNodeData nodeData = new MapNodeData(node.X, node.Y);
                foreach (var nextNode in node.NextNodes)
                {
                    int index = floor.IndexOf(nextNode);
                    if (index != -1)
                    {
                        nodeData.NextNodeIndices.Add(index);
                    }
                }
                floorData.Nodes.Add(nodeData);
            }
            mapData.Floors.Add(floorData);
        }
        return mapData;
    }

    public void LoadMapFromData(MapData mapData)
    {
        if (mapData == null || mapData.Floors.Count == 0)
        {
            GenerateMap();
            return;
        }

        Map = new List<List<MapNode>>();

        foreach (FloorData floorData in mapData.Floors)
        {
            List<MapNode> floor = new List<MapNode>();
            foreach (MapNodeData nodeData in floorData.Nodes)
            {
                floor.Add(new MapNode(nodeData.X, nodeData.Y));
            }
            Map.Add(floor);
        }

        for (int i = 0; i < Map.Count - 1; i++)
        {
            FloorData floorData = mapData.Floors[i];
            for (int j = 0; j < floorData.Nodes.Count; j++)
            {
                foreach (int nextIndex in floorData.Nodes[j].NextNodeIndices)
                {
                    Map[i][j].Connect(Map[i + 1][nextIndex]);
                }
            }
        }
    }

    private void DebugPrintMap()
    {
        string s = "";
        foreach (List<MapNode> floor in Map)
        {
            foreach (MapNode node in floor)
            {
                s += "x= " + node.X + " ,y= " + node.Y + " ,connectionsNext = ";
                foreach (MapNode nextNode in node.NextNodes)
                {
                    s += $"[{nextNode.X},{nextNode.Y}], ";
                }
                s += "connectionsPrevious = ";
                foreach (MapNode nextNode in node.PreviousNodes)
                {
                    s += $"[{nextNode.X},{nextNode.Y}], ";
                }
                s += "\n";
            }
            s += "-----NEXT FLOOR-----\n";
        }
        Debug.Log(s);
    }

    private void DebugValidateMaps()
    {
        int num = 10000;
        for (int n = 1; n <= num; n++)
        {
            GenerateMap();
            foreach (List<MapNode> floor in Map)
            {
                for (int i = 0; i < floor.Count; i++)
                {
                    for (int j = i + 1; j < floor.Count; j++)
                    {
                        Assert.AreNotEqual(floor[i].Y, floor[j].Y);
                    }
                }
            }
            Map.Clear();
            if (n % (num / 10) == 0)
            {
                Debug.Log($"Validated {n * 100 / num}% out of {num} maps");
            }
        }
    }

    private void DebugSaveMapToFile(string filePath)
    {
        MapData mapData = new MapData();
        for (int i = 0; i < Map.Count; i++)
        {
            var floor = Map[i];
            FloorData floorData = new FloorData();
            foreach (var node in floor)
            {
                MapNodeData nodeData = new MapNodeData(node.X, node.Y);
                foreach (var nextNode in node.NextNodes)
                {
                    if (i < Map.Count)
                    {
                        var nextFLoor = Map[i + 1];
                        int index = nextFLoor.IndexOf(nextNode);
                        if (index != -1)
                        {
                            nodeData.NextNodeIndices.Add(index);
                        }
                    }
                }
                floorData.Nodes.Add(nodeData);
            }
            mapData.Floors.Add(floorData);
        }

        string json = JsonUtility.ToJson(mapData, true);
        File.WriteAllText(filePath, json);
    }

    private void DebugLoadMapFromFile(string filePath)
    {
        string fullPath = filePath;
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"File not found: {fullPath}");
            return;
        }

        string json = File.ReadAllText(fullPath);
        MapData mapData = JsonUtility.FromJson<MapData>(json);

        // Load nodes so they can be connected
        Map = new List<List<MapNode>>();
        foreach (FloorData floorData in mapData.Floors)
        {
            List<MapNode> floor = new List<MapNode>();
            foreach (MapNodeData nodeData in floorData.Nodes)
            {
                floor.Add(new MapNode(nodeData.X, nodeData.Y));
            }
            Map.Add(floor);
        }

        // Connect nodes
        for (int floorNum = 0; floorNum < Map.Count - 1; floorNum++)
        {
            FloorData floorData = mapData.Floors[floorNum];
            for (int nodeNum = 0; nodeNum < Map[floorNum].Count; nodeNum++)
            {
                MapNodeData nodeData = floorData.Nodes[nodeNum];
                foreach (int index in nodeData.NextNodeIndices)
                {
                    Map[floorNum][nodeNum].Connect(Map[floorNum + 1][index]);
                }
            }
        }
    }
}
