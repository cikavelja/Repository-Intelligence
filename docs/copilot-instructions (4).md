# Copilot Instructions — Python Agent Cost Optimization Demo

## Project Purpose

This project demonstrates how to build a Python multi-agent solution using Microsoft Agent Framework and Foundry IQ, with a React UI that allows the user to compare non-optimized and cost-effective agent execution.

The main goal is to show how different agentic design choices affect:

- token usage
- estimated cost
- latency
- context size
- retrieval frequency
- model choice
- output quality
- number of agent and subagent calls

The demo must make cost differences visible, measurable, and controllable from the UI.

The main theme of this project is not only reducing tokens. The goal is to demonstrate that better agent quality, better context engineering, better model routing, and better workflow design naturally reduce wasted cost.

---

## Required Architecture

The solution must contain this architecture:

```text
React UI
  |
  | HTTP / SSE / WebSocket
  v
Python API
  |
  v
DemoRunnerAgent
  |
  v
ArchitectOrchestratorAgent
  |
  |-- ResearchAgent
  |-- PlannerAgent
  |-- ImplementationAgent
```

There are two top-level agents:

1. `DemoRunnerAgent`
2. `ArchitectOrchestratorAgent`

The `ArchitectOrchestratorAgent` must call three subagents:

1. `ResearchAgent`
2. `PlannerAgent`
3. `ImplementationAgent`

---

## Main Demo Modes

The system must support two execution modes and one comparison mode.

### 1. Non-Optimized Mode

This mode intentionally demonstrates inefficient agent usage.

Behavior:

- Use the same expensive reasoning model for all agents unless the UI overrides it.
- Pass the full user prompt, full transcript, and full accumulated context to every subagent when configured.
- Allow every subagent to query Foundry IQ independently.
- Use verbose prompts.
- Return verbose outputs.
- Do not reuse retrieval results.
- Do not compress context.
- Always execute all three subagents unless the UI disables them.
- Allow unnecessary tool calls.
- Allow long conversation-style responses.

This mode is used to show higher token usage, repeated retrieval, higher cost, and lower context efficiency.

### 2. Cost-Effective Mode

This mode demonstrates optimized agentic design.

Behavior:

- Use model routing.
- Use reasoning models only where reasoning is valuable.
- Use cheaper or faster models for summarization, narrow implementation, and structured output.
- Retrieve from Foundry IQ only when needed.
- Prefer one retrieval pass by `ResearchAgent`.
- Pass summarized context to `PlannerAgent`.
- Pass only the final plan and required file list to `ImplementationAgent`.
- Use concise prompts.
- Use structured outputs.
- Avoid repeated retrieval.
- Avoid passing the full transcript to every subagent.
- Support optional subagent skipping when not needed.
- Track token and cost metrics per agent step.

### 3. Compare Mode

Compare mode must run both workflows with the same user request and configuration baseline.

It must return:

- non-optimized result
- optimized result
- token difference
- cost difference
- latency difference
- Foundry IQ call difference
- percentage saving
- step-by-step comparison

---

## React UI Requirements

Create a React frontend that allows live demonstration of how cost changes when workflow settings change.

Use React with TypeScript.

Suggested frontend stack:

```text
React
TypeScript
Vite
Tailwind CSS
Recharts
Axios or TanStack Query
```

The UI must provide controls for the following areas.

---

## UI Control: Execution Mode

The UI must allow the user to select:

```text
Non-Optimized
Cost-Effective
Run Both and Compare
```

---

## UI Control: Model Selection

The UI must allow changing model assignment per agent.

Example:

```text
DemoRunnerAgent:              reasoning / fast / custom
ArchitectOrchestratorAgent:   reasoning / fast / custom
ResearchAgent:                reasoning / fast / custom
PlannerAgent:                 reasoning / fast / custom
ImplementationAgent:          reasoning / fast / custom
```

Do not hardcode exact model names in UI logic. Model names must come from backend configuration.

The UI should support model aliases such as:

