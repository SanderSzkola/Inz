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

    public virtual void Execute(Unit caster, Unit target, MessageLog messageLog)
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
    }

    public bool IsOnCooldown() => remainingCooldown > 0;

    public Spell Clone()
    {
        return (Spell)this.MemberwiseClone();
    }

    protected string GetUnitColoring(Unit unit)
    {
        if (unit == null) return "<color=white>";
        if (unit.isPlayerUnit) return "<color=green>";
        else return "<color=red>";
    }

    public virtual string Description()
    {
        return "Should be overriden";
    }
}

public class AttackSpell : Spell
{
    public AttackSpell(string name, int power, int mpCost, int cooldown, TargetingMode targetingMode, Element element, Sprite graphic)
        : base(name, power, mpCost, cooldown, targetingMode, element, graphic)
    {
        // If VariantSpell had something unique that is not in baseSpell it would be here, todo?
    }

    public override void Execute(Unit caster, Unit target, MessageLog messageLog)
    {
        base.Execute(caster, target, messageLog);
        int unitPower = Element == Element.None ? caster.PAtk : caster.MAtk;
        int targetDefense = Element == Element.None ? target.PDef : target.MDef;
        float resistance = target.GetResistance(Element) / 100f;

        int damage = (int)Mathf.Max((Power / 100f * unitPower) * (1f - resistance) - targetDefense, 0);

        messageLog.AddMessage($"{GetUnitColoring(caster)}{caster.unitName}</color> cast {Name} on {GetUnitColoring(target)}{target.unitName}</color> dealing <color=red>{damage}</color> damage.");
        target.ApplyDamage(damage);

        StartCooldown();
        caster.canAct = false;
    }

    public override string Description()
    {
        string damageType = Element == Element.None ? "Psyhical" : $"Magical";
        string s = $"{Name}";
        s += $"\nDamage: {Power}% of {damageType} attack";
        if (Element != Element.None) s += $"\nElement: {Element}";
        s += $"\nMP cost: {MPCost}";
        s += $"\nCooldown: {Cooldown}";
        s += $"\nTargeting mode: {TargetingMode}";
        return s;
    }
}

public class RestoreSpell : Spell
{
    public RestoreSpell(string name, int power, int mpCost, int cooldown, TargetingMode targetingMode, Element element, Sprite graphic)
        : base(name, power, mpCost, cooldown, targetingMode, element, graphic)
    {
        // If VariantSpell had something unique that is not in baseSpell it would be here, todo?
    }

    public override void Execute(Unit caster, Unit target, MessageLog messageLog)
    {
        base.Execute(caster, target, messageLog);
        messageLog.AddMessage($"{GetUnitColoring(caster)}{caster.unitName}</color> restored <color=blue>{Power}</color> MP.");
        caster.ChangeMPBy(Power);

        StartCooldown();
        caster.canAct = false;
    }
    public override string Description()
    {
        string s = $"{Name}";
        s += $"\nWeak Restore spell. Increases MP by: {Power}";
        s += $"\nCooldown: {Cooldown}";
        s += $"\nTargeting mode: {TargetingMode}";
        return s;
    }
}
