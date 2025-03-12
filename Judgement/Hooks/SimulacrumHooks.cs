using System;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace Judgement
{
    public class SimulacrumHooks
    {
        private SpawnCard lockBox = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Junk/TreasureCache/iscLockbox.asset").WaitForCompletion();
        private SpawnCard lockBoxVoid = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/TreasureCacheVoid/iscLockboxVoid.asset").WaitForCompletion();
        private SpawnCard freeChest = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/FreeChest/iscFreeChest.asset").WaitForCompletion();
        private SpawnCard greenPrinter = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset").WaitForCompletion();
        private SpawnCard voidChest = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/VoidChest/iscVoidChest.asset").WaitForCompletion();

        public SimulacrumHooks()
        {
            On.RoR2.InfiniteTowerRun.SpawnSafeWard += SetupInteractables;
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
                    self.baseCredits = 140;
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
                float num1 = 1.5f * (self.waveIndex * 0.85f); // make scaling less harsh as the waves increase
                float num2 = 0.0506f * (difficultyDef.scalingValue * 2f); // increase scaling since the run is shorter
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

        private void SetupInteractables(On.RoR2.InfiniteTowerRun.orig_SpawnSafeWard orig, InfiniteTowerRun self, InteractableSpawnCard spawnCard, DirectorPlacementRule placementRule)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
                if (judgementRun.waveIndex == 10)
                {
                    GameObject director = GameObject.Find("Director");
                    if (director && NetworkServer.active)
                    {
                        GameObject.Destroy(self.fogDamageController.gameObject);
                        foreach (CombatDirector cd in director.GetComponents<CombatDirector>())
                            GameObject.Destroy(cd);
                    }
                    return;
                }

                GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest((SpawnCard)spawnCard, placementRule, self.safeWardRng));
                if ((bool)gameObject)
                {
                    NetworkServer.Spawn(gameObject);
                    self.safeWardController = gameObject.GetComponent<InfiniteTowerSafeWardController>();
                    if ((bool)self.safeWardController)
                        self.safeWardController.onActivated += new Action<InfiniteTowerSafeWardController>(self.OnSafeWardActivated);
                    HoldoutZoneController component = gameObject.GetComponent<HoldoutZoneController>();
                    if ((bool)component)
                        component.calcAccumulatedCharge += new HoldoutZoneController.CalcAccumulatedChargeDelegate(self.CalcHoldoutZoneCharge);
                    if (!(bool)self.fogDamageController)
                        return;
                    self.fogDamageController.AddSafeZone(self.safeWardController.safeZone);

                    if (NetworkServer.active)
                    {
                        Vector3 position = self.safeWardController.transform.position;

                        if (judgementRun.waveIndex == 4 || judgementRun.waveIndex == 8)
                        {
                            for (int i = 0; i < Run.instance.participatingPlayerCount; i++)
                            {
                                SpawnInteractable(voidChest, position, false);
                            }
                        }

                        foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
                        {
                            if (readOnlyInstances.inventory.GetItemCount(DLC1Content.Items.RegeneratingScrap) > 0)
                            {
                                SpawnInteractable(greenPrinter, position, false);
                                break;
                            }
                        }

                        for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
                        {
                            CharacterMaster master = CharacterMaster.readOnlyInstancesList[i];
                            if (master.inventory.GetItemCount(RoR2Content.Items.TreasureCache) > 0)
                            {
                                SpawnInteractable(lockBox, position, false);
                            }
                        }

                        for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
                        {
                            CharacterMaster master = CharacterMaster.readOnlyInstancesList[i];
                            if (master.inventory.GetItemCount(DLC1Content.Items.TreasureCacheVoid) > 0)
                            {
                                SpawnInteractable(lockBoxVoid, position, false);
                            }
                        }

                        for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
                        {
                            CharacterMaster master = CharacterMaster.readOnlyInstancesList[i];
                            if (master.inventory.GetItemCount(DLC1Content.Items.FreeChest) > 0)
                            {
                                SpawnInteractable(freeChest, position, false);
                            }
                        }
                    }
                }
            }
            else orig(self, spawnCard, placementRule);
        }

        private void SpawnInteractable(SpawnCard spawnCard, Vector3 position, bool isFree = true)
        {
            DirectorCore instance = DirectorCore.instance;
            DirectorPlacementRule placementRule = new DirectorPlacementRule();
            placementRule.placementMode = DirectorPlacementRule.PlacementMode.Approximate;
            placementRule.position = position;
            placementRule.minDistance = 2;
            placementRule.maxDistance = 20;
            DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, Run.instance.runRNG);
            GameObject interactable = instance.TrySpawnObject(directorSpawnRequest);

            if (!interactable)
            {
                Log.Error($"Judgement: Failed to spawn pre-wave interactable {spawnCard.name}.");
            }

            if (isFree)
                SetPurchaseCostFree(interactable);
        }

        private void SetPurchaseCostFree(GameObject chest)
        {
            PurchaseInteraction purchaseInteraction = chest.GetComponent<PurchaseInteraction>();
            if (purchaseInteraction)
            {
                purchaseInteraction.cost = 0;
                purchaseInteraction.Networkcost = 0;
            }
            else
                Log.Error($"Judgement: Failed to set {chest.name} cost to free.");
        }
    }
}