```text
reasoning
fast
cheap
custom
```

The backend maps aliases to actual deployed model names.

---

## UI Control: Context Controls

The UI must allow controlling:

```text
Include full transcript: yes/no
Include previous agent outputs: yes/no
Compress research result: yes/no
Max context tokens
Max output tokens
Context handoff strategy
```

Context handoff strategy options:

```text
Full context to every agent
Research summary only
Plan only
Scoped context per subagent
```

The UI must show an estimated token impact before the run where possible.

---

## UI Control: Foundry IQ Controls

The UI must allow controlling:

```text
Enable Foundry IQ retrieval: yes/no
Top K chunks
Allow each subagent to retrieve: yes/no
Reuse ResearchAgent retrieval result: yes/no
Minimum relevance score
Use mock Foundry IQ mode: yes/no
```

The UI must clearly show how many Foundry IQ calls each workflow made.

---

## UI Control: Agent Execution Controls

The UI must allow controlling:

```text
Run ResearchAgent: yes/no
Run PlannerAgent: yes/no
Run ImplementationAgent: yes/no
Allow conditional subagent skipping: yes/no
Run agents sequentially
Run selected subagents in parallel where possible
```

The default demo should run sequentially because it is easier to explain.

Parallel execution can be an optional advanced demo mode.

---

## UI Control: Prompt Controls

The UI must allow selecting prompt profiles:

```text
Verbose generic prompt
Short optimized prompt
JSON-only prompt
Code-only prompt
Custom prompt
```

The UI should show the actual prompt sent to every agent.

The UI should also show:

- prompt token estimate
- context token estimate
- output token limit
- estimated cost before execution

---

## UI Control: Cost Controls

The UI must allow setting pricing manually:

```text
Input token price per 1K
Output token price per 1K
Cached input token price per 1K
Currency
```

Do not hardcode model prices. Prices change. Keep all prices configurable.

---

# UI Pages

## 1. Dashboard Page

Route:

```text
/
```

Purpose:

Shows the high-level comparison between optimized and non-optimized runs.

Display:

- total input tokens
- total output tokens
- total cached input tokens
- total tokens
- estimated cost
- latency
- number of agent calls
- number of subagent calls
- number of Foundry IQ calls
- number of tool calls
- percentage saving
- cost difference
- token difference

Charts:

- bar chart: optimized vs non-optimized cost
- bar chart: optimized vs non-optimized total tokens
- stacked chart: tokens per agent
- bar chart: cost per agent
- line or timeline chart: execution steps

---

## 2. Run Configuration Page

Route:

```text
/configure
```

Purpose:

Allows the user to configure the demo.

Sections:

- execution mode
- model routing
- context strategy
- Foundry IQ options
- subagent execution
- pricing
- prompt profile
- user request

Include a button:

```text
Run Demo
```

When the user clicks the button, the UI should call:

```text
POST /api/demo/run
```

---

## 3. Execution Trace Page

Route:

```text
/runs/:runId
```

Purpose:

Shows step-by-step execution.

For every agent step show:

- agent name
- mode
- model used
- input tokens
- output tokens
- cached input tokens
- estimated cost
- latency
- prompt preview
- context preview
- output preview
- Foundry IQ calls
- tools used
- status
- notes

The execution trace must make repeated retrieval and repeated context handoff visible.

---

## 4. Prompt Lab Page

Route:

```text
/prompts
```

Purpose:

Allows editing and comparing prompts.

Features:

- edit prompt for each agent
- save prompt profile
- compare verbose vs optimized prompt
- estimate token count before running
- show prompt length and estimated cost impact
- preview resolved prompt with template variables

---

## 5. Reports Page

Route:

```text
/reports
```

Purpose:

Shows previous demo runs and exported comparison reports.

Features:

- table of previous runs
- filter by mode
- compare two runs
- export Markdown report
- export JSON report

---

# Backend Requirements

Use Python.

Suggested backend stack:

