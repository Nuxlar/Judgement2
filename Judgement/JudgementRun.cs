using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Judgement
{
    public class JudgementRun : InfiniteTowerRun
    {
        public int currentWave = 0;
        public bool shouldGoBazaar = true;
        public bool isFirstStage = true;
        public Vector3 safeWardPos = Vector3.zero;
        public Xoroshiro128Plus bazaarRng;
        public Dictionary<NetworkInstanceId, float> persistentHP = new Dictionary<NetworkInstanceId, float>();
        public Dictionary<NetworkInstanceId, int> persistentCurse = new Dictionary<NetworkInstanceId, int>();
    }
}
