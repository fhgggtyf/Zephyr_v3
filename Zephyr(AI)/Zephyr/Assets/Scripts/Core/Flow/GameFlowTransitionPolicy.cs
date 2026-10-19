using Zephyr.Contracts.Flow;

namespace Zephyr.Core.Flow
{
    public static class GameFlowTransitionPolicy
    {
        public static bool IsValidTransition(GameState from, GameState to, bool hasCompletedTutorial)
        {
            if (to == GameState.MainMenu) return from != GameState.MainMenu;

            switch (from)
            {
                case GameState.Boot: return to == GameState.MainMenu;
                case GameState.MainMenu:
                    return (to == GameState.Tutorial && !hasCompletedTutorial) ||
                           to == GameState.MetaHub || to == GameState.LoadingRun;
                case GameState.Tutorial: return to == GameState.MetaHub;
                case GameState.MetaHub: return to == GameState.LoadingRun || to == GameState.InRun;
                case GameState.LoadingRun: return to == GameState.InRun || to == GameState.MetaHub;
                case GameState.InRun: return to == GameState.Paused || to == GameState.MetaHub;
                case GameState.Paused: return to == GameState.InRun || to == GameState.MetaHub;
                default: return false;
            }
        }

        public static bool IsDirectOpenState(GameState state)
        {
            return state == GameState.MainMenu || state == GameState.Tutorial ||
                   state == GameState.MetaHub || state == GameState.LoadingRun ||
                   state == GameState.InRun;
        }

        public static bool IsRunScene(SceneId sceneId)
        {
            return sceneId == SceneId.Room_Generic || sceneId == SceneId.Room_Boss ||
                   sceneId == SceneId.LV1Placeholder || sceneId == SceneId.LV2Placeholder ||
                   sceneId == SceneId.LV3Placeholder;
        }

        public static SceneSO SceneForState(GameState state, SceneSO mainMenu, SceneSO tutorial,
            SceneSO metaHub, SceneSO loadingRun)
        {
            switch (state)
            {
                case GameState.MainMenu: return mainMenu;
                case GameState.Tutorial: return tutorial;
                case GameState.MetaHub: return metaHub;
                case GameState.LoadingRun: return loadingRun;
                default: return null;
            }
        }
    }
}
