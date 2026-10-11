using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Zephyr.Architecture.Tests
{
    public sealed class RuntimeEditorDependencyTests
    {
        [Test]
        public void RuntimeScriptsDoNotImportUnityEditorOutsideEditorFolders()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string[] files = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/Editor/")) continue;
                if (normalized.Contains("/Tests/")) continue;

                string source = File.ReadAllText(file);
                bool usesEditorApi = source.Contains("using UnityEditor") || source.Contains("UnityEditor.");
                if (!usesEditorApi) continue;

                Assert.That(source.Contains("#if UNITY_EDITOR") && source.Contains("#endif"), Is.True,
                    "Runtime script uses UnityEditor without an explicit UNITY_EDITOR guard: " + normalized);
            }
        }

        [Test]
        public void PhaseTwoDomainAssembliesExistWithExpectedDirection()
        {
            AssertAssemblyReferences("Zephyr.Weapons", "Zephyr.Core");
            AssertAssemblyReferences("Zephyr.Projectiles", "Zephyr.Core");
            AssertAssemblyReferences("Zephyr.Items", "Zephyr.Core");
            AssertAssemblyReferences("Zephyr.Enemies", "Zephyr.Core", "Zephyr.Core.StateMachine");
            AssertAssemblyReferences("Zephyr.Rooms", "Zephyr.Core");
        }

        [Test]
        public void DomainMigrationBridgesAreRemoved()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string enemiesAsmdef = File.ReadAllText(Path.Combine(scriptsRoot, "Gameplay", "Enemies", "Zephyr.Enemies.asmdef"));
            string itemsAsmdef = File.ReadAllText(Path.Combine(scriptsRoot, "Core", "Items", "Zephyr.Items.asmdef"));

            StringAssert.DoesNotContain("\"Zephyr.Items\"", enemiesAsmdef);
            StringAssert.DoesNotContain("\"Zephyr.Weapons\"", enemiesAsmdef);
            StringAssert.DoesNotContain("\"Zephyr.Weapons\"", itemsAsmdef);

            string dropSystem = File.ReadAllText(Path.Combine(scriptsRoot, "Core", "Runtime", "Items", "DropSystem.cs"));
            string bossReward = File.ReadAllText(Path.Combine(scriptsRoot, "Gameplay", "Enemies", "BossRewardDrop.cs"));
            StringAssert.DoesNotContain("WeaponSO", dropSystem);
            StringAssert.DoesNotContain("Zephyr.Core.Weapons", dropSystem);
            StringAssert.DoesNotContain("WeaponSO", bossReward);
            StringAssert.Contains("IWeaponPickup", bossReward);
        }

        [Test]
        public void PhaseABContractsAssemblyIsExplicitAndDoesNotDependOnLegacyCore()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string contractsRoot = Path.Combine(scriptsRoot, "Core", "Contracts");
            string asmdefPath = Path.Combine(contractsRoot, "Zephyr.Core.Contracts.asmdef");

            Assert.That(File.Exists(asmdefPath), Is.True);
            string asmdef = File.ReadAllText(asmdefPath);
            StringAssert.Contains("\"references\": []", asmdef);
            StringAssert.Contains("\"autoReferenced\": false", asmdef);
            StringAssert.DoesNotContain("Zephyr.Core\"", asmdef);

            string[] contractFiles = Directory.GetFiles(contractsRoot, "*.cs", SearchOption.AllDirectories);
            Assert.That(contractFiles, Is.Not.Empty);
            foreach (string file in contractFiles)
            {
                string source = File.ReadAllText(file);
                StringAssert.DoesNotContain("SceneManager.", source, "Contracts must not load scenes: " + file);
                StringAssert.DoesNotContain("Resources.Load", source, "Contracts must not load resources: " + file);
                StringAssert.DoesNotContain("GameFlow.Instance", source, "Contracts must not resolve concrete flow services: " + file);
            }
        }

        [Test]
        public void PhaseABConsumersReferenceContractsDirectly()
        {
            AssertAssemblyReferences("Zephyr.Core", "Zephyr.Core.Contracts");
            AssertAssemblyReferences("Zephyr.Enemies", "Zephyr.Core.Contracts");
            AssertAssemblyReferences("Zephyr.Gameplay.Interaction", "Zephyr.Core.Contracts");
            AssertAssemblyReferences("Zephyr.Rooms", "Zephyr.Core.Contracts");
            AssertAssemblyReferences("Zephyr.Player", "Zephyr.Core.Contracts");
            AssertAssemblyReferences("Zephyr.Items", "Zephyr.Core.Contracts");
            AssertAssemblyReferences("Zephyr.Projectiles", "Zephyr.Core.Contracts");
            AssertAssemblyReferences("Zephyr.Weapons", "Zephyr.Core.Contracts");
            AssertAssemblyReferences("Zephyr.UI", "Zephyr.Core.Contracts");
            AssertAssemblyReferences("Zephyr.Testing", "Zephyr.Core.Contracts");
            AssertAssemblyReferences("Zephyr.Tests.EditMode", "Zephyr.Core.Contracts");
        }

        [Test]
        public void PhaseCRuntimeAssemblyHasOnlyContractsDependency()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string runtimeRoot = Path.Combine(scriptsRoot, "Core", "Runtime");
            string asmdefPath = Path.Combine(runtimeRoot, "Zephyr.Core.Runtime.asmdef");

            Assert.That(File.Exists(asmdefPath), Is.True);
            string asmdef = File.ReadAllText(asmdefPath);
            StringAssert.Contains("\"Zephyr.Core.Contracts\"", asmdef);
            StringAssert.Contains("\"autoReferenced\": false", asmdef);
            StringAssert.DoesNotContain("\"Zephyr.Core\"", asmdef);

            string[] runtimeFiles = Directory.GetFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories);
            Assert.That(runtimeFiles, Is.Not.Empty);
            foreach (string file in runtimeFiles)
            {
                string source = File.ReadAllText(file);
                StringAssert.DoesNotContain("SceneManager.", source, "Runtime must not load scenes: " + file);
                StringAssert.DoesNotContain("Resources.Load", source, "Runtime must not resolve assets by path: " + file);
                StringAssert.DoesNotContain("DontDestroyOnLoad", source, "Runtime services must use scene lifetime: " + file);
                StringAssert.DoesNotContain("Zephyr.Core.Flow", source, "Runtime must not depend on Flow implementation: " + file);
                StringAssert.DoesNotContain("Zephyr.Core.Save", source, "Runtime must not depend on Persistence implementation: " + file);
                StringAssert.DoesNotContain("Zephyr.Core.Combat", source, "Runtime must not depend on Combat implementation: " + file);
            }
        }

        [Test]
        public void PhaseCRuntimeConsumersReferenceRuntimeDirectly()
        {
            AssertAssemblyReferences("Zephyr.Core", "Zephyr.Core.Runtime");
            AssertAssemblyReferences("Zephyr.Enemies", "Zephyr.Core.Runtime");
            AssertAssemblyReferences("Zephyr.Gameplay.Interaction", "Zephyr.Core.Runtime");
            AssertAssemblyReferences("Zephyr.Rooms", "Zephyr.Core.Runtime");
            AssertAssemblyReferences("Zephyr.Player", "Zephyr.Core.Runtime");
            AssertAssemblyReferences("Zephyr.Items", "Zephyr.Core.Runtime");
            AssertAssemblyReferences("Zephyr.Projectiles", "Zephyr.Core.Runtime");
            AssertAssemblyReferences("Zephyr.Weapons", "Zephyr.Core.Runtime");
            AssertAssemblyReferences("Zephyr.UI", "Zephyr.Core.Runtime");
            AssertAssemblyReferences("Zephyr.Testing", "Zephyr.Core.Runtime");
            AssertAssemblyReferences("Zephyr.Tests.EditMode", "Zephyr.Core.Runtime");
        }

        [Test]
        public void LevelsAndInteractionDoNotReferenceConcretePlayerOrEnemyTypes()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string levelsRoot = Path.Combine(scriptsRoot, "Gameplay", "Levels");
            string interactionRoot = Path.Combine(scriptsRoot, "Gameplay", "Interaction");

            AssertSourceTreeDoesNotContain(levelsRoot, "Zephyr.Gameplay.Enemies");
            AssertSourceTreeDoesNotContain(interactionRoot, "Zephyr.Gameplay.Player");
        }

        [Test]
        public void SceneLoaderDoesNotReferenceGameFlowBusinessLogic()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Core", "Flow", "SceneLoader.cs");
            string source = File.ReadAllText(path);
            StringAssert.DoesNotContain("GameFlow.Instance", source);
            StringAssert.Contains("SceneLoaded", source);
        }

        [Test]
        public void PhaseThreeOrchestratorsAreSeparatedFromControllerFacades()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            Assert.That(File.Exists(Path.Combine(scriptsRoot, "Player", "Flow", "PotentialLossService.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(scriptsRoot, "Player", "Flow", "PlayerDownUseCase.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(scriptsRoot, "Player", "Flow", "DeathPresentationController.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(scriptsRoot, "Player", "Flow", "RespawnCoordinator.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(scriptsRoot, "Core", "Flow", "Scenes", "MainMenuUseCase.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(scriptsRoot, "Core", "Flow", "GameFlowTransitionPolicy.cs")), Is.True);

            string deathController = File.ReadAllText(Path.Combine(scriptsRoot, "Player", "Flow", "PlayerDeathController.cs"));
            StringAssert.Contains("PotentialLossService", deathController);
            StringAssert.Contains("PlayerDownUseCase", deathController);
            StringAssert.DoesNotContain("Mathf.Pow", deathController);

            string mainMenuController = File.ReadAllText(Path.Combine(scriptsRoot, "Core", "Flow", "Scenes", "MainMenuController.cs"));
            StringAssert.Contains("MainMenuUseCase", mainMenuController);
            StringAssert.DoesNotContain("save.TryCreateSlot", mainMenuController);
            StringAssert.DoesNotContain("save.TrySelectSlot", mainMenuController);
            StringAssert.DoesNotContain("SaveSystem.Instance?.DeleteSlot", mainMenuController);
            StringAssert.Contains("ISaveSlotGateway", File.ReadAllText(
                Path.Combine(scriptsRoot, "Core", "Flow", "Scenes", "MainMenuUseCase.cs")));
        }

        [Test]
        public void PhaseThreeUiAndSingletonBoundariesAreExplicit()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string scenesRoot = Path.Combine(scriptsRoot, "Core", "Flow", "Scenes");
            Assert.That(File.Exists(Path.Combine(scenesRoot, "MainMenuView.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(scenesRoot, "SettingsPresenter.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(scriptsRoot, "Core", "Localization", "ILocalizationGateway.cs")), Is.True);
            string controller = File.ReadAllText(Path.Combine(scenesRoot, "MainMenuController.cs"));
            StringAssert.Contains("MainMenuView", controller);
            StringAssert.Contains("SettingsPresenter", controller);
            StringAssert.DoesNotContain("LocalizationManager.Instance?.SetLocale", controller);
            string death = File.ReadAllText(Path.Combine(scriptsRoot, "Player", "Flow", "PlayerDeathController.cs"));
            StringAssert.Contains("ResolveDependencies", death);
            StringAssert.DoesNotContain("SaveSystem.Instance?.", death);
            StringAssert.DoesNotContain("GameFlow.Instance?.", death);
            StringAssert.DoesNotContain("PlayerSpawner.Instance !=", death);
            string respawn = File.ReadAllText(Path.Combine(scriptsRoot, "Player", "Flow", "RespawnCoordinator.cs"));
            StringAssert.Contains("IPlayerRespawnGateway", respawn);
            StringAssert.Contains("IGameFlowGateway", respawn);
            StringAssert.DoesNotContain("PlayerSpawner.Instance", respawn);
            StringAssert.DoesNotContain("GameFlow.Instance", respawn);
        }

        [Test]
        public void UiLocalizationConsumersUseTheSharedGatewayBoundary()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string[] roots =
            {
                Path.Combine(scriptsRoot, "UI"),
                Path.Combine(scriptsRoot, "Player", "UI"),
                Path.Combine(scriptsRoot, "Gameplay", "Interaction"),
                Path.Combine(scriptsRoot, "Core", "UI")
            };

            foreach (string root in roots)
            {
                foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    string source = File.ReadAllText(file);
                    StringAssert.DoesNotContain("LocalizationManager.Instance", source,
                        "UI consumers must bind through LocalizedBehaviour: " + file);
                }
            }

            string localizedBehaviour = File.ReadAllText(Path.Combine(
                scriptsRoot, "Core", "Localization", "LocalizedBehaviour.cs"));
            StringAssert.Contains("LocalizationManager.Instance", localizedBehaviour);
            StringAssert.Contains("GetOrFallback", localizedBehaviour);
            StringAssert.Contains("OnLocaleChanged", localizedBehaviour);
        }

        [Test]
        public void MainMenuViewConsumesPresentationDataInsteadOfLocalizationKeys()
        {
            string scenesRoot = Path.Combine(Application.dataPath, "Scripts", "Core", "Flow", "Scenes");
            string view = File.ReadAllText(Path.Combine(scenesRoot, "MainMenuView.cs"));
            string presentation = File.ReadAllText(Path.Combine(scenesRoot, "MainMenuPresentation.cs"));

            StringAssert.Contains("MainMenuPresentation", view);
            StringAssert.Contains("Refresh(MainMenuPresentation", view);
            StringAssert.DoesNotContain("RefreshText", view);
            StringAssert.DoesNotContain("menu.", view);
            StringAssert.Contains("MainMenuTextPresenter", presentation);
            StringAssert.Contains("SettingsPresenter", presentation);
        }

        [Test]
        public void LocalizationTablesHaveMatchingKeySets()
        {
            string localizationRoot = Path.Combine(Application.dataPath, "Resources", "Localization");
            HashSet<string> english = ReadLocalizationKeys(Path.Combine(localizationRoot, "strings.en_US.json"));
            HashSet<string> chinese = ReadLocalizationKeys(Path.Combine(localizationRoot, "strings.zh_CN.json"));

            Assert.That(english, Is.Not.Empty);
            Assert.That(chinese, Is.EqualTo(english));
        }

        private static HashSet<string> ReadLocalizationKeys(string path)
        {
            string source = File.ReadAllText(path);
            var keys = new HashSet<string>();
            foreach (Match match in Regex.Matches(source, "\"([^\"]+)\"\\s*:"))
                keys.Add(match.Groups[1].Value);
            return keys;
        }

        [Test]
        public void RuntimeScriptsDoNotUseGlobalPlayerTagLookup()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string[] files = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/Editor/") || normalized.Contains("/Tests/")) continue;

                string source = File.ReadAllText(file);
                StringAssert.DoesNotContain("FindGameObjectWithTag(\"Player\")", source,
                    "Runtime player lookup must use PlayerTargetChangedEvent: " + normalized);
                StringAssert.DoesNotContain("FindWithTag(\"Player\")", source,
                    "Runtime player lookup must use PlayerTargetChangedEvent: " + normalized);
            }
        }

        [Test]
        public void RuntimeScriptsDoNotUseLegacyGlobalGameObjectFind()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string[] files = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/Editor/") || normalized.Contains("/Tests/")) continue;

                string source = File.ReadAllText(file);
                StringAssert.DoesNotContain("GameObject.Find(", source,
                    "Runtime global GameObject.Find is forbidden: " + normalized);
                StringAssert.DoesNotContain("GameObject.FindWithTag(", source,
                    "Runtime global GameObject.FindWithTag is forbidden: " + normalized);
                StringAssert.DoesNotContain("GameObject.FindGameObjectWithTag(", source,
                    "Runtime global GameObject.FindGameObjectWithTag is forbidden: " + normalized);
            }
        }

        [Test]
        public void RuntimeResourcesLoadIsNotUsedForServiceDependencies()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string[] files = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/Editor/") || normalized.Contains("/Tests/")) continue;

                string source = File.ReadAllText(file);
                StringAssert.DoesNotContain("Resources.Load", source,
                    "Runtime resource lookup must be replaced by explicit dependencies: " + normalized);
            }
        }

        [Test]
        public void PlayerSpawnerResolvesGameFlowOnceAtCompositionBoundary()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string source = File.ReadAllText(Path.Combine(scriptsRoot, "Player", "Flow", "PlayerSpawner.cs"));
            StringAssert.Contains("IGameFlowGateway", source);
            StringAssert.Contains("_gameFlow = GameFlow.Instance", source);
            StringAssert.DoesNotContain("GameFlow.Instance.CurrentState", source);
        }

        [Test]
        public void PlayerTargetLifecycleContractIsPresent()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string eventPath = Path.Combine(scriptsRoot, "Core", "Contracts", "Events", "PlayerTargetChangedEvent.cs");
            string eventBusPath = Path.Combine(scriptsRoot, "Core", "Runtime", "Events", "EventBus.cs");
            string spawnerPath = Path.Combine(scriptsRoot, "Player", "Flow", "PlayerSpawner.cs");
            string hudPath = Path.Combine(scriptsRoot, "Player", "UI", "GameplayHudController.cs");

            Assert.That(File.Exists(eventPath), Is.True);
            StringAssert.Contains("PlayerTargetChangedEvent", File.ReadAllText(eventPath));
            StringAssert.Contains("PublishPlayerTargetChanged", File.ReadAllText(spawnerPath));
            string hudSource = File.ReadAllText(hudPath);
            StringAssert.Contains("Subscribe<PlayerTargetChangedEvent>", hudSource);
            StringAssert.DoesNotContain("PlayerSpawner.Instance", hudSource);
        }

        [Test]
        public void RuntimeSceneLoadingHasOnlyApprovedAuthorities()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string[] files = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/Editor/") || normalized.Contains("/Tests/")) continue;
                if (normalized.EndsWith("/SceneLoader.cs") || normalized.Contains("/Flow/Bootstrap/")) continue;

                string source = File.ReadAllText(file);
                StringAssert.DoesNotContain("SceneManager.LoadScene", source,
                    "Unexpected scene load authority: " + normalized);
                StringAssert.DoesNotContain("SceneManager.UnloadScene", source,
                    "Unexpected scene unload authority: " + normalized);
            }
        }

        [Test]
        public void MigratedManagedReferencesDoNotUseLegacyAssemblies()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string[] assetPaths =
            {
                "Assets/ScriptableObjects/WeaponConfigs/Excalibur.asset",
                "Assets/ScriptableObjects/WeaponConfigs/LightningBolt.asset",
                "Assets/ScriptableObjects/Enemies/BasicEnemy/BasicEnemy.asset"
            };

            foreach (string assetPath in assetPaths)
            {
                string source = File.ReadAllText(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
                StringAssert.DoesNotContain("asm: Zephyr.Core", source, "Legacy weapon assembly reference remains: " + assetPath);
                StringAssert.DoesNotContain("asm: Assembly-CSharp", source, "Legacy enemy assembly reference remains: " + assetPath);
            }
        }

        [Test]
        public void PhaseDCombatAssemblyHasOnlyStableDependencies()
        {
            AssertAssemblyReferences("Zephyr.Core.Combat", "Zephyr.Core.Contracts", "Zephyr.Core.Runtime");

            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string asmdef = File.ReadAllText(Path.Combine(scriptsRoot, "Core", "Combat", "Zephyr.Core.Combat.asmdef"));
            StringAssert.DoesNotContain("Zephyr.Core\"", asmdef);
            StringAssert.DoesNotContain("Zephyr.Player", asmdef);
            StringAssert.DoesNotContain("Zephyr.Enemies", asmdef);
            StringAssert.DoesNotContain("Zephyr.Weapons", asmdef);
            StringAssert.DoesNotContain("Zephyr.UI", asmdef);

            string combatRoot = Path.Combine(scriptsRoot, "Core", "Combat");
            foreach (string file in Directory.GetFiles(combatRoot, "*.cs", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(file);
                StringAssert.DoesNotContain("SceneManager.LoadScene", source, file);
                StringAssert.DoesNotContain("SceneManager.UnloadScene", source, file);
                StringAssert.DoesNotContain("Resources.Load", source, file);
                StringAssert.DoesNotContain("GameFlow.Instance", source, file);
            }
        }

        [Test]
        public void PhaseDStatModifierBridgeLivesOutsideCombatImplementation()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string registryPath = Path.Combine(scriptsRoot, "Core", "Runtime", "Stats", "StatModifierRegistry.cs");
            string controllerPath = Path.Combine(scriptsRoot, "Core", "Flow", "SceneStatsModifierController.cs");
            string statsPath = Path.Combine(scriptsRoot, "Core", "Combat", "Stats", "StatsCore.cs");

            Assert.That(File.Exists(registryPath), Is.True);
            StringAssert.Contains("StatModifierRegistry.Apply", File.ReadAllText(statsPath));
            StringAssert.Contains("StatModifierRegistry.Register", File.ReadAllText(controllerPath));
            StringAssert.DoesNotContain("SceneStatsModifierController", File.ReadAllText(statsPath));
        }

        [Test]
        public void PhaseEPersistenceAssemblyIsolatedFromRuntimeImplementations()
        {
            AssertAssemblyReferences("Zephyr.Core.Persistence", "Zephyr.Core.Runtime");

            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string persistenceRoot = Path.Combine(scriptsRoot, "Core", "Persistence");
            string asmdef = File.ReadAllText(Path.Combine(persistenceRoot, "Zephyr.Core.Persistence.asmdef"));
            StringAssert.Contains("\"Zephyr.Core.Runtime\"", asmdef);
            StringAssert.Contains("\"autoReferenced\": false", asmdef);

            foreach (string file in Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(file);
                StringAssert.DoesNotContain("Zephyr.Core.Flow", source, file);
                StringAssert.DoesNotContain("Zephyr.Core.Meta", source, file);
                StringAssert.DoesNotContain("SceneManager.", source, file);
            }
        }

        [Test]
        public void PhaseEFlowAssemblyOwnsSceneIntegrationAndDependsDownward()
        {
            AssertAssemblyReferences("Zephyr.Core.Flow", "Zephyr.Core.Contracts", "Zephyr.Core.Runtime",
                "Zephyr.Core.Persistence", "Zephyr.Core");

            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string flowRoot = Path.Combine(scriptsRoot, "Core", "Flow");
            string flowAsmdef = File.ReadAllText(Path.Combine(flowRoot, "Zephyr.Core.Flow.asmdef"));
            StringAssert.DoesNotContain("Zephyr.Core.Combat", flowAsmdef);
            Assert.That(File.Exists(Path.Combine(flowRoot, "SceneStatsModifierController.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(flowRoot, "Events", "SceneInfoEventSO.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(flowRoot, "RoomController.cs")), Is.True);

            string coreAsmdef = File.ReadAllText(Path.Combine(scriptsRoot, "Core", "Zephyr.Core.asmdef"));
            StringAssert.Contains("\"Zephyr.Core.Persistence\"", coreAsmdef);
            StringAssert.DoesNotContain("\"Zephyr.Core.Flow\"", coreAsmdef);
        }

        [Test]
        public void PhaseEFlowAndPersistenceSourceTreesRemainSeparated()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string persistenceRoot = Path.Combine(scriptsRoot, "Core", "Persistence");
            string flowRoot = Path.Combine(scriptsRoot, "Core", "Flow");

            Assert.That(File.Exists(Path.Combine(persistenceRoot, "SaveSystem.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(persistenceRoot, "MetaData.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(persistenceRoot, "RunData.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(flowRoot, "GameFlow.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(flowRoot, "SceneLoader.cs")), Is.True);
            StringAssert.DoesNotContain("SaveSystem", File.ReadAllText(Path.Combine(flowRoot, "SceneLoader.cs")));
        }

        [Test]
        public void PhaseFMetaAssemblyDependsOnlyOnStableCoreLayers()
        {
            AssertAssemblyReferences("Zephyr.Core.Meta", "Zephyr.Core.Contracts", "Zephyr.Core.Runtime",
                "Zephyr.Core.Persistence", "Unity.Localization");

            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string metaRoot = Path.Combine(scriptsRoot, "Core", "Meta");
            string asmdef = File.ReadAllText(Path.Combine(metaRoot, "Zephyr.Core.Meta.asmdef"));
            StringAssert.DoesNotContain("Zephyr.Core\"", asmdef);
            StringAssert.DoesNotContain("Zephyr.Core.Flow", asmdef);
            StringAssert.DoesNotContain("Zephyr.Core.Combat", asmdef);
            StringAssert.DoesNotContain("Zephyr.Player", asmdef);
            StringAssert.DoesNotContain("Zephyr.Enemies", asmdef);
            StringAssert.DoesNotContain("Zephyr.Weapons", asmdef);
            StringAssert.DoesNotContain("Zephyr.UI", asmdef);

            foreach (string file in Directory.GetFiles(metaRoot, "*.cs", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(file);
                StringAssert.DoesNotContain("SceneManager.", source, file);
                StringAssert.DoesNotContain("GameFlow.Instance", source, file);
            }
        }

        [Test]
        public void PhaseFMetaConsumersReferenceMetaDirectly()
        {
            AssertAssemblyReferences("Zephyr.Core", "Zephyr.Core.Meta");
            AssertAssemblyReferences("Zephyr.Core.Flow", "Zephyr.Core.Meta");
            AssertAssemblyReferences("Zephyr.Gameplay.Interaction", "Zephyr.Core.Meta");
            AssertAssemblyReferences("Zephyr.Player", "Zephyr.Core.Meta");
            AssertAssemblyReferences("Zephyr.Testing.Editor", "Zephyr.Core.Meta");
        }

        [Test]
        public void PhaseFMetaSourceDoesNotDependOnLegacyCoreImplementation()
        {
            string metaRoot = Path.Combine(Application.dataPath, "Scripts", "Core", "Meta");
            foreach (string file in Directory.GetFiles(metaRoot, "*.cs", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(file);
                StringAssert.DoesNotContain("using Zephyr.Core;", source, file);
                StringAssert.DoesNotContain("using Zephyr.Core.Flow", source, file);
                StringAssert.DoesNotContain("using Zephyr.Core.Combat", source, file);
                StringAssert.DoesNotContain("using Zephyr.Core.UI", source, file);
            }
        }

        [Test]
        public void SaveAndMetaGatewayBoundariesRemainNarrow()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string persistenceRoot = Path.Combine(scriptsRoot, "Core", "Persistence");
            string metaRoot = Path.Combine(scriptsRoot, "Core", "Meta");

            string saveContract = File.ReadAllText(Path.Combine(persistenceRoot, "ISaveSlotGateway.cs"));
            StringAssert.Contains("interface ISaveSlotGateway", saveContract);
            StringAssert.Contains("interface ISaveDataGateway", saveContract);
            StringAssert.Contains("interface ISaveGateway", saveContract);
            StringAssert.DoesNotContain("MetaData Meta", saveContract.Split(new[] { "public interface ISaveDataGateway" }, StringSplitOptions.None)[0]);
            StringAssert.DoesNotContain("RunData CurrentRun", saveContract.Split(new[] { "public interface ISaveDataGateway" }, StringSplitOptions.None)[0]);

            string saveSystem = File.ReadAllText(Path.Combine(persistenceRoot, "SaveSystem.cs"));
            StringAssert.Contains("ISaveGateway", saveSystem);

            string metaGateway = File.ReadAllText(Path.Combine(metaRoot, "IMetaGateway.cs"));
            StringAssert.Contains("interface IMetaGateway", metaGateway);
            StringAssert.Contains("class MetaGateway", metaGateway);
            StringAssert.Contains("ISaveDataGateway", metaGateway);

            string story = File.ReadAllText(Path.Combine(metaRoot, "StoryProgressionService.cs"));
            StringAssert.Contains("IMetaGateway", story);
            StringAssert.DoesNotContain("SaveSystem.Instance", story);

            string upgrades = File.ReadAllText(Path.Combine(metaRoot, "MetaUpgradeSystem.cs"));
            StringAssert.DoesNotContain("SaveSystem.Instance", upgrades);

            string settlement = File.ReadAllText(Path.Combine(metaRoot, "RunEndSettlement.cs"));
            StringAssert.Contains("SettleCurrentRun(ISaveDataGateway save)", settlement);
            StringAssert.DoesNotContain("SaveSystem.Instance", settlement);
        }

        [Test]
        public void SaveMetaConsumersCacheInterfacesAtCompositionBoundaries()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string gameFlow = File.ReadAllText(Path.Combine(scriptsRoot, "Core", "Flow", "GameFlow.cs"));
            StringAssert.Contains("ISaveDataGateway _saveData", gameFlow);
            StringAssert.Contains("RunEndSettlement.SettleCurrentRun(_saveData)", gameFlow);

            string mainMenu = File.ReadAllText(Path.Combine(scriptsRoot, "Core", "Flow", "Scenes", "MainMenuController.cs"));
            StringAssert.Contains("ISaveSlotGateway _saveSlots", mainMenu);
            StringAssert.Contains("ISaveDataGateway _saveData", mainMenu);

            string death = File.ReadAllText(Path.Combine(scriptsRoot, "Player", "Flow", "PlayerDeathController.cs"));
            StringAssert.Contains("ISaveDataGateway _saveGateway", death);

            string interaction = File.ReadAllText(Path.Combine(scriptsRoot, "Gameplay", "Interaction", "MetaUpgradePanel.cs"));
            StringAssert.Contains("IMetaGateway _metaGateway", interaction);
            StringAssert.DoesNotContain("SaveSystem.Instance", interaction);
        }

        [Test]
        public void SingletonServicesDeclareOwnerLifetimeAndReleaseStaticReferences()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            var services = new[]
            {
                new { Path = Path.Combine(scriptsRoot, "Core", "Runtime", "Events", "EventBus.cs"), Lifetime = "Persistent", Scene = "Persistent" },
                new { Path = Path.Combine(scriptsRoot, "Core", "Persistence", "SaveSystem.cs"), Lifetime = "Persistent", Scene = "Persistent" },
                new { Path = Path.Combine(scriptsRoot, "Core", "Flow", "GameFlow.cs"), Lifetime = "Persistent", Scene = "Persistent" },
                new { Path = Path.Combine(scriptsRoot, "Core", "Flow", "SceneLoader.cs"), Lifetime = "Persistent", Scene = "Persistent" },
                new { Path = Path.Combine(scriptsRoot, "Core", "Runtime", "ObjectPool", "PoolRegistry.cs"), Lifetime = "Persistent", Scene = "Persistent" },
                new { Path = Path.Combine(scriptsRoot, "Core", "Runtime", "Timer", "TimerService.cs"), Lifetime = "Persistent", Scene = "Persistent" },
                new { Path = Path.Combine(scriptsRoot, "Core", "Audio", "AudioManager.cs"), Lifetime = "Persistent", Scene = "Persistent" },
                new { Path = Path.Combine(scriptsRoot, "Core", "Localization", "LocalizationManager.cs"), Lifetime = "Persistent", Scene = "Persistent" },
                new { Path = Path.Combine(scriptsRoot, "Core", "Meta", "StoryProgressionService.cs"), Lifetime = "Persistent", Scene = "Persistent" },
                new { Path = Path.Combine(scriptsRoot, "Core", "UI", "Dialogue", "DialogueManager.cs"), Lifetime = "GameManager", Scene = "GameManager" },
                new { Path = Path.Combine(scriptsRoot, "Core", "Flow", "SceneStatsModifierController.cs"), Lifetime = "GameManager", Scene = "GameManager" },
                new { Path = Path.Combine(scriptsRoot, "Player", "Flow", "PlayerSpawner.cs"), Lifetime = "GameManager", Scene = "GameManager" },
                new { Path = Path.Combine(scriptsRoot, "Gameplay", "Levels", "SceneProgressionController.cs"), Lifetime = "GameManager", Scene = "GameManager" }
            };

            foreach (var service in services)
            {
                Assert.That(File.Exists(service.Path), Is.True, service.Path);
                string source = File.ReadAllText(service.Path);
                StringAssert.Contains("SingletonLifetime(SingletonLifetime." + service.Lifetime + ", \"" + service.Scene + "\")", source, service.Path);
                StringAssert.Contains("SingletonLifecycle.TryClaim", source, service.Path);
                StringAssert.Contains("SingletonLifecycle.Release", source, service.Path);
                StringAssert.Contains("OnDestroy", source, service.Path);
                StringAssert.DoesNotContain("DontDestroyOnLoad", source, service.Path);
            }
        }

        [Test]
        public void SingletonOwnerScenesContainTheirDeclaredServiceObjects()
        {
            string scenesRoot = Path.Combine(Application.dataPath, "Scenes", "Core");
            string persistent = File.ReadAllText(Path.Combine(scenesRoot, "Persistent.unity"));
            string gameManager = File.ReadAllText(Path.Combine(scenesRoot, "GameManager.unity"));

            foreach (string name in new[] { "EventBus", "SaveSystem", "GameFlow", "SceneLoader", "PoolRegistry", "TimerService", "AudioManager", "LocalizationManager", "StoryProgressionService" })
                StringAssert.Contains(name, persistent, "Persistent owner missing " + name);

            foreach (string name in new[] { "GameManagerBootstrap", "PlayerSpawner", "DialogueManager", "SceneStatsModifierController", "SceneProgressionController" })
                StringAssert.Contains(name, gameManager, "GameManager owner missing " + name);
        }

        [Test]
        public void PhaseGLegacyCoreHostIsExplicitAndDoesNotDependUpward()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string coreRoot = Path.Combine(scriptsRoot, "Core");
            string coreAsmdef = File.ReadAllText(Path.Combine(coreRoot, "Zephyr.Core.asmdef"));

            StringAssert.Contains("\"autoReferenced\": false", coreAsmdef);
            StringAssert.DoesNotContain("\"Zephyr.Core.Flow\"", coreAsmdef);
            StringAssert.DoesNotContain("\"Zephyr.Core.Combat\"", coreAsmdef);
            StringAssert.DoesNotContain("\"Zephyr.Player\"", coreAsmdef);
            StringAssert.DoesNotContain("\"Zephyr.Enemies\"", coreAsmdef);
            StringAssert.DoesNotContain("\"Zephyr.Weapons\"", coreAsmdef);
            StringAssert.DoesNotContain("\"Zephyr.Items\"", coreAsmdef);
            StringAssert.DoesNotContain("\"Zephyr.Projectiles\"", coreAsmdef);

            string[] coreConsumers =
            {
                "Zephyr.Core.Flow",
                "Zephyr.Items",
                "Zephyr.Projectiles",
                "Zephyr.Weapons",
                "Zephyr.Enemies",
                "Zephyr.Gameplay.Interaction",
                "Zephyr.Rooms",
                "Zephyr.Player",
                "Zephyr.Testing",
                "Zephyr.Testing.Editor",
                "Zephyr.Tests.EditMode",
                "Zephyr.UI"
            };

            foreach (string consumer in coreConsumers)
                AssertAssemblyReferences(consumer, "Zephyr.Core");
        }

        [Test]
        public void PhaseGCoreSplitCloseoutHasNoImplicitRuntimeCoreConsumers()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            foreach (string asmdefPath in Directory.GetFiles(scriptsRoot, "*.asmdef", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(asmdefPath);
                if (source.Contains("\"name\": \"Zephyr.Core\"")) continue;
                if (source.Contains("\"includePlatforms\": [\n        \"Editor\"")) continue;

                string assemblyName = Path.GetFileNameWithoutExtension(asmdefPath);
                bool importsAudioNamespace = false;
                bool importsCameraNamespace = false;
                bool importsLegacyCoreNamespace = false;
                string assemblyDirectory = Path.GetDirectoryName(asmdefPath);
                foreach (string file in Directory.GetFiles(assemblyDirectory, "*.cs", SearchOption.AllDirectories))
                {
                    string code = File.ReadAllText(file);
                    importsAudioNamespace |= code.Contains("using Zephyr.Core.Audio");
                    importsCameraNamespace |= code.Contains("using Zephyr.Core.Camera");
                    importsLegacyCoreNamespace |= code.Contains("using Zephyr.Core.Localization")
                        || code.Contains("using Zephyr.Core.UI");
                }

                if (importsAudioNamespace)
                    StringAssert.Contains("\"Zephyr.Core.Audio\"", source, assemblyName);
                if (importsCameraNamespace)
                    StringAssert.Contains("\"Zephyr.Core.Camera\"", source, assemblyName);
                if (importsLegacyCoreNamespace)
                    StringAssert.Contains("\"Zephyr.Core\"", source, assemblyName);
            }
        }

        [Test]
        public void AudioAndCameraAssembliesAreIndependentFromLegacyCoreHost()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string audioAsmdef = File.ReadAllText(Path.Combine(scriptsRoot, "Core", "Audio", "Zephyr.Core.Audio.asmdef"));
            string cameraAsmdef = File.ReadAllText(Path.Combine(scriptsRoot, "Core", "Camera", "Zephyr.Core.Camera.asmdef"));

            StringAssert.Contains("\"name\": \"Zephyr.Core.Audio\"", audioAsmdef);
            StringAssert.Contains("\"Zephyr.Core.Runtime\"", audioAsmdef);
            StringAssert.Contains("\"autoReferenced\": false", audioAsmdef);
            StringAssert.DoesNotContain("\"Zephyr.Core\"", audioAsmdef);

            StringAssert.Contains("\"name\": \"Zephyr.Core.Camera\"", cameraAsmdef);
            StringAssert.Contains("\"autoReferenced\": false", cameraAsmdef);
            StringAssert.DoesNotContain("\"Zephyr.Core\"", cameraAsmdef);
        }

        private static void AssertAssemblyReferences(string assemblyName, params string[] expectedReferences)
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string[] matches = Directory.GetFiles(scriptsRoot, assemblyName + ".asmdef", SearchOption.AllDirectories);
            Assert.That(matches, Has.Length.EqualTo(1), "Expected exactly one asmdef for " + assemblyName);

            string source = File.ReadAllText(matches[0]);
            foreach (string expectedReference in expectedReferences)
                StringAssert.Contains("\"" + expectedReference + "\"", source, assemblyName + " must reference " + expectedReference);
        }

        private static void AssertSourceTreeDoesNotContain(string root, string forbiddenText)
        {
            string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string source = File.ReadAllText(file);
                StringAssert.DoesNotContain(forbiddenText, source, "Unexpected dependency in " + file);
            }
        }
    }
}
