using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

public class NodeMapGenerator : MonoBehaviour
{
    public static NodeMapGenerator Instance { get; private set; }

    public List<List<MapNode>> Map;
    public MapNode CurrentNode;

    public readonly int NumFloors = 10; // length of map, number of horizontal steps from start to end
    public readonly int FloorWidth = 5; // width of a map, number of different vertical positions possible to generate
    private readonly int MaxNodesPerFloor = 5; // determines possible variations of paths, lower means less branching nodes
    private readonly int MaxLengthOfStraigthPath = 3; // used to avoid dull-looking map shapes


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

    //private void Start()
    //{
    //    string path = "testMap";
    //    if(Map == null)
    //    {
    //        GenerateMap();
    //    }
    //    DebugSaveMapToFile(path);
    //    DebugLoadMapFromFile(path);
    //    DebugSaveMapToFile(path + "2");
    //}

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
        for (int floorIndex = 1; floorIndex < NumFloors - 1; floorIndex++)
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

        // Add boss floor
        List<MapNode> lastFloor = new List<MapNode>();
        MapNode bossNode = new MapNode(NumFloors, FloorWidth / 2);
        lastFloor.Add(bossNode);

        foreach (MapNode baseNode in Map[NumFloors - 2])
        {
            baseNode.Connect(bossNode);
        }

        Map.Add(lastFloor);

        AssignEncounterTypes();
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

    // straighten crossed "X" connections
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

    private void AssignEncounterTypes()
    {
        int middlePoint = NumFloors / 2;
        int endPoint = NumFloors - 2;
        foreach (var floor in Map)
        {
            foreach (MapNode node in floor)
            {
                // Easy start: first nodes are always BATTLE
                if (node.X <= 1)
                {
                    node.EncounterType = EncounterType.BATTLE;
                }
                // Rest in the middle and end floor
                else if (node.X == middlePoint || node.X == endPoint)
                {
                    node.EncounterType = EncounterType.REST;
                }
                else if (node.X > endPoint)
                {
                    node.EncounterType = EncounterType.BOSS;
                }
                // Assign random encounter type, reroll if necessary
                else
                {
                    do
                    {
                        node.EncounterType = EncounterHelper.GetRandomEncounterType();

                        bool hasConflict = false;
                        foreach (MapNode prevNode in node.PreviousNodes)
                        {
                            if (prevNode.EncounterType != EncounterType.BATTLE
                                && prevNode.EncounterType == node.EncounterType
                                || node.EncounterType == EncounterType.REST
                                && (node.X == middlePoint - 1 
                                || node.X == endPoint - 1))
                            {
                                hasConflict = true;
                                break;
                            }
                        }
                        if (!hasConflict)
                            break;

                    } while (true);
                }
            }
        }
    }

    public MapData GetMapData()
    {
        if (Map == null)
        {
            throw new IOException("Tried to get MapData before it was initialized");
        }

        MapData mapData = new MapData();
        for (int floorNum = 0; floorNum < Map.Count; floorNum++)
        {
            var floor = Map[floorNum];
            var nextFloor = floorNum < Map.Count - 1 ? Map[floorNum + 1] : null;
            FloorData floorData = new FloorData();
            foreach (var node in floor)
            {
                MapNodeData nodeData = new MapNodeData(node.X, node.Y, node.EncounterType, node.IsPlayerHere);
                foreach (var nextNode in node.NextNodes)
                {
                    if (nextFloor == null) break;
                    int index = nextFloor.IndexOf(nextNode);
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
        if (mapData == null 
            || mapData.Floors == null 
            || mapData.Floors.Count == 0)
        {
            GenerateMap();
            CurrentNode = null;
            return;
        }

        Map = new List<List<MapNode>>();
        foreach (FloorData floorData in mapData.Floors)
        {
            List<MapNode> floor = new List<MapNode>();
            foreach (MapNodeData nodeData in floorData.Nodes)
            {
                MapNode mapNode = new MapNode(nodeData.X,
                    nodeData.Y,
                    nodeData.EncounterType,
                    nodeData.IsPlayerHere);
                floor.Add(mapNode);
                if (mapNode.IsPlayerHere)
                {
                    CurrentNode = mapNode;
                }
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
                MapNodeData nodeData = new MapNodeData(node.X, node.Y, node.EncounterType, node.IsPlayerHere);
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
                floor.Add(new MapNode(nodeData.X, nodeData.Y, nodeData.EncounterType, nodeData.IsPlayerHere));
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
