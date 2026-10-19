using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using GlobalObjects = UnityEngine.GameObject;
using Pipeline = UnityEditor.Compilation.CompilationPipeline;

namespace Zephyr.Architecture.Tests
{
    public sealed class StructuralArchitectureTests
    {
        [Test]
        public void ProjectAssemblyGraphIsExplicitAcyclicAndReviewed()
        {
            Dictionary<string, AssemblyDefinition> definitions = ArchitectureInspection.ReadDefinitions();
            Dictionary<string, string[]> graph = ArchitectureInspection.BuildGraph(definitions);
            ArchitecturePolicy policy = ArchitectureInspection.ReadPolicy();
            Assert.That(ArchitectureInspection.FindCycle(graph), Is.Null, "Cycle: " + ArchitectureInspection.FindCycle(graph));
            Assert.That(definitions.ContainsKey("Zephyr.Core"), Is.False, "Legacy Core host must remain deleted.");
            Assert.That(policy.assemblies.Select(assembly => assembly.name), Is.Unique);
            CollectionAssert.AreEquivalent(definitions.Keys, policy.assemblies.Select(assembly => assembly.name),
                "New or removed assemblies require a reviewed policy update.");
            var allowed = policy.assemblies.ToDictionary(assembly => assembly.name, assembly => assembly.references);
            var available = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetName().Name));
            foreach (string path in AssetDatabase.GetAllAssetPaths().Where(path => path.EndsWith(".asmdef", StringComparison.Ordinal)))
                available.Add(ArchitectureInspection.ParseDefinition(File.ReadAllText(path), path).name);

            foreach (AssemblyDefinition definition in definitions.Values)
            {
                Assert.That(definition.autoReferenced, Is.False, definition.name + " must declare explicit references.");
                Assert.That(graph[definition.name], Is.Unique, "Duplicate dependency: " + definition.name);
                Assert.That(graph[definition.name], Does.Not.Contain("Zephyr.Core"));
                CollectionAssert.AreEquivalent(allowed[definition.name], graph[definition.name],
                    "Dependency policy changed for " + definition.name + "; review the boundary and update the ledger.");
                foreach (string reference in graph[definition.name])
                {
                    Assert.That(available.Contains(reference), Is.True, "Unresolved dependency: " + definition.name + " -> " + reference);
                    if (!definition.EditorOnly && !definition.IsTest && definitions.TryGetValue(reference, out AssemblyDefinition target))
                    {
                        Assert.That(target.EditorOnly, Is.False, definition.name + " -> editor-only " + reference);
                        Assert.That(target.IsTest, Is.False, definition.name + " -> test assembly " + reference);
                    }
                }
            }
        }

        [Test]
        public void CompiledScriptsHaveExplicitOwnersAndNoRuntimeEditorEdges()
        {
            var definitions = ArchitectureInspection.ReadDefinitions();
            var compiled = Pipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Editor);
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            foreach (string file in Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                string assetPath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                string ownerPath = Pipeline.GetAssemblyDefinitionFilePathFromScriptPath(assetPath);
                Assert.That(ownerPath, Is.Not.Null.And.Not.Empty, "Implicit Assembly-CSharp owner: " + assetPath);
                AssemblyDefinition owner = ArchitectureInspection.ParseDefinition(File.ReadAllText(ownerPath), ownerPath);
                Assert.That(definitions.ContainsKey(owner.name), Is.True, "Unreviewed script owner: " + assetPath + " -> " + owner.name);
            }
            CollectionAssert.IsSubsetOf(definitions.Keys, compiled.Select(assembly => assembly.name),
                "A project assembly is absent from the current compilation; do not silently skip its checks.");
            foreach (var assembly in compiled.Where(assembly => definitions.ContainsKey(assembly.name)))
            {
                AssemblyDefinition definition = definitions[assembly.name];
                if (definition.EditorOnly || definition.IsTest) continue;
                foreach (var target in assembly.assemblyReferences)
                {
                    if (target.name.StartsWith("UnityEditor", StringComparison.Ordinal) ||
                        target.name.StartsWith("UnityEditor.", StringComparison.Ordinal)) continue;
                    Assert.That((target.flags & UnityEditor.Compilation.AssemblyFlags.EditorAssembly) == 0, Is.True,
                        "Compiled runtime-to-editor edge: " + assembly.name + " -> " + target.name);
                }
            }
        }

        [Test]
        public void RestrictedRuntimeCallsMatchReviewedMethodBudgets()
        {
            List<ArchitectureCall> calls = ArchitectureInspection.InspectRuntime();
            ArchitecturePolicy policy = ArchitectureInspection.ReadPolicy();
            Assert.That(calls, Is.Not.Empty, "Scanner must not pass on an empty inventory.");
            Assert.That(policy.diagnosticAssemblies, Is.Empty,
                "Runtime assemblies must not be exempted from restricted-call inspection.");
            foreach (CallAllowance allowance in policy.calls)
            {
                Assert.That(allowance.maxCount, Is.GreaterThan(0));
                Assert.That(allowance.owner, Is.Not.Null.And.Not.Empty, allowance.caller);
                Assert.That(allowance.reason, Is.Not.Null.And.Not.Empty, allowance.caller);
                Assert.That(allowance.risk, Is.Not.Null.And.Not.Empty, allowance.caller);
                Assert.That(allowance.replacementBoundary, Is.Not.Null.And.Not.Empty, allowance.caller);
                Assert.That(allowance.removalCondition, Is.Not.Null.And.Not.Empty, allowance.caller);
                Assert.That(new[] { "Singleton", "SceneScan", "SceneLoad" }, Does.Contain(allowance.rule),
                    "Global lookups, Resources loads and DontDestroyOnLoad have no grandfathered allowance.");
            }
            Assert.That(ArchitectureInspection.ValidateCalls(calls, policy.calls), Is.Empty);
            foreach (CallAllowance allowance in policy.calls)
            {
                int actual = calls.Count(call => call.Caller == allowance.caller && call.Target == allowance.target && call.Rule == allowance.rule);
                Assert.That(actual, Is.EqualTo(allowance.maxCount),
                    "Removed or reduced calls must reduce the policy budget too: " + allowance.caller + " -> " + allowance.target);
            }
            TestContext.WriteLine(ArchitectureInspection.DescribeCalls());
        }

        [Test]
        public void AssemblyJsonParsingIgnoresFormattingAndUnrelatedStrings()
        {
            AssemblyDefinition definition = ArchitectureInspection.ParseDefinition(
                "{\"description\":\"Zephyr.Core\",\"references\":[\"Zephyr.Core.Contracts\"],\"name\":\"Sample\",\"autoReferenced\":false}", "fixture");
            Assert.That(definition.name, Is.EqualTo("Sample"));
            Assert.That(definition.references, Is.EqualTo(new[] { "Zephyr.Core.Contracts" }));
            Assert.That(definition.autoReferenced, Is.False);
            Assert.Throws<InvalidDataException>(() => ArchitectureInspection.ParseDefinition("{}", "fixture"));
        }

        [Test]
        public void DependencyGraphDetectsLongCyclesAndSelfReferences()
        {
            var graph = new Dictionary<string, string[]>
            {
                { "A", new[] { "B" } }, { "B", new[] { "C" } }, { "C", new[] { "A" } }
            };
            Assert.That(ArchitectureInspection.FindCycle(graph), Is.EqualTo("A -> B -> C -> A"));
            graph["C"] = Array.Empty<string>();
            Assert.That(ArchitectureInspection.FindCycle(graph), Is.Null);
            graph["C"] = new[] { "C" };
            Assert.That(ArchitectureInspection.FindCycle(graph), Is.EqualTo("C -> C"));
        }

        [Test]
        public void GuidAssemblyReferencesResolveToDeclaredNames()
        {
            string[] paths = AssetDatabase.FindAssets("Zephyr.Core.Contracts t:AssemblyDefinitionAsset");
            Assert.That(paths.Length, Is.EqualTo(1));
            Assert.That(ArchitectureInspection.ResolveReference("GUID:" + paths[0]), Is.EqualTo("Zephyr.Core.Contracts"));
            Assert.Throws<InvalidDataException>(() => ArchitectureInspection.ResolveReference("GUID:00000000000000000000000000000000"));
        }

        [Test]
        public void CallInspectionDetectsAliasesButIgnoresCommentsAndStrings()
        {
            List<ArchitectureCall> calls = ArchitectureInspection.ReadCalls(FixtureMethod(nameof(AliasedLookup))).ToList();
            Assert.That(calls.Count, Is.EqualTo(1));
            Assert.That(calls[0].Rule, Is.EqualTo("GlobalLookup"));
            Assert.That(calls[0].Target, Is.EqualTo("UnityEngine.GameObject::Find"));
            Assert.That(ArchitectureInspection.ReadCalls(FixtureMethod(nameof(OnlyText))), Is.Empty);
        }

        [Test]
        public void IteratorCallsAreAttributedToTheirAuthoredMethod()
        {
            MethodInfo authored = FixtureMethod(nameof(IteratorLookup));
            Type state = authored.GetCustomAttributes<System.Runtime.CompilerServices.IteratorStateMachineAttribute>().Single().StateMachineType;
            MethodInfo moveNext = state.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance);
            List<ArchitectureCall> calls = ArchitectureInspection.ReadCalls(moveNext).ToList();
            Assert.That(calls.Count, Is.EqualTo(1));
            Assert.That(calls[0].Caller, Is.EqualTo(ArchitectureInspection.MethodKey(authored)));
            Assert.That(calls[0].Rule, Is.EqualTo("GlobalLookup"));
        }

        [Test]
        public void CallBudgetsRejectNewMethodsAndIncreasedCounts()
        {
            var calls = new[] { new ArchitectureCall { Caller = "A::Start()", Target = "Service::get_Instance", Rule = "Singleton" } };
            var budgets = new[] { new CallAllowance { caller = calls[0].Caller, target = calls[0].Target, rule = calls[0].Rule, maxCount = 1 } };
            Assert.That(ArchitectureInspection.ValidateCalls(calls, budgets), Is.Empty);
            Assert.That(ArchitectureInspection.ValidateCalls(calls.Concat(calls), budgets).Single(), Does.StartWith("Call budget exceeded"));
            calls[0].Caller = "A::Update()";
            Assert.That(ArchitectureInspection.ValidateCalls(calls, budgets).Single(), Does.StartWith("Unapproved call"));
        }

        private static MethodInfo FixtureMethod(string name) => typeof(StructuralArchitectureTests)
            .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);

        private static GameObject AliasedLookup() => GlobalObjects.Find("ArchitectureNegativeFixture");
        private static string OnlyText() => "GameObject.Find and SaveSystem.Instance are inert documentation text";
        private static IEnumerator IteratorLookup()
        {
            yield return GlobalObjects.Find("ArchitectureIteratorFixture");
        }
    }
}
