using RoR2;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using EntityStates.Missions.BrotherEncounter;
using MonoMod.Cil;

namespace Judgement
{
    public class RunHooks
    {

        private BasicPickupDropTable dtEquip = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtEquipment.asset").WaitForCompletion();
        private BasicPickupDropTable dtWhite = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier1Item.asset").WaitForCompletion();
        private BasicPickupDropTable dtGreen = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier2Item.asset").WaitForCompletion();
        private BasicPickupDropTable dtRed = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier3Item.asset").WaitForCompletion();
        private BasicPickupDropTable dtYellow = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/DuplicatorWild/dtDuplicatorWild.asset").WaitForCompletion();

        private GameObject potentialPickup = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();

        private SceneDef voidPlains = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itgolemplains/itgolemplains.asset").WaitForCompletion();
        private SceneDef voidAqueduct = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itgoolake/itgoolake.asset").WaitForCompletion();
        private SceneDef voidAphelian = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itancientloft/itancientloft.asset").WaitForCompletion();
        private SceneDef voidRPD = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itfrozenwall/itfrozenwall.asset").WaitForCompletion();
        private SceneDef voidAbyssal = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itdampcave/itdampcave.asset").WaitForCompletion();
        private SceneDef voidMeadow = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itskymeadow/itskymeadow.asset").WaitForCompletion();

        private GameEndingDef judgementRunEnding = Addressables.LoadAssetAsync<GameEndingDef>("RoR2/Base/WeeklyRun/PrismaticTrialEnding.asset").WaitForCompletion();

        private GameObject tpOutController = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/TeleportOutController.prefab").WaitForCompletion();

        public RunHooks()
        {
            IL.RoR2.SceneDirector.PopulateScene += RemoveExtraLoot;
            On.RoR2.PurchaseInteraction.OnInteractionBegin += PreventPrinterCheese;
            On.RoR2.Run.PickNextStageScene += SetFirstStage;
            On.RoR2.CharacterMaster.OnBodyStart += CreateDropletsOnStart;
            On.RoR2.MusicController.PickCurrentTrack += SetJudgementMusic;
            On.EntityStates.Missions.BrotherEncounter.BossDeath.OnEnter += EndRun;
            On.RoR2.CharacterBody.Start += ManageSurvivorStats;
            On.RoR2.SceneExitController.Begin += ManageStageSelection;
        }

        private void PreventPrinterCheese(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
            if (judgementRun)
            {
                if (self.name == "VoidChest(Clone)")
                {
                    CharacterBody body = activator.GetComponent<CharacterBody>();
                    if (judgementRun.persistentCurse.TryGetValue(body.master.netId, out int _))
                        judgementRun.persistentCurse[body.master.netId] += 20;
                    else
                        judgementRun.persistentCurse.Add(body.master.netId, 20);
                    for (int i = 0; i < 20; i++)
                        body.AddBuff(RoR2Content.Buffs.PermanentCurse);
                    orig(self, activator);
                }
                else if (self.name == "DuplicatorLarge(Clone)")
                {
                    int count = activator.GetComponent<CharacterBody>().inventory.GetItemCount(DLC1Content.Items.RegeneratingScrap);
                    if (count == 0)
                        return;
                    orig(self, activator);
                }
                else
                    orig(self, activator);
            }
            else
                orig(self, activator);
        }

        private void SetFirstStage(On.RoR2.Run.orig_PickNextStageScene orig, Run self, WeightedSelection<SceneDef> choices)
        {
            if (Run.instance.stageClearCount == 0 && self.name.Contains("Judgement"))
            {
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
                if (judgementRun.waveIndex == 0)
                {
                    SceneDef sceneDef = SceneCatalog.FindSceneDef("itgolemplains");
                    self.nextStageScene = sceneDef;
                }
            }
            else
                orig(self, choices);
        }

        public void SavePersistentHP()
        {
            JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
            foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
            {
                if (judgementRun.persistentHP.TryGetValue(instance.master.netId, out float hp))
                    judgementRun.persistentHP[instance.master.netId] = instance.master.GetBody().healthComponent.health;
                else
                    judgementRun.persistentHP.Add(instance.master.netId, instance.master.GetBody().healthComponent.health);

            }
        }

        public void LoadPersistentHP(CharacterBody body)
        {
            JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
            if (body.master && body.healthComponent && judgementRun.persistentHP.TryGetValue(body.master.netId, out float hp))
                body.healthComponent.health = hp;
        }

        public void LoadPersistentCurse(CharacterBody body)
        {
            JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
            if (body.master && judgementRun.persistentCurse.TryGetValue(body.master.netId, out int curseStacks))
            {
                for (int i = 0; i < curseStacks; i++)
                    body.AddBuff(RoR2Content.Buffs.PermanentCurse);
            }
        }

        private void RemoveExtraLoot(ILContext il)
        {
            ILCursor ilCursor = new ILCursor(il);

            static int ItemFunction(int itemCount)
            {
                if (Run.instance && Run.instance.name.Contains("Judgement"))
                    return 0;
                return itemCount;
            }

            if (ilCursor.TryGotoNext(0, x => ILPatternMatchingExt.MatchLdsfld(x, typeof(RoR2Content.Items), "TreasureCache")))
            {
                ilCursor.Index += 2;
                ilCursor.EmitDelegate<Func<int, int>>((count) => ItemFunction(count));
            }
            else
                Log.Error("Judgement: TreasureCache IL hook failed");

            ilCursor.Index = 0;

            if (ilCursor.TryGotoNext(0, x => ILPatternMatchingExt.MatchLdsfld(x, typeof(DLC1Content.Items), "TreasureCacheVoid")))
            {
                ilCursor.Index += 2;
                ilCursor.EmitDelegate<Func<int, int>>((count) => ItemFunction(count));
            }
            else
                Log.Error("Judgement: TreasureCacheVoid IL hook failed");

            ilCursor.Index = 0;

            if (ilCursor.TryGotoNext(0, x => ILPatternMatchingExt.MatchLdsfld(x, typeof(DLC1Content.Items), "FreeChest")))
            {
                ilCursor.Index += 2;
                ilCursor.EmitDelegate<Func<int, int>>((count) => ItemFunction(count));
            }
            else
                Log.Error("Judgement: FreeChest IL hook failed");
        }

        private void CreateDropletsOnStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
            orig(self, body);
            PlayerCharacterMasterController pcmc = self.GetComponent<PlayerCharacterMasterController>();
            if (Run.instance && Run.instance.name.Contains("Judgement") && pcmc && NetworkServer.active)
            {
                string sceneName = SceneManager.GetActiveScene().name;
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
                if (sceneName == "moon2" && !body.HasBuff(RoR2Content.Buffs.Immune))
                    TeleportHelper.TeleportBody(body, new Vector3(127, 500, 101), false);

                if (sceneName != "moon2" && !body.HasBuff(RoR2Content.Buffs.Immune))
                {
                    double angle = 360.0 / 5;
                    Vector3 velocity = Vector3.up * 10 + Vector3.forward * 2;
                    Vector3 up = Vector3.up;
                    Quaternion quaternion = Quaternion.AngleAxis((float)angle, up);

                    CreateDrop(PickupCatalog.FindPickupIndex(ItemTier.Tier1), dtWhite, Run.instance.runRNG);
                    CreateDrop(PickupCatalog.FindPickupIndex(ItemTier.Tier1), dtWhite, Run.instance.runRNG);
                    CreateDrop(PickupCatalog.FindPickupIndex(ItemTier.Tier1), dtWhite, Run.instance.runRNG);

                    switch (judgementRun.waveIndex)
                    {
                        case 0:
                            CreateDrop(PickupCatalog.FindPickupIndex(ItemTier.Tier2), dtGreen, Run.instance.runRNG);
                            CreateDrop(PickupCatalog.FindPickupIndex(EquipmentCatalog.FindEquipmentIndex("LifestealOnHit")), dtEquip, Run.instance.runRNG);
                            break;
                        case 4:
                            CreateDrop(PickupCatalog.FindPickupIndex(ItemTier.Tier3), dtRed, Run.instance.runRNG);
                            CreateDrop(PickupCatalog.FindPickupIndex(EquipmentCatalog.FindEquipmentIndex("LifestealOnHit")), dtEquip, Run.instance.runRNG);
                            break;
                        case 6:
                            CreateDrop(PickupCatalog.FindPickupIndex(ItemTier.Tier2), dtGreen, Run.instance.runRNG);
                            CreateDrop(PickupCatalog.FindPickupIndex(ItemTier.Boss), dtYellow, Run.instance.runRNG);
                            break;
                        case 8:
                            CreateDrop(PickupCatalog.FindPickupIndex(ItemTier.Tier3), dtRed, Run.instance.runRNG);
                            CreateDrop(PickupCatalog.FindPickupIndex(ItemTier.Tier2), dtGreen, Run.instance.runRNG);
                            break;
                        default:
                            CreateDrop(PickupCatalog.FindPickupIndex(ItemTier.Tier2), dtGreen, Run.instance.runRNG);
                            CreateDrop(PickupCatalog.FindPickupIndex(ItemTier.Tier2), dtGreen, Run.instance.runRNG);
                            break;
                    }

                    void CreateDrop(PickupIndex pickupIndex, BasicPickupDropTable dropTable, Xoroshiro128Plus rng)
                    {
                        GenericPickupController.CreatePickupInfo pickupInfo = new GenericPickupController.CreatePickupInfo()
                        {
                            pickupIndex = pickupIndex,
                            position = body.corePosition + Vector3.up * 1.5f,
                            pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dropTable, rng),
                            prefabOverride = potentialPickup,
                        };
                        PickupDropletController.CreatePickupDroplet(pickupInfo, pickupInfo.position, velocity);
                        velocity = quaternion * velocity;
                    }
                }
            }
        }

        private static void SetJudgementMusic(On.RoR2.MusicController.orig_PickCurrentTrack orig, MusicController self, ref MusicTrackDef newTrack)
        {
            orig(self, ref newTrack);
            if (Run.instance && Run.instance.name.Contains("Judgement") && SceneManager.GetActiveScene().name != "moon2")
                newTrack = MusicTrackCatalog.FindMusicTrackDef("muSong23");
        }

        private void ManageStageSelection(On.RoR2.SceneExitController.orig_Begin orig, SceneExitController self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();

                if (NetworkServer.active)
                    SavePersistentHP();

                if (judgementRun.waveIndex == 0)
                    Run.instance.nextStageScene = voidPlains;
                if (judgementRun.waveIndex == 2)
                {
                    int[] array = Array.Empty<int>();
                    WeightedSelection<SceneDef> weightedSelection = new WeightedSelection<SceneDef>();
                    weightedSelection.AddChoice(voidAqueduct, 1f);
                    weightedSelection.AddChoice(voidAphelian, 1f);
                    int toChoiceIndex = weightedSelection.EvaluateToChoiceIndex(Run.instance.runRNG.nextNormalizedFloat, array);
                    WeightedSelection<SceneDef>.ChoiceInfo choice = weightedSelection.GetChoice(toChoiceIndex);
                    Run.instance.nextStageScene = choice.value;
                }
                if (judgementRun.waveIndex == 4)
                    Run.instance.nextStageScene = voidRPD;
                if (judgementRun.waveIndex == 6)
                    Run.instance.nextStageScene = voidAbyssal;
                if (judgementRun.waveIndex == 8)
                    Run.instance.nextStageScene = voidMeadow;
                if (judgementRun.waveIndex == 10)
                {
                    SceneDef sceneDef = SceneCatalog.FindSceneDef("moon2");
                    Run.instance.nextStageScene = sceneDef;
                }
                ReadOnlyCollection<CharacterMaster> onlyInstancesList = CharacterMaster.readOnlyInstancesList;
                for (int index = 0; index < onlyInstancesList.Count; ++index)
                {
                    CharacterMaster component = onlyInstancesList[index].GetComponent<CharacterMaster>();
                    if ((bool)component.GetComponent<SetDontDestroyOnLoad>())
                    {
                        GameObject bodyObject = component.GetBodyObject();
                        if ((bool)bodyObject)
                        {
                            GameObject gameObject = UnityEngine.Object.Instantiate(tpOutController, bodyObject.transform.position, Quaternion.identity);
                            gameObject.GetComponent<TeleportOutController>().Networktarget = bodyObject;
                            NetworkServer.Spawn(gameObject);
                        }
                    }
                }
                self.SetState(SceneExitController.ExitState.Finished);
            }
            else
                orig(self);
        }

        private void ManageSurvivorStats(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (Run.instance && Run.instance.name.Contains("Judgement") && self.isPlayerControlled && !self.HasBuff(RoR2Content.Buffs.Immune))
            {
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();

                if (NetworkServer.active)
                {
                    LoadPersistentHP(self);
                    LoadPersistentCurse(self);
                }

                if (Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse1 && judgementRun.waveIndex == 0)
                    self.healthComponent.health = self.healthComponent.fullHealth;

                // self.baseDamage *= 1.25f;
                self.baseRegen = 0f;
                self.levelRegen = 0f;
                string sceneName = SceneManager.GetActiveScene().name;

                if (sceneName == "moon2" && self.isPlayerControlled)
                {
                    GameObject gameObject1 = GameObject.Find("HOLDER: Final Arena");
                    if ((bool)gameObject1)
                    {
                        if (!gameObject1.transform.GetChild(3).gameObject.activeSelf)
                        {
                            Log.Info("Judgement: Mithrix mod found, increasing player base speed by 10%");
                            self.baseMoveSpeed *= 1.1f;
                        }
                    }
                }
            }
        }

        private void EndRun(On.EntityStates.Missions.BrotherEncounter.BossDeath.orig_OnEnter orig, BossDeath self)
        {
            orig(self);
            if (Run.instance && Run.instance.name.Contains("Judgement"))
                Run.instance.BeginGameOver(judgementRunEnding);
        }

    }
}