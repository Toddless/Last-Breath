# Торговля и кошелёк

Статус: описание существующей реализации. Основа и границы статической сверки — [в отчёте](../Guidelines/SystemDocumentationAudit.md).

## Устройство и поведение

- **Деньги**: `IWalletService` — кошелёк-счётчик (НЕ предмет; сейв-секция `wallet` v1, session reset к `startingGold`). Золото входит в мир кучкой `GoldItem` (`ICurrencyItem`) из остатка лут-бюджета (см. «Система лута»); подбор кладёт в кошелёк мимо сумки.
- **Цена = оценщик экземпляра** `ItemValuation` (единственная точка золотых цен): `basePrice(блюпринт/предмет) × редкость × (1 + 0.1×заточка) × возвышение`; для экипировки база берётся из blueprint, для обычных предметов — из basePrice; нулевая база означает неторгуемость. Исключение — аугмент: его база вычисляется как augmentBasePrice × augmentTierMultiplier^tier, затем применяется редкость экземпляра. Множители — `SharedData/Trade/TradeConfiguration.json`. `TradePricing` поверх: покупка ×(1+`Perk_Price_Change` фракции торговца), продажа ×buyback(0.4)×(1−перк). ЛУТ-у.е. ≠ золото.
- **Торговцы**: `SharedData/Traders/Traders.json` → `TraderProvider` → `TraderService` — гибрид-полка (авторский каталог id+count+chance с шансом появления на рестоке + случайные экип-слоты через настоящий минтер; уники/мифики не роллятся), ленивый рестока по игровым минутам, `TakeMany` all-or-nothing. **Buyback**: проданное ждёт на полке торговца по ЗАРАБОТАННОЙ цене (кап 12, FIFO; переживает ресток, сохраняется вместе с полкой и сбрасывается при новой сессии). **Полка сейвится** (секция `traderShelf` v1, порядок восстановления `TraderShelf` = после `worldClock`, по игровым минутам которого сверяется ресток): предметы пишутся целиком той же формой, что и сумка (`InventoryItemSaveConverter`) — перезаход не перекатывает роллы и не двигает рестока; счётчик `offerCounter` едет в секции, иначе новый оффер займёт id восстановленного. Файл без секции = свежая полка (стек сессионного сброса).
- **Операции через шину** (ворота — хендлеры, UI зеркалит): `BuyItemRequest(trader, offer, amount)` — кошелёк check-then-spend, полная сумка откатывает; `SellItemRequest(trader, instance, amount)` — стак продаётся до фактического наличия, экип уходит целиком в buyback. Вход: `OpenTradeWindowMessage(traderId)` ← диалог-действие `StartTrade` (json: `{type: "StartTrade", traderId}`; вешать на узел, ЗАВЕРШАЮЩИЙ диалог) и консоль `trade [traderId]`.
- **Все регистрации — Main** (модульная дисциплина): `RegisterTradeServices` в GameServiceProvider. `TradeWindow` разрешён в World|Dialogue (исключение из карты — окно должно пережить флип контекста закрывающегося диалога). Сцена Main/UI/View/TradeWindow.tscn существует; TradeWindow.Initialize загружает её по UID. Наличие сцены подтверждено статически, текущий визуальный прогон не выполнялся.

## Проверенные точки реализации

Пути относительно `src` LastBreath: `Core/Trade/TradePricing.cs`, `Core/Trade/ItemValuation.cs`, `Core/Trade/TraderService.cs`, `Main/UI/TradeWindow.cs`, `Main/UI/View/TradeWindow.tscn`.

## Связанные документы

- [Генерация лута](../Loot/CurrentSystem.md)
- [Репутация и рейды](../Reputation/CurrentSystem.md)
- [Экипировка и гранты предметов](../Items/CurrentSystem.md)
- [Окна, HUD и всплывающие элементы](../UI/CurrentSystem.md)
- [Карта документации](../README.md)
