using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Zephyr.Core.Meta;
using Zephyr.Gameplay.Interaction;

namespace Zephyr.Gameplay.Testing.Editor
{
    public static class MetaHubProgressionAssetBuilder
    {
        [MenuItem("Zephyr/Build MetaHub Progression Assets")]
        public static void Build()
        {
            EnsureFolder("Assets/ScriptableObjects");
            EnsureFolder("Assets/ScriptableObjects/MetaUpgrades");
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Core");
            EnsureFolder("Assets/Prefabs/Core/Progression");
            MetaUpgradeSO budget = GetOrCreateUpgrade("Assets/ScriptableObjects/MetaUpgrades/PotentialBudgetPlus2.asset", "potential_budget_plus_2", MetaUpgradeTarget.PotentialBudget, 2f, "meta.upgrades.budget", "meta.upgrades.budget.description");
            MetaUpgradeSO alloc = GetOrCreateUpgrade("Assets/ScriptableObjects/MetaUpgrades/PotentialAllocatablePlus1.asset", "potential_allocatable_plus_1", MetaUpgradeTarget.PotentialAllocatable, 1f, "meta.upgrades.allocatable", "meta.upgrades.allocatable.description");
            GameObject upgradePanel = GetOrCreatePanel("Assets/Prefabs/Core/Progression/MetaUpgradePanel.prefab", false);
            GameObject potentialPanel = GetOrCreatePanel("Assets/Prefabs/Core/Progression/PotentialAllocationPanel.prefab", true);
            Sprite upgradeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtResources/Placeholders/Interaction/MetaUpgradeStation_3x3_192x192.png");
            Sprite runSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtResources/Placeholders/Interaction/RunStartInteractable_2x4_128x256.png");
            GetOrCreateStation("Assets/Prefabs/Core/Progression/MetaUpgradeStation.prefab", "MetaUpgradeStation", upgradePanel, upgradeSprite, new Object[] { budget, alloc }, new Vector2(3f, 3f), true);
            GetOrCreateStation("Assets/Prefabs/Core/Progression/RunStartInteractable.prefab", "RunStartInteractable", potentialPanel, runSprite, new Object[0], new Vector2(2f, 4f), false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MetaHub progression assets built.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string name = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static MetaUpgradeSO GetOrCreateUpgrade(string path, string id, MetaUpgradeTarget target, float amount, string displayKey, string descriptionKey)
        {
            MetaUpgradeSO asset = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MetaUpgradeSO>();
                AssetDatabase.CreateAsset(asset, path);
            }
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayNameKey").stringValue = displayKey;
            so.FindProperty("_descriptionKey").stringValue = descriptionKey;
            so.FindProperty("_cost").longValue = 0;
            so.FindProperty("_target").enumValueIndex = (int)target;
            so.FindProperty("_targetKey").stringValue = "default";
            so.FindProperty("_amount").floatValue = amount;
            so.FindProperty("_repeatable").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static GameObject GetOrCreatePanel(string path, bool potential)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path), typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.layer = 5;
            root.SetActive(false);
            Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 500;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(960f, 540f); scaler.matchWidthOrHeight = 0.5f;
            if (potential) root.AddComponent<PotentialAllocationPanel>(); else root.AddComponent<MetaUpgradePanel>();
            Image backdrop = CreateImage("Backdrop", root.transform, new Color(0.02f, 0.04f, 0.08f, 0.82f), new Vector2(0.5f, 0.5f), new Vector2(960f, 540f), Vector2.zero);
            Image panel = CreateImage("Panel", root.transform, new Color(0.07f, 0.11f, 0.18f, 0.98f), new Vector2(0.5f, 0.5f), potential ? new Vector2(860f, 520f) : new Vector2(760f, 430f), Vector2.zero);
            TextObject("Title", panel.transform, 30, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(680f, 44f), new Vector2(0f, -32f));
            if (potential)
            {
                TextObject("Summary", panel.transform, 18, TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 1f), new Vector2(0.5f, 1f), new Vector2(700f, 32f), new Vector2(0f, -72f));
                TextObject("Remaining", panel.transform, 22, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.45f), new Vector2(0.5f, 1f), new Vector2(700f, 34f), new Vector2(0f, -104f));
                RectTransform rows = new GameObject("Rows", typeof(RectTransform)).GetComponent<RectTransform>(); rows.SetParent(panel.transform, false); Place(rows, new Vector2(0.5f, 1f), new Vector2(720f, 440f), new Vector2(0f, -124f));
                ButtonObject("StartButton", panel.transform, "Start Game", new Vector2(0.5f, 0f), new Vector2(220f, 44f), new Vector2(140f, 28f));
                ButtonObject("CancelButton", panel.transform, "Cancel", new Vector2(0.5f, 0f), new Vector2(160f, 44f), new Vector2(-140f, 28f));
            }
            else
            {
                TextObject("Currency", panel.transform, 20, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.45f), new Vector2(0.5f, 1f), new Vector2(680f, 34f), new Vector2(0f, -72f));
                CreateUpgradeRow(panel.transform, "BudgetRow", 70f, "Buy (+2) - 0");
                CreateUpgradeRow(panel.transform, "AllocatableRow", -35f, "Buy (+1) - 0");
                ButtonObject("CloseButton", panel.transform, "Close", new Vector2(0.5f, 0f), new Vector2(180f, 46f), new Vector2(0f, 28f));
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateUpgradeRow(Transform parent, string name, float y, string buttonLabel)
        {
            RectTransform row = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); row.SetParent(parent, false); Place(row, new Vector2(0.5f, 0.5f), new Vector2(680f, 90f), new Vector2(0f, y));
            TextObject("Value", row, 26, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 0.5f), new Vector2(110f, 42f), new Vector2(55f, 10f));
            TextObject("Description", row, 18, TextAnchor.MiddleLeft, Color.white, new Vector2(0.5f, 0.5f), new Vector2(300f, 42f), new Vector2(70f, 10f));
            ButtonObject("BuyButton", row, buttonLabel, new Vector2(1f, 0.5f), new Vector2(180f, 48f), new Vector2(-100f, 10f));
        }

        private static GameObject GetOrCreateStation(string path, string name, GameObject panel, Sprite sprite, Object[] upgrades, Vector2 colliderSize, bool isTrigger)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject root = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D));
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = name == "MetaUpgradeStation" ? new Color(0.18f, 0.45f, 1f, 1f) : new Color(1f, 0.65f, 0.08f, 1f); renderer.sortingOrder = 10;
            BoxCollider2D collider = root.GetComponent<BoxCollider2D>(); collider.size = colliderSize; collider.isTrigger = isTrigger;
            InteractableFocusVisual focus = root.AddComponent<InteractableFocusVisual>(); SerializedObject focusSo = new SerializedObject(focus); focusSo.FindProperty("_targetRenderer").objectReferenceValue = renderer; focusSo.FindProperty("_highlightColor").colorValue = Color.white; focusSo.FindProperty("_highlightScale").floatValue = 1.04f; focusSo.FindProperty("_sortingOrderOffset").intValue = 1; focusSo.ApplyModifiedPropertiesWithoutUndo();
            if (name == "MetaUpgradeStation")
            {
                MetaUpgradeStation component = root.AddComponent<MetaUpgradeStation>();
                SerializedObject so = new SerializedObject(component); so.FindProperty("_panelPrefab").objectReferenceValue = panel; SerializedProperty array = so.FindProperty("_upgrades"); array.arraySize = upgrades.Length; for (int i = 0; i < upgrades.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = upgrades[i]; so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                RunStartInteractable component = root.AddComponent<RunStartInteractable>(); SerializedObject so = new SerializedObject(component); so.FindProperty("_panelPrefab").objectReferenceValue = panel; so.ApplyModifiedPropertiesWithoutUndo();
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root); return prefab;
        }

        private static Image CreateImage(string name, Transform parent, Color color, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); Place((RectTransform)go.transform, anchor, size, pos); go.GetComponent<Image>().color = color; return go.GetComponent<Image>();
        }

        private static Text TextObject(string name, Transform parent, int size, TextAnchor alignment, Color color, Vector2 anchor, Vector2 dimensions, Vector2 position)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false); Place((RectTransform)go.transform, anchor, dimensions, position); Text text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.alignment = alignment; text.color = color; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false; return text;
        }

        private static Button ButtonObject(string name, Transform parent, string label, Vector2 anchor, Vector2 dimensions, Vector2 position)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false); Place((RectTransform)go.transform, anchor, dimensions, position); Image image = go.GetComponent<Image>(); image.color = new Color(0.18f, 0.28f, 0.36f, 1f); Button button = go.GetComponent<Button>(); button.targetGraphic = image; Text text = TextObject("Label", go.transform, 20, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 0.5f), new Vector2(dimensions.x - 16f, dimensions.y - 8f), Vector2.zero); text.text = label; return button;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = size; rect.anchoredPosition = position;
        }
    }
}
