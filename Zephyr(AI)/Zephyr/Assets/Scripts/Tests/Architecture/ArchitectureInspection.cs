using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Zephyr.Architecture.Tests
{
    [Serializable]
    internal sealed class AssemblyDefinition
    {
        public string name;
        public string[] references = Array.Empty<string>();
        public string[] includePlatforms = Array.Empty<string>();
        public string[] optionalUnityReferences = Array.Empty<string>();
        public bool autoReferenced = true;
        [NonSerialized] public string Path;
        public bool EditorOnly => includePlatforms.Length == 1 && includePlatforms[0] == "Editor";
        public bool IsTest => optionalUnityReferences.Contains("TestAssemblies");
    }

    [Serializable]
    internal sealed class ArchitecturePolicy
    {
        public int version;
        public AssemblyAllowance[] assemblies;
        public CallAllowance[] calls;
        public string[] diagnosticAssemblies;
    }

    [Serializable]
    internal sealed class AssemblyAllowance
    {
        public string name;
        public string[] references;
    }

    [Serializable]
    internal sealed class CallAllowance
    {
        public string caller;
        public string target;
        public string rule;
        public int maxCount;
        public string owner;
        public string reason;
        public string risk;
        public string replacementBoundary;
        public string removalCondition;
    }

    internal sealed class ArchitectureCall
    {
        public string Caller;
        public string Target;
        public string Rule;
        public int Offset;
        public string Key => Caller + " -> " + Target + " [" + Rule + "]";
    }

    internal static class ArchitectureInspection
    {
        internal const string PolicyPath = "Assets/Scripts/Tests/Architecture/ArchitecturePolicy.json";
        private static readonly Dictionary<short, OpCode> Opcodes = typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null))
            .ToDictionary(opcode => opcode.Value);

        internal static AssemblyDefinition ParseDefinition(string json, string path)
        {
            AssemblyDefinition definition = JsonUtility.FromJson<AssemblyDefinition>(json);
            if (definition == null || string.IsNullOrWhiteSpace(definition.name))
                throw new InvalidDataException("Assembly definition has no name: " + path);
            definition.Path = path;
            definition.references = definition.references ?? Array.Empty<string>();
            definition.includePlatforms = definition.includePlatforms ?? Array.Empty<string>();
            definition.optionalUnityReferences = definition.optionalUnityReferences ?? Array.Empty<string>();
            return definition;
        }

        internal static ArchitecturePolicy ReadPolicy()
        {
            ArchitecturePolicy policy = JsonUtility.FromJson<ArchitecturePolicy>(File.ReadAllText(PolicyPath));
            if (policy == null || policy.version != 1 || policy.assemblies == null || policy.calls == null ||
                policy.diagnosticAssemblies == null)
                throw new InvalidDataException("Architecture policy is missing or has an unsupported schema.");
            return policy;
        }

        internal static Dictionary<string, AssemblyDefinition> ReadDefinitions()
        {
            var definitions = new Dictionary<string, AssemblyDefinition>(StringComparer.Ordinal);
            foreach (string path in Directory.GetFiles(Path.Combine(Application.dataPath, "Scripts"),
                         "*.asmdef", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
            {
                AssemblyDefinition definition = ParseDefinition(File.ReadAllText(path), path);
                if (definitions.ContainsKey(definition.name))
                    throw new InvalidDataException("Duplicate assembly name: " + definition.name);
                definitions.Add(definition.name, definition);
            }
            return definitions;
        }

        internal static string ResolveReference(string reference)
        {
            if (!reference.StartsWith("GUID:", StringComparison.Ordinal)) return reference;
            string path = AssetDatabase.GUIDToAssetPath(reference.Substring(5));
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new InvalidDataException("Unresolved assembly GUID reference: " + reference);
            return ParseDefinition(File.ReadAllText(path), path).name;
        }

        internal static Dictionary<string, string[]> BuildGraph(Dictionary<string, AssemblyDefinition> definitions)
        {
            return definitions.ToDictionary(pair => pair.Key,
                pair => pair.Value.references.Select(ResolveReference).ToArray(), StringComparer.Ordinal);
        }

        internal static string FindCycle(Dictionary<string, string[]> graph)
        {
            var finished = new HashSet<string>(StringComparer.Ordinal);
            var active = new List<string>();
            foreach (string assembly in graph.Keys.OrderBy(name => name, StringComparer.Ordinal))
            {
                string cycle = Visit(assembly, graph, finished, active);
                if (cycle != null) return cycle;
            }
            return null;
        }

        private static string Visit(string assembly, Dictionary<string, string[]> graph,
            HashSet<string> finished, List<string> active)
        {
            int activeIndex = active.IndexOf(assembly);
            if (activeIndex >= 0) return string.Join(" -> ", active.Skip(activeIndex).Concat(new[] { assembly }));
            if (finished.Contains(assembly) || !graph.ContainsKey(assembly)) return null;
            active.Add(assembly);
            foreach (string reference in graph[assembly])
            {
                string cycle = Visit(reference, graph, finished, active);
                if (cycle != null) return cycle;
            }
            active.RemoveAt(active.Count - 1);
            finished.Add(assembly);
            return null;
        }

        internal static string MethodKey(MethodBase method)
        {
            return method.DeclaringType.FullName + "::" + method.Name + "(" +
                   string.Join(",", method.GetParameters().Select(parameter => TypeKey(parameter.ParameterType))) + ")";
        }

        private static string TypeKey(Type type)
        {
            if (type.IsByRef) return TypeKey(type.GetElementType()) + "&";
            if (type.IsArray) return TypeKey(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            if (type.IsGenericType)
                return type.GetGenericTypeDefinition().FullName + "<" +
                       string.Join(",", type.GetGenericArguments().Select(TypeKey)) + ">";
            return type.FullName ?? type.Name;
        }

        private static MethodBase SourceMethod(MethodBase method)
        {
            Type generatedType = method.DeclaringType;
            Type parent = generatedType.DeclaringType;
            if (parent == null) return method;
            int end = method.Name.IndexOf('>');
            string authoredName = method.Name.StartsWith("<", StringComparison.Ordinal) && end > 1
                ? method.Name.Substring(1, end - 1)
                : null;
            for (Type owner = parent; owner != null; owner = owner.DeclaringType)
            {
                foreach (MethodInfo candidate in owner.GetMethods(BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(candidate => authoredName == null || candidate.Name == authoredName))
                {
                    StateMachineAttribute stateMachine = candidate.GetCustomAttributes<StateMachineAttribute>(false)
                        .FirstOrDefault(attribute => attribute.StateMachineType == generatedType);
                    if (stateMachine != null) return candidate;
                    if (authoredName != null && candidate.DeclaringType == owner) return candidate;
                }
            }
            return method;
        }

        internal static IEnumerable<ArchitectureCall> ReadCalls(MethodBase method)
        {
            MethodBody body = method.GetMethodBody();
            if (body == null) yield break;
            byte[] bytes = body.GetILAsByteArray();
            int position = 0;
            Type[] typeArguments = method.DeclaringType.IsGenericType ? method.DeclaringType.GetGenericArguments() : null;
            Type[] methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;
            while (position < bytes.Length)
            {
                int offset = position;
                short value = bytes[position++];
                if (value == 0xfe) value = (short)(0xfe00 | bytes[position++]);
                if (!Opcodes.TryGetValue(value, out OpCode opcode))
                    throw new InvalidDataException("Unknown IL opcode in " + MethodKey(method) + " at " + offset);
                MemberInfo target = null;
                if (opcode.OperandType == OperandType.InlineMethod)
                    target = method.Module.ResolveMethod(BitConverter.ToInt32(bytes, position), typeArguments, methodArguments);
                else if (opcode.OperandType == OperandType.InlineField)
                    target = method.Module.ResolveField(BitConverter.ToInt32(bytes, position), typeArguments, methodArguments);
                position += OperandSize(opcode.OperandType, bytes, position);
                string rule = Classify(target);
                if (rule != null)
                    yield return new ArchitectureCall
                    {
                        Caller = MethodKey(SourceMethod(method)),
                        Target = target.DeclaringType.FullName + "::" + target.Name,
                        Rule = rule,
                        Offset = offset
                    };
            }
        }

        private static int OperandSize(OperandType operand, byte[] bytes, int position)
        {
            switch (operand)
            {
                case OperandType.InlineNone: return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar: return 1;
                case OperandType.InlineVar: return 2;
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR: return 4;
                case OperandType.InlineI8:
                case OperandType.InlineR: return 8;
                case OperandType.InlineSwitch: return 4 + 4 * BitConverter.ToInt32(bytes, position);
                default: throw new InvalidDataException("Unsupported IL operand: " + operand);
            }
        }

        internal static string Classify(MemberInfo target)
        {
            if (target?.DeclaringType == null) return null;
            string type = target.DeclaringType.FullName;
            string member = target.Name;
            if (target.DeclaringType.Assembly.GetName().Name.StartsWith("Zephyr.", StringComparison.Ordinal) &&
                (member == "get_Instance" || (target is FieldInfo field && field.IsStatic && member == "Instance")))
                return "Singleton";
            if (type == "UnityEngine.SceneManagement.SceneManager" &&
                (member.StartsWith("LoadScene", StringComparison.Ordinal) ||
                 member.StartsWith("UnloadScene", StringComparison.Ordinal))) return "SceneLoad";
            if (type == "UnityEngine.Object" &&
                (member.StartsWith("FindObject", StringComparison.Ordinal) ||
                 member.StartsWith("FindAnyObject", StringComparison.Ordinal) ||
                 member.StartsWith("FindFirstObject", StringComparison.Ordinal))) return "SceneScan";
            if (type == "UnityEngine.GameObject" && (member == "Find" || member.Contains("WithTag"))) return "GlobalLookup";
            if (type == "UnityEngine.Resources" && member.StartsWith("Load", StringComparison.Ordinal)) return "ResourceLoad";
            if (type == "UnityEngine.Object" && member == "DontDestroyOnLoad") return "PersistenceOverride";
            return null;
        }

        internal static List<ArchitectureCall> InspectRuntime()
        {
            var definitions = ReadDefinitions();
            ArchitecturePolicy policy = ReadPolicy();
            var names = new HashSet<string>(definitions.Values.Where(definition =>
                    !definition.EditorOnly && !definition.IsTest && !policy.diagnosticAssemblies.Contains(definition.name))
                .Select(definition => definition.name), StringComparer.Ordinal);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly =>
                    names.Contains(assembly.GetName().Name)).ToDictionary(assembly => assembly.GetName().Name);
            if (!names.SetEquals(assemblies.Keys))
                throw new InvalidOperationException("Runtime assemblies are not all loaded: " +
                    string.Join(", ", names.Except(assemblies.Keys)));
            var calls = new List<ArchitectureCall>();
            foreach (Assembly assembly in assemblies.Values)
            foreach (Type type in assembly.GetTypes())
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                           BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                foreach (MethodBase method in type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags)))
                    calls.AddRange(ReadCalls(method));
                if (type.TypeInitializer != null && !type.GetConstructors(flags).Contains(type.TypeInitializer))
                    calls.AddRange(ReadCalls(type.TypeInitializer));
            }
            return calls;
        }

        public static string DescribeCalls()
        {
            return string.Join("\n", InspectRuntime().GroupBy(call => call.Key)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => group.Key + " count=" + group.Count()));
        }

        internal static List<string> ValidateCalls(IEnumerable<ArchitectureCall> calls, IEnumerable<CallAllowance> allowances)
        {
            var budgets = allowances.ToDictionary(allowance =>
                allowance.caller + " -> " + allowance.target + " [" + allowance.rule + "]", StringComparer.Ordinal);
            var failures = new List<string>();
            foreach (var group in calls.GroupBy(call => call.Key))
            {
                if (!budgets.TryGetValue(group.Key, out CallAllowance budget))
                    failures.Add("Unapproved call: " + group.Key + " IL=" + string.Join(",", group.Select(call => call.Offset)));
                else if (group.Count() > budget.maxCount)
                    failures.Add("Call budget exceeded: " + group.Key + " expected <= " + budget.maxCount + ", actual=" + group.Count());
            }
            return failures;
        }
    }
}
