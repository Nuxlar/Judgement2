using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

public class CheckWavePickups : MonoBehaviour
{
    int pickupCount = 0;

    private void Start()
    {
        if (Run.instance && Run.instance.name.Contains("Judgement"))
        {
            pickupCount = Run.instance.participatingPlayerCount;
        }
    }

    public bool RemovePickup()
    {
        bool pickupRemoved = false;

        if (pickupCount > 0)
        {
            pickupCount--;
            pickupRemoved = true;
        }

        return pickupRemoved;
    }

    public bool CanStartWave()
    {
        bool canStartWave = false;

        return canStartWave;
    }
}