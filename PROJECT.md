# Zombie Rush — карта проекта

Краткий ориентир для людей и для агента в Cursor. Детальный дизайн — в **[Documentation/](Documentation/)** (GDD).

## Движок и репозиторий

| | |
|--|--|
| **Unity** | **6000.1.5f1** (`ProjectSettings/ProjectVersion.txt`) |
| **Платформа прототипа** | веб (ориентир WebGL), см. GDD |
| **MCP for Unity** | Coplay, см. `Assets/MCPForUnity/package.json` |
| **Удалённый Git (origin)** | `https://github.com/Poponin48/Zombie-Rush.git` |

### Ссылки на Git

- **Репозиторий:** [github.com/Poponin48/Zombie-Rush](https://github.com/Poponin48/Zombie-Rush)
- **README на GitHub:** [Zombie-Rush#readme](https://github.com/Poponin48/Zombie-Rush?tab=readme-ov-file#zombie-rush)
- **Последний стабильный тег прототипа:** *(добавить после `git tag` + `git push origin prototype-x.y`, ссылка вида `https://github.com/Poponin48/Zombie-Rush/releases/tag/prototype-0.1`)*

Инструкции по веткам, тегам и откату версий: **[Documentation/GIT.md](Documentation/GIT.md)**.

---

## GDD — основные документы

| Документ | Содержание |
|----------|------------|
| [Documentation/Общая концепция.md](Documentation/Общая%20концепция.md) | Концепт, цикл, камера, управление, UI |
| [Documentation/Механика транспорт.md](Documentation/Механика%20транспорт.md) | Транспорт, топливо, HP, структура колёс |
| [Documentation/Транспорт.md](Documentation/Транспорт.md) | **Единый справочник:** префаб, параметры Inspector, подвеска, ассеты |
| [Documentation/Механика зомби.md](Documentation/Механика%20зомби.md) | AI, NavMesh, толпа, урон по HP |
| [Documentation/Сущности и архитектура.md](Documentation/Сущности%20и%20архитектура.md) | Сущности, компоненты |

**Правило:** при изменении механики обновляйте соответствующий файл в `Documentation/` и при необходимости строки в таблице ниже.

---

## Чеклист механик (сводка vs GDD)

Статусы: **готово** · **частично** · **не сделано** · **отложено**

| Механика | Статус | Где в GDD / примечание |
|----------|--------|-------------------------|
| Аркадное движение (WASD, WheelCollider) | готово | [Механика транспорт](Documentation/Механика%20транспорт.md) |
| Камера за машиной (top-down / follow) | готово | [Общая концепция](Documentation/Общая%20концепция.md) |
| Топливо + UI | готово | `FuelSystem`, `FuelUI` |
| Нитро (Shift) | *уточнить в сцене* | концепт в GDD |
| Зомби: преследование, NavMesh | готово | [Механика зомби](Documentation/Механика%20зомби.md) |
| Толпа замедляет машину | готово | `VehicleCrowdBrake` |
| Урон по HP грузовика от зомби | готово | [Механика зомби](Documentation/Механика%20зомби.md) |
| Сбивание зомби на скорости | готово (заглушка смерти) | без регдолла |
| База: выбор авто, апгрейды, модули | не сделано / прототип | GDD |
| Модификации (ковш, турель, прицеп, коса) | не сделано | GDD |
| Цепляние зомби к кузову | отложено | [Механика зомби](Documentation/Механика%20зомби.md) |

*Обновляйте таблицу при завершении крупных задач; детали — всегда в GDD-файлах.*

---

## Связь коммитов с прототипом

Рекомендуемые **теги** для «рабочих срезов» (см. [GIT.md](Documentation/GIT.md)):

- `prototype-0.1`, `prototype-0.2`, … — после стабильной проверки в редакторе / билде.

В сообщениях коммитов указывайте область: `vehicle:`, `zombies:`, `ui:`, `docs:`.
