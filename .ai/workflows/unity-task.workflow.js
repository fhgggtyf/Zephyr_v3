// Zephyr Unity task: tiered DeepSeek -> Claude -> optional GPT.
// Tier 0: orchestrator only (no child agents).
// Tier 1 lean: orchestrator writes the plan and reviews the diff; workflow runs Claude only.
//             Optional gptPlan (GPT architect) and gptReview (GPT reviewer) are opt-in.
// Tier 2: external plan (audit/spec) skips GPT architecture; otherwise GPT plans.
//         Then Claude implements and GPT-6.1-sol reviews.
// The orchestrator (DeepSeek native) keeps commit/verification authority.
// This file is the *body* passed to the DSH `workflow` tool, not a Node module.

const ROOT = 'D:\\GitHub\\Zephyr_v3';
const UNITY = 'D:\\GitHub\\Zephyr_v3\\Zephyr(AI)\\Zephyr';
const AI = 'D:\\GitHub\\Zephyr_v3\\.ai';

const DEEPSEEK = { provider: 'deepseek-account', model: 'deepseek-flash' };
const CLAUDE = { provider: 'fastllm', model: 'claude-opus-5-5' };
const GPT_ASTRA = { provider: 'fastllm', model: 'gpt-6-astra' };
const GPT_REVIEW = { provider: 'fastllm', model: 'gpt-6.1-sol' };

const task = args && args.task ? String(args.task) : '';
const tier = args && args.tier !== undefined ? String(args.tier) : '';
const scope = args && args.scope ? String(args.scope) : '(未提供，由计划推导)';
const acceptance = args && args.acceptance ? String(args.acceptance) : '(未提供，按 AGENTS.md 第 7 节)';
const brief = args && args.brief ? String(args.brief) : '';
if (brief.length > 4000) {
  return { ok: false, error: 'args.brief too long (max 4000 chars); compress it to <=10 lines' };
}
const maxRounds = Math.min(Math.max(Number(args && args.maxRounds) || 2, 1), 3);
const mode = args && args.mode === 'fix' ? 'fix' : 'implement';
const issues = Array.isArray(args && args.issues) ? args.issues.map(String) : [];
const gptPlan = Boolean(args && args.gptPlan);
const gptReview = Boolean(args && args.gptReview);
const screenshots = Array.isArray(args && args.screenshots) ? args.screenshots.map(String) : [];

const firstShot = screenshots[0] || '';
const derivedScreenshotDir = firstShot.includes('\\') || firstShot.includes('/')
  ? firstShot.replace(/[\\/][^\\/]+$/, '')
  : '';
const screenshotDir = args && args.screenshotDir ? String(args.screenshotDir) : derivedScreenshotDir;
const sep = screenshotDir.includes('\\') ? '\\' : '/';
const resultsDir = args && args.resultsDir
  ? String(args.resultsDir)
  : (screenshotDir ? screenshotDir + sep + 'results' : '');

if (!task) {
  return { ok: false, error: 'args.task is required' };
}
if (!['0', '1', '2'].includes(tier)) {
  return {
    ok: false,
    error: 'args.tier is required: 0 (trivial) | 1 (ordinary, lean) | 2 (cross-module/architecture)'
  };
}
if (tier === '0') {
  return {
    ok: true,
    stopped: 'tier-0',
    task,
    note: 'DeepSeek orchestrator handles this directly (or via the persistent worker). No child agents.'
  };
}
if (mode === 'fix' && issues.length === 0) {
  return { ok: false, error: 'mode "fix" requires a non-empty args.issues[]' };
}

const common = `项目根：${ROOT}
Unity 项目：${UNITY}
AI 目录：${AI}
任务：${task}
档次：Tier ${tier}
模式：${mode}
允许范围：${scope}
验收标准：${acceptance}
任务简报（优先，若与长文冲突以简报为准）：
${brief || '(未提供；实现者只按下面的紧凑计划执行)'}
最高铁律：${ROOT}\\AGENTS.md。任何冲突以 AGENTS.md 为准。
Unity 工具路由：测试运行用 mcp__unityMCP__run_tests；其他 Unity 操作只用 mcp__unity_mcp__*；图片生成用 mcp__unityMCP__*，但当前禁用。
安全：不 git reset / checkout / clean / stash；不 commit；不改任务外文件；保留用户已有未提交改动。
路径：读写可以用绝对路径；返回的 files/changedFiles 必须用仓库相对路径（从 ${ROOT} 起），不要绝对路径。
视觉证据：${screenshots.length ? screenshots.join(' | ') : '(未提供)'}。涉及 UI/画面时先用 read_image 看这些路径；输入图只读，不要修改/移动/覆盖。图片预算：实现者最多 2 张（input + 1 张 before），评审最多 4 张；不要读 results 下全部截图。
截图规则：AI 生成图必须存到 ${resultsDir || '(未提供；有任务文件夹时用 <任务文件夹>/results)'}（不存在就创建）；命名 [yyyyMMdd_HHmmss]_[stage]_[场景名]_[说明].png，stage 用 audit/before/after/review/final。
流程产物白名单：.ai/ledger/**、.ai/state/**、.ai/snapshots/**、.ai/reports/**、.ai/audits/**、Assets/MCPTest/Screenshots/<任务文件夹>/results/** 是强制流程产物，不算实现范围；只有 plan.files 之外的其他文件才算越界。`;

