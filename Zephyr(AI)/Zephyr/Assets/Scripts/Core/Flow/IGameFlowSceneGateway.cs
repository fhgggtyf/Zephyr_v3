using System;
using UnityEngine.SceneManagement;
using Zephyr.Contracts.Flow;
using Zephyr.Contracts.Interfaces;

namespace Zephyr.Core.Flow
{
    /// <summary>Scene-transition surface exposed to gameplay orchestrators.</summary>
    public interface IGameFlowSceneGateway : IGameFlowGateway
    {
        void RequestSceneLoad(SceneSO targetScene, SceneSO sceneToReplace = null,
            LoadSceneMode mode = LoadSceneMode.Additive);
        void RequestProgressionSceneTransition(SceneSO targetScene, SceneSO sceneToReplace,
            GameState nextState);
    }

    public interface ISceneLoadingGateway
    {
        bool IsLoading { get; }
        void RequireReadySignal();
        void SignalReady();
        event Action<SceneSO, SceneSO> SceneUnloading;
        event Action LoadingStarted;
        event Action LoadingCompleted;
        event Action LoadingFailed;
    }

    public readonly struct GameplayHudReady
    {
    }
}
