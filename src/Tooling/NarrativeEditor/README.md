# Narrative Editor

Второй хост тулинга на той же библиотеке, что и `src/Tooling/DataEditor`: отдельный Godot-проект,
который открывает те же данные (`src/SharedData`) и показывает нарратив не полями записи, а
**структурным аутлайном** — узлами, репликами и опциями диалога, стадиями, целями, переходами и
исходами квеста. Правка любой строки — это правка того же json, той же историей и тем же
инспектором, что и в редакторе данных.

Вкладка **graph** — тот же диалог картой переходов: узлы стоят колонками-слоями (колонка = сколько
выборов от правил, открывающих разговор; последняя колонка — те, до кого не доходит ни один маршрут,
под заголовком `nothing opens these`), маршруты нарисованы стрелками между ними. Карта, а не второй
редактор: ничего не тянется и не соединяется, клик по узлу ставит курсор на этот узел в аутлайне —
там и правится. Перерисовывается по мере набора, а не по кнопке.

Как читаются маршруты:

- **пунктир** — второй выход опции с броском (`failNext`), сплошная — обычный `next`;
- **красный обрубок** — маршрут в узел, которого в этом диалоге нет (dangling): рисуется коротким
  хвостом с именем несуществующего узла, потому что вести его некуда;
- **синий** — маршрут назад: приходит в свой слой или в более ранний (кольца, возврат к приветствию);
  внутри колонки «никто не открывает» синие все — колонка одна и вперёд там некуда;
- точка на маршруте = у опции есть условия, то есть предлагается она не всегда;
- узел, на котором стоит курсор аутлайна, обведён жёлтым; строка над картой считает узлы, колонки и
  маршруты и отдельно называет число висящих маршрутов и недостижимых узлов.

## Что показывается

- **Слева** — записи двумя секциями: `Dialogues` (по `npcId`) и `Quests` (по `id`). Строка секции
  считает записи и не выбирается. Звёздочка перед именем = файл записи не записан на диск.
- **Посередине** — аутлайн выбранной записи:
  - диалог: `entry rules (N)` (каждое правило — `→ узел · priority N`), затем `nodes (N)`; под
    узлом — реплики (`спикер · ключ · «перевод»`) и опции (`ключ · «перевод» → next`);
  - квест: `stages (N)`; под стадией — цели (`objective · id`, необязательная помечена), маршруты
    (`→ to`) и исход (`outcome · id`, `· fails` у проваливающего).
  - Границы видны, а не проглатываются: у записи без узлов ветка стоит с `(0)`, узел без реплик
    подписан `· no lines`, опция без `next` — `· ends`, пустое поле — `—`.
  - Перевод берётся из `.po` через `TranslationServer`; ключ без перевода показывается как есть.
- **Справа** — инспектор из общей библиотеки (`Tooling.Ui/InspectorPanel.cs`): поля того элемента,
  на строке которого стоит курсор дерева (узел, опция, реплика, стадия, цель, переход, исход).
  Строка-ветка (`nodes`, `stages`) — это коллекция, а не запись: инспектор показывает саму запись.
  Условия и действия пока — сырой json (`Any` в схеме).
- **Внизу** — корень данных, счётчики, что сделано последним, сколько файлов не записано и что
  отменит следующий Ctrl+Z.

Ctrl+S / Ctrl+Z / Ctrl+Y (Ctrl+Shift+Z), диалог выхода с несохранённой работой — общие для всех
тулов (`Tooling.Ui/ToolShell.cs`).

## Запуск

1. Открыть Godot 4.7 mono → **Import** → выбрать `src/Tooling/NarrativeEditor/project.godot`.
   Первый импорт долгий: через симлинк `Data/Shared` в проект попадает весь `SharedData`.
2. Собрать C#-решение (**Build**): рядом с `project.godot` лежит `NarrativeEditor.sln`, его и
   собирает плагин .NET.
3. **F5** (главная сцена — `main.tscn`).

Из командной строки:

```powershell
dotnet build src\Tooling\NarrativeEditor\NarrativeEditor.sln
dotnet test src\Tooling\Tooling.Tests
```

## Export

From the repository root, run:

```powershell
./src/Tooling/export.ps1 -Project NarrativeEditor
```

Omit `-Project` to export both editors. Use `-Godot <path>` or `GODOT` to select another Godot
4.7 mono executable. The script builds `ExportRelease`, creates the destination and `.gdignore`,
and exports the `Windows Desktop` preset with `--export-release`.

Output: `src/Tooling/NarrativeEditor/Export/NarrativeEditor.exe` and `NarrativeEditor.pck`.
The PCK contains the managed assemblies and .NET runtime, as in PassiveTreeEditor; keep it beside
the EXE. No loose `data_NarrativeEditor_windows_x86_64` directory is required. Export output is ignored
by Git and by Godot's project scanner. Export logs are kept in the same directory.

`Export/Data/Shared` is a relative symbolic link to `src/SharedData`. Windows Developer Mode or
an elevated shell is required to create it. The exported editor changes the repository's actual
catalogs. When distributing the editor outside this checkout, provide a real `Data/Shared` folder
beside it or pass `--data <path>`; the repository link is not a portable copy of the catalogs.

After the first scripted export, exporting from Godot also uses `Export/NarrativeEditor.exe`.
Clear **Export With Debug**: only the release template is installed on the current workstation.
The script always selects Release. Presets use Godot's installed templates without absolute paths.

Data root lookup remains: explicit `--data`, then `Data/Shared` beside the EXE, then the nearest
repository `src/SharedData`. A missing root is reported in the application. JSON catalogs remain
external; the PCK contains the UI resources and localization required to display the editor.
## Границы и общий код

- Чтение записи как аутлайна — `Tooling.Core/Narrative/Outline.cs` (без Godot), тесты —
  `Tooling.Tests/Narrative/OutlineTests.cs`. Строка аутлайна несёт свой `JsonPointer`, свою схему и
  ключ локализации; хост только рисует её деревом.
- Общие для тулов скрипты живут в `src/Tooling/Tooling.Ui` и подключены сюда симлинком
  `Source/Shared` (Godot компилирует скрипты только внутри проекта, поэтому не `ProjectReference`, а
  ссылка на папку — как `Data/Shared` у данных).
- Если `Data/Shared` или `Source/Shared` выглядят пустыми либо числятся в git обычными файлами,
  симлинки не восстановились при клонировании: `.\restore-links.ps1` в корне репозитория ставит их
  на место (список ссылок скрипт берёт из индекса git).
- Словарь условий/действий (`LastBreath.Descriptors.NarrativeSchemas`) сюда пока не подключён:
  фабрики условий и действий собираются в игровом `GameServiceProvider` поверх живых сервисов, и
  вне запущенной игры их список получить неоткуда.
