# LastBreath

Конвенции, стек и правила работы лежат в `src/Docs/CLAUDE.md` — импорт ниже подтягивает их в каждую сессию и в каждого субагента, отдельно читать файл не нужно.

@src/Docs/CLAUDE.md

## Где что лежит

- Задачи и очередь — доска #3 (`Toddless/Last-Breath`), единственный трекер.
- Замысел по игровому дизайну — Obsidian-хранилище `D:\Programms\Obsidian\Projects\Projects\Last Breath`; `src/Docs/CombatLayers.md` устарел.
- Архитектурный контекст — `src/Docs/HANDOFF.md`, принятые решения — `src/Docs/decisions.md`, бэклог minor/nit — `src/Docs/backlog.md`.
- Правки в `src/Docs` зеркалятся в `docs/last-breath/` приватного репозитория `Toddless/agent-settings`.
- Роли агентов — `.claude/agents/executor.md`, `.claude/agents/reviewer.md`, ведущий — команда `/lead`. Источник ролей и шаблонов — тот же `agent-settings`, `.claude/` здесь под gitignore.
