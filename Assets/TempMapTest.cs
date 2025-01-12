using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class NodeMapGenerator : MonoBehaviour
{
    public int NumFloors = 10;
    public int FloorWidth = 6;
    public int SoftDesiredNumOfConnections = 5;

    public List<List<MapNode>> floors;

    private void Start()
    {
        GenerateMap();
        SaveMapToFile("GeneratedMap.json");
        LoadMapFromFile("GeneratedMap.json");
    }

    private void GenerateMap()
    {
        floors = new List<List<MapNode>>();
        int startY1 = Mathf.FloorToInt(FloorWidth / 3);
        int startY2 = Mathf.CeilToInt(2 * FloorWidth / 3);

        // Initialize the first floor with 2 starting nodes
        List<MapNode> floor0 = new List<MapNode>
        {
            new MapNode(0, startY1),
            new MapNode(0, startY2)
        };
        floors.Add(floor0);

        // Generate next floors
        for (int floorIndex = 1; floorIndex < NumFloors; floorIndex++)
        {
            List<MapNode> currentFloor = new List<MapNode>();
            List<MapNode> previousFloor = floors[floorIndex - 1];

            // Generate 1 new node for each old node
            foreach (MapNode baseNode in previousFloor)
            {
                if (baseNode.NextNodes.Count < 1)
                {
                    int newX = baseNode.X + 1;
                    int newY;
                    do
                    {
                        newY = Mathf.Clamp(baseNode.Y + UnityEngine.Random.Range(-1, 2), 0, FloorWidth - 1);
                    } while (IsStraightPath(floors, newY, floorIndex, 2));

                    MapNode newNode = new MapNode(newX, newY);
                    baseNode.Connect(newNode);
                    currentFloor.Add(newNode);
                }
            }

            // Fill the remaining nodes randomly
            while (currentFloor.Count < SoftDesiredNumOfConnections)
            {
                MapNode baseNode = previousFloor[UnityEngine.Random.Range(0, previousFloor.Count)];
                int newX = baseNode.X + 1;
                int newY;
                do
                {
                    newY = Mathf.Clamp(baseNode.Y + UnityEngine.Random.Range(-1, 2), 0, FloorWidth - 1);
                } while (IsStraightPath(floors, newY, floorIndex, 2));

                MapNode newNode = new MapNode(newX, newY);
                baseNode.Connect(newNode);
                currentFloor.Add(newNode);
            }

            CorrectPaths(currentFloor, floors[floorIndex - 1]);
            RemoveDuplicates(currentFloor);
            floors.Add(currentFloor);
        }
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

    private void CorrectPaths(List<MapNode> currentFloor, List<MapNode> previousFloor)
    {
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
                            int tempY = nextA.Y;
                            nextA.Y = nextB.Y;
                            nextB.Y = tempY;
                        }
                    }
                }
            }
        }
    }

    public static void RemoveDuplicates(List<MapNode> currentFloor)
    {
        // Use a HashSet to keep track of processed nodes
        HashSet<MapNode> processedNodes = new HashSet<MapNode>();

        for (int i = 0; i < currentFloor.Count; i++)
        {
            MapNode nodeA = currentFloor[i];

            if (processedNodes.Contains(nodeA)) continue; // Skip already processed nodes

            for (int j = i + 1; j < currentFloor.Count; j++)
            {
                MapNode nodeB = currentFloor[j];

                if (nodeA.OccupiesSameSpace(nodeB))
                {
                    // Transfer connections from B to A
                    foreach (var nextNode in nodeB.NextNodes)
                    {
                        if (!nodeA.NextNodes.Contains(nextNode))
                        {
                            nodeA.Connect(nextNode);
                        }
                    }

                    foreach (var prevNode in nodeB.PreviousNodes)
                    {
                        if (!prevNode.NextNodes.Contains(nodeA))
                        {
                            prevNode.Connect(nodeA);
                        }
                    }

                    // Disconnect nodeB from all its connections
                    foreach (var nextNode in nodeB.NextNodes.ToList())
                    {
                        nodeB.Disconnect(nextNode);
                    }

                    foreach (var prevNode in nodeB.PreviousNodes.ToList())
                    {
                        prevNode.Disconnect(nodeB);
                    }

                    // Remove nodeB from the floor
                    currentFloor.RemoveAt(j);
                    j--;
                }
            }

            // Mark nodeA as processed
            processedNodes.Add(nodeA);
        }
    }


    private void SaveMapToFile(string filePath)
    {
        MapData mapData = new MapData();
        for (int i = 0; i < floors.Count; i++)
        {
            var floor = floors[i];
            FloorData floorData = new FloorData();
            foreach (var node in floor)
            {
                MapNodeData nodeData = new MapNodeData(node.X, node.Y);
                foreach (var nextNode in node.NextNodes)
                {
                    if (i < floors.Count)
                    {
                        var nextFLoor = floors[i + 1];
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

    private void LoadMapFromFile(string filePath)
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
        floors = new List<List<MapNode>>();
        foreach (FloorData floorData in mapData.Floors)
        {
            List<MapNode> floor = new List<MapNode>();
            foreach (MapNodeData nodeData in floorData.Nodes)
            {
                floor.Add(new MapNode(nodeData.X, nodeData.Y));
            }
            floors.Add(floor);
        }

        // Connect nodes
        for (int floorNum = 0; floorNum < floors.Count - 1; floorNum++)
        {
            FloorData floorData = mapData.Floors[floorNum];
            for (int nodeNum = 0; nodeNum < floors[floorNum].Count; nodeNum++)
            {
                MapNodeData nodeData = floorData.Nodes[nodeNum];
                foreach (int index in nodeData.NextNodeIndices)
                {
                    floors[floorNum][nodeNum].Connect(floors[floorNum + 1][index]);
                }
            }
        }
    }

    private void DebugPrintMap()
    {
        string s = "";
        foreach (List<MapNode> floor in floors)
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

    private void ValidateMaps()
    {
        int num = 10000;
        for (int n = 1; n <= num; n++)
        {
            GenerateMap();
            foreach (List<MapNode> floor in floors)
            {
                for (int i = 0; i < floor.Count; i++)
                {
                    for (int j = i + 1; j < floor.Count; j++)
                    {
                        Assert.AreNotEqual(floor[i].Y, floor[j].Y);
                    }
                }
            }
            floors.Clear();
            if (n % (num / 10) == 0)
            {
                Debug.Log($"Validated {n * 100 / num}% out of {num} maps");
            }
        }
    }
}
