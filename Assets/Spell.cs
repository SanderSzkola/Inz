using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;

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
    public Element Element { get; private set; }
    public Sprite Graphic { get; protected set; }

    private int remainingCooldown = 0;
    public int RemainingCooldown => remainingCooldown; // the Capital is public getter, the small is private

    protected Spell(string name, int power, int mpCost, int cooldown, TargetingMode targetingMode, Element element, Sprite graphic)
    {
        Name = name;
        Power = power;
        MPCost = mpCost;
        Cooldown = cooldown;
        TargetingMode = targetingMode;
        Element = element;
        Graphic = graphic;
    }

    public bool IsReady() => remainingCooldown == 0;

    public void StartCooldown() => remainingCooldown = Cooldown;

    public void ReduceCooldown() => remainingCooldown = Mathf.Max(remainingCooldown - 1, 0);

    public abstract void Execute(Unit caster, Unit target, MessageLog messageLog);

    public bool IsOnCooldown() => remainingCooldown > 0;

    public Spell Clone()
    {
        return (Spell)this.MemberwiseClone();
    }

    protected string getUnitColoring(Unit unit)
    {
        if (unit == null) return "<color=white>";
        if (unit.isPlayerUnit) return "<color=green>";
        else return "<color=red>";
    }
}

public class AttackSpell : Spell
{
    public AttackSpell(string name, int power, int mpCost, int cooldown, TargetingMode targetingMode, Element element, Sprite graphic)
        : base(name, power, mpCost, cooldown, targetingMode, element, graphic)
    {
        // If attackSpell had something unique that is not in baseSpell it would be here, todo?
    }

    public override void Execute(Unit caster, Unit target, MessageLog messageLog)
    {
        if (!IsReady()) return;

        if (!caster.CanAffordCast(MPCost))
        {
            if (caster.isPlayerUnit)
            {
                messageLog.AddTemporaryMessage($"<color=blue>Not enough MP to cast {Name}.</color>");
            }
            return;
        }

        caster.ChangeMPBy(MPCost * -1);
        int baseDamage = Element == Element.None ? caster.pAtk : caster.mAtk;
        int targetDefense = Element == Element.None ? target.pDef : target.mDef;
        int resistance = target.GetResistance(Element);

        int damage = Mathf.Max((Power + baseDamage - targetDefense) - resistance, 0);

        messageLog.AddMessage($"{getUnitColoring(caster)}{caster.unitName}</color> cast {Name} on {getUnitColoring(target)}{target.unitName}</color> for <color=red>{damage}</color> damage.");
        target.ApplyDamage(damage);

        StartCooldown();
        caster.canAct = false;
    }
}
