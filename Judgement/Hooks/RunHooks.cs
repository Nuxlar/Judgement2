using RoR2;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using EntityStates.Missions.BrotherEncounter;
using MonoMod.Cil;
using RoR2.Artifacts;
using RoR2.ContentManagement;

namespace Judgement
{
    public class RunHooks
    {
        private BasicPickupDropTable dtEquip;
        private BasicPickupDropTable dtWhite;
        private BasicPickupDropTable dtGreen;
        private BasicPickupDropTable dtRed;
        private BasicPickupDropTable dtYellow;

        private GameObject potentialPickup;

        private SceneDef voidPlains;
        private SceneDef voidAqueduct;
        private SceneDef voidAphelian;
        private SceneDef voidRPD;
        private SceneDef voidAbyssal;
        private SceneDef voidMeadow;

        private GameEndingDef judgementRunEnding;

        private GameObject tpOutController;
        // private GameObject voidSkybox = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/voidraid/Weather, Void Raid.prefab").WaitForCompletion();

        public RunHooks()
        {
            // voidSkybox.AddComponent<NetworkIdentity>();
            LoadAssets();

            IL.RoR2.SceneDirector.PopulateScene += RemoveExtraLoot;
            // On.RoR2.SceneDirector.Start += AddVoidSkyToMoon;
            // On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 += TrackPotentials;
            On.RoR2.PurchaseInteraction.OnInteractionBegin += PreventPrinterCheese;
            On.RoR2.Run.PickNextStageScene += SetFirstStage;
            On.RoR2.CharacterMaster.OnBodyStart += CreateDropletsOnStart;
            On.RoR2.MusicController.PickCurrentTrack += SetJudgementMusic;
            On.EntityStates.Missions.BrotherEncounter.BossDeath.OnEnter += EndRun;
            On.RoR2.CharacterBody.Start += ManageSurvivorStats;
            On.RoR2.SceneExitController.Begin += ManageStageSelection;
        }

        private void TrackPotentials(
            On.RoR2.PickupDropletController.orig_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 orig,
            GenericPickupController.CreatePickupInfo pickupInfo,
            Vector3 position,
            Vector3 velocity
            )
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();
                if (judgementRun.safeWardController)
                {
                    CheckWavePickups pickupChecker = judgementRun.safeWardController.gameObject.GetComponent<CheckWavePickups>();
                    Debug.LogWarning($"Pickup Checker? {pickupChecker}");
                    if (pickupChecker)
                    {
                        if (CommandArtifactManager.IsCommandArtifactEnabled)
                            pickupInfo.artifactFlag |= GenericPickupController.PickupArtifactFlag.COMMAND;

                        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(PickupDropletController.pickupDropletPrefab, position, Quaternion.identity);
                        PickupDropletController component1 = gameObject.GetComponent<PickupDropletController>();
                        if ((bool)component1)
                        {
                            component1.createPickupInfo = pickupInfo;
                            component1.NetworkpickupIndex = pickupInfo.pickupIndex;
                        }
                        Rigidbody component2 = gameObject.GetComponent<Rigidbody>();
                        component2.velocity = velocity;
                        component2.AddTorque(UnityEngine.Random.Range(150f, 120f) * UnityEngine.Random.onUnitSphere);
                        pickupChecker.AddPickup(gameObject);
                        Debug.LogWarning("Added Pickup");
                        NetworkServer.Spawn(gameObject);
                    }
                    else orig(pickupInfo, position, velocity);
                }
                else orig(pickupInfo, position, velocity);
            }
            else orig(pickupInfo, position, velocity);
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

                    if (Run.instance.selectedDifficulty < DifficultyIndex.Eclipse8)
                    {
                        for (int i = 0; i < 20; i++)
                            body.AddBuff(RoR2Content.Buffs.PermanentCurse);
                    }

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
                    /* Moon Testing
                    SceneDef sceneDef = SceneCatalog.FindSceneDef("moon2");
                    self.nextStageScene = sceneDef;
                    */
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
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                string sceneName = SceneManager.GetActiveScene().name;
                JudgementRun judgementRun = Run.instance.gameObject.GetComponent<JudgementRun>();

                if (Run.instance.selectedDifficulty > DifficultyIndex.Eclipse1 && body.teamComponent && body.teamComponent.teamIndex == TeamIndex.Player && (!body.HasBuff(RoR2Content.Buffs.Immune) || judgementRun.waveIndex == 0))
                {
                    HealthComponent healthComponent = body.healthComponent;
                    if ((bool)healthComponent)
                        healthComponent.Networkhealth = healthComponent.fullHealth;
                }
                if (pcmc)
                {
                    if (sceneName == "moon2" && !body.HasBuff(RoR2Content.Buffs.Immune))
                    {
                        Vector3 center = new Vector3(127, 500, 101);
                        float maxRadius = 5f; // Set your desired radius here

                        float randomAngle = UnityEngine.Random.Range(0f, 360f);
                        float randomDistance = maxRadius * Mathf.Sqrt(UnityEngine.Random.Range(0f, 1f));

                        Vector3 randomPos = center + new Vector3(
                            randomDistance * Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                            0f, // Keeps the same Y coordinate
                            randomDistance * Mathf.Sin(randomAngle * Mathf.Deg2Rad)
                        );

                        TeleportHelper.TeleportBody(body, randomPos, false);
                    }

                    if (NetworkServer.active)
                    {

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
                if (!SceneExitController.isRunning)
                    self.BeginStagePreload();
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
                // self.baseRegen = 0f;
                //self.levelRegen = 0f;
                string sceneName = SceneManager.GetActiveScene().name;

                if (sceneName == "moon2" && self.isPlayerControlled)
                {
                    GameObject gameObject1 = GameObject.Find("HOLDER: Final Arena");
                    if ((bool)gameObject1)
                    {
                        if (!gameObject1.transform.GetChild(3).gameObject.activeSelf)
                        {
                            Log.Info("Judgement: Mithrix mod found, increasing player base speed by 25%");
                            self.baseMoveSpeed *= 1.25f;
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
        /*
        private void AddVoidSkyToMoon(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            orig(self);
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                string sceneName = SceneManager.GetActiveScene().name;
                if (sceneName == "moon2")
                {
                    GameObject moonAmb = GameObject.Find("PP + Amb");
                    if (moonAmb)
                    {
                        moonAmb.SetActive(false);
                    }
                    
                    GameObject gameObject = UnityEngine.Object.Instantiate(voidSkybox, Vector3.zero, Quaternion.identity);
                    if (NetworkServer.active)
                        NetworkServer.Spawn(gameObject);

                    GameObject moonSkybox = GameObject.Find("HOLDER: Skybox");
                    if (moonSkybox)
                        moonSkybox.SetActive(false);

                        
                }
            }
        }
*/
        private void LoadAssets()
        {
            AssetReferenceT<BasicPickupDropTable> dtEquipRef = new AssetReferenceT<BasicPickupDropTable>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common.dtEquipment_asset);
            AssetReferenceT<BasicPickupDropTable> dtWhiteRef = new AssetReferenceT<BasicPickupDropTable>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common.dtTier1Item_asset);
            AssetReferenceT<BasicPickupDropTable> dtGreenRef = new AssetReferenceT<BasicPickupDropTable>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common.dtTier2Item_asset);
            AssetReferenceT<BasicPickupDropTable> dtRedRef = new AssetReferenceT<BasicPickupDropTable>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common.dtTier3Item_asset);
            AssetReferenceT<BasicPickupDropTable> dtYellowRef = new AssetReferenceT<BasicPickupDropTable>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_DuplicatorWild.dtDuplicatorWild_asset);
            AssetReferenceT<GameObject> potentialPickupRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_OptionPickup.OptionPickup_prefab);
            AssetReferenceT<GameObject> tpOutRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common.TeleportOutController_prefab);
            AssetReferenceT<SceneDef> voidPlainsRef = new AssetReferenceT<SceneDef>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_itgolemplains.itgolemplains_asset);
            AssetReferenceT<SceneDef> voidAqueductRef = new AssetReferenceT<SceneDef>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_itgoolake.itgoolake_asset);
            AssetReferenceT<SceneDef> voidAphelianRef = new AssetReferenceT<SceneDef>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_itancientloft.itancientloft_asset);
            AssetReferenceT<SceneDef> voidRPDRef = new AssetReferenceT<SceneDef>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_itfrozenwall.itfrozenwall_asset);
            AssetReferenceT<SceneDef> voidAbyssalRef = new AssetReferenceT<SceneDef>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_itdampcave.itdampcave_asset);
            AssetReferenceT<SceneDef> voidMeadowRef = new AssetReferenceT<SceneDef>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_itskymeadow.itskymeadow_asset);
            AssetReferenceT<GameEndingDef> endingRef = new AssetReferenceT<GameEndingDef>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_WeeklyRun.PrismaticTrialEnding_asset);

            AssetAsyncReferenceManager<BasicPickupDropTable>.LoadAsset(dtEquipRef).Completed += (x) => dtEquip = x.Result;
            AssetAsyncReferenceManager<BasicPickupDropTable>.LoadAsset(dtWhiteRef).Completed += (x) => dtWhite = x.Result;
            AssetAsyncReferenceManager<BasicPickupDropTable>.LoadAsset(dtGreenRef).Completed += (x) => dtGreen = x.Result;
            AssetAsyncReferenceManager<BasicPickupDropTable>.LoadAsset(dtRedRef).Completed += (x) => dtRed = x.Result;
            AssetAsyncReferenceManager<BasicPickupDropTable>.LoadAsset(dtYellowRef).Completed += (x) => dtYellow = x.Result;
            AssetAsyncReferenceManager<GameObject>.LoadAsset(potentialPickupRef).Completed += (x) => potentialPickup = x.Result;
            AssetAsyncReferenceManager<GameObject>.LoadAsset(tpOutRef).Completed += (x) => tpOutController = x.Result;
            AssetAsyncReferenceManager<SceneDef>.LoadAsset(voidPlainsRef).Completed += (x) => voidPlains = x.Result;
            AssetAsyncReferenceManager<SceneDef>.LoadAsset(voidAqueductRef).Completed += (x) => voidAqueduct = x.Result;
            AssetAsyncReferenceManager<SceneDef>.LoadAsset(voidAphelianRef).Completed += (x) => voidAphelian = x.Result;
            AssetAsyncReferenceManager<SceneDef>.LoadAsset(voidRPDRef).Completed += (x) => voidRPD = x.Result;
            AssetAsyncReferenceManager<SceneDef>.LoadAsset(voidAbyssalRef).Completed += (x) => voidAbyssal = x.Result;
            AssetAsyncReferenceManager<SceneDef>.LoadAsset(voidMeadowRef).Completed += (x) => voidMeadow = x.Result;
            AssetAsyncReferenceManager<GameEndingDef>.LoadAsset(endingRef).Completed += (x) => judgementRunEnding = x.Result;
        }
    }
}