# Git и прототипы (Zombie Rush)

Как подключить Git к проекту Unity, сохранять **рабочие версии** прототипа и **откатываться** назад. Согласовано с **[PROJECT.md](../PROJECT.md)** и GDD в папке `Documentation/`.

---

## 1. Установить Git (Windows)

1. Скачайте **Git for Windows**: [https://git-scm.com/download/win](https://git-scm.com/download/win)
2. Установите (по умолчанию подойдёт; опционально: **Git from the command line and also from 3rd-party software**).
3. Перезапустите терминал и Cursor. Проверка:

```powershell
git --version
```

Если команда не найдена — добавьте Git в `PATH` или перезагрузите ПК.

---

## 2. Первый раз: репозиторий в папке проекта

В PowerShell из **корня проекта** (папка, где лежат `Assets`, `Packages`, `ProjectSettings`):

```powershell
cd "c:\v1\Deadline Delivery\My project"
git init
```

Проверьте, что в корне есть **`.gitignore`** (у проекта уже есть — игнорируются `Library/`, `Temp/`, `UserSettings/` и т.д.). Не коммитьте сгенерированные папки Unity.

Создайте первый коммит:

```powershell
git add .
git status
git commit -m "chore: initial Unity project snapshot"
```

---

## 3. Удалённый репозиторий (GitHub / GitLab / Azure DevOps)

1. Создайте **пустой** репозиторий на сайте (без README, если уже есть локальный проект — иначе будет лишний merge).
2. Добавьте `remote` и отправьте ветку (имя ветки может быть `main` или `master`):

```powershell
git remote add origin https://github.com/ВАШ_ЛОГИН/zombie-rush.git
git branch -M main
git push -u origin main
```

3. Заполните в **[PROJECT.md](../PROJECT.md)** поля «Удалённый Git» и постоянные ссылки на репозиторий и теги.

**LFS (большие бинарники):** если позже добавятся тяжёлые ассеты, рассмотрите [Git LFS](https://git-lfs.com/) — отдельная настройка.

---

## 4. Ветки под задачи

| Практика | Зачем |
|----------|--------|
| `main` | стабильная линия, всегда открывается в Unity без сюрпризов |
| `feature/имя` | новая механика (например `feature/nitro-balance`) |
| `fix/имя` | исправления |

Пример:

```powershell
git checkout -b feature/base-menu
# ... работа, коммиты ...
git checkout main
git merge feature/base-menu
```

Перед merge крупных изменений — **закройте Unity** или сделайте коммит, чтобы не потерять сцены с несохранённым YAML.

---

## 5. Теги = срезы «рабочего прототипа»

Теги удобны, чтобы **пометить версию**, которую можно показать или к которой вернуться **без поиска по хешу коммита**.

Создать аннотированный тег после проверки в редакторе:

```powershell
git tag -a prototype-0.1 -m "Прототип: езда + зомби + топливо, см. PROJECT.md"
git push origin prototype-0.1
```

Список тегов:

```powershell
git tag -l "prototype-*"
```

**Вернуться к состоянию тега** (только просмотр / новая ветка — безопаснее, чем ломать `main`):

```powershell
git checkout -b review-from-tag prototype-0.1
```

Открыть проект в Unity из этой ветки, проверить, затем вернуться:

```powershell
git checkout main
```

**Жёсткий откат ветки `main` к тегу** (опасно, переписывает историю — используйте осознанно):

```powershell
git checkout main
git reset --hard prototype-0.1
git push --force-with-lease origin main
```

`--force-with-lease` безопаснее голого `--force`, но всё равно согласуйте с командой.

---

## 6. Что коммитить в Unity-проекте

| Коммитить | Обычно не коммитить |
|-----------|---------------------|
| `Assets/`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/` | `Library/`, `Temp/`, `Logs/`, `UserSettings/` (уже в `.gitignore`) |
| Сцены `.unity`, префабы `.prefab`, скрипты `.cs` | Локальные кэши IDE по желанию |
| `Documentation/`, `PROJECT.md`, `.cursor/rules/` | Секреты ключей API |

Скриншоты из MCP (`Assets/Screenshots/`) — по желанию: либо коммитить как артефакты, либо добавить в `.gitignore`, если засоряют историю.

---

## 7. Cursor + MCP + Git

- После **`git checkout`**, **`git merge`**, **`git pull`** откройте Unity и дождитесь импорта; при странностях — **Start Server** в MCP заново (см. чеклист в `.cursor/rules/unity-mcp.mdc`).
- Конфликты в `.unity` / `.prefab` лучше решать в **Unity** (Smart Merge / ручное слияние), не вслепую в текстовом редакторе.

---

## 8. Полезные команды

```powershell
git status
git log --oneline -10
git diff
```

Отмена незакоммиченных правок в файле (осторожно):

```powershell
git restore -- путь/к/файлу
```

---

## Ссылки

- Официальная книга Pro Git (на русском есть зеркала): [git-scm.com/book](https://git-scm.com/book/en/v2)
- Unity + Git: [Unity Manual — Smart Merge](https://docs.unity3d.com/Manual/SmartMerge.html) (при необходимости)
