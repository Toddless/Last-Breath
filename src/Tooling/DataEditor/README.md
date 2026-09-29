# Data Editor

Внешний редактор JSON-каталогов игры. Отдельный Godot-проект рядом с игрой (как
`src/PassiveTreeEditor`), тонкий хост: схему каталогов он берёт у описателей адаптера
(`LastBreath.Descriptors.CatalogDescriptors.All`), документы — из `src/SharedData`.

## Текущее состояние

Есть:

- правка скалярных полей, коллекций (списки, словари) и вариантов записи;
- история правок на файл: Ctrl+Z / Ctrl+Y (Ctrl+Shift+Z) шагают по истории того файла, в котором
  лежит запись на экране;
- Ctrl+S пишет канон только грязных файлов;
- три жеста над записями каталога (`+ ⧉ ×`) с раскладкой по файлу: у каталога, разложенного полем
  записи, файл выбирается из значений поля; у каталога, который тулинг раскладывает сам, имя файла
  пишется руками (новое имя = новый файл);
- пикер ссылок с поиском и подсветкой битых.

Нет: локализации имён, переименования id с распространением по ссылкам, панели проверок.

## Запуск

1. Открыть Godot 4.7 mono → **Import** → выбрать `src/Tooling/DataEditor/project.godot`.
   Первый импорт долгий: через симлинк `Data/Shared` в проект попадает весь `SharedData`.
2. Godot предложит собрать C#-решение (**Build**). Собирается `DataEditor.csproj` с
   `ProjectReference` на `..\LastBreath.Descriptors\LastBreath.Descriptors.csproj` и
   `..\Tooling.Core\Tooling.Core.csproj`; игра приезжает транзитивно через адаптер.
3. **F5** (главная сцена — `main.tscn`).

Из командной строки код проверяется вместе со всей библиотекой тулинга:

```powershell
dotnet build src\Tooling\Tooling.sln
dotnet test src\Tooling\Tooling.Tests
```

## Откуда берутся данные

Каталоги читаются из корня, который зависит от того, как запущен инструмент:

- `--data <путь>` в командной строке — корень назван явно и берётся как написан, папка есть или нет
  (несуществующий путь называется в диалоге, а не подменяется молча другим);
- запуск из редактора Godot — `res://Data/Shared`, то есть симлинк
  `src/Tooling/DataEditor/Data/Shared → ../../../SharedData`;
- запуск собранного exe — папка `Data/Shared` рядом с бинарником, если она там есть; иначе ближайший
  `src/SharedData` вверх по дереву от бинарника (в экспортированном виде `res://` лежит внутри `.pck`,
  а инструменту нужна папка, в которую он сможет писать).

Каталог — подпапка корня с именем каталога (`Npc`, `LootTables`, …); читаются все `*.json` в ней,
включая вложенные папки, — ровно так же, как их читает игра.

Если папка `Data/Shared` выглядит пустой или в git числится обычным файлом, симлинк не восстановился
при клонировании: `.\restore-links.ps1` в корне репозитория ставит его на место (список ссылок
скрипт берёт из git, отдельной записи для этого проекта не нужно).

## Export

From the repository root, run:

```powershell
./src/Tooling/export.ps1 -Project DataEditor
```

Omit `-Project` to export both editors. Use `-Godot <path>` or `GODOT` to select another Godot
4.7 mono executable. The script builds `ExportRelease`, creates the destination and `.gdignore`,
and exports the `Windows Desktop` preset with `--export-release`.

Output: `src/Tooling/DataEditor/Export/DataEditor.exe` and `DataEditor.pck`.
The PCK contains the managed assemblies and .NET runtime, as in PassiveTreeEditor; keep it beside
the EXE. No loose `data_DataEditor_windows_x86_64` directory is required. Export output is ignored
by Git and by Godot's project scanner. Export logs are kept in the same directory.

`Export/Data/Shared` is a relative symbolic link to `src/SharedData`. Windows Developer Mode or
an elevated shell is required to create it. The exported editor changes the repository's actual
catalogs. When distributing the editor outside this checkout, provide a real `Data/Shared` folder
beside it or pass `--data <path>`; the repository link is not a portable copy of the catalogs.

After the first scripted export, exporting from Godot also uses `Export/DataEditor.exe`.
Clear **Export With Debug**: only the release template is installed on the current workstation.
The script always selects Release. Presets use Godot's installed templates without absolute paths.

Data root lookup remains: explicit `--data`, then `Data/Shared` beside the EXE, then the nearest
repository `src/SharedData`. A missing root is reported in the application. JSON catalogs remain
external; the PCK contains the UI resources and localization required to display the editor.
## Что показывается

- **Слева** — каталоги, для которых у игры есть описатель. Их пока меньше, чем каталогов данных;
  строка статуса называет обе цифры.
- **Посередине** — записи выбранного каталога. Имя строки — id записи (`RecordSchema.IdField`);
  запись без id называется своим местом в секции (`#3`). Если каталог разложен по нескольким файлам,
  рядом с id стоит имя файла.
- **Справа** — поля записи по её схеме: вложенные объекты отдельными блоками, массивы и словари — со
  счётчиком элементов, ссылки — с именами каталогов, куда они смотрят, свободные структуры — сырым
  json.
- **Внизу** — корень данных, число каталогов, файлов, записей и заметок.

Всё, что не прочиталось (нет папки каталога, файл не разбирается, секция написана не той формой),
собирается в заметки и показывается диалогом при старте. Инструмент открывается на тех каталогах,
которые прочитались.

## Границы

Чистая часть (сбор файлов каталога, перечисление записей по форме схемы, id записи) живёт в
`Tooling.Core/Catalogs` и покрыта тестами в `Tooling.Tests/DataEditor`; в этом проекте остаётся
только Godot-обёртка над ней. Решение `DataEditor.sln` лежит рядом с `project.godot` — его и собирает
плагин .NET: библиотеки тулинга отображены в нём `ExportDebug → Debug|Any CPU`,
`ExportRelease → Release|Any CPU`, игровые Godot-проекты — сами в себя, поэтому экспорт в exe собирает
те же проекты, что и F5.
