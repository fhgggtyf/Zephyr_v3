using System;
using UnityEngine;
using Zephyr.Core;
using Zephyr.Gameplay.Player.UI;

namespace Zephyr.Gameplay.Player.Flow
{
    public sealed class DeathPresentationController : IDisposable
    {
        private readonly DeathView _view;
        private readonly Action _respawn;
        private readonly Action _giveUp;

        public DeathPresentationController(DeathView prefab, Transform parent, Action respawn, Action giveUp)
        {
            if (prefab == null) return;
            _respawn = respawn;
            _giveUp = giveUp;
            _view = UnityEngine.Object.Instantiate(prefab, parent);
            _view.RespawnRequested += _respawn;
            _view.GiveUpRequested += _giveUp;
            _view.Hide();
        }

        public void Show(StatType target, float before, float loss, bool canRespawn) => _view?.Show(target, before, loss, canRespawn);

        public void Hide() => _view?.Hide();

        public void Dispose()
        {
            if (_view == null) return;
            _view.RespawnRequested -= _respawn;
            _view.GiveUpRequested -= _giveUp;
        }
    }
}
