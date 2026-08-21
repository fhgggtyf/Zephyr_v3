using System.Collections.Generic;
using UnityEngine;

namespace Zephyr.Core.UI
{
    public abstract class UIPanel : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        internal void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open;
            gameObject.SetActive(open);
            if (open) OnOpen(); else OnClose();
        }
        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
        protected virtual void OnRefresh() { }
        public void Refresh() => OnRefresh();
    }

    /// <summary>Scene-local panel stack. Panels are authored as child objects or prefabs.</summary>
    public sealed class UIManager : MonoBehaviour
    {
        private readonly Stack<GameObject> _panelStack = new Stack<GameObject>();
        public int OpenPanelCount => _panelStack.Count;

        public void Open(GameObject panel)
        {
            if (panel == null) return;
            if (_panelStack.Count > 0)
            {
                var previous = _panelStack.Peek();
                previous.GetComponent<UIPanel>()?.SetOpen(false);
                previous.SetActive(false);
            }
            panel.SetActive(true);
            _panelStack.Push(panel);
            panel.GetComponent<UIPanel>()?.SetOpen(true);
            GameplayTimePause.SetModalOpen(this, true);
        }

        public void CloseTop()
        {
            if (_panelStack.Count == 0) return;
            var top = _panelStack.Pop();
            top.GetComponent<UIPanel>()?.SetOpen(false);
            top.SetActive(false);
            if (_panelStack.Count > 0)
            {
                var previous = _panelStack.Peek();
                previous.SetActive(true);
                previous.GetComponent<UIPanel>()?.SetOpen(true);
            }
            else
            {
                GameplayTimePause.SetModalOpen(this, false);
            }
        }

        public void Clear()
        {
            while (_panelStack.Count > 0)
            {
                var panel = _panelStack.Pop();
                panel.GetComponent<UIPanel>()?.SetOpen(false);
                panel.SetActive(false);
            }
            GameplayTimePause.SetModalOpen(this, false);
        }

        private void OnEnable()
        {
            if (_panelStack.Count > 0) GameplayTimePause.SetModalOpen(this, true);
        }

        private void OnDisable() => GameplayTimePause.SetModalOpen(this, false);
    }
}
