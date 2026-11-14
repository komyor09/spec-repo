# Проект по специализации

У каждого студента своя папка `Фамилия_Имя/` и своя ветка `student/Фамилия_Имя`.
В `master` попадает только то, что проверил и одобрил учитель.

## Студенту
```bash
git clone <url> && cd <repo>
git checkout student/Фамилия_Имя
# работаете ТОЛЬКО в папке Фамилия_Имя/
git add Фамилия_Имя/ && git commit -m "Что сделано" && git push
```
Когда готово — откройте Pull Request `student/Фамилия_Имя → master`.
Проверка **guard** упадёт, если вы тронули чужие файлы.

## Учителю
1. Впишите студентов в `.classroom/students.csv` (`github,folder`),
   свой логин в `.classroom/teacher` и в `.github/CODEOWNERS`.
2. `gh auth login`, затем `bash scripts/setup.sh`.
3. Проверка работ: вкладка Pull requests → Files changed → комментарии
   по строкам → Approve или Request changes → Merge.

## Как устроена защита
| Что | Чем обеспечено |
|---|---|
| В master нельзя пушить напрямую | Branch protection |
| Нужен апрув учителя | CODEOWNERS + required review |
| Только своя папка и своя ветка | Action `guard` (обязательная проверка) |
| Студент не может отключить проверку | `pull_request_target` — workflow берётся из master |

## Логины GitHub собираются постепенно
В `.classroom/students.csv` первая колонка `github` пока пустая — это нормально:
папки и ветки создаются для всех сразу, приглашения уходят только тем, у кого вписан логин.
Когда студент даст логин:
1. впишите его в первую колонку его строки;
2. `git commit -am "Логин <Фамилия>" && git push origin master`;
3. снова `bash scripts/setup.sh` — он пригласит новых и ничего не сломает у старых.
