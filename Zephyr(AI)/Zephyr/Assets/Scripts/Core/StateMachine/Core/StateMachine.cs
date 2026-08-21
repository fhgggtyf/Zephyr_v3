/*
 * StateMachine.cs
 * ---------------
 * Module:  Core / StateMachine / Core
 * Purpose: Central MonoBehaviour-based state machine controller. Manages current/previous
 *          state, evaluates transitions every frame in Update(), and performs state
 *          transitions (OnStateExit → OnStateEnter). Caches component references to avoid
 *          repeated GetComponent calls — provides TryGetCachedComponent<T>() and
 *          GetCachedComponent<T>() methods (named differently from Unity's methods to
 *          avoid shadowing). Hosts the StateMachineDebugger for editor-only transition
 *          logging. Initialized from a TransitionTableSO at runtime.
 *
 * ==== FOUNDATION MODIFICATION (ONE-TIME EXCEPTION, ALREADY MERGED) ====
 * A one-time approved exception changed TryGetCachedComponent<T>() from base.TryGetComponent<T>()
 * to GetComponentInChildren<T>(true). This allows actor-logic components to live on child
 * GameObjects (e.g., a "Core" child holding MovementCore, HurtReceiver, etc.) while the
 * StateMachine itself lives on the root. This change is already applied — the StateMachine
 * foundation remains fully locked; no further modifications are permitted.
 * Components are still cached once at Awake time, so there is no runtime cost difference.
 * GetComponentInChildren uses depth-first search; if multiple components of the same type
 * exist in the hierarchy, only the first match is cached (Red Line: unique types only).
 *
 * ==== CHILD-HIERARCHY PATTERN ====
 * Root:    StateMachine + StatsCore (core shared component) + Rigidbody2D + Colliders
 * Core:    MovementCore, HurtReceiver, StaminaResource, EnergyResource,
 *          PlayerInputReader, PlayerInteractor
 * Sprite:  SpriteRenderer + Animator
 * WeaponSocket: Empty Transform for runtime weapon attachment
 *
 * Dependencies: TransitionTableSO (ScriptableObject data source), State, StateTransition,
 *               StateAction, StateCondition (runtime types).
 * Scene:    GameManager (attached to player, enemies, and any entity with state behavior).
 * Ch.Ref:   Ch.6 State Machine Architecture.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.StateMachine
{
    public class StateMachine : MonoBehaviour
    {
        [Tooltip("Set the initial state of this StateMachine")]
        [SerializeField] private ScriptableObjects.TransitionTableSO _transitionTableSO = default;

#if UNITY_EDITOR
        [Space]
        [SerializeField]
        internal Debugging.StateMachineDebugger _debugger = default;
#endif

        private readonly Dictionary<Type, Component> _cachedComponents = new Dictionary<Type, Component>();
        private bool _isInitialized;
        private bool _hasStarted;
        private bool _isStateEntered;
        internal State _currentState;
        internal State _previousState;

        public State GetPreviousState() {
            return _previousState;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
#endif

            if (!_isInitialized)
            {
                Initialize();
            }

            if (_hasStarted && _isInitialized && !_isStateEntered)
            {
                EnterCurrentState();
            }
        }

        private void OnDisable()
        {
            if (_isStateEntered && _currentState != null)
            {
                _currentState.OnStateExit();
                _isStateEntered = false;
            }

#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
#endif
        }

#if UNITY_EDITOR
        private void OnAfterAssemblyReload()
        {
            _isInitialized = false;
            _isStateEntered = false;
            Initialize();

            if (_hasStarted && _isInitialized && isActiveAndEnabled)
            {
                EnterCurrentState();
            }
        }
#endif

        private void Start()
        {
            _hasStarted = true;
            if (!_isInitialized || _isStateEntered) return;

            EnterCurrentState();
        }

        /// <summary>
        /// Cached component lookup. Searches root and all children (depth-first),
        /// including inactive GameObjects. Returns false when the component is not present
        /// anywhere in the actor hierarchy.
        /// Intentionally NOT named <c>TryGetComponent</c> to avoid shadowing Unity's base
        /// method (shadowing dispatches by static type and would silently bypass the cache
        /// when the StateMachine is referenced as a <c>Component</c>/<c>MonoBehaviour</c>).
        /// </summary>
        public bool TryGetCachedComponent<T>(out T component) where T : Component
        {
            var type = typeof(T);
            if (!_cachedComponents.TryGetValue(type, out var value))
            {
                component = GetComponentInChildren<T>(true);
                if (component != null)
                    _cachedComponents.Add(type, component);

                return component != null;
            }

            component = (T)value;
            return true;
        }

        public T GetOrAddComponent<T>() where T : Component
        {
            if (!TryGetCachedComponent<T>(out var component))
            {
                component = gameObject.AddComponent<T>();
                _cachedComponents.Add(typeof(T), component);
            }

            return component;
        }

        /// <summary>
        /// Cached component lookup. Throws when the component is not present.
        /// Intentionally NOT named <c>GetComponent</c> to avoid shadowing Unity's base
        /// method (which returns null on miss rather than throwing).
        /// </summary>
        public T GetCachedComponent<T>() where T : Component
        {
            return TryGetCachedComponent(out T component)
                ? component : throw new InvalidOperationException($"{typeof(T).Name} not found in {name}.");
        }

        private void Initialize()
        {
            if (_transitionTableSO == null)
            {
                Debug.LogError($"[{GetType().Name}] Transition table is not assigned.", this);
                _isInitialized = false;
                enabled = false;
                return;
            }

            try
            {
                var initialState = _transitionTableSO.GetInitialState(this);
                _currentState = initialState;
                _previousState = initialState;
                _isInitialized = true;
            }
            catch (Exception exception)
            {
                _currentState = null;
                _previousState = null;
                _isInitialized = false;
                Debug.LogError($"[{GetType().Name}] Failed to initialize: {exception.Message}", this);
                enabled = false;
                return;
            }

#if UNITY_EDITOR
            _debugger ??= new Debugging.StateMachineDebugger();
            _debugger.Awake(this);
#endif
        }

        private void EnterCurrentState()
        {
            if (_currentState == null) return;

            _currentState.OnStateEnter();
            _isStateEntered = true;
        }

        private void Update()
        {
            if (!_isInitialized || !_isStateEntered || _currentState == null) return;

            if (_currentState.TryGetTransition(out var transitionState))
                Transition(transitionState);

            _currentState.OnUpdate();
        }

        private void Transition(State transitionState)
        {
            var fromName = _currentState != null ? _currentState.GetType().Name : "null";
            var toName = transitionState != null ? transitionState.GetType().Name : "null";
            Debug.Log($"[StateMachine] Transition: {fromName} → {toName}");
            _currentState.OnStateExit();
            _previousState = _currentState;
            _currentState = transitionState;
            _currentState.OnStateEnter();
        }
    }
}
