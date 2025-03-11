using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Judgement
{
    public class SimulacrumHooks
    {
        public SimulacrumHooks()
        {
            On.RoR2.InfiniteTowerRun.SpawnSafeWard += ManageWaves;
            On.RoR2.InfiniteTowerRun.MoveSafeWard += PreventCrabMovement;
            On.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal += IncreaseScaling;
            On.RoR2.InfiniteTowerWaveController.DropRewards += PreventCrabDrops;
            On.RoR2.InfiniteTowerWaveController.OnEnable += ManageWaveCredits;
            On.RoR2.InfiniteTowerBossWaveController.PreStartClient += GuaranteeBoss;
        }

        private void ManageWaveCredits(On.RoR2.InfiniteTowerWaveController.orig_OnEnable orig, InfiniteTowerWaveController self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                if (self is InfiniteTowerBossWaveController)
                    self.baseCredits = 400;
                else
                    self.baseCredits = 125;
                // 159 500
            }
            orig(self);
        }

        private void PreventCrabMovement(On.RoR2.InfiniteTowerRun.orig_MoveSafeWard orig, InfiniteTowerRun self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
                return;
            orig(self);
        }

        private void PreventCrabDrops(On.RoR2.InfiniteTowerWaveController.orig_DropRewards orig, InfiniteTowerWaveController self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
                return;
            orig(self);
        }

        private void GuaranteeBoss(On.RoR2.InfiniteTowerBossWaveController.orig_PreStartClient orig, InfiniteTowerBossWaveController self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
                self.guaranteeInitialChampion = true;
            orig(self);
        }

        private void IncreaseScaling(On.RoR2.InfiniteTowerRun.orig_RecalculateDifficultyCoefficentInternal orig, InfiniteTowerRun self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(self.selectedDifficulty);
                float num1 = 1.5f * self.waveIndex;
                float num2 = 0.0506f * (difficultyDef.scalingValue * 2f);
                float num3 = Mathf.Pow(1.02f, self.waveIndex);
                self.difficultyCoefficient = (float)(1.0 + (double)num2 * (double)num1) * num3;
                self.compensatedDifficultyCoefficient = self.difficultyCoefficient;
                self.ambientLevel = Mathf.Min((float)((self.difficultyCoefficient - 1.0) / 0.33000001311302185 + 1.0), 9999f);
                int ambientLevelFloor = self.ambientLevelFloor;
                self.ambientLevelFloor = Mathf.FloorToInt(self.ambientLevel);
                if (ambientLevelFloor == self.ambientLevelFloor || ambientLevelFloor == 0 || self.ambientLevelFloor <= ambientLevelFloor)
                    return;
                self.OnAmbientLevelUp();
            }
            else
                orig(self);
        }

        private void ManageWaves(On.RoR2.InfiniteTowerRun.orig_SpawnSafeWard orig, InfiniteTowerRun self, InteractableSpawnCard spawnCard, DirectorPlacementRule placementRule)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                if (SceneManager.GetActiveScene().name == "bazaar")
                {
                    if (self.fogDamageController && NetworkServer.active)
                    {
                        Debug.LogWarning("Destroying Fog Damage");
                        GameObject.Destroy(self.fogDamageController.gameObject);
                    }
                    return;
                }
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
                judgementRun.shouldGoBazaar = true;
                judgementRun.isFirstStage = false;
                if (judgementRun.currentWave == 10)
                {
                    GameObject.Destroy(self.fogDamageController.gameObject);
                    GameObject director = GameObject.Find("Director");
                    if (director && NetworkServer.active)
                    {
                        Debug.LogWarning("Deleting Combat Director");
                        foreach (CombatDirector cd in director.GetComponents<CombatDirector>())
                            GameObject.Destroy(cd);
                    }
                    return;
                }
                judgementRun.currentWave += 2;
            }
            orig(self, spawnCard, placementRule);
        }
    }
}