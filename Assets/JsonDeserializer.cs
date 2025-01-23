using System;
using System.Collections.Generic;

[System.Serializable]
public class SaveFileData
{
    public UnitData[] playerUnits;
    public MapData mapData;
}

[System.Serializable]
public class UnitData
{
    // Parameters
    public string Name;
    public bool IsPlayerUnit;

    public int MaxHP;
    public int CurrHP;
    public int MaxMP;
    public int CurrMP;
    public int MPRegen;
    public int PAtk;
    public int PDef;
    public int MAtk;
    public int MDef;

    public int Exp;
    public int ExpToNextLevel;
    public int SkillPoints;
    public int ExpOnDeath;

    public int FireRes;
    public int IceRes;

    public List<string> SpellNames = new List<string>();

    // Graphics
    public float maskRed;
    public float maskGreen;
    public float maskBlue;

    public float ColliderOffsetX;
    public float ColliderOffsetY;
    public float ColliderSizeX;
    public float ColliderSizeY;
    public float SpritePosX;
    public float SpritePosY;
    public float SpritePosZ;
    public float SpriteWidth;
    public float SpriteHeight;

    public void ProcessRest()
    {
        while (Exp > ExpToNextLevel)
        {
            MaxHP += 50;
            MaxMP += 20;
            SkillPoints += 1;
            PAtk += 5;
            MAtk += 5;
            PDef += 2;
            MDef += 2;
            FireRes += 5;
            IceRes += 5;
            Exp -= ExpToNextLevel;
            ExpToNextLevel *= 2;
        }
    }
}

[System.Serializable]
public class LevelDefsList
{
    public string[] levelDefs;
}

[System.Serializable]
public class EnemyDefsList
{
    public EnemyDef[] enemyDefs;
}

[System.Serializable]
public class EnemyDef
{
    public string key;
    public UnitData unitData;
}

[System.Serializable]
public class SpellData
{
    public string name;
    public int power;
    public int mpCost;
    public int cooldown;
    public string targetingMode;
    public string element;
    public string graphicPath;
}

[System.Serializable]
public class SpellDataList
{
    public List<SpellData> spells;
}

[Serializable]
public class MapNodeData
{
    public int X;
    public int Y;
    public List<int> NextNodeIndices;
    public string EncounterType;
    public bool IsPlayerHere;

    public MapNodeData(int x, int y, EncounterType encounterType, bool isPlayerHere)
    {
        X = x;
        Y = y;
        NextNodeIndices = new List<int>();
        EncounterType = encounterType.ToString();
        IsPlayerHere = isPlayerHere;
    }
}

[Serializable]
public class FloorData
{
    public List<MapNodeData> Nodes;

    public FloorData()
    {
        Nodes = new List<MapNodeData>();
    }
}

[Serializable]
public class MapData
{
    public List<FloorData> Floors;
    public int CurrentFloor;

    public MapData(int currentFloor)
    {
        Floors = new List<FloorData>();
        CurrentFloor = currentFloor;
    }
}
