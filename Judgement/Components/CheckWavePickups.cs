using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

public class CheckWavePickups : MonoBehaviour
{
    List<GameObject> potentialList = new List<GameObject>();

    public void AddPickup(GameObject pickup)
    {
        potentialList.Add(pickup);
    }

    public bool CanStartWave()
    {
        bool canStartWave = false;
        Debug.LogWarning($"PotentialList Count Before {potentialList.Count}");
        potentialList.RemoveAll(potential =>
        {
            Debug.LogWarning($"Potential object {potential}");
            if (potential == null)
            { return true; }
            else return false;
        });
        Debug.LogWarning($"PotentialList Count After {potentialList.Count}");

        if (potentialList.Count <= 0)
            canStartWave = true;

        Debug.LogWarning($"Can Start Wave? {canStartWave}");

        return canStartWave;
    }
}