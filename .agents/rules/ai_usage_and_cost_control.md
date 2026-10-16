# Rule: AI Usage and Cost-Control Architecture Rules

These rules apply to the current AI Child Progress Insights implementation and all future EnjoyEveryday features.

## Objective

Keep EnjoyEveryday a reliable daycare management application with selective AI capabilities.

AI is an optional supporting service, not a mandatory dependency for core application operations.

## 1. Rules-First Architecture

Use ordinary application code, PostgreSQL, existing repositories, and deterministic business rules for:

- Authentication and authorization.
- Tenant and organization isolation.
- Attendance and reporting.
- Scheduling and conflict detection.
- Billing, payments, and marketplace entitlements.
- Consent and safety workflows.
- Memory retrieval and chronological timelines.
- Data validation and record updates.
- Dashboard metrics and operational reporting.

Do not invoke an AI model for tasks that can be performed reliably using ordinary application logic.

## 2. AI Only When It Adds Clear Value

Use the existing Google ADK integration selectively for tasks such as:

- Explaining why an eligible activity may be suitable.
- Summarizing authorized child observations.
- Suggesting possible learning opportunities from existing evidence.
- Drafting a weekly plan for teacher review.
- Improving the wording of a parent communication draft.

Do not create new agents, AI services, or model calls if an existing service can support the requirement.

## 3. On-Demand Execution

Do not continuously generate AI insights or recommendations in the background by default.

Generate insights when an authorized user requests them, or when a documented business requirement justifies scheduled generation.

Avoid automatically regenerating insights on every page load, filter change, navigation event, or minor database update.

Use bounded inputs, appropriate timeouts, cancellation, and request limits.

## 4. Deterministic Fallback

Every AI-assisted feature must have a useful non-AI behavior where practical.

Examples:

- Activity recommendations use existing eligibility and ranking rules.
- Memory timelines use normal database queries.
- Dashboards use SQL aggregates.
- Parent summaries can use approved templates.
- Child progress views can display actual records even when AI is unavailable.

Fallbacks must not fabricate facts or imply that an AI analysis was completed.

Clearly distinguish a deterministic summary from an AI-generated interpretation when necessary.

## 5. Cost and Performance Controls

Reuse the existing model provider and configuration.

Do not introduce another AI provider or framework without a documented business reason.

Implement appropriate controls for:

- Maximum input size.
- Maximum output size.
- Request timeouts.
- Retry limits.
- Rate limiting.
- Token usage and estimated cost tracking, where provider metadata permits.
- Caching, only when safe and appropriate.
- Duplicate request prevention.
- AI provider failure handling.

Do not cache personalized child insights across users or organizations without a rigorously designed authorization-aware cache strategy.

Do not store sensitive prompts or child information in logs unnecessarily.

## 6. Data Minimization

Retrieve and send only the data required for the requested AI task.

Use aggregate classroom context when individual child details are unnecessary.

Never send unrestricted database records, full child histories, family information, credentials, or access tokens to the model.

Apply tenant, organization, child, record visibility, and consent checks before assembling the model input.

AI output must never grant access to a record that the requesting user could not otherwise access.

## 7. No AI-Controlled Business Decisions

AI must not independently:

- Grant permissions or memberships.
- Approve consent.
- Verify payments.
- Grant marketplace entitlements.
- Publish or commercially license experiences.
- Override safety rules.
- Record attendance or participation.
- Create authoritative child observations or milestones.
- Send parent communications without the existing authorization and approval workflow.

Validate all AI-generated identifiers and suggestions against trusted application data.

## 8. Keep the Architecture Simple

Prefer the existing application services and Google ADK integration.

Do not introduce microservices, event streaming, a vector database, additional agent frameworks, or complex orchestration solely to support these features.

Use PostgreSQL and existing retrieval interfaces unless measured requirements justify additional infrastructure.

Do not add pgvector just because AI is present.

## 9. Implementation Requirements

For every new AI-assisted feature:

1. Identify the exact business problem AI solves.
2. Determine whether deterministic logic is sufficient.
3. Reuse existing services where practical.
4. Make AI invocation optional wherever possible.
5. Implement a safe fallback.
6. Validate output before presenting or using it.
7. Add authorization, failure, and cost-control tests.
8. Document the expected operational cost and remaining risks.

Do not add AI to a feature simply to label it AI-powered.

## Final Rule

EnjoyEveryday must continue to perform its essential daycare operations when the AI provider is unavailable, misconfigured, rate-limited, or disabled.

Prioritize reliability, child privacy, security, maintainability, predictable cost, and teacher usability over the number of AI features.
