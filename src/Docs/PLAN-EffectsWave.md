# План волны «Эффекты + D» (2026-08-18)

Канон: `01_Ядро/1_Механики боя.md` (типы урона), `04_Эффекты/0_Эффекты.md`, `1_Список временных Эффектов.md` (49 позиций). Решения владельца — `decisions.md` (три секции 2026-08-18). Разведка: два отчёта (слой эффектов; типы урона/митигация) — выжимки в задачах.

## Ф0 — несущие правки (по одной задаче, последовательно при пересечении файлов)

| ID | Задача | Статус |
|---|---|---|
| Ф0-1 | **Пул DoT по родам**: `EffectApplyingContext.Damage: float` → снапшот компонентов урона (роль staging-контекста сохраняется); кормильцы (~8: Burning/Bleeding-пассивки, PoisonOnHitRider, PoisonAttackContextModifier…) передают компоненты финального урона хита; `DamageOverTurnEffect`: яд = % суммы, горение = % огненного, кровь = % физического; чтение `EntityParameter.*DamageMultiplier` кастера в базовой формуле тика (решение: вариант Б); снять TODO «развести типы» | — |
| Ф0-2 | **`EffectPower` (Weak/Strong/Absolute; имя НЕ Priority) + развеивание**: поле на `IEffect` (дефолт Weak, печати Absolute), `Dispel(power, scope)` на компоненте (с себя — повреждающие+дебафы, с цели — бафы; Absolute неснимаем); фикс `RemoveEffectBySource` (мутация в итерации) | — |
| Ф0-3 | **Единая точка броска шанса + удача/неудача**: снос `CalculateSucceeded` (устарел, подписчиков нет) и `LuckyChanceDecorator` (no-op); удача = 2 броска лучший, неудача = 2 броска худший, вместе — гасятся; для всех шансовых (крит, доп. атака, уклон, блок…); `Effect_Lucky_Crit_Chance` на новый механизм; обе копии Player/BaseNpc | — |
| Ф0-4 | **Раскол цепочки поглощения под Скверну**: обходящая часть (Blight) отделяется от поглощаемой до `DamageResolutionChain` (щит → барьер), страж стадии уважается ВСЕГДА (решение 1); `IgnoreBarrier`-флаг согласовать с новой формой | — |

## Ф1 — типы урона (после Ф0-4)

Ветки Sacred/Blight в `MitigateComponent` (вместо fallback `_`); резист+пенетрация яда МИМО флага `IgnoreResistances` (по образцу горения) + границы + агрегаты (`AllResistance` включает яд — решение 2); гард `DamagingCastPlan.DamageType` (дефолт/required — `(DamageType)0` не должен протекать); fallback `GetDamageType()` → Physical; хвосты Pure→Sacred (классы/id/тег `pure`/`AttackPureConversion` с переименованием — решение 3/мифик-пул/иконка/.po/доки, `CombatLayers.md` починить — сейчас врёт про Sacred=0); представление (цвета TextPalette/FlyNumbers для Blight, ключи `DamageType_Sacred`/`DamageType_Blight`, ParameterFormats, окно персонажа, записи пулов для новых параметров); тест-гарды (яд-резист не снимается IgnoreResistances; Blight мимо щита/барьера, но не мимо Панциря и не мимо стража; переписать `Mitigation_PoisonAndPurePassUntouched`).

## Ф2 — канон эффектов под новый список 49

Сила поимённо (дефолт Weak, печати Absolute); печати до конца (макс 1 Печати замедления в EffectsData+тестах; эффективность множит числовую нагрузку с округлением ВНИЗ; `SlownessSeal.amount` через Effective); перевод ~18 эффектов с сырых float на `EffectValue`/`Effective` (список в отчёте разведки: CritLeech, Execution, NextAbilityCooldown, Curse, Fragility, Regeneration, Shield, SacrificeCharge, OnEdge, семья Fury…; формы OverloadCharge согласовать); повторное наложение при капе = только рефреш длительности (не вытеснение старейшего) + «рефреш до полной» вместо Max; новый «Заряд» из дизайн-листа (250+(85%+75%) молнией при максимуме стаков — НЕ существующий ChargeEffect детонации СтатДоспеха, имена развести); полная сверка 49 позиций дока с EffectsData.json.

## Ф3 — волна D

D-1 след способности: `AbilityId`/`CastId` на эффекте + `EffectApplyingContext`; «продлевает/усиливает СВОЙ яд» — правка кучных колл-сайтов (ExtendPoisonOnHitRider, PoisonExplosion, TransferPoisonOnDeath, DeepFreeze `GetBy(_=>true)`). D-2 соперничество: тай-брейк Replace, «проигравший дар платит цену», отчёт о победителе на сокет (значок спящего), пер-копийность райдеров; детач-хук `IImpactRider` (закрывает утечку отписки из бэклога). `TriggerTurnEnd` async void → `Task` (детерминированный тик). Род `Reaction` — расстановка.

## Вне волны (решения зафиксированы)

Квирк «длительность+1» (решение 7); блок (отложен); ужимка XML-доков — НЕ слой эффектов (он чист, 9.4%), а `IAbilitySocketBoard` (72% доков), `Core/Save`, `PassiveTree` — отдельной мелкой задачей; схлопывание Battle/Internal↔Main копий; мёртвые NotImplementedException (DarkShroud/Fireball/ManaDevour).
