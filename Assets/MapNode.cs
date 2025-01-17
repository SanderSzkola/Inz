using System.Collections.Generic;

public class MapNode
{
    public int X { get; private set; }
    public int Y { get; set; }
    public List<MapNode> NextNodes { get; private set; }
    public List<MapNode> PreviousNodes { get; set; }
    public NodeButton NodeButton { get; set; }

    public MapNode(int x, int y)
    {
        X = x;
        Y = y;
        NextNodes = new List<MapNode>();
        PreviousNodes = new List<MapNode>();
    }

    public void Connect(MapNode nextNode)
    {
        if (!NextNodes.Contains(nextNode))
        {
            NextNodes.Add(nextNode);
            nextNode.PreviousNodes.Add(this);
        }
    }

    public void Disconnect(MapNode nextNode)
    {
        if (NextNodes.Contains(nextNode))
        {
            NextNodes.Remove(nextNode);
            nextNode.PreviousNodes.Remove(this);
        }
    }

    public bool OccupiesSameSpace(MapNode mapNode)
    {
        return (X == mapNode.X && Y == mapNode.Y);
    }
}

public enum EncounterType { BATTLE, HARDBATTLE, SKILL, REST, BOSS}