```text
FastAPI
Pydantic
Microsoft Agent Framework
Azure Identity
Foundry IQ integration
Uvicorn
```

The backend must expose APIs for:

```text
POST /api/demo/run
GET  /api/demo/runs
GET  /api/demo/runs/{run_id}
GET  /api/demo/runs/{run_id}/report
POST /api/tokens/estimate
GET  /api/config/defaults
POST /api/prompts/estimate
GET  /api/prompts
PUT  /api/prompts/{profile_name}
```

Optional streaming endpoint:

```text
GET /api/demo/runs/{run_id}/stream
```

Use SSE or WebSocket to stream live execution events to the React UI.

---

# Backend Project Structure

Use this structure unless there is a strong reason to change it:

```text
backend/
  app/
    main.py
    settings.py

    api/
      demo_routes.py
      config_routes.py
      prompt_routes.py
      token_routes.py

    agents/
      demo_runner_agent.py
      architect_orchestrator_agent.py
      research_agent.py
      planner_agent.py
      implementation_agent.py

    workflows/
      non_optimized_workflow.py
      optimized_workflow.py
      comparison_workflow.py

    foundry_iq/
      knowledge_client.py
      mock_knowledge_client.py
      models.py

    telemetry/
      token_meter.py
      cost_calculator.py
      run_report.py
      event_stream.py

    prompts/
      non_optimized/
        orchestrator.md
        research.md
        planner.md
        implementation.md

      optimized/
        orchestrator.md
        research.md
        planner.md
        implementation.md

    storage/
      run_repository.py
      file_run_repository.py

    models/
      demo_config.py
      agent_metrics.py
      run_result.py
      prompt_profile.py
      pricing.py

  data/
    transcript.txt

  tests/
    test_cost_calculator.py
    test_token_meter.py
    test_workflow_comparison.py
    test_mock_foundry_iq.py
```

Frontend:

```text
frontend/
  src/
    app/
      router.tsx

    pages/
      DashboardPage.tsx
      ConfigureRunPage.tsx
      ExecutionTracePage.tsx
      PromptLabPage.tsx
      ReportsPage.tsx

    components/
      AgentFlowDiagram.tsx
      CostSummaryCards.tsx
      TokenComparisonChart.tsx
      AgentStepTimeline.tsx
      ModelRoutingEditor.tsx
      ContextControlsPanel.tsx
      FoundryIqControlsPanel.tsx
      PricingEditor.tsx
      PromptPreview.tsx
      RunModeSelector.tsx
      RunButton.tsx
      ReportTable.tsx

    api/
      demoApi.ts
      configApi.ts
      promptApi.ts

    types/
      demo.ts
      metrics.ts
      prompts.ts
      pricing.ts

    utils/
      formatCurrency.ts
      formatTokens.ts
      percentages.ts
```

---

# Important Backend Data Models

Create strongly typed Pydantic models on the backend.

## TokenPricingConfig

```python
from pydantic import BaseModel, Field


class TokenPricingConfig(BaseModel):
    input_price_per_1k: float = Field(default=0.0, ge=0)
    output_price_per_1k: float = Field(default=0.0, ge=0)
    cached_input_price_per_1k: float = Field(default=0.0, ge=0)
    currency: str = "USD"
```

## DemoRunConfig

