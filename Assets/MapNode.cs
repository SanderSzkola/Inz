using System;
using System.Collections.Generic;
using System.IO;

public class MapNode
{
    public int X { get; private set; }
    public int Y { get; set; }
    public List<MapNode> NextNodes { get; set; }
    public List<MapNode> PreviousNodes { get; set; }
    public NodeButton NodeButton { get; set; }
    public bool IsPlayerHere { get; set; }

    public EncounterType EncounterType { get; set; }

    public MapNode(int x, int y, string encounterTypeString, bool isPlayerHere)
    {
        X = x;
        Y = y;
        NextNodes = new List<MapNode>();
        PreviousNodes = new List<MapNode>();
        IsPlayerHere = isPlayerHere;
        if (string.IsNullOrEmpty(encounterTypeString)) return;
        if (Enum.TryParse(encounterTypeString, true, out EncounterType EncounterType))
        {
            this.EncounterType = EncounterType;
        }
        else
        {
            throw new IOException($"Error loading MapNode data for EncounterType: {encounterTypeString}");
        }
    }

    // Simpler constructor without enum, calling default one
    public MapNode(int x, int y) : this(x, y, null, false)
    {
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