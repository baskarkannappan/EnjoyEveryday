\# EnjoyEveryday



\## Strict Implementation Sequence, Process Flow \& Wireframe Compliance Guide



\*\*Purpose:\*\*

This document defines exactly \*\*how EnjoyEveryday must be implemented\*\*, in what order, how each process must flow through the system, and how Antigravity must use the approved wireframes.



This document is an \*\*implementation control document\*\*.



It is not permission to redesign the product.



\---



\# 1. GOLDEN RULE



\## The approved wireframe is the UI source of truth.



Antigravity must strictly implement the approved wireframe.



Do NOT:



\* redesign the page

\* change the navigation

\* invent a new layout

\* move major components

\* remove buttons

\* add new buttons

\* change terminology

\* change workflows

\* add dashboards that are not in the wireframe

\* replace the design with a preferred design system

\* introduce a different UX pattern because it seems better

\* simplify the workflow by removing required steps

\* create additional pages without approval



If the implementation discovers a technical limitation or missing requirement:



\*\*STOP → REPORT → ASK\*\*



Do not silently redesign.



\---



\# 2. WHAT THE WIREFRAME DEFINES



The wireframe defines the intended:



\* page structure

\* navigation

\* menu names

\* buttons

\* actions

\* forms

\* fields

\* cards

\* dialogs

\* modals

\* filters

\* search

\* tabs

\* states

\* empty states

\* loading states

\* error states

\* confirmation flows

\* user interactions

\* visual hierarchy

\* terminology

\* page relationships



The backend implementation must support the wireframe.



The wireframe must NOT be changed simply to fit the backend.



\---



\# 3. WHAT THE WIREFRAME DOES NOT DEFINE



The wireframe does not necessarily define:



\* database structure

\* API implementation

\* SQL

\* Dapper implementation

\* domain services

\* authorization implementation

\* validation implementation

\* audit implementation

\* AI orchestration

\* background processing

\* caching

\* graph projection

\* internal architecture



These must be implemented behind the wireframe.



Therefore:



```text

WIREFRAME

&#x20;   ↓

USER ACTION

&#x20;   ↓

BLazor UI

&#x20;   ↓

REST API

&#x20;   ↓

APPLICATION LOGIC

&#x20;   ↓

DOMAIN RULES

&#x20;   ↓

DAPPER

&#x20;   ↓

POSTGRESQL

```



For AI:



```text

USER

&#x20;↓

BLazor

&#x20;↓

API

&#x20;↓

AI APPLICATION SERVICE

&#x20;↓

GOOGLE ADK

&#x20;↓

AGENT

&#x20;↓

CONTROLLED TOOLS

&#x20;↓

APPLICATION VALIDATION

&#x20;↓

DATABASE

```



AI must never bypass application rules.



\---



\# 4. IMPLEMENTATION ORDER



Antigravity must follow this sequence.



\## Phase 1 — Foundation



Implement:



1\. Solution structure

2\. Configuration

3\. PostgreSQL connection

4\. Dapper infrastructure

5\. REST API

6\. Swagger/OpenAPI

7\. Blazor Server

8\. Fluent UI

9\. Authentication foundation

10\. Authorization foundation

11\. Tenant context

12\. Error handling

13\. Logging

14\. Audit foundation



Do not build AI yet.



Do not build analytics yet.



Do not build advanced dashboards yet.



\---



\# 5. PHASE 2 — ORGANIZATION



Implement:



1\. Tenant

2\. Organization

3\. Branch

4\. Classroom

5\. Organization settings

6\. Tenant configuration

7\. Environment/material configuration



Process:



```text

Create Tenant

&#x20;   ↓

Create Organization

&#x20;   ↓

Create Branch

&#x20;   ↓

Create Classroom

&#x20;   ↓

Configure Classroom

```



Every record must contain appropriate tenant ownership.



No tenant can access another tenant's data.



\---



\# 6. PHASE 3 — PEOPLE



Implement:



1\. Users

2\. Roles

3\. Permissions

4\. Teachers

5\. Children

6\. Parents

7\. Family relationships



Process:



```text

Create Child

&#x20;   ↓

Assign Child to Classroom

&#x20;   ↓

Create Parent/Family Relationship

&#x20;   ↓

Authorize Family Access

```



A parent must only see children they are authorized to access.



\---



\# 7. PHASE 4 — EXPERIENCE FOUNDATION