```python
from typing import Literal
from pydantic import BaseModel, Field


class DemoRunConfig(BaseModel):
    mode: Literal["non_optimized", "optimized", "compare"]

    user_request: str

    include_full_transcript: bool = True
    include_previous_agent_outputs: bool = True
    compress_research_result: bool = False

    max_context_tokens: int = Field(default=16000, ge=1000)
    max_output_tokens: int = Field(default=2000, ge=100)

    context_handoff_strategy: Literal[
        "full_context_to_every_agent",
        "research_summary_only",
        "plan_only",
        "scoped_context_per_subagent",
    ] = "full_context_to_every_agent"

    enable_foundry_iq: bool = True
    foundry_top_k: int = Field(default=5, ge=1, le=20)
    allow_each_subagent_to_retrieve: bool = True
    reuse_research_retrieval_result: bool = False
    minimum_relevance_score: float = Field(default=0.5, ge=0, le=1)
    foundry_iq_mock_mode: bool = True

    run_research_agent: bool = True
    run_planner_agent: bool = True
    run_implementation_agent: bool = True
    allow_conditional_subagent_skipping: bool = False
    allow_parallel_subagents: bool = False

    prompt_profile: Literal[
        "verbose_generic",
        "short_optimized",
        "json_only",
        "code_only",
        "custom",
    ] = "verbose_generic"

    model_routing: dict[str, str]
    pricing: TokenPricingConfig
```

## AgentStepMetric

```python
from pydantic import BaseModel


class AgentStepMetric(BaseModel):
    step_id: str
    agent_name: str
    mode: str
    model: str

    input_tokens: int
    output_tokens: int
    cached_input_tokens: int = 0

    estimated_cost: float
    latency_ms: int

    foundry_iq_calls: int
    tool_calls: int

    prompt_preview: str
    context_preview: str
    output_preview: str

    notes: str | None = None
```

## DemoRunResult

```python
from datetime import datetime
from pydantic import BaseModel


class DemoRunResult(BaseModel):
    run_id: str
    mode: str
    started_at: datetime
    completed_at: datetime | None

    config: DemoRunConfig
    steps: list[AgentStepMetric]

    total_input_tokens: int
    total_output_tokens: int
    total_cached_input_tokens: int
    total_tokens: int

    total_estimated_cost: float
    total_latency_ms: int

    foundry_iq_calls: int
    tool_calls: int

    final_output: str
```

## ComparisonResult

```python
from pydantic import BaseModel


class ComparisonResult(BaseModel):
    non_optimized: DemoRunResult
    optimized: DemoRunResult

    tokens_saved: int
    token_reduction_percent: float
    cost_saved: float
    cost_reduction_percent: float
    latency_saved_ms: int
    foundry_iq_calls_saved: int
```

---

# Telemetry Requirements

Every agent and subagent call must produce telemetry.

Track:

```text
agent name
model name
input tokens
output tokens
cached input tokens
estimated cost
latency
Foundry IQ call count
tool call count
prompt size
context size
output size
```

The React UI must display these metrics per step and in aggregate.

Implement token counting in a dedicated service.

Do not scatter token counting logic across agents.

Use this abstraction:

```python
class TokenMeter:
    def estimate_text_tokens(self, text: str) -> int:
        ...

    def estimate_messages_tokens(self, messages: list[dict]) -> int:
        ...
```

---

# Cost Calculation Rules

Use configurable pricing.

Create:

```python
class CostCalculator:
    def calculate(
        self,
        input_tokens: int,
        output_tokens: int,
        cached_input_tokens: int,
        pricing: TokenPricingConfig,
    ) -> float:
        ...
```

Formula:

```text
input cost = input_tokens / 1000 * input_price_per_1k
output cost = output_tokens / 1000 * output_price_per_1k
cached input cost = cached_input_tokens / 1000 * cached_input_price_per_1k

total = input cost + output cost + cached input cost
```

Round cost to 6 decimal places for step-level metrics.

Round cost to 4 decimal places for UI summaries.

---

# Microsoft Agent Framework Guidelines

Use Microsoft Agent Framework concepts for agents and orchestration.

Keep framework-specific implementation isolated.

Create an abstraction layer if needed:

```python
class AgentInvoker:
    async def invoke_agent(
        self,
        agent_name: str,
        model: str,
        instructions: str,
        input_text: str,
        tools: list | None = None,
    ) -> AgentInvocationResult:
        ...
```

This allows the demo to work even if the exact Microsoft Agent Framework API changes.

Do not put framework-specific calls directly inside FastAPI routes.

FastAPI routes should call workflow services only.

---

# Foundry IQ Guidelines

