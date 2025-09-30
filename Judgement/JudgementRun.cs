using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Judgement
{
    public class JudgementRun : InfiniteTowerRun
    {
        public Vector3 safeWardPos = Vector3.zero;
        public bool healShrineUsed = false;
        [SyncVar]
        public Dictionary<NetworkInstanceId, float> persistentHP = new Dictionary<NetworkInstanceId, float>();
    }
}
