using UnityEngine;
using UnityEngine.UI;

namespace Zephyr.Gameplay.Player.UI
{
    public enum PlayerStatusValueKind { Core, Potential, Meta, Resource, Category }

    public sealed class PlayerStatusValueRow : MonoBehaviour
    {
        [SerializeField] private PlayerStatusValueKind _kind;
        [SerializeField] private string _key;
        [SerializeField] private Text _label;
        [SerializeField] private Text _value;

        public PlayerStatusValueKind Kind => _kind;
        public string Key => _key;

        public void Set(string label, string value)
        {
            if (_label != null) _label.text = label;
            if (_value != null) _value.text = value;
        }
    }
}
