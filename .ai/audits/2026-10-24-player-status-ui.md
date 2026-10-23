# PlayerStatusPanel UI 审计（2026-10-24）

> 来源：DSH 会话 `session-6cdb53d3-31ec-46d4-b3af-e6e092068545`（标题：状态 UI 审计与布局修复）。
> 审计类型：Tier 0，只读，未改游戏文件；实施应走 Tier 2。
> 输入图：`Assets/MCPTest/Screenshots/2026_10_24_0_08_status_UI/input.png`
> 结果图：`Assets/MCPTest/Screenshots/2026_10_24_0_08_status_UI/results/`

## 原始需求

按 .ai 流程做一次 UI 审计，先不要改文件。



截图：D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_0_08_status_UI\input.png

目标：background过小，不能起到background的作用。alothai portrait被错误的放在了蒙版后面，应该被挪上来保证清晰。weapontooltip背景太小无法放下前面的显示文字。playerstatus栏左右应该分开，potential栏的输入是不需要scrollbar的，所以在potential栏时scrollbar应该disable。而scrollbar本身也应该是scrollbar底端碰到后面的滑动的地方的底边的时候就得停下而不是视觉上滑出去了。



要求：

1. 先读 AGENTS.md、.ai/README.md、.ai/roles/orchestrator.md

2. 用 read_image 看输入图；读相关 Prefab/Controller/Localization

3. 结果图写进 ...\2026_10_24_0_08_status_UI\results\

4. 输出 Tier 0 审计 spec：改哪些文件、锚点/尺寸/间距/Key、960x540 验收

5. 不要改文件、不要 commit

---

# PlayerStatusPanel · Tier 0 UI 审计（只读，未改文件）

## 0. 结论

- **本单只做 Tier 0 审计**：读了 `AGENTS.md`、`.ai/README.md`、`.ai/roles/orchestrator.md`、`checklists/tiers.md`、`checklists/unity-acceptance.md`、输入图、Prefab/Controller/Scene/Localization。**没有修改任何游戏文件，没有 commit。**
- **实施应定 Tier 2**：要动 `.prefab` + `.unity` + 3 个 UI 脚本 + 中英文本地化 JSON + `960×540` 验收，命中“Prefab/场景/本地化/UI 视觉”风险触发器。本 spec 可直接作为 Tier 2 派单依据。
- 6 个问题的根因全部定位到具体节点和数值。两个容易看错的地方先说：
  1. **右侧属性区空白不是没数据**：`GameManager.unity` 对 `Content.m_AnchoredPosition.y` 存了 **820** 的场景覆盖，等于打开就滚到底（Potential 行全在视口上方）。
  2. **Tooltip 的 `Background` 是最后一个 sibling**（`Name | Icon | Details | Background`），它不但在“后面”，实际画在**文字上面**；只把尺寸改大而不改 sibling 顺序，依然会盖字。
- 本轮 preflight 快照：`D:\GitHub\Zephyr_v3\.ai\snapshots\20261024-002434-audit-status-ui-20261024`；branch `main @ 90fa416`；Unity `6000.5.6f1`；旧 MCP 8080 在线。
- Unity Console（旧 MCP `Unity_GetConsoleLogs`）：**Error 0 / Warning 0**（审计期间没有编译或资源错误）。

结果图（3 张，已写入 `results/`）：

- [findings.png](<D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_0_08_status_UI\results\20261024_003500_audit_PlayerStatusPanel_findings.png>) — 输入图 + 6 项标注 + 现状→目标
- [target-960x540.png](<D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_0_08_status_UI\results\20261024_003500_audit_PlayerStatusPanel_target-960x540.png>) — 目标布局
- [tooltip.png](<D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_0_08_status_UI\results\20261024_003500_audit_PlayerStatusPanel_tooltip.png>) — Tooltip 前后对比

![findings](<D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_0_08_status_UI\results\20261024_003500_audit_PlayerStatusPanel_findings.png>)

---

## 1. 证据与读取范围

### 1.1 读了什么

