using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Use this calss to enable animations bypassing server authority
/// </summary>
public class OwnerNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