This is the most important domain.



Create the Experience domain before building AI.



Experience is the central domain concept.



Basic lifecycle:



```text

Experience Idea

&#x20;     ↓

Experience Created

&#x20;     ↓

Experience Saved

&#x20;     ↓

Experience Approved

&#x20;     ↓

Experience Scheduled

&#x20;     ↓

Experience Started

&#x20;     ↓

Experience Happens

&#x20;     ↓

Experience Feedback

&#x20;     ↓

Experience History

&#x20;     ↓

Experience Harvest

```



Everything later depends on this lifecycle.



\---



\# 8. EXPERIENCE CREATION PROCESS



The teacher/management workflow must follow the approved Experience Studio wireframe.



Conceptual process:



```text

Start with an Idea

&#x20;       ↓

Explore Possibilities

&#x20;       ↓

Shape Experience

&#x20;       ↓

Experience DNA

&#x20;       ↓

Safety Check

&#x20;       ↓

Preview

&#x20;       ↓

Save

&#x20;       ↓

Submit / Publish

```



The system should support:



\* Mission

\* Discovery

\* Challenge

\* Together

\* Create

\* Child Choice

\* Magic Moment

\* Accomplishment

\* Reflection

\* Teacher Guidance

\* Enjoyment DNA

\* Safety



The teacher remains the creator.



AI may assist.



AI must not silently create or publish content.



\---



\# 9. EXPERIENCE LIBRARY



After Experience creation, implement the Experience Library.



Library responsibilities:



\* store experiences

\* search experiences

\* filter experiences

\* favorite experiences

\* duplicate experiences

\* edit experiences

\* archive experiences

\* schedule experiences

\* view history

\* view versions



Library concept:



```text

EXPERIENCE LIBRARY

&#x20;       |

&#x20;       +-- Created by Us

&#x20;       +-- Created by Me

&#x20;       +-- AI Suggested

&#x20;       +-- Favorites

&#x20;       +-- Used

&#x20;       +-- Recently Used

&#x20;       +-- Needs Improvement

```



The Library is institutional memory.



It should become the organization's collection of experiences.



\---



\# 10. EXPERIENCE VERSIONING



Never overwrite important experience history.



Example:



```text

Our Little City v1

&#x20;       ↓

Teacher feedback

&#x20;       ↓

Bridge challenge added

&#x20;       ↓

Our Little City v2

&#x20;       ↓

Animal rescue added

&#x20;       ↓

Our Little City v3

```



Each version should retain:



\* version number

\* creator

\* date

\* reason for change

\* changes

\* feedback

\* status



Old versions must remain traceable.



\---



\# 11. MONTHLY PLANNER



Only after Experience Library and Versioning are working.



Process:



```text

Management opens Planner

&#x20;       ↓

Select Month

&#x20;       ↓

Select Classroom / Age Group

&#x20;       ↓

Select Experience

&#x20;       ↓

Assign Date

&#x20;       ↓

Assign Time

&#x20;       ↓

Publish

```



The planner is a guide.



It is not a rigid command system.



Teachers must be able to adapt when children's interests change.



\---



\# 12. TEACHER TODAY



The Teacher's primary screen is TODAY.



It should answer:



1\. Who is here?

2\. What is happening?

3\. What should I do next?

4\. What do I need?

5\. Can the system help me?



Process:



```text

Teacher opens Today

&#x20;       ↓

Current Classroom Context

&#x20;       ↓

Today's Experiences

&#x20;       ↓

Current Experience

&#x20;       ↓

START

```



The teacher should not need to navigate through administration screens to start an experience.



\---



\# 13. EXPERIENCE EXECUTION



When the teacher clicks START:



```text

Scheduled

&#x20;   ↓

Started

&#x20;   ↓

Experience In Progress

&#x20;   ↓

Experience Happens

&#x20;   ↓

Teacher Completes

```



During execution, show only information useful to the teacher.



Examples:



\* Mission

\* Teacher prompt

\* What to watch

\* Questions

\* Variations

\* Safety reminders

\* Materials



Do not turn the experience into a software task list.



The child remains the focus.



\---



\# 14. EXPERIENCE FEEDBACK



After the experience:



```text

Experience Completed

&#x20;       ↓

Teacher Feedback

&#x20;       ↓

What Worked?

&#x20;       ↓

What Didn't?

&#x20;       ↓

Optional Observation

&#x20;       ↓

Optional Voice Note

&#x20;       ↓

Optional Accomplishment

&#x20;       ↓

Save

```



