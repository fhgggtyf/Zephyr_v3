/*
 * GameState.cs
 * ------------
 * Module:  Core / GameFlow
 * Purpose: Defines the high-level game states used by GameFlow to track the current scene
 *          and gameplay mode. Boot = initial pre-MainMenu state, MainMenu = title screen,
 *          Tutorial = one-time scripted tutorial, MetaHub = upgrade hub between runs,
 *          LoadingRun = transition scene before InRun, InRun = active dungeon gameplay,
 *          Paused = pause overlay during InRun. Valid transitions are defined in GameFlow.
 * Dependencies: None (pure enum).
 * Scene:    N/A (compilation-only; runtime value stored in GameFlow).
 * Ch.Ref:   Ch.14.3 State Transition Flow.
 */
namespace Zephyr.Core.Flow
{
    /// <summary>
    /// Game states. Boot = initial (before MainMenu), MainMenu = menu, Tutorial = one-time tutorial,
    /// MetaHub = upgrade hub, InRun = gameplay, Paused = pause overlay.
    /// </summary>
    public enum GameState
    {
        Boot,
        MainMenu,
        Tutorial,
        MetaHub,
        LoadingRun,
        InRun,
        Paused
    }
}
