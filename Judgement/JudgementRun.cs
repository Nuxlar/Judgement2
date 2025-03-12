using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Judgement
{
    public class JudgementRun : InfiniteTowerRun
    {
        public Vector3 safeWardPos = Vector3.zero;
        [SyncVar]
        public Dictionary<NetworkInstanceId, float> persistentHP = new Dictionary<NetworkInstanceId, float>();
        [SyncVar]
        public Dictionary<NetworkInstanceId, int> persistentCurse = new Dictionary<NetworkInstanceId, int>();
    }
}