const planSchema = {
  type: 'object',
  properties: {
    classification: { type: 'string', enum: ['ordinary-feature', 'cross-module', 'architecture-special'] },
    summary: { type: 'string' },
    ownerModule: { type: 'string' },
    lifecycleOwner: { type: 'string' },
    dataOwnership: { type: 'string' },
    dependencyDirection: { type: 'string' },
    files: { type: 'array', items: { type: 'string' } },
    asmdefImpact: { type: 'string' },
    unityAssets: { type: 'array', items: { type: 'string' } },
    acceptance: { type: 'array', items: { type: 'string' } },
    risks: { type: 'array', items: { type: 'string' } },
    rollback: { type: 'string' },
    needsApproval: { type: 'boolean' }
  },
  required: [
    'classification', 'summary', 'ownerModule', 'lifecycleOwner', 'dataOwnership',
    'dependencyDirection', 'files', 'asmdefImpact', 'unityAssets', 'acceptance',
    'risks', 'rollback', 'needsApproval'
  ],
  additionalProperties: false
};

const implSchema = {
  type: 'object',
  properties: {
    status: { type: 'string', enum: ['done', 'blocked', 'needs-approval'] },
    changedFiles: { type: 'array', items: { type: 'string' } },
    diff: { type: 'string' },
    diffSummary: { type: 'string' },
    evidence: { type: 'array', items: { type: 'string' } },
    unityOps: { type: 'array', items: { type: 'string' } },
    blockers: { type: 'array', items: { type: 'string' } }
  },
  required: ['status', 'changedFiles', 'diff', 'diffSummary', 'evidence', 'unityOps', 'blockers'],
  additionalProperties: false
};

const reviewSchema = {
  type: 'object',
  properties: {
    verdict: { type: 'string', enum: ['PASS', 'FAIL'] },
    blocking: { type: 'array', items: { type: 'string' } },
    nonBlocking: { type: 'array', items: { type: 'string' } },
    requiredTests: { type: 'array', items: { type: 'string' } },
    notes: { type: 'string' }
  },
  required: ['verdict', 'blocking', 'nonBlocking', 'requiredTests', 'notes'],
  additionalProperties: false
};

function validPlan(plan) {
  return Boolean(
    plan &&
    typeof plan === 'object' &&
    typeof plan.classification === 'string' &&
    Array.isArray(plan.files) &&
    plan.files.length > 0 &&
    Array.isArray(plan.acceptance) &&
    plan.acceptance.length > 0 &&
    typeof plan.needsApproval === 'boolean'
  );
}

function compactPlan(plan) {
  return {
    classification: plan.classification,
    files: plan.files,
    unityAssets: plan.unityAssets,
    acceptance: plan.acceptance,
    risks: plan.risks,
    rollback: plan.rollback
  };
}

function implementationPrompt(plan) {
  return `${common}

你是实现者。先读 ${AI}\\roles\\implementer.md；AGENTS.md 只读 §4.4/§6.2/§10/§11（用 grep/offset，不要全文读）。
只按任务简报 + 紧凑计划实现；不要复述审计；只读 plan.files 相关段，不读全仓；不要扩大范围；需要细节只读计划列出的文件。
保持最小 diff；不要 commit。
Unity 资产/场景操作只用 mcp__unity_mcp__*，并在每次操作后查 Console、必要时截图。
如果任务涉及 UI/画面：先 read_image 看视觉证据；改完后用 Unity MCP 截图，存到 resultsDir（不存在就创建），把 PNG 路径写进 evidence；不要手写 Unity 序列化 YAML。
${mode === 'fix' ? `这是修复轮。上一轮问题（逐条修复）：\n- ${issues.join('\n- ')}\n不要扩大范围。\n` : ''}
紧凑计划：
${JSON.stringify(compactPlan(plan), null, 2)}`;
}

