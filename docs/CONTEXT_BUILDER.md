# Context Builder

`ContextBuilder` assembles the minimum useful context for reasoning without dumping the full database.

## Prioritization

The context window is intentionally ordered around the current conversation:

1. Recent messages from the active conversation
2. Relevant memories scored against the active topic and recent user text
3. Open tasks
4. Active goals
5. Active projects
6. Open loops
7. Pending approvals
8. Chief Of Staff insights

## Limits

- Recent messages: 12
- Relevant memories: 6
- Open tasks: 8
- Active goals: 6
- Active projects: 6
- Open loops: 6
- Pending approvals: 5
- Chief Of Staff insights: 4

These limits keep prompts stable and predictable while still giving the reasoning engine enough signal to be useful.

## Memory Selection

Relevant memories are scored by:

- exact or near-exact matches to the current topic
- term overlap with recent user messages
- stored memory importance
- recency of reference

This biases context toward memories that are both meaningful and timely.
