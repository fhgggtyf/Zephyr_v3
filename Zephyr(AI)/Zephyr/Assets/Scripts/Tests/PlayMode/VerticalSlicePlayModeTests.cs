using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zephyr.Contracts.Damage;
using Zephyr.Contracts.Flow;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Flow;
using Zephyr.Core.Flow.Scenes;
using Zephyr.Core.Save;
using Zephyr.Gameplay.Enemies;
using Zephyr.Gameplay.Levels;
using Zephyr.Gameplay.Player.Flow;

namespace Zephyr.Tests.PlayMode
{
    public sealed class VerticalSlicePlayModeTests
    {
        private const string InitializerSceneName = "Initializer";
        private const string MainMenuSceneName = "MainMenu";
        private const string TutorialPhaseOneSceneName = "Tutorial_Phase_1";
        private const string TutorialPhaseTwoSceneName = "Tutorial_Phase_2";
        private const string MetaHubSceneName = "MetaHub";
        private const string LoadingRunSceneName = "LoadingRun";
        private const int TestSlotIndex = 0;

        private string _temporarySavePath;
        private SaveSystem _save;

        [UnityTest]
        [Category("VerticalSlice")]
        public IEnumerator FullVerticalSliceUsesDisposableSaveStorage()
        {
            yield return LoadInitializerAndWaitForMainMenu();

            _save = SaveSystem.Instance;
            Assert.That(_save, Is.Not.Null, "Persistent SaveSystem was not created.");
            ConfigureDisposableSaveStorage(_save);

            Assert.That(_save.TryCreateSlot(TestSlotIndex), Is.True,
                "The disposable test slot could not be created.");
            Assert.That(_save.Meta, Is.Not.Null);
            Assert.That(_save.CurrentRun, Is.Null);
            Assert.That(GameFlow.Instance.CurrentState, Is.EqualTo(GameState.MainMenu));

            GameFlow.Instance.RequestTransition(GameState.Tutorial);
            yield return WaitForStateAndScene(GameState.Tutorial, TutorialPhaseOneSceneName);

            SceneProgressionController progression = UnityEngine.Object.FindAnyObjectByType<SceneProgressionController>();
            Assert.That(progression, Is.Not.Null, "GameManager progression controller is missing.");
            Assert.That(progression.AdvanceFromCurrentScene(), Is.True,
                "Tutorial phase one did not accept its progression request.");
            yield return WaitForScene(TutorialPhaseTwoSceneName);
            Assert.That(GameFlow.Instance.CurrentState, Is.EqualTo(GameState.Tutorial));
            Assert.That(_save.Meta.HasCompletedTutorial, Is.False);

            yield return WaitFor(() => PlayerSpawner.Instance != null &&
                                      PlayerSpawner.Instance.CurrentPlayer != null,
                "PlayerSpawner did not create a player for the final tutorial phase.");
            TutorialObjectiveExit finalExit = UnityEngine.Object.FindAnyObjectByType<TutorialObjectiveExit>();
            Assert.That(finalExit, Is.Not.Null, "Final tutorial exit is missing.");
            DefeatTutorialObjectivesThroughCombatContract();
            yield return WaitFor(() => finalExit.RemainingTargets == 0,
                DescribeRemainingObjectives(finalExit));
            Assert.That(finalExit.RemainingTargets, Is.EqualTo(0),
                DescribeRemainingObjectives(finalExit));
            Collider2D playerCollider = PlayerSpawner.Instance.CurrentPlayer.GetComponent<Collider2D>();
            Assert.That(playerCollider, Is.Not.Null, "The test player has no Collider2D for exit validation.");
            finalExit.SendMessage("OnTriggerEnter2D", playerCollider, SendMessageOptions.RequireReceiver);
            yield return WaitForStateAndScene(GameState.MetaHub, MetaHubSceneName);
            Assert.That(_save.Meta.HasCompletedTutorial, Is.True,
                "Completing the final tutorial phase did not persist the tutorial flag.");

            MetaHubController metaHub = UnityEngine.Object.FindAnyObjectByType<MetaHubController>();
            Assert.That(metaHub, Is.Not.Null, "MetaHub controller is missing.");
            metaHub.StartRun();
            yield return WaitForStateAndScene(GameState.LoadingRun, LoadingRunSceneName);

            yield return WaitFor(() => GameFlow.Instance.CurrentState == GameState.InRun &&
                                      _save.CurrentRun != null &&
                                      HasLoadedRunScene(),
                "LoadingRun did not produce an active run scene.");

            Assert.That(_save.CurrentRun, Is.Not.Null);
            Assert.That(_save.CurrentRun.Seed, Is.Not.EqualTo(0));
            Assert.That(PlayerSpawner.Instance, Is.Not.Null, "PlayerSpawner is missing from GameManager.");
            yield return WaitFor(() => PlayerSpawner.Instance.CurrentPlayer != null,
                "PlayerSpawner did not create a player for the first run scene.");

            _save.CurrentRun.RunCurrency = 7;
            _save.SaveRun();
            GameFlow.Instance.RequestTransition(GameState.MetaHub);
            yield return WaitForStateAndScene(GameState.MetaHub, MetaHubSceneName);

            Assert.That(_save.CurrentRun, Is.Null, "Run settlement did not clear the active run.");
            Assert.That(_save.Meta.MetaCurrency, Is.EqualTo(7));
            Assert.That(_save.GetSlotInfo(TestSlotIndex).HasRun, Is.False);

            Assert.That(_save.TrySelectSlot(TestSlotIndex), Is.True,
                "The disposable profile could not be selected after settlement.");
            Assert.That(_save.LoadMeta(), Is.Not.Null);
            Assert.That(_save.LoadMeta().HasCompletedTutorial, Is.True);
            Assert.That(_save.LoadRun(), Is.Null);
        }

        [UnityTearDown]
        public IEnumerator RemoveDisposableSaveStorage()
        {
            if (_save != null)
            {
                _save.DeleteSlot(TestSlotIndex);
                _save = null;
            }

            if (!string.IsNullOrWhiteSpace(_temporarySavePath) && Directory.Exists(_temporarySavePath))
                Directory.Delete(_temporarySavePath, true);

            yield return null;
        }

        private IEnumerator LoadInitializerAndWaitForMainMenu()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(InitializerSceneName, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, "Initializer scene could not be loaded.");
            yield return load;

            yield return WaitFor(() => GameFlow.Instance != null &&
                                      GameFlow.Instance.CurrentState == GameState.MainMenu &&
                                      IsSceneLoaded(MainMenuSceneName),
                "Initializer did not reach MainMenu.");
        }

        private IEnumerator WaitForStateAndScene(GameState state, string sceneName)
        {
            yield return WaitFor(() => GameFlow.Instance != null &&
                                      GameFlow.Instance.CurrentState == state &&
                                      IsSceneLoaded(sceneName),
                $"GameFlow did not reach {state} with scene {sceneName}.");
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            yield return WaitFor(() => IsSceneLoaded(sceneName),
                $"Expected scene {sceneName} was not loaded.");
        }

        private static IEnumerator WaitFor(Func<bool> predicate, string failureMessage, float timeoutSeconds = 45f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup >= deadline)
                    Assert.Fail(failureMessage);

                yield return null;
            }
        }

        private void ConfigureDisposableSaveStorage(SaveSystem save)
        {
            _temporarySavePath = Path.Combine(Application.temporaryCachePath,
                "ZephyrVerticalSlice_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_temporarySavePath);

            FieldInfo savePath = typeof(SaveSystem).GetField("_savePath",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(savePath, Is.Not.Null, "SaveSystem test storage hook changed unexpectedly.");
            savePath.SetValue(save, _temporarySavePath);
        }

        private static bool IsSceneLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        private static bool HasLoadedRunScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (scene.name.StartsWith("LV", StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static string DescribeRemainingObjectives(TutorialObjectiveExit exit)
        {
            List<string> remaining = new List<string>();
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (behaviour is ILevelObjective objective && behaviour.isActiveAndEnabled && !objective.IsComplete)
                    remaining.Add($"{behaviour.name} ({behaviour.GetType().FullName})");
            }

            return $"Tutorial exit has {exit.RemainingTargets} remaining target(s): {string.Join(", ", remaining)}";
        }

        private static void DefeatTutorialObjectivesThroughCombatContract()
        {
            EnemyBrain[] enemies = UnityEngine.Object.FindObjectsByType<EnemyBrain>(FindObjectsInactive.Include);
            foreach (EnemyBrain enemy in enemies)
            {
                if (enemy == null || enemy.IsComplete || enemy.Health == null) continue;

                enemy.Health.TakeDamage(new DamageInfo(
                    100000f,
                    DamageType.True,
                    sourcePosition: enemy.transform.position,
                    source: enemy.gameObject));
            }
        }
    }
}
