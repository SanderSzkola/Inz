[System.Serializable]
public class LevelDefsList
{
    public string[] levelDefs;
}

[System.Serializable]
public class UnitDataList
{
    public UnitData[] units;
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