function reviewPrompt(plan, implementation) {
  return `${common}

你是独立评审。先读 ${AI}\\roles\\reviewer.md；AGENTS.md 只读 §4.4/§6.2/§9/§10/§11（grep/offset，不要全文）。
只根据任务简报、紧凑计划、实现结果和证据评审；不要看实现者的解释或思维过程；不要改文件、不要提交。
范围判定：只把 plan.files 里的任务文件算实现范围；process artifacts（ledger/state/reports/results）不算越界。不要用 git status 把流程产物当阻塞项。
如果实现 evidence 里有截图路径，先用 read_image 看 before/after；确认路径在 resultsDir，输入图只读；检查 960x540、裁切、本地化、层级。
实现结果：
${JSON.stringify(implementation, null, 2)}

紧凑计划：
${JSON.stringify(compactPlan(plan), null, 2)}`;
}

// ---------------------------------------------------------------------------
// Tier 1 lean: orchestrator plans and reviews; workflow runs Claude only.
// ---------------------------------------------------------------------------
if (tier === '1') {
  let plan = args && args.plan ? args.plan : null;

  if (!plan && gptPlan) {
    phase('architect');
    log('Tier 1 架构：GPT-6-astra（显式 gptPlan）');
    plan = await agent(`${common}

你是架构师。Tier 1 快速计划：只读任务相关文件，控制在一页内；不要为小改动做全仓分析。
先读 ${AI}\\roles\\architect.md 和 ${ROOT}\\AGENTS.md 第 4.5/4.6 节。
只做设计和边界判断，不改任何文件。`, {
      provider: GPT_ASTRA.provider,
      model: GPT_ASTRA.model,
      label: '架构师 GPT-6-astra',
      schema: planSchema
    });
    if (!plan) {
      return { ok: false, error: 'architect failed', tier: '1' };
    }
  }

  if (!plan) {
    return {
      ok: false,
      error: 'tier-1 requires args.plan (the orchestrator writes it) or gptPlan:true'
    };
  }
  if (!validPlan(plan)) {
    return {
      ok: false,
      error: 'invalid tier-1 plan: need classification, files[], acceptance[], needsApproval',
      tier: '1',
      plan
    };
  }
  if (plan.needsApproval || plan.classification === 'architecture-special') {
    return { ok: true, stopped: 'needs-approval', task, tier: '1', plan };
  }

  phase(mode === 'fix' ? 'fix' : 'implement');
  log(`Tier 1 ${mode === 'fix' ? '修复' : '实现'}：${CLAUDE.provider}/${CLAUDE.model}`);
  const implementation = await agent(implementationPrompt(plan), {
    provider: CLAUDE.provider,
    model: CLAUDE.model,
    label: `实现者 ${CLAUDE.model} (${mode})`,
    schema: implSchema
  });

  if (!implementation) {
    return { ok: false, error: 'implementer failed', tier: '1', plan, mode };
  }
  if (implementation.status !== 'done') {
    return { ok: true, stopped: implementation.status, task, tier: '1', plan, implementation, mode };
  }
  if (implementation.diff && implementation.diff.length > 80000) {
    return {
      ok: true,
      stopped: 'diff-too-large',
      task,
      tier: '1',
      plan,
      implementation,
      mode,
      hint: 'Split the task or review per file.'
    };
  }

  let review = null;
  if (gptReview) {
    phase('review');
    log(`Tier 1 关键节点评审：${GPT_REVIEW.provider}/${GPT_REVIEW.model}`);
    review = await agent(reviewPrompt(plan, implementation), {
      provider: GPT_REVIEW.provider,
      model: GPT_REVIEW.model,
      label: '评审 GPT-6.1-sol',
      schema: reviewSchema
    });
    if (!review) {
      return { ok: false, error: 'reviewer failed', tier: '1', plan, implementation, mode };
    }
  }

  return {
    ok: true,
    task,
    tier: '1',
    mode,
    plan,
    implementation,
    review,
    needsOrchestratorReview: !gptReview,
    note: gptReview
      ? 'GPT review is advisory; the orchestrator still runs static-gate + Unity acceptance.'
      : 'Lean Tier 1: the orchestrator reviews the diff against the task brief/acceptance and runs gates before commit.'
  };
}

// ---------------------------------------------------------------------------
// Tier 2: external plan (audit/spec) can skip GPT architecture; otherwise GPT plans.
// Then Claude implements and GPT reviews, with a bounded fix loop.
// ---------------------------------------------------------------------------
let plan = args && args.plan ? args.plan : null;
let planSource = 'gpt';

if (plan) {
  if (!validPlan(plan)) {
    return {
      ok: false,
      error: 'invalid tier-2 plan: need classification, files[], acceptance[], needsApproval',
      tier: '2',
      plan
    };
  }
  planSource = 'external';
  log('Tier 2 使用外部计划（审计/spec），跳过 GPT 架构');
} else {
  phase('architect');
  log(`Tier 2 架构：${GPT_ASTRA.provider}/${GPT_ASTRA.model}`);
  plan = await agent(`${common}

你是架构师。Tier 2：跨模块/架构专项。写完整架构记录：六问、依赖方向、asmdef/资产影响、迁移顺序、回滚、验收。
先读 ${AI}\\roles\\architect.md、${ROOT}\\AGENTS.md 第 4.5/4.6 节、${UNITY}\\Assets\\Scripts\\Tests\\Architecture\\ArchitecturePolicy.json。
只做设计和边界判断，不改任何文件。若为 architecture-special，needsApproval=true。`, {
    provider: GPT_ASTRA.provider,
    model: GPT_ASTRA.model,
    label: '架构师 GPT-6-astra',
    schema: planSchema
  });

  if (!plan) {
    return { ok: false, error: 'architect failed', tier: '2' };
  }
}

if (plan.needsApproval || plan.classification === 'architecture-special') {
  return { ok: true, stopped: 'needs-approval', task, tier: '2', plan, planSource };
}

let implementation = null;
let review = null;
let round = 0;
let reviewIssues = [];

while (round < maxRounds) {
  round += 1;
  phase(`implement-${round}`);
  log(`Tier 2 实现：${CLAUDE.provider}/${CLAUDE.model}，第 ${round} 轮`);

  implementation = await agent(`${common}

你是实现者。先读 ${AI}\\roles\\implementer.md；AGENTS.md 只读 §4.4/§6.2/§10/§11（用 grep/offset，不要全文读）。
只按任务简报 + 紧凑计划实现；不要复述审计；只读 plan.files 相关段，不读全仓；不要扩大范围；需要细节只读计划列出的文件。
保持最小 diff；不要 commit。
Unity 资产/场景操作只用 mcp__unity_mcp__*，并在每次操作后查 Console、必要时截图。
如果任务涉及 UI/画面：先 read_image 看视觉证据；改完后用 Unity MCP 截图，存到 resultsDir（不存在就创建），把 PNG 路径写进 evidence；不要手写 Unity 序列化 YAML。
${reviewIssues.length ? `上一轮评审的阻塞问题（逐条修复）：\n- ${reviewIssues.join('\n- ')}\n` : ''}
紧凑计划：
${JSON.stringify(compactPlan(plan), null, 2)}`, {
    provider: CLAUDE.provider,
    model: CLAUDE.model,
    label: `实现者 ${CLAUDE.model} #${round}`,
    schema: implSchema
  });

  if (!implementation) {
    return { ok: false, error: 'implementer failed', tier: '2', plan, round, planSource };
  }
  if (implementation.status !== 'done') {
    return { ok: true, stopped: implementation.status, task, tier: '2', plan, implementation, round, planSource };
  }
  if (implementation.diff && implementation.diff.length > 80000) {
    return {
      ok: true,
      stopped: 'diff-too-large',
      task,
      tier: '2',
      plan,
      implementation,
      round,
      planSource,
      hint: 'Split the task or review per file.'
    };
  }

  phase(`review-${round}`);
  log(`Tier 2 评审：${GPT_REVIEW.provider}/${GPT_REVIEW.model}，第 ${round} 轮`);

  review = await agent(reviewPrompt(plan, implementation), {
    provider: GPT_REVIEW.provider,
    model: GPT_REVIEW.model,
    label: `评审 GPT-6.1-sol #${round}`,
    schema: reviewSchema
  });

  if (!review) {
    return { ok: false, error: 'reviewer failed', tier: '2', plan, implementation, round, planSource };
  }
  if (review.verdict === 'PASS') {
    break;
  }
  reviewIssues = review.blocking || [];
  if (reviewIssues.length === 0) {
    break;
  }
}

return {
  ok: true,
  task,
  tier: '2',
  planSource,
  plan,
  implementation,
  review,
  rounds: round,
  needsOrchestratorVerification: true
};
