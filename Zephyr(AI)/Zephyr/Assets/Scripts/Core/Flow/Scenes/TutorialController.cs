using System;
using UnityEngine;using Zephyr.Core.Save;

namespace Zephyr.Core.Flow.Scenes
{
public sealed class TutorialController : MonoBehaviour
    {
        public event Action OnTutorialComplete;

        public void CompleteTutorial()
        {
            var save = SaveSystem.Instance;
            if (save?.Meta == null)
            {
                save?.LoadMeta();
            }

            if (save?.Meta != null)
            {
                save.Meta.HasCompletedTutorial = true;
                save.SaveMeta();
            }

            // Scene exits own progression through SceneProgressionController.
            // This method only records completion and notifies listeners.
            OnTutorialComplete?.Invoke();
        }
    }
}