Foundry IQ must be used as the knowledge source.

Keep the integration isolated behind a client:

```python
class FoundryIqKnowledgeClient:
    async def search(
        self,
        query: str,
        top_k: int,
        minimum_score: float,
    ) -> list[KnowledgeChunk]:
        ...
```

The app must support mock mode:

```text
FOUNDRY_IQ_MOCK_MODE=true
```

In mock mode, return deterministic chunks from local files.

This is required so the demo can run locally without cloud configuration.

---

# Non-Optimized Workflow Rules

In `non_optimized_workflow.py`, intentionally do the following:

```text
- Build one large context object.
- Include full transcript if enabled.
- Include all previous agent outputs if enabled.
- Use verbose prompt profile by default.
- Let every subagent call Foundry IQ if enabled.
- Use the same model for every agent unless overridden.
- Do not reuse retrieval results.
- Do not compress context.
- Return verbose output.
```

This workflow should clearly show higher token usage.

---

# Optimized Workflow Rules

In `optimized_workflow.py`, intentionally do the following:

```text
- Use scoped context.
- Retrieve once through ResearchAgent where possible.
- Reuse ResearchAgent results.
- Summarize retrieval results before passing to PlannerAgent.
- Pass only the approved plan to ImplementationAgent.
- Use cheaper models for narrow tasks.
- Use concise prompt profile by default.
- Return structured output.
- Skip unnecessary subagents if allowed by config.
```

This workflow should clearly show lower token usage.

---

# Agent Responsibilities

## DemoRunnerAgent

Responsible for:

```text
- reading the demo configuration
- choosing optimized, non-optimized, or compare mode
- starting workflows
- collecting telemetry
- producing comparison report
```

Must not perform research, planning, or implementation directly.

## ArchitectOrchestratorAgent

Responsible for:

```text
- coordinating ResearchAgent, PlannerAgent, and ImplementationAgent
- deciding what context each subagent receives
- enforcing workflow mode behavior
- collecting step results
```

Must not directly call Foundry IQ unless required by selected non-optimized behavior.

## ResearchAgent

Responsible for:

```text
- retrieving relevant knowledge from Foundry IQ
- returning short factual summaries
- returning source metadata
```

Must not create the full architecture plan.

## PlannerAgent

Responsible for:

```text
- creating architecture
- splitting work into implementation tasks
- defining agent responsibilities
- defining file structure
```

Must not perform Foundry IQ retrieval in optimized mode.

## ImplementationAgent

Responsible for:

```text
- generating code skeletons
- generating configuration examples
- generating tests
```

Must not redesign the architecture.

---

# Prompt Design Rules

Prompts must be stored as Markdown files.

Do not hardcode long prompts inside Python classes.

Use separate prompt files for optimized and non-optimized modes.

Example:

```text
backend/app/prompts/non_optimized/research.md
backend/app/prompts/optimized/research.md
```

Prompt templates may use placeholders:

```text
{{ user_request }}
{{ context }}
{{ research_summary }}
{{ plan }}
{{ output_format }}
```

Keep optimized prompts short and specific.

Non-optimized prompts may intentionally be verbose for demonstration purposes.

---

# React UX Requirements

The React UI must make cost impact obvious.

Use cards for totals:

```text
Total Tokens
Input Tokens
Output Tokens
Cached Input Tokens
Estimated Cost
Latency
Foundry IQ Calls
Agent Calls
Tool Calls
```

Use charts for comparison:

```text
Cost by workflow
Tokens by workflow
Tokens by agent
Cost by agent
Foundry IQ calls by workflow
```

Use an agent flow diagram:

```text
DemoRunnerAgent
  -> ArchitectOrchestratorAgent
      -> ResearchAgent
      -> PlannerAgent
      -> ImplementationAgent
```

Visually distinguish:

```text
optimized
non-optimized
expensive steps
cheap steps
repeated retrieval
reused context
large context handoff
scoped context handoff
```

