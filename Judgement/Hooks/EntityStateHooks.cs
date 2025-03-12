using EntityStates.InfiniteTowerSafeWard;
using RoR2;
using UnityEngine;

namespace Judgement
{
    public class EntityStateHooks
    {

        public EntityStateHooks()
        {
            On.EntityStates.InfiniteTowerSafeWard.AwaitingActivation.OnEnter += ChangeBaseWardRadius;
            On.EntityStates.InfiniteTowerSafeWard.Active.OnEnter += ChangeActiveWardRadius;
        }

        private void ChangeBaseWardRadius(On.EntityStates.InfiniteTowerSafeWard.AwaitingActivation.orig_OnEnter orig, AwaitingActivation self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                self.radius = 30f;
            }
            orig(self);
        }

        private void ChangeActiveWardRadius(On.EntityStates.InfiniteTowerSafeWard.Active.orig_OnEnter orig, Active self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                self.radius = 75f;
            }
            orig(self);
        }

    }
}