using System;
using UnityEngine;
using Zephyr.Contracts.Interfaces;
using Zephyr.Core.Save;

namespace Zephyr.Core.Flow.Scenes
{
public sealed class TutorialController : MonoBehaviour, IGameManagerDependencyConsumer, ITutorialCompletionGateway
    {
        public event Action OnTutorialComplete;
        private ISaveDataGateway _saveData;

        public void Configure(IGameManagerServices services)
        {
            _saveData = services?.SaveData;
        }

        public void CompleteTutorial()
        {
            MarkCompleted();
            OnTutorialComplete?.Invoke();
        }

        public void MarkCompleted()
        {
            if (_saveData?.Meta == null)
            {
                _saveData?.LoadMeta();
            }

            if (_saveData?.Meta != null)
            {
                _saveData.Meta.HasCompletedTutorial = true;
                _saveData.SaveMeta();
            }
        }
    }
}