Simple feedback should be possible.



Example:



```text

❤️ Loved it



🙂 Good



😐 Difficult



🙅 Didn't work

```



The teacher should not be forced to complete a long form.



\---



\# 15. EXPERIENCE HISTORY



Every execution creates history.



Example:



```text

Experience:

Our Little City



Used:

12 times



Teachers:

7



Common observation:

Children enjoyed building bridges.



Common adjustment:

Teachers added animals.



Common difficulty:

Drawing phase sometimes lost attention.

```



History is factual evidence.



Do not fabricate information.



\---



\# 16. EXPERIENCE HARVEST



Harvest converts experience history into learning.



Process:



```text

Experience History

&#x20;       ↓

Feedback

&#x20;       ↓

Observations

&#x20;       ↓

Pattern Detection

&#x20;       ↓

Experience Insight

&#x20;       ↓

Suggested Improvement

&#x20;       ↓

Human Review

&#x20;       ↓

New Version

```



Example:



```text

Observed repeatedly:



Children enjoy bridge building.



Suggestion:



Add a bridge challenge earlier.



Action:



Create v2.

```



The system must distinguish:



\*\*Observed fact\*\*



from



\*\*AI suggestion\*\*



They must never be presented as the same thing.



\---



\# 17. CHILD JOURNEY



Child Journey comes after the experience lifecycle works.



Process:



```text

Child participates

&#x20;       ↓

Experience happens

&#x20;       ↓

Teacher observes

&#x20;       ↓

Meaningful observation

&#x20;       ↓

Accomplishment / Interest

&#x20;       ↓

Child Journey

```



Child Journey should describe the child as a human being.



Example:



```text

Aarav



Interests:

Building

Bugs

Helping friends



Recent moments:

Built the bridge again after it fell.



Accomplishment:

Kept trying after the first attempt failed.

```



Avoid:



\* scores

\* rankings

\* percentages

\* leaderboards

\* competitive comparisons



\---



\# 18. PARENT EXPERIENCE



Parent experience is built after Child Journey.



Parent process:



```text

Child Experience

&#x20;       ↓

Meaningful Moment

&#x20;       ↓

Approved Story

&#x20;       ↓

Parent View

&#x20;       ↓

Memory

&#x20;       ↓

Child Journey

```



Example:



> “Today Aarav built a bridge. It fell three times. The fourth time he said, ‘Let's make the bottom bigger.’”



Parents should feel they are looking into their child's world.



\---



\# 19. SAFETY \& TRUST



Safety must exist across the entire platform.



It is not only a page.



Safety applies to:



\* children

\* permissions

\* consent

\* media

\* AI

\* access

\* audit

\* incidents

\* privacy

\* data sharing



Core rule:



```text

Authorization

&#x20;     ↓

Relationship

&#x20;     ↓

Consent

&#x20;     ↓

Purpose

&#x20;     ↓

Data Sensitivity

&#x20;     ↓

Allow / Deny

```



Safety rules must be enforced server-side.



Never rely only on UI hiding.



\---



\# 20. ORGANIZATION VIEW



After the core operational workflow works, build organization-level visibility.



Management should understand:



\* branches

\* classrooms

\* experience rhythm

\* adoption

\* planning

\* operational needs

\* safety/trust

\* meaningful trends



Do not turn the organization view into teacher rankings.



The purpose is:



\*\*Help management create a good environment.\*\*



\---



\# 21. AI FOUNDATION



AI is implemented only after the core domain is working.



AI architecture:



```text

Application

&#x20;   ↓

AI Gateway

&#x20;   ↓

Google ADK

&#x20;   ↓

Agent

&#x20;   ↓

Controlled Tool

&#x20;   ↓

Application Validation

&#x20;   ↓

Database

```



Initial agents:



\* Experience Planner

\* Experience Improvement

\* Classroom Context

\* Child Journey

\* Parent Story

\* Experience Search

\* Safety Review

\* Coordinator



\---



\# 22. AI RULES



The following rule is mandatory:



\## AI PROPOSES.



\## APPLICATION VALIDATES.



\## HUMAN DECIDES.



\## DATABASE RECORDS.



AI must NOT:



\* bypass authorization

\* bypass consent

\* override safety

\* invent observations

\* invent child events

\* diagnose children

\* directly modify protected records

\* access another tenant

\* automatically publish content

\* create fake memories

\* expose private information



\---



\# 23. GRAPH INTELLIGENCE



SQLite Graph is a projection.



PostgreSQL is authoritative.



Therefore:



```text

PostgreSQL

&#x20;    ↓

Graph Projection

&#x20;    ↓

SQLite Graph

```



Do not make SQLite Graph the source of truth.



If the graph is deleted:



```text

PostgreSQL

&#x20;    ↓

Rebuild Graph

```



The system must continue to work.



\---



\# 24. DATABASE RULE



PostgreSQL is the system of record.



Use:



\* PostgreSQL

\* Dapper

\* pgvector



Use SQLite Graph for relationship intelligence.



Do not store authoritative business data only inside the graph.



\---



\# 25. API RULE



Every important UI action must have an application/API path.



Example:



```text

Teacher clicks START

&#x20;       ↓

Blazor

&#x20;       ↓

REST API

&#x20;       ↓

Application Service

&#x20;       ↓

Validate

&#x20;       ↓

Domain Rules

&#x20;       ↓

Dapper

&#x20;       ↓

PostgreSQL

&#x20;       ↓

Result

&#x20;       ↓

Blazor

```



Do not put business logic only inside Blazor.



\---



\# 26. DAPPER RULE



Dapper is the database access mechanism.



Do not create random database access throughout the UI.



Use a clear structure:



```text

API

&#x20;↓

Application Service

&#x20;↓

Repository / Query Service

&#x20;↓

Dapper

&#x20;↓

PostgreSQL

```



Keep SQL understandable and maintainable.



\---



\# 27. TENANT ISOLATION



Every tenant-sensitive operation must carry tenant context.



Tenant context must flow through:



```text

User

&#x20;↓

Blazor

&#x20;↓

API

&#x20;↓

Application

&#x20;↓

Dapper

&#x20;↓

PostgreSQL

&#x20;↓

AI

&#x20;↓

Graph

&#x20;↓

Background Jobs

```



Never trust a tenant ID supplied by the browser without server-side validation.



\---



\# 28. AUTHORIZATION



Authorization must happen at the backend.



Do not rely on:



```text

if user can see button

```



as security.



Instead:



```text

User

&#x20;↓

Authentication

&#x20;↓

Claims

&#x20;↓

Policy

&#x20;↓

Resource

&#x20;↓

Tenant

&#x20;↓

Action

&#x20;↓

Allow / Deny

```



The UI may hide unavailable actions.



The backend must still enforce them.



\---



\# 29. AUDIT



Audit important operations.



Examples:



\* child created

\* child updated

\* parent relationship changed

\* experience created

\* experience approved

\* experience version created

\* experience scheduled

\* experience completed

\* observation recorded

\* media permission changed

\* AI request made

\* AI recommendation accepted

\* AI recommendation rejected

\* sensitive data accessed



Audit should answer:



```text

WHO

WHAT

WHEN

WHERE

WHY

RESULT

```



\---



\# 30. WIRE FRAME IMPLEMENTATION PROCESS



For every wireframe page, Antigravity must follow this process.



\## Step 1 — Inspect



Read the existing code.



Do not create new code immediately.



Determine:



\* existing components

\* existing layouts

\* existing services

\* existing APIs

\* existing database objects

\* existing styles

\* existing navigation



\---



\## Step 2 — Analyze Wireframe



Identify:



\* page

\* layout

\* navigation

\* sections

\* cards

\* buttons

\* forms

\* fields

\* modals

\* filters

\* actions

\* states

\* workflows



Create an internal implementation checklist.



\---



\## Step 3 — Map UI to Domain



For every UI action determine:



```text

UI Element

&#x20;   ↓

Action

&#x20;   ↓

API Endpoint

&#x20;   ↓

Application Service

&#x20;   ↓

Database Operation

```



Example:



```text

\[START]



POST /api/experiences/{id}/start



&#x20;       ↓



StartExperience()



&#x20;       ↓



Validate:

\- tenant

\- classroom

\- authorization

\- schedule

\- current state



&#x20;       ↓



PostgreSQL



&#x20;       ↓



Experience = InProgress

```



\---



\# 31. DO NOT IMPLEMENT UI FIRST AND BACKEND LATER



The feature should be implemented vertically.



Correct:



```text

Experience

&#x20;↓

Database

&#x20;↓

Dapper

&#x20;↓

Application

&#x20;↓

API

&#x20;↓

Swagger

&#x20;↓

Authorization

&#x20;↓

Blazor

&#x20;↓

Fluent UI

&#x20;↓

Wireframe Interaction

```



Not:



```text

Create beautiful UI

&#x20;       ↓

figure out backend later

```



\---



\# 32. WIRE FRAME FIDELITY CHECK



Before marking a page complete, compare implementation against the wireframe.



Check:



\### Layout



\* Is the page structure the same?

\* Are major sections present?

\* Is navigation correct?



\### Components



\* Are all cards present?

\* Are all buttons present?

\* Are all fields present?

\* Are all dialogs present?



\### Terminology



\* Are labels exactly aligned with the approved design?

\* Were terms changed unnecessarily?



\### Interaction



\* Does every button perform the intended action?

\* Do filters work?

\* Does search work?

\* Do modals work?

\* Do forms validate?

\* Do state changes work?



\### Responsive behavior



\* Desktop

\* Tablet

\* Mobile



\### States



\* Loading

\* Empty

\* Success

\* Error

\* Disabled

\* In-progress



\---



\# 33. NO SILENT DESIGN CHANGES



If Antigravity thinks:



> “This page would be better if we moved this button.”



Do not do it.



If it thinks:



> “This workflow would be easier with another page.”



Do not do it.



If it thinks:



> “This component would look better another way.”



Do not do it.



Instead report:



```text

WIRE FRAME CONFLICT



Page:

Experience Studio



Issue:

Current backend requires an additional approval state.



Impact:

The existing wireframe does not show this state.



Recommendation:

Need product decision before implementation.



STATUS:

BLOCKED — awaiting decision

```



\---



\# 34. NEW UI REQUIREMENT RULE



If implementation requires something not present in the wireframe:



\### Case 1 — Technical requirement



Implement it invisibly if possible.



Example:



\* loading state

\* API error handling

\* security enforcement

\* database transaction



These do not require redesign.



\### Case 2 — User-facing requirement



Do NOT invent the UI.



Ask for approval.



Examples:



\* new button

\* new page

\* new dialog

\* new workflow

\* new navigation item

\* new form field

\* new status



\---



\# 35. EXISTING UI REUSE RULE



Before creating a new component:



1\. Search existing code.

2\. Find reusable component.

3\. Reuse it if compatible.

4\. Extend it if necessary.

5\. Create a new component only when required.



Do not duplicate:



\* buttons

\* dialogs

\* tables

\* cards

\* forms

\* notification components

\* navigation

\* API clients



\---



\# 36. NO FAKE DATA IN PRODUCTION LOGIC



Demo/mock data may be used only when explicitly building a wireframe prototype.



Once real functionality is being implemented:



Do not fake:



\* children

\* observations

\* accomplishments

\* parent messages

\* experience history

\* AI insights

\* safety events

\* tenant data



If data is unavailable, show the correct empty state.



\---



\# 37. ERROR HANDLING



Every workflow must handle:



\### Success



```text

Action completed

```



\### Validation failure



```text

Explain what must be corrected.

```



\### Authorization failure



```text

Access denied.

```



\### Not found



```text

Record no longer exists.

```



\### Conflict



```text

Record changed by another user.

```



\### System failure



```text

Something went wrong.

Try again.

```



Do not expose technical stack traces to users.



\---



\# 38. TRANSACTION RULE



Operations that change multiple related records must use appropriate transactional boundaries.



Example:



```text

Complete Experience

&#x20;       ↓

Create Experience History

&#x20;       ↓

Save Feedback

&#x20;       ↓

Create Observation

&#x20;       ↓

Create Accomplishment

```



The application must define which operations are atomic.



\---



\# 39. STATE MACHINE RULE



Important lifecycle states must be explicit.



Example:



```text

Draft

&#x20;↓

Approved

&#x20;↓

Scheduled

&#x20;↓

InProgress

&#x20;↓

Completed

```



Invalid transitions must be rejected.



For example:



```text

Archived → InProgress

```



should not happen unless explicitly allowed by the domain.



\---



\# 40. END-TO-END COMPLETION RULE



A requirement is NOT complete because:



\* the page renders

\* the button exists

\* the API exists

\* the database table exists



A requirement is complete only when:



```text

Requirement

&#x20;↓

Database

&#x20;↓

Dapper

&#x20;↓

Application Logic

&#x20;↓

API

&#x20;↓

Swagger

&#x20;↓

Authorization

&#x20;↓

Validation

&#x20;↓

Blazor

&#x20;↓

Fluent UI

&#x20;↓

Wireframe Interaction

&#x20;↓

Error Handling

&#x20;↓

Audit where required

&#x20;↓

End-to-End Workflow

```



\---



\# 41. REQUIREMENT COMPLETION MATRIX



At the end of every requirement, generate a matrix.



Example:



| Layer           | Status   |

| --------------- | -------- |

| Requirement     | Complete |

| Database        | Complete |

| Dapper          | Complete |

| Application     | Complete |

| API             | Complete |

| Swagger         | Complete |

| Authorization   | Complete |

| Validation      | Complete |

| Blazor          | Complete |

| Fluent UI       | Complete |

| Wireframe       | Complete |

| Error Handling  | Complete |

| Audit           | Complete |

| End-to-End Flow | Complete |



If something is incomplete, clearly mark it.



Do not claim completion prematurely.



\---



\# 42. DEVELOPMENT SEQUENCE



The overall implementation sequence is:



```text

1\. Foundation

&#x20;       ↓

2\. Organization

&#x20;       ↓

3\. Branch

&#x20;       ↓

4\. Classroom

&#x20;       ↓

5\. Users / Roles

&#x20;       ↓

6\. Children

&#x20;       ↓

7\. Families

&#x20;       ↓

8\. Experience

&#x20;       ↓

9\. Experience Library

&#x20;       ↓

10\. Experience Studio

&#x20;       ↓

11\. Experience Versioning

&#x20;       ↓

12\. Monthly Planner

&#x20;       ↓

13\. Teacher Today

&#x20;       ↓

14\. Experience Execution

&#x20;       ↓

15\. Experience Feedback

&#x20;       ↓

16\. Experience History

&#x20;       ↓

17\. Experience Harvest

&#x20;       ↓

18\. Child Journey

&#x20;       ↓

19\. Parent Experience

&#x20;       ↓

20\. Safety \& Trust

&#x20;       ↓

21\. Organization View

&#x20;       ↓

22\. AI Foundation

&#x20;       ↓

23\. AI Experience Intelligence

&#x20;       ↓

24\. AI Child Journey Intelligence

&#x20;       ↓

25\. AI Parent Story

&#x20;       ↓

26\. Graph Intelligence

&#x20;       ↓

27\. Advanced Organization Intelligence

```



Do not jump ahead unless explicitly instructed.



\---



\# 43. FIRST COMPLETE PRODUCT FLOW



The first real end-to-end demonstration must be:



```text

Create Tenant

&#x20;     ↓

Create Branch

&#x20;     ↓

Create Classroom

&#x20;     ↓

Create Teacher

&#x20;     ↓

Create Children

&#x20;     ↓

Create Parent

&#x20;     ↓

Create Experience

&#x20;     ↓

Save Experience

&#x20;     ↓

Add to Library

&#x20;     ↓

Schedule Experience

&#x20;     ↓

Teacher Opens TODAY

&#x20;     ↓

Teacher Starts Experience

&#x20;     ↓

Experience Happens

&#x20;     ↓

Teacher Records Feedback

&#x20;     ↓

Experience History Created

&#x20;     ↓

Observation Recorded

&#x20;     ↓

Accomplishment Recorded

&#x20;     ↓

Child Journey Updated

&#x20;     ↓

Parent Story Created

&#x20;     ↓

Parent Views Moment

```



Only after this complete flow works should advanced AI be added.



\---



\# 44. AI SHOULD COME AFTER THE PRODUCT HAS SOMETHING TO LEARN FROM



Do not build an AI recommendation engine before there is real domain data.



The correct progression is:



```text

Build Experiences

&#x20;       ↓

Use Experiences

&#x20;       ↓

Capture Feedback

&#x20;       ↓

Capture History

&#x20;       ↓

Build Child Journey

&#x20;       ↓

Build Memory

&#x20;       ↓

Then AI learns

```



AI should learn from the product.



The product should not be built around an AI chatbot.



\---



\# 45. ANTIGRAVITY WORKING MODE



For every requirement, Antigravity should work in this order:



