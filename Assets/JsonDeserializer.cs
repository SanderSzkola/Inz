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

    public int FireRes;
    public int IceRes;

    public string SpellNames;

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
}

[System.Serializable]
public class LevelDefsList
{
    public string[] levelDefs;
}

[System.Serializable]
public class UnitDataList
{
    public UnitData[] playerUnits;
}

[System.Serializable]
public class EnemyDefsWrapper
{
    public EnemyDef[] enemyDefs;
}

[System.Serializable]
public class EnemyDef
{
    public string key;
    public UnitData unitData;
}
