using System;
using UnityEngine;

public class CombatEncounterManager : MonoBehaviour
{
    public event Action<PartyGridMover, EnemyUnit> CombatStarted;

    public bool IsCombatActive { get; private set; }
    public PartyGridMover ActiveParty { get; private set; }
    public EnemyUnit ActiveEnemy { get; private set; }

    public bool BeginCombat(PartyGridMover party, EnemyUnit enemy)
    {
        if (party == null || enemy == null)
            return false;

        Debug.Log(
            $"Combat encounter requested between party '{party.PartyId}' and enemy '{enemy.EnemyId}'. " +
            "Combat flow is not implemented yet.",
            this);
        CombatStarted?.Invoke(party, enemy);
        return false;
    }

    public void ClearCombatState()
    {
        IsCombatActive = false;
        ActiveParty = null;
        ActiveEnemy = null;
    }
}