| 类型 | 文件 | 关键点 |
|---|---|---|
| Prefab | [PlayerStatusPanel.prefab](<Zephyr(AI)/Zephyr/Assets/Prefabs/UIPrefabs/Gameplay/PlayerStatusPanel.prefab>) | 820×450 Window、100×100 Background、Portrait、StatsViewport、Scrollbar、WeaponTooltip |
| 场景 | [GameManager.unity](<Zephyr(AI)/Zephyr/Assets/Scenes/Core/GameManager.unity#L792>) | PrefabInstance `76558699`：Portrait `PreserveAspect=1`、**Content.y=820**、root pivot/anchors 0 |
| 控制器 | [PlayerStatusPanelController.cs](<Zephyr(AI)/Zephyr/Assets/Scripts/Player/UI/PlayerStatusPanelController.cs#L131>) | `SetContentVisibility` 只切行，不处理 ScrollRect/Scrollbar；`Refresh` 用原始 Key |
| 行 | [PlayerStatusValueRow.cs](<Zephyr(AI)/Zephyr/Assets/Scripts/Player/UI/PlayerStatusValueRow.cs>) | 只有 `_kind/_key/_label/_value`；`Set(label,value)` 不本地化 |
| 武器槽 | [WeaponSlotHover.cs](<Zephyr(AI)/Zephyr/Assets/Scripts/Player/UI/WeaponSlotHover.cs#L23>) | 用 `weapon.DisplayName`，未用 `DisplayNameLocalizationKey` |
| Tooltip | [WeaponStatsTooltipView.cs](<Zephyr(AI)/Zephyr/Assets/Scripts/Player/UI/WeaponStatsTooltipView.cs#L45>) | Details 硬编码英文 5 行；无背景尺寸逻辑 |
| 本地化 | [strings.en_US.json](<Zephyr(AI)/Zephyr/Assets/Resources/Localization/strings.en_US.json>)、[strings.zh_CN.json](<Zephyr(AI)/Zephyr/Assets/Resources/Localization/strings.zh_CN.json>) | 69 个 key；`common.close`、10 个 `stat.*`、`ui.hud.*` 已有；**无 `ui.player_status.*`、无 `ui.weapon.tooltip.*`** |
| 边界 | [LocalizedBehaviour.cs](<Zephyr(AI)/Zephyr/Assets/Scripts/Core/Localization/LocalizedBehaviour.cs>)、[IGameManagerDependencyConsumer.cs](<Zephyr(AI)/Zephyr/Assets/Scripts/Core/Flow/Bootstrap/IGameManagerDependencyConsumer.cs>) | `IGameManagerServices.Localization` 可用；UI 目录禁止直接 `LocalizationManager.Instance` |
| 参考 | [GameplayHudView.prefab](<Zephyr(AI)/Zephyr/Assets/Prefabs/UIPrefabs/Gameplay/GameplayHudView.prefab>)、[WeaponReplacementPanel.prefab](<Zephyr(AI)/Zephyr/Assets/Prefabs/UIPrefabs/Gameplay/WeaponReplacementPanel.prefab>) | HUD sortingOrder 100、status 200、weapon replacement 500；HUD 左上 260×122 |

### 1.2 运行时实测（旧 MCP，只读）

- `PlayerStatusPanel` Canvas：`ScreenSpaceOverlay`，pixelRect **2560×1440**，scaleFactor **2.6667**，sortingOrder **200**。
- `StatsScrollbar/Handle` 运行时：`rect = 24 × 141.27`（`sizeDelta=(12,70)` + anchor 高度 `0.2545×280=71.27`），轨道 `12×280`，**左右各溢出 6、上下溢出 70**。
- `Portrait` 父链 `Window/LeftHalf` **只有 RectTransform**；全 Prefab 唯一的 `Mask` 在 `StatsViewport` 上。所以“被放在蒙版后面”不是层级遮挡，而是 **`Image.color=(0.16,0.2,0.28)` 把 152×152 的 sprite 乘暗**，看起来像蒙了一层。
- `GameManager.unity` 对 `Content`（fileID `6830896662843507431`）覆盖了 `m_AnchoredPosition.y = 820`，正好等于 `1100-280`，即打开就在滚动底部。

> 注意：输入截图是 **非 16:9** 的 Game View（约 893×697），所以 HUD 的 EnergyBar 会压住 `Alothai`；在严格 960×540 下两者只差 0–1px。验收必须用 960×540 的 16:9 画面，不能用这张截图的绝对像素。

---

## 2. 六项发现（现状 → 目标）

### F1 · Background 过小，盖不住面板

- **节点**：`PlayerStatusPanel/Window/Background`，fileID `2293155335356247687`，Image。
- **现状**：anchors `(0.5,0.5)-(0.5,0.5)`，pos `(0,0)`，size **100×100**，pivot 中心，color `(0.055,0.075,0.1,0.98)`。截图中间那块 93×93 的深色方块就是它。
- **根因**：只有 100×100 有底色，Window 的 820×450 其余区域全透明，Dimmer 后的世界（棕色泥地）直接透出来。
- **一并修**：`StatsViewport` 的 Image 颜色 `(0.055,0.08,0.1,0.96)` 本身就是 Mask 的 graphic，但 `Mask.m_ShowMaskGraphic=0` → **列表区底板同样没画出来**。要么把 `m_ShowMaskGraphic` 改成 `1`，要么加 `ViewportBackdrop`（350×280，色 `(0.055,0.08,0.1,0.96)`，sibling 0）。
- **目标**：
  - anchors `(0,0)-(1,1)`，offsetMin/offsetMax `(0,0)`，sizeDelta `(0,0)`，pivot `(0.5,0.5)` → **820×450**；
  - color `(0.035,0.05,0.07,0.985)`（alpha ≥0.98，可沿用原色，但必须不透明到能挡住世界）；
  - sibling index **0**（在 LeftHalf/RightHalf 之前）；
  - 960×540 屏幕 rect：**(70,45)-(890,495)**。
  - `StatsViewport` 的 `Mask.m_ShowMaskGraphic=1`（或新增 `ViewportBackdrop` 350×280，sibling 0）。

### F2 · Alothai 肖像发暗 / 像在蒙版后

- **节点**：`Window/LeftHalf/Portrait`，fileID `6693911820338853831`，Image。
- **证据**：sprite `AlothaiPortrait.png` 152×152 / PPU 32 / pivot 0.5；场景覆盖 `m_PreserveAspect=1`；`Image.color=(0.16,0.2,0.28,1)`。父链无 Mask（`Window`、`LeftHalf` 只有 RectTransform）。
- **结论**：不是“被蒙版挡住”，是 **Image tint 把整张肖像乘暗**（白色头发变灰蓝、彩虹裙变暗），加上 152×152 的方形 sprite 底边在 rect 底边被截断，视觉上像“被切在蒙版后”。
- **目标**：
  - `Image.color = (1,1,1,1)`；
  - `anchoredPosition.y = 45 → 54`（**+9**）：肖像 top `149` 正好贴住 `PlayerName` bottom `149`，bottom `-41` 到 `WeaponsTitle` top `-61` 留 **20**（原 11）；
  - `preserveAspect=1` 保留；rect 250×190 不变；
  - 960×540：肖像 **(150,121)-(400,311)**；`Alothai` **(125,89)-(425,121)**。
  - **不要**再把 `PlayerName` 上移：960×540 下 HUD EnergyBar 底边约在 y=89，上移会压到 HUD；非 16:9 截图里的 HUD 重叠是 aspect 问题（P2 建议开面板时隐藏 HUD）。

### F3 · WeaponTooltip 背景放不下前面的文字

- **节点**：`WeaponTooltip` fileID `4306935315940975990`；`Background` `5578121591464464982`；`Name` `61329226545695039`；`Icon` `2388229601214556057`；`Details` `3851513347736754239`。
- **现状**：panel **250×190**；`Background` **100×100**；sibling 顺序 `Name | Icon | Details | Background` → **Background 最后画，盖在 Details 文字上**；`Details` 190×80 / 字号 12 / `VerticalOverflow=Truncate`，5 行英文勉强、中文换行会截断。
- **目标**：
  - panel **280×210**（pivot 中心，`FollowPointer` 的 clamp 会自动适配，无需改算法）；
  - `Background`：anchors `(0,0)-(1,1)`，offsets 0，sizeDelta 0，**sibling index 0**；color `(0.03,0.04,0.06,0.98)`；
  - `Name`：**240×28 @ (0,82)**，字号 18；
  - `Icon`：**56×56 @ (-108,14)**；
  - `Details`：**200×120 @ (24,-38)**，字号 12，`HorizontalOverflow=Wrap`，**`VerticalOverflow=Overflow`**；
  - padding：Details 右边到 panel 右 16、底 7、Icon 与 Details 间隙 4；
  - 960×540 内 tooltip 到屏幕四边都被 clamp，不越界。
- 说明图：见 [tooltip.png](<D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_0_08_status_UI\results\20261024_003500_audit_PlayerStatusPanel_tooltip.png>)。

### F4 · Player Status 左右栏没有分开

- **现状**：`LeftHalf` 370×410 @ x=-205（window x -390..-20）；`RightHalf` 390×410 @ x=205（10..400）；中间 gap **30**；两者都没有 Image，没有分隔，和窗口背景糊成一片。
- **目标**：
  - `LeftHalf` 加 `LeftBackdrop`（first child，anchors 拉伸，offsets 0 → 370×410，color `(0.075,0.1,0.14,0.9)`）；
  - `RightHalf` 加 `RightBackdrop`（390×410，同色）；
  - `Window` 加 `Divider`：pos `x=-5`，size **2×410**，color `(0.2,0.34,0.48,0.9)`，sibling index 3（在 RightHalf 之后，保证不被盖）；
  - 960×540：Left **(90,65)-(460,475)**、Right **(490,65)-(880,475)**、Divider **x=475**（474..476），30px 沟清晰可见。
- **顺手修 Close**：现在 `CloseButton` y=-215，box 底边 **-229**，比 Window 底 -225 低 **4px**；目标 y **-207**，box `-221..-193`，960×540 屏幕 **(785,463)-(865,491)**，完全在窗口内。
- `Title` 仍居中在右栏 `(510,79)-(860,111)`；`Potential/Stats` tab 位置不变。

### F5 · Scrollbar handle 滑出轨道

- **节点**：`RightHalf/StatsScrollbar` `5671596917507538627`；`SlidingArea` `7443914088906140430`；`Handle` `7974757566827225025`。
- **现状**：`Handle.sizeDelta = (12,70)`；Scrollbar 运行时把 anchor 高度设成 `0.2545`，实际 `rect = 24 × 141.27`。轨道只有 12×280：
  - 宽度 24（左右各出 6）；
  - 高度 141.27（0.2545×280 + 70），value=0 时**底边越出轨道 70**，就是截图里那条浅蓝 handle 滑到面板底下的原因。
- **目标**：
  - `Handle.sizeDelta = (-4, 0)`（anchors x 0..1 → 宽 **8**，左右各留 2px）或 `(0,0)`（宽 12，贴满轨道）；**高度必须由 Scrollbar 驱动，不能带固定 70**；
  - anchors 序列化 `(0,0)-(1,1)`，pivot `(0.5,1)` 保留；
  - 轨道 `12×280`，960×540 屏幕 **(862,165)-(874,445)**；handle 在 value=1 时 **8×71.3**，top=165；value=0 时 bottom=445，**完全在轨道内**。
  - `Scrollbar` 的 `m_Value` 在打开时重置为 1（顶部），避免沿用上次滚动位置。

### F6 · Potential 栏空 + 不需要 scrollbar

- **场景 bug**：`GameManager.unity` PrefabInstance `76558699` 对 `Content`（`6830896662843507431`）覆盖 `m_AnchoredPosition.y = 820`。Content 高 1100、viewport 高 280，820 正好是最大滚动量 → 打开就在底部，Potential 行（y 103..-122）全在视口上方。
- **数据**：
  - Potential 10 行：y **103 → -122**，跨度 225 < viewport 280 → **不需要滚动条**；
  - Stats 41 行（kind 0/2/3/4）：y **103 → -922**，跨度 1025 → content 1100，需要滚动条；
  - 所有 51 行都是 authored 在同一 Content 里、不同 kind 共享槽位，**不要加 VerticalLayoutGroup**，会打乱两页共用的坐标。
- **目标**：
  - 用 Unity MCP **删除/还原**该场景覆盖（`PrefabUtility.RevertPropertyOverride`，不要手改 `.unity` YAML）；Content 回到 prefab 的 `(0,0)`；
  - `PlayerStatusPanelController` 增加 `_scrollRect` / `_scrollbar` 序列化引用；
  - `SetContentVisibility(true)`（Potential）：
    ```csharp
    _scrollRect.vertical = false;
    _scrollRect.verticalNormalizedPosition = 1f;
    if (_scrollbar != null) _scrollbar.gameObject.SetActive(false);
    ```
  - `SetContentVisibility(false)`（Stats）：`vertical = true; _scrollbar.gameObject.SetActive(true);`
  - `Open()` 里先 `ShowPotential()` 再 `Refresh()`，保证默认页无滚动条、从顶部开始；
  - 960×540：Potential 第 1 行 top 167、第 10 行 bottom 392，底部余 53；Stats 可滚到 `Legendary` 行，handle 到轨道底。

---

## 3. 改动文件清单（实施单）

| 文件 | 改什么 |
|---|---|
| [PlayerStatusPanel.prefab](<Zephyr(AI)/Zephyr/Assets/Prefabs/UIPrefabs/Gameplay/PlayerStatusPanel.prefab>) | F1 Background 820×450；F1b StatsViewport Mask showMaskGraphic=1；F2 Portrait color/y；F3 Tooltip 280×210 + Background sibling 0 + Name/Icon/Details；F4 左右底板/Divider/Close y=-207；F5 Handle sizeDelta；新增 `_scrollRect`/`_scrollbar` 序列化引用 |
| [GameManager.unity](<Zephyr(AI)/Zephyr/Assets/Scenes/Core/GameManager.unity#L792>) | 仅删除 Content 的 `m_AnchoredPosition.y=820` PrefabInstance 覆盖 |
| [PlayerStatusPanelController.cs](<Zephyr(AI)/Zephyr/Assets/Scripts/Player/UI/PlayerStatusPanelController.cs>) | 缓存 `services.Localization`；静态文案/行标签本地化；locale change 刷新；Potential/Stats 切换 scrollbar；Open 重置滚动；`Configure` 注入 tooltip/slot 的 localization |
| [WeaponStatsTooltipView.cs](<Zephyr(AI)/Zephyr/Assets/Scripts/Player/UI/WeaponStatsTooltipView.cs>) | `Configure(ILocalizationGateway)`；详情走 `ui.weapon.tooltip.stats`；武器名优先 `DisplayNameLocalizationKey`；locale change 时若可见则重建 |
| [WeaponSlotHover.cs](<Zephyr(AI)/Zephyr/Assets/Scripts/Player/UI/WeaponSlotHover.cs>) | `Configure(ILocalizationGateway)`；槽位名优先 `DisplayNameLocalizationKey` |
| [strings.en_US.json](<Zephyr(AI)/Zephyr/Assets/Resources/Localization/strings.en_US.json>) | 新增 5 个 UI key + 30 个 `stat.*` key + tooltip 格式 key |
| [strings.zh_CN.json](<Zephyr(AI)/Zephyr/Assets/Resources/Localization/strings.zh_CN.json>) | 与 en 完全同 key 集（架构测试强制） |

**不需要改** `PlayerStatusValueRow.cs`：控制器把本地化后的 label 传进 `row.Set(label, value)`。

---

## 4. 代码改动要点

### 4.1 `PlayerStatusPanelController`

新增字段：

```csharp
[SerializeField] private ScrollRect _scrollRect;
[SerializeField] private Scrollbar _scrollbar;
private ILocalizationGateway _localization;
```

`Configure`：

```csharp
_localization = services?.Localization;
if (_localization != null)
    _localization.OnLocaleChanged += HandleLocaleChanged;
_primaryWeapon?.Configure(_localization);
_secondaryWeapon?.Configure(_localization);
_tooltip?.Configure(_localization);
RefreshStaticText();
```

`OnDestroy` 退订 `OnLocaleChanged`；`HandleLocaleChanged` 里 `RefreshStaticText(); RefreshWeapons(); Refresh();`。

`RefreshStaticText`（只做一次，不要每帧）：Title / Potential tab / Stats tab / Close / WeaponsTitle 五个 `Text`。

`SetContentVisibility` 按 F6 增加滚动条逻辑。`Refresh` 里行标签改成：

```csharp
row.Set(_localization?.GetOrFallback("stat." + row.Key, row.Key) ?? row.Key, value);
```

同时把 `Refresh()` 从 `Update()` 的每帧调用改成“打开时 + `PlayerTargetChangedEvent` + locale change + tab 切换”时调用（P2：现在每帧 `ToString`/`string.Format` 会产生 GC）。

### 4.2 `WeaponStatsTooltipView`

- 增加 `ILocalizationGateway _localization; public void Configure(ILocalizationGateway l) => _localization = l;`
- `Show` 里武器名：

```csharp
_name.text = !string.IsNullOrWhiteSpace(weapon.DisplayNameLocalizationKey)
    ? (_localization?.GetOrFallback(weapon.DisplayNameLocalizationKey, weapon.DisplayName) ?? weapon.DisplayName)
    : weapon.DisplayName;
```

- `Details` 走格式化 key（9 个参数：flat damage、percent、speed、crit chance、crit multiplier、flat pen、percent pen、element、category），避免硬编码英文。
- 保存 `_currentWeapon`，locale change 时若 `gameObject.activeSelf` 重新 `Show(_currentWeapon)`。

### 4.3 `WeaponSlotHover`

`Configure(ILocalizationGateway)`，`SetWeapon` 用 `DisplayNameLocalizationKey` 解析显示名，空槽仍显示 `"-"`。不要在 `Player/UI` 里直接读 `LocalizationManager.Instance`（架构测试会拦）。

---

## 5. 本地化 Key

### 5.1 新增 UI key（en / zh-Hans）

| Key | en | zh-Hans |
|---|---|---|
| `ui.player_status.title` | Player Status | 玩家状态 |
| `ui.player_status.tab.potential` | Potential | 潜力 |
| `ui.player_status.tab.stats` | Stats | 属性 |
| `ui.player_status.weapons` | Weapons | 武器 |
| `ui.weapon.tooltip.stats` | `Damage {0} x{1}\nSpeed {2}\nCrit {3}  Crit Dmg {4}\nPen {5} / {6}\n{7} | {8}` | `伤害 {0} x{1}\n速度 {2}\n暴击 {3}  暴击伤害 {4}\n穿透 {5} / {6}\n{7} | {8}` |

`Close` 复用已有 `common.close`（en/zh 都有）。`Alothai` 作为专有名词暂保留 authored 文本；若要中文化，后续用角色表 key（Characters 表里已有 `Alothai`），不在本单阻塞项。

### 5.2 补齐 30 个 `stat.*` key

现有 10 个：`Attack / MagicAttack / Armor / MagicResist / MaxHp / Stamina / Luck / Tenacity / Energy / AttackSpeed`。缺的 30 个（en 值取自然英文，zh 值如下）：

```text
stat.CurrentHp          当前生命        stat.CurrentStamina    当前体力
stat.MaxStamina         最大体力        stat.CurrentEnergy     当前能量
stat.MaxEnergy          最大能量
stat.CritChance         暴击率          stat.CritDamage        暴击伤害
stat.Piercing           穿透            stat.MaxShield         最大护盾
stat.ShieldCoeffBonus   护盾系数加成    stat.MoveSpeed         移动速度
stat.PickupRange        拾取范围        stat.VarianceReduction 波动降低
stat.LuckBonus          幸运加成        stat.DamageReduction   伤害减免
stat.PotentialBudget    潜力预算        stat.UnboundedHealth   无界生命
stat.Melee              近战            stat.Ranged            远程
stat.Physical           物理            stat.Fire              火焰
stat.Ice                冰霜            stat.Lightning         闪电
stat.Wind               风              stat.Earth             大地
stat.Common             普通            stat.Uncommon          罕见
stat.Rare               稀有            stat.Epic              史诗
stat.Legendary          传奇
```

必须 en/zh 同时加：`LocalizationTablesHaveMatchingKeySets` 会逐 key 比对。`Player/UI` 目录也不允许出现 `LocalizationManager.Instance`（`UiLocalizationConsumersUseTheSharedGatewayBoundary`）。

---

## 6. 960×540 验收

| 区域 | 目标 rect（屏幕左上原点） | 检查 |
|---|---|---|
| Window | (70,45)-(890,495) | 全部在 960×540 内，无裁切 |
| Background | 同 Window | 四边齐平，世界/泥地不透出；alpha ≥0.98 |
| Left / Right | (90,65)-(460,475) / (490,65)-(880,475) | 两栏底板可见，30px 沟 + 2px divider（x=475） |
| Title / Tabs | Title (510,79)-(860,111)；Potential (520,118)-(666,152)；Stats (704,118)-(850,152) | 标题居中右栏，tab 不被裁 |
| Viewport | (510,165)-(860,445) | 350×280；Mask `showMaskGraphic=1`，列表底板可见 |
| Potential rows | 10 行，第一行 top 167，间距 25 | 无需滚动条；滚轮无效；最后一行 bottom 392 |
| Stats rows | 41 行，content 1100 | 滚动条可见；能滚到 Legendary；无越界 |
| Scrollbar | 轨道 (862,165)-(874,445)；handle 8×71.3 | value=1 top=165；value=0 bottom=445；左右不出 862..874 |
| Close | (785,463)-(865,491) | 完全在 Window 内，距底 4 |
| Portrait | (150,121)-(400,311) | color 白；不压 Alothai；底部到 Weapons 20 |
| PlayerName / Weapons | (125,89)-(425,121) / (125,331)-(425,359) | 不被 HUD 压（见 P2） |
| Tooltip | 280×210，跟随鼠标 | 四角 clamp；Background 盖住 Name/Icon/Details；中英 5 行不截断 |

**功能验收**：

1. Potential 页：无 scrollbar，10 行从顶部显示；滚轮/拖动无效。
2. Stats 页：scrollbar 出现，能滚到底；handle 在 value=0/1 时都停在轨道内。
3. 打开面板不再“空列表”（场景覆盖已删）；关闭再打开仍在顶部。
4. Tooltip：悬停 Primary/Secondary；中英切换后文字正确；背景不盖字。
5. 语言切换：标题/按钮/关闭/Weapons/行标签/tooltip 全部刷新；Console 无 `Localization: missing key`。
6. Console Error=0；`LocalizationTablesHaveMatchingKeySets`、`UiLocalizationConsumersUseTheSharedGatewayBoundary` 通过。
7. 截图必须 **960×540、16:9**；存 `<task>/results/[timestamp]_after_PlayerStatusPanel_*.png`，汇报绝对路径。

---

## 7. 实施与验证步骤（Tier 2 派单用）

1. **退出 Play Mode**，保存当前场景（现在 Editor 在 Play Mode，Prefab/场景修改必须在 Edit Mode）。
2. **Prefab**：只用旧 MCP `mcp__unity_mcp__*`，通过 `Unity_RunCommand` + `PrefabUtility.LoadPrefabContents / SaveAsPrefabAsset` 改；**禁止手写 `.prefab` YAML**。所有 RectTransform 改完在 Prefab Stage 里核对 anchor/offset。
3. **场景覆盖**：同场景下用 `PrefabUtility.RevertPropertyOverride` 去掉 `Content.m_AnchoredPosition.y=820`；不要 apply 到 prefab。
4. **脚本**：按 §4 改 3 个 `.cs`；编译后 `Unity_GetConsoleLogs` Error=0。
5. **本地化**：en/zh 同步加 key；跑 `mcp__unityMCP__run_tests`（EditMode / Architecture），至少：
   - `LocalizationTablesHaveMatchingKeySets`
   - `UiLocalizationConsumersUseTheSharedGatewayBoundary`
6. **Play Mode 验收**：Game View 设 16:9 / 960×540；截 before/after；按 §6 逐项核对；结果图写入 `results/`。
7. **静态门禁**：`.ai/scripts/static-gate.ps1 -Files <changed>` + `git diff --check`；只 stage 本单文件，不 `git add -A`。

**MCP 路由**（AGENTS §10）：Prefab/场景/Console/截图全部用旧 `mcp__unity_mcp__*`；测试运行只用新 `mcp__unityMCP__run_tests`；图片生成本单不需要（当前也被 §2.2 禁止）。

---

## 8. 残余风险 / P2

1. **HUD 遮挡**：HUD Canvas sortingOrder 100 < panel 200；960×540 下 EnergyBar 底边与 `Alothai` 只差 0–1px，非 16:9 会直接压住。新不透明底板还会把 HUD 左上条切掉。建议面板打开时隐藏 HUD（`GameplayHudView` 淡出/失活），关闭恢复；这会多一次跨 UI 协调，单独列 P2。
2. **Close 越界 4px**：F4 已含，但它是独立缺陷，验收时要单独看。
3. **30 个 stat key**：不加的话 Stats 页会显示 `CritChance` 这类原始 key；F6 修好后会立刻暴露，必须同单补齐。
4. **Tooltip 的 element/category 枚举**仍会显示 `Physical / Melee` 等英文（与现有 `weapon.replacement.stats` 一致）；要完全中文化需再加 `element.*` / `category.*`，P2。
5. **每帧 Refresh**：`PlayerStatusPanelController.Update` 现在每帧 `Refresh()`；本地化后每帧 `GetOrFallback`/`ToString` 会放大 GC，建议改为事件驱动。
6. **两张面板同时开**：截图里 `WeaponReplacementPanel`（sortingOrder 500）在 status 200 之上，左下角出现中文武器卡。若这是同时打开的两个 modal，属于模态栈问题，本单不处理；验收时只截 status 面板单独打开的状态。
7. **Prefab 根 `m_LocalScale=(0,0,0)`**：Overlay Canvas 运行时会驱动为 1，不要“顺手修”成 1，除非你确认场景行为；它不在本单问题列表。
8. **本单未写 `§〇 handoff` / ledger**：按“先不要改文件”的明确要求，只读审计不写回；需要我补写时再说。

---

## 9. 结果图（绝对路径）

- `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_0_08_status_UI\results\20261024_003500_audit_PlayerStatusPanel_findings.png`
- `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_0_08_status_UI\results\20261024_003500_audit_PlayerStatusPanel_target-960x540.png`
- `D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_0_08_status_UI\results\20261024_003500_audit_PlayerStatusPanel_tooltip.png`

![target 960x540](<D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_0_08_status_UI\results\20261024_003500_audit_PlayerStatusPanel_target-960x540.png>)

![tooltip before/after](<D:\GitHub\Zephyr_v3\Zephyr(AI)\Zephyr\Assets\MCPTest\Screenshots\2026_10_24_0_08_status_UI\results\20261024_003500_audit_PlayerStatusPanel_tooltip.png>)

---

**状态**：Tier 0 审计完成；`git status` 仅 `results/` 与 `results.meta` 为新增（任务要求的结果图），**游戏 Prefab/场景/脚本/本地化 JSON 全部未改，未 commit**。下一步如果要实施，按 Tier 2 派单：Claude 实现 → GPT-6.1-sol 评审 → 我跑 Unity 门禁和 960×540 截图验收。
