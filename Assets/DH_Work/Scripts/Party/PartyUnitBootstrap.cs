using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PartyComposition))]
[RequireComponent(typeof(PartyIdentity))]
public class PartyUnitBootstrap : MonoBehaviour
{
    [Header("Bootstrap")]
    [SerializeField] private bool populateOnStart = true;
    [SerializeField] private int startingUnitCount = 3;
    [SerializeField] private bool onlyWhenAllSlotsEmpty = true;

    private PartyComposition partyComposition;
    private PartyIdentity partyIdentity;

    private void Awake()
    {
        partyComposition = GetComponent<PartyComposition>();
        partyIdentity = GetComponent<PartyIdentity>();
    }

    private void Start()
    {
        if (!Application.isPlaying || !populateOnStart)
            return;

        InitializeStartingUnits();
    }

    [ContextMenu("Initialize Starting Units")]
    public void InitializeStartingUnits()
    {
        if (partyComposition == null)
            partyComposition = GetComponent<PartyComposition>();

        if (partyIdentity == null)
            partyIdentity = GetComponent<PartyIdentity>();

        PersistentUnitRepository repository = PersistentUnitRepository.Instance;
        if (repository == null || partyComposition == null)
            return;

        startingUnitCount = Mathf.Max(0, startingUnitCount);
        partyComposition.EnsureSlotCount(startingUnitCount);

        if (onlyWhenAllSlotsEmpty && !AreAllTargetSlotsEmpty())
            return;

        for (int i = 0; i < startingUnitCount; i++)
        {
            if (partyComposition.GetUnitIndexAt(i) > 0)
                continue;

            int unitIndex = repository.CreateUnit();
            partyComposition.SetUnitIndexAt(i, unitIndex);
        }

        string partyLabel = partyIdentity != null ? partyIdentity.PartyId : gameObject.name;
        Debug.Log($"Party '{partyLabel}' initialized with {startingUnitCount} unit slot(s).", this);
    }

    private bool AreAllTargetSlotsEmpty()
    {
        for (int i = 0; i < startingUnitCount; i++)
        {
            if (partyComposition.GetUnitIndexAt(i) > 0)
                return false;
        }

        return true;
    }
}
