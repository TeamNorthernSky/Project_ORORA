using System;
using UnityEngine;

public class PartyRegistry : MonoBehaviour
{
    [SerializeField] private PartyGridMover[] partyMovers;

    public PartyGridMover[] PartyMovers => partyMovers ?? Array.Empty<PartyGridMover>();
}
