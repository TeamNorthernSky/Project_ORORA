using System;

[Serializable]
public struct StatBlock
{
    // scalar stats
    public float HP;
    public float Atk;
    public float DEF;
    public float Luck;

    // rate/speed stats
    public float Speed;
    public float CriticalRate;
    public float CounterRate;
    public float AvoidRate;

    public StatBlock(
        float hp,
        float atk,
        float def,
        float luck,
        float speed,
        float criticalRate,
        float counterRate,
        float avoidRate)
    {
        HP = hp;
        Atk = atk;
        DEF = def;
        Luck = luck;
        Speed = speed;
        CriticalRate = criticalRate;
        CounterRate = counterRate;
        AvoidRate = avoidRate;
    }

    public static StatBlock operator +(StatBlock a, StatBlock b)
    {
        return new StatBlock(
            a.HP + b.HP,
            a.Atk + b.Atk,
            a.DEF + b.DEF,
            a.Luck + b.Luck,
            a.Speed + b.Speed,
            a.CriticalRate + b.CriticalRate,
            a.CounterRate + b.CounterRate,
            a.AvoidRate + b.AvoidRate
        );
    }


    public void ClampToMinimumOne()
    {
       
        Atk = Math.Max(1f, Atk);
        DEF = Math.Max(0f, DEF);
        HP = Math.Max(1f, HP);

        
        CriticalRate = Math.Max(0f, CriticalRate);
        CounterRate = Math.Max(0f, CounterRate);
        AvoidRate = Math.Max(0f, AvoidRate);

        Speed = Math.Max(0f, Speed);
        Luck = Math.Max(0f, Luck);
    }
}
