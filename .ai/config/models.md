# 模型与 provider 对照

> 2026-10-23 核对 `https://fastllm.top/v1/models`（138 个模型）。
> 不在仓库里保存任何密钥；中转站密钥在 DSH 凭证库 `FASTLLM_API_KEY`。
> 可复制的 DSH 配置片段：[cordis.patch.snippet.yml](cordis.patch.snippet.yml)（合并进 profile，不要覆盖现有 MCP/UI 条目）。

## 1. 角色 → 模型

| 角色 | provider | model | 备注 |
|---|---|---|---|
| 指挥 / 日志 / commit | **`deepseek-account`** | **`deepseek-flash`** | `reasoningEffort: max`；主会话默认 |
| Tier 1 计划 | `deepseek-account` | `deepseek-flash` | **控制器自己写，不起架构子代理** |
| Tier 1 自评审 | `deepseek-account` | `deepseek-flash` | 控制器审 diff + 门禁；`gptReview` 时加 GPT |
| Tier 1 GPT 计划（可选） | `fastllm` | `gpt-6-astra` | `gptPlan: true` 时只出计划 |
| Tier 1 GPT 评审（可选） | `fastllm` | `gpt-6.1-sol` | 风险触发器 / 每 5 个抽 1 个盲审 |
| 架构（Tier 2） | `fastllm` | `gpt-6-astra` | 完整架构记录 |
| 实现（Tier 1/2） | `fastllm` | `claude-opus-5-5` | 代码实现 |
| 评审（Tier 2） | `fastllm` | `gpt-6.1-sol` | `gpt-6-sol` 工具探测不稳定，只作兜底 |
| 常驻 worker | 继承主会话 | `deepseek-flash` | Tier 0/1 机械段 |

> **DeepSeek 不再走中转站。** 指挥固定本家，目的是保住缓存（实测 20.6M cache read / 195k uncached）。
> `fastllm` 只承载 GPT/Claude。

## 2. 当前 DSH 配置（无密钥）

```yaml
- id: agent-default-model
  name: "@deepseek-ai/dsh-agent-default-model"
  config:
    provider: deepseek-account
    model: deepseek-flash
    reasoningEffort: max

- id: llm-pi-ai
  name: "@deepseek-ai/dsh-llm-pi-ai"
  config:
    providers:
      fastllm:
        apiKeyEnv: FASTLLM_API_KEY
        api: openai-responses
        baseURL: https://fastllm.top/v1
        models:
          - id: gpt-6-astra
            input: [text, image]
          - id: gpt-6.1-sol
            input: [text, image]
          - id: gpt-6-sol
            input: [text, image]
          - id: claude-opus-5-5
            input: [text, image]
```

子代理白名单（`subagent-model-selection-settings`）：
`deepseek-account/deepseek-flash`、`fastllm/gpt-6-astra`、`fastllm/claude-opus-5-5`、`fastllm/gpt-6.1-sol`。

## 3. 为什么指挥不用中转站 DeepSeek

| | 本家 `deepseek-account` | 中转站 `fastllm` DeepSeek |
|---|---|---|
| 缓存 | 实测命中率 ~99% | 取决于中转站，不能假设 |
| 成本 | 便宜 + 缓存折价 | 按 token 全价风险 |
| 稳定性 | 官方 | 受中转站限流/改版影响 |

指挥是长会话，缓存收益最大；把它放本家，GPT/Claude 只在 Tier 2 或 `gptReview` 时按需开。

## 4. 失败回退

| 失败 | 处理 |
|---|---|
| `fastllm` 不可用 | Tier 2 降为 Tier 1；架构/评审回退 DeepSeek，**汇报里标明“本轮非目标模型”** |
| `claude-opus-5-5` 在 `openai-responses` 下报 400/不支持 | 新建 `fastllm-anthropic`：`api: anthropic-messages`，`baseURL: https://fastllm.top/v1`，只挂 Claude |
| `gpt-6.1-sol` 失败 | 退 `gpt-6-sol`（若仍不行，让指挥把 diff 直接贴进 prompt，评审仍能工作） |
| 模型不支持 tool call | 该角色不能跑 agent 循环；换型号，或降级为“只出文本、由指挥落盘” |

## 5. 能力核对

- `claude-opus-5-5` 同时支持 `anthropic` 与 `openai` 端点；
- `gpt-6-astra`、`gpt-6.1-sol`、`gpt-6-sol` 标记为 `openai`；
- 上线前必须实测一次 **tool call**（读文件/写文件/Unity MCP）。

### 5.1 多模态

`fastllm` 的 GPT/Claude 已在 `llm-pi-ai` 里声明 `input: [text, image]`；实测 `read_image` 可用：

| 模型 | 读截图 |
|---|---|
| 本家 `deepseek-flash` | ✅ 能描述画面/UI |
| `claude-opus-5-5` | ✅ 能描述 UI（声明 `input` 后；之前是 `NO_IMAGE_TOOL`） |
| `gpt-6.1-sol` | ✅ 能描述 UI（声明 `input` 后） |

UI 任务把截图路径放进 `args.screenshots`；架构/实现/评审都能用 `read_image` 看。若中转站某型号不接受图像，会返回错误/空结果，去掉该型号的 `input` 即可，文本能力不受影响。

## 6. 实测记录（2026-10-23）

| 测试 | 结果 |
|---|---|
| 文本烟测 | 4 模型全部返回 OK |
| 工具读文件 | DeepSeek / `gpt-6-astra` / `claude-opus-5-5` / `gpt-6.1-sol` 通过；`gpt-6-sol` 首次 null、重试通过；`gpt-6` null |
| 视觉读图 | 本家 DeepSeek / Claude / GPT-6.1-sol 都能 `read_image`；未声明 `input` 前 Claude/GPT 返回 `NO_IMAGE_TOOL` |
| 结构化 schema | `enum` + `array` + `required` 通过 |
| 缓存探针（4 次只读小调用） | GPT 第 2 次仍有 41k 未缓存；Claude 第 2 次 99k 未缓存、仅 2.8k cache read |
| 当前指挥会话 | 20,636,544 cache read / 195,383 uncached（~99% 命中） |

结论：**Tier 1 默认由控制器自评审（DeepSeek），只有 `gptReview: true` 才交 `gpt-6.1-sol`**；不要假设中转站有稳定缓存。

## 7. 改模型时同步三处

1. 本文件；
2. `workflows/unity-task.workflow.js` 顶部四个路由常量 `DEEPSEEK` / `CLAUDE` / `GPT_ASTRA` / `GPT_REVIEW`；
3. `cordis.patch.yml` 的 `llm-pi-ai` / `agent-default-model` / 子代理白名单。