Do not hide technical details. This is a developer and architect demo.

---

# API Response Shape

The frontend should receive enough data to render everything without recalculating business logic.

Example:

```json
{
  "runId": "run_123",
  "mode": "compare",
  "nonOptimized": {
    "totalTokens": 42000,
    "estimatedCost": 0.084,
    "steps": []
  },
  "optimized": {
    "totalTokens": 5200,
    "estimatedCost": 0.011,
    "steps": []
  },
  "savings": {
    "tokensSaved": 36800,
    "tokenReductionPercent": 87.62,
    "costSaved": 0.073,
    "costReductionPercent": 86.9
  }
}
```

---

# Testing Requirements

Write tests for:

```text
cost calculation
token estimation
optimized workflow uses fewer tokens than non-optimized workflow
Foundry IQ mock retrieval
workflow config validation
agent step metric aggregation
comparison result calculation
```

Important test:

```python
def test_optimized_workflow_is_cheaper_than_non_optimized():
    ...
```

The test should verify:

```text
optimized.total_estimated_cost < non_optimized.total_estimated_cost
optimized.total_tokens < non_optimized.total_tokens
optimized.foundry_iq_calls <= non_optimized.foundry_iq_calls
```

---

# Demo Data

Use the transcript as a sample knowledge source.

Place it here:

```text
backend/data/transcript.txt
```

In mock Foundry IQ mode, retrieve relevant chunks from this local file.

The transcript content should be chunked into sections such as:

```text
agent quality
context window
model selection
prompt strategy
tests as guardrails
agent configuration
MCP
subagents
long-term strategy
```

---

# Report Requirements

Generate Markdown reports.

Each report should include:

```text
# Agent Cost Optimization Report

## Configuration

## Non-Optimized Result

## Optimized Result

## Savings

## Agent Step Breakdown

## Observations

## Recommendations
```

Include a table:

```md
| Metric | Non-Optimized | Optimized | Savings |
|---|---:|---:|---:|
| Input tokens | | | |
| Output tokens | | | |
| Cached input tokens | | | |
| Total tokens | | | |
| Estimated cost | | | |
| Foundry IQ calls | | | |
| Latency | | | |
```

---

# Coding Style

Python:

- use Python 3.11+
- use type hints
- use Pydantic models
- keep functions small
- prefer dependency injection
- keep agents separate from API routes
- keep telemetry separate from business logic
- write async code for agent calls and retrieval
- isolate external service integrations

React:

- use TypeScript
- use functional components
- keep API logic in `src/api`
- keep shared types in `src/types`
- keep UI components reusable
- avoid large page components
- use clear names
- keep charts simple and readable

---

# Do Not Do

Do not:

```text
- hardcode model pricing
- hardcode cloud credentials
- place Foundry IQ logic inside React
- place Agent Framework calls inside FastAPI routes
- make all agents use the same context in optimized mode
- make optimized and non-optimized workflows behave the same
- hide token and cost details
- skip telemetry
- generate one huge file
- create fake savings without explaining where savings came from
- couple UI directly to provider-specific SDKs
```

---

# Expected Demo Story

The final app should allow the presenter to say:

```text
First, I will run the non-optimized workflow.
Every subagent receives full context, every subagent retrieves knowledge, and all agents use the same expensive model.

Now I will run the optimized workflow.
Only the research agent retrieves from Foundry IQ, the result is summarized, the planner gets only the summary, and the implementation agent gets only the plan.

Now we compare the cost.
The optimized workflow uses fewer tokens, fewer retrieval calls, and lower estimated cost.
```

The demo must make this difference visible in the UI.

---

# Final Goal

The project should clearly demonstrate that agent cost optimization is not only about using fewer tokens.

It is about:

```text
- better context engineering
- better model routing
- better workflow design
- fewer repeated retrievals
- smaller handoffs between agents
- clearer prompts
- deterministic telemetry
- measurable cost control
```

Build the project so it can be used as a practical presentation and demo for developers and architects.
