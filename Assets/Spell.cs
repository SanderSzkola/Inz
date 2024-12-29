using UnityEditor.Experimental.GraphView;
using UnityEngine;

public enum Element
{
    None,
    Fire,
    Ice
}

public enum TargetingMode
{
    Self,
    Ally,
    Enemy,
    RandomEnemy,
    AllAllies,
    AllEnemies
}

public abstract class Spell
{
    public string Name { get; protected set; }
    public int Power { get; protected set; }
    public int MPCost { get; protected set; }
    public int Cooldown { get; protected set; }
    public TargetingMode TargetingMode { get; protected set; }
    public Sprite Graphic { get; protected set; }

    private int remainingCooldown = 0;
    public int RemainingCooldown => remainingCooldown; // the Capital is public getter, the small is private

    protected Spell(string name, int power, int mpCost, int cooldown, TargetingMode targetingMode, Sprite graphic)
    {
        Name = name;
        Power = power;
        MPCost = mpCost;
        Cooldown = cooldown;
        TargetingMode = targetingMode;
        Graphic = graphic;
    }

    public bool IsReady() => remainingCooldown == 0;

    public void StartCooldown() => remainingCooldown = Cooldown;

    public void ReduceCooldown() => remainingCooldown = Mathf.Max(remainingCooldown - 1, 0);

    public abstract void Execute(Unit caster, Unit target);

    public bool IsOnCooldown() => remainingCooldown > 0;
}

public class AttackSpell : Spell
{
    public Element Element { get; private set; }

    public AttackSpell(string name, int power, int mpCost, int cooldown, TargetingMode targetingMode, Element element, Sprite graphic)
        : base(name, power, mpCost, cooldown, targetingMode, graphic)
    {
        Element = element;
    }

    public override void Execute(Unit caster, Unit target)
    {
        if (!IsReady()) return;
        caster.changeMPBy(MPCost * -1);
        int baseDamage = Element == Element.None ? caster.pAtk : caster.mAtk;
        int targetDefense = Element == Element.None ? target.pDef : target.mDef;
        int resistance = target.GetResistance(Element);

        int damage = Mathf.Max((Power + baseDamage - targetDefense) - resistance, 0);

        target.ApplyDamage(damage);
        Debug.Log($"{caster.unitName} cast {Name} on {target.unitName} for {damage} damage");

        StartCooldown();
        caster.canAct = false;
    }
}