```text

READ MASTER DOCUMENT

&#x20;       ↓

READ THIS IMPLEMENTATION GUIDE

&#x20;       ↓

READ APPROVED WIREFRAME

&#x20;       ↓

INSPECT EXISTING CODE

&#x20;       ↓

IDENTIFY EXISTING COMPONENTS

&#x20;       ↓

IDENTIFY DOMAIN OBJECTS

&#x20;       ↓

DESIGN DATABASE CHANGE

&#x20;       ↓

IMPLEMENT DAPPER

&#x20;       ↓

IMPLEMENT APPLICATION LOGIC

&#x20;       ↓

IMPLEMENT API

&#x20;       ↓

UPDATE SWAGGER

&#x20;       ↓

IMPLEMENT AUTHORIZATION

&#x20;       ↓

IMPLEMENT VALIDATION

&#x20;       ↓

IMPLEMENT BLAZOR

&#x20;       ↓

IMPLEMENT FLUENT UI

&#x20;       ↓

MATCH WIREFRAME

&#x20;       ↓

CONNECT REAL INTERACTIONS

&#x20;       ↓

HANDLE ERRORS

&#x20;       ↓

AUDIT

&#x20;       ↓

RUN END-TO-END FLOW

&#x20;       ↓

COMPARE AGAINST WIREFRAME

&#x20;       ↓

GENERATE COMPLETION MATRIX

```



\---



\# 46. ANTIGRAVITY MUST NOT DO THESE THINGS



Do not:



\* redesign the approved wireframe

\* invent product requirements

\* invent workflows

\* invent business rules

\* invent child observations

\* invent AI conclusions

\* create unnecessary microservices

\* add unnecessary technologies

\* bypass REST APIs

\* put business logic in Blazor

\* let AI directly modify protected data

\* use SQLite Graph as the source of truth

\* bypass tenant isolation

\* bypass authorization

\* skip validation

\* skip error handling

\* skip audit where required

\* replace PostgreSQL with another database

\* replace Dapper without approval

\* replace Fluent UI without approval

\* replace Google ADK without approval

\* create competing navigation systems

\* create duplicate pages

\* create duplicate components

\* mark incomplete functionality as complete



\---



\# 47. DECISION PRIORITY



When two things conflict, use this priority:



```text

1\. Safety

2\. Approved Product Requirement

3\. Approved Wireframe

4\. Domain Rules

5\. Architecture Rules

6\. Existing Implementation

7\. Developer Convenience

```



Developer convenience must never override product behavior.



\---



\# 48. PRODUCT PHILOSOPHY MUST NEVER BE LOST



EnjoyEveryday is not supposed to become:



> another daycare administration system.



The system should help create:



\*\*Wonderful days for children.\*\*



Therefore:



Teacher:



> “Help me create a wonderful day.”



Management:



> “Help me create a good environment.”



Parent:



> “Show me what happened in my child's world.”



AI:



> “Here is something interesting I noticed.”



Child:



> \*\*“Enjoy Everyday.”\*\*



\---



\# 49. FINAL IMPLEMENTATION PRINCIPLE



Build slowly.



Build vertically.



Build one domain at a time.



Make every workflow real.



Keep PostgreSQL authoritative.



Keep AI behind application rules.



Keep tenant boundaries strict.



Keep the child at the center.



And most importantly:



\# DO NOT REDESIGN THE WIREFRAME.



The approved wireframe represents the product decision.



Antigravity's job is to \*\*implement the approved experience faithfully\*\*, not to replace it with its own interpretation.



If a change is necessary:



\*\*STOP → EXPLAIN → ASK → WAIT FOR APPROVAL → IMPLEMENT.\*\*



\---



\# 50. DEFINITION OF DONE



A feature is DONE only when:



\*\*The database works.\*\*



\*\*The application logic works.\*\*



\*\*The API works.\*\*



\*\*Swagger represents the API.\*\*



\*\*Authorization works.\*\*



\*\*Validation works.\*\*



\*\*Dapper works.\*\*



\*\*Blazor works.\*\*



\*\*Fluent UI matches the approved wireframe.\*\*



\*\*Every intended interaction works.\*\*



\*\*Errors are handled.\*\*



\*\*Audit is implemented where required.\*\*



\*\*The complete business process works end-to-end.\*\*



And:



> \*\*The implemented UI must remain faithful to the approved wireframe.\*\*



This document, together with the \*\*EnjoyEveryday Product Vision and Implementation Order\*\* document and the approved wireframes, forms the implementation baseline for Antigravity.



