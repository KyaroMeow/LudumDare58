# Редактирование интерфейса Sorter

Все пути ниже относятся к сцене `Assets/Scenes/Main.unity`.

## Инвентарь игрока

1. В Hierarchy откройте `UI_Root/InventoryCanvas`.
2. В компоненте `InventoryUIController` найдите секцию **Interface Text**.
3. Здесь редактируются:
   - `Inventory Title` — заголовок инвентаря;
   - `Inventory Channel` — правый технокод;
   - `Inventory Description` — строка под заголовком;
   - `Empty Slot Text` — надпись пустой ячейки;
   - `Close Hint Text` — подсказка закрытия.
4. Размеры, положение и основные цвета находятся в секции **Terminal Layout** того же компонента.

Названия и иконки самих предметов берутся не из Canvas. Нажмите на нужный объект в поле `Item`/`Cutscene Click Item`, затем в открывшемся `InventoryItemDefinition` измените `Display Name` или `Icon`.

## Крафт руки

Общий заголовок модуля находится на корневом объекте руки:

1. Найдите в Hierarchy корневой объект `hand_low`.
2. В компоненте `VentHandInteractable` откройте секцию **Craft UI Text**.
3. Здесь редактируются заголовок `Craft Title`, технокод `Craft Channel`, описание и подсказка закрытия.

Тексты отдельных рецептов находятся здесь:

- `UI_Root/InventoryCanvas/CraftPanel/CraftRecipe_AtomToaster`;
- `UI_Root/InventoryCanvas/CraftPanel/CraftRecipe_Bomb`.

В компоненте `CraftGroupView`, секция **Recipe UI Text**, можно изменить название рецепта, код схемы, надпись действия и состояния доступности.

Названия и иконки компонентов/результата:

1. Откройте дочерние `Input*` или `Result*` выбранного рецепта.
2. В компоненте `CraftCell` нажмите на объект в поле `Item`.
3. В `InventoryItemDefinition` измените `Display Name` или `Icon`.

## Интерфейс мусорки

1. Найдите в Hierarchy корневой объект `Мусорка_Low`.
2. В компоненте `TrashBinInteractable` откройте секцию **Waste UI Text**.
3. Здесь находятся заголовок, технокод, приглашение выбрать предмет, предупреждение, текст кнопки уничтожения и подсказка закрытия.

Панель `TechTrashModule` создаётся при открытии мусорки в Play Mode. Её постоянные тексты нужно менять на `TrashBinInteractable`, а не на временных дочерних объектах.

## Диалог руки

1. Откройте `Systems_Root/VentHandIntroController`.
2. В компоненте `VentHandIntroController` найдите секцию **Dialogue**.
3. `Intro Lines` — массив всех реплик. Для каждой реплики доступны:
   - `Text` — текст;
   - `Blip Interval` — частота звука печати;
   - `Voice Blips` — варианты звуков;
   - `Give Key After This Line` — выдать ключ после реплики.
4. `Dialogue Contact Label`, `Dialogue Channel Label` и `Dialogue Advance Hint` отвечают за постоянные подписи окна.

Inspector теперь является источником истины: код больше не заменяет `Intro Lines` при запуске. Команда контекстного меню компонента **Restore Default Dialogue Script** намеренно восстанавливает стандартный сценарий и перезаписывает массив.

## Runtime-иерархия

В Play Mode внутри `UI_Root/InventoryCanvas/Panel` появятся объекты:

- `TechInventoryTerminal/InventoryModule` — новый инвентарь;
- `TechTrashModule` — модуль мусорки, только когда он впервые открыт.

Внутри `CraftPanel` добавляются рамки и подписи к существующим `CraftRecipe_*`. Эти объекты генерируются для визуала; редактируемые тексты всегда находятся в компонентах, перечисленных выше.
