/*
 * InitOnlyAttribute.cs
 * --------------------
 * Module:  Core / StateMachine / Utilities
 * Purpose: Custom property attribute that marks serialized fields as initialization-only.
 *          When the StateMachine is in Play mode, changes to these fields in the inspector
 *          won't be reflected on existing StateMachines (they were already instantiated from
 *          the pre-play data). Works in conjunction with InitOnlyAttributeDrawer to display
 *          a warning message in the editor during Play mode.
 * Dependencies: System, UnityEngine (PropertyAttribute).
 * Scene:    N/A (editor-only; controls inspector display behavior).
 * Ch.Ref:   Ch.6 State Machine Architecture (editor authoring tool).
 */
using System;
using UnityEngine;

namespace Zephyr.Core.StateMachine
{
	[AttributeUsage(AttributeTargets.Field)]
	public class InitOnlyAttribute : PropertyAttribute { }
}
