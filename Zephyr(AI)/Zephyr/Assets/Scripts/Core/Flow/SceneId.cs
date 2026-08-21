/*
 * SceneId.cs
 * ----------
 * Module:  Core / GameFlow
 * Purpose: Enumerates all scene identifiers in the Zephyr project. Maps directly to scene
 *          file names in Assets/Scenes/. Includes: Initializer (boot loader), Persistent
 *          (infrastructure, never unloaded), GameManager (gameplay systems, loaded for
 *          Tutorial/MetaHub/InRun), MainMenu, Tutorial, MetaHub, LoadingRun, Room_Generic
 *          (procedural rooms), Room_Boss (boss encounter rooms). Used by SceneLoader and
 *          GameFlow for scene transition management.
 * Dependencies: None (pure enum).
 * Scene:    N/A (compilation-only; used at runtime by SceneLoader).
 * Ch.Ref:   Ch.14.2 Scene Architecture, Ch.14.4 Scene Loading Lifecycle.
 */
namespace Zephyr.Core.Flow
{
    /// <summary>
    /// Scene identifiers. Maps to scene files in Assets/Scenes/.
    /// </summary>
    public enum SceneId
    {
        Initializer,
        Persistent,
        GameManager,
        MainMenu,
        Tutorial,
        MetaHub,
        LoadingRun,
        Room_Generic,
        Room_Boss
    }
}
