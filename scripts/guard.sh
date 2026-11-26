#!/usr/bin/env bash
# Проверяет, что PR студента меняет файлы только в его папке
# и отправлен из его ветки student/<Папка>.
# Использование: guard.sh <github_login> <head_branch> <base_ref> <head_ref>
set -euo pipefail
LOGIN="$1"; BRANCH="$2"; BASE="$3"; HEAD="$4"
TEACHER=$(head -n1 .classroom/teacher | tr -d '[:space:]')

if [[ "${LOGIN,,}" == "${TEACHER,,}" ]]; then
  echo "✅ Учитель ($LOGIN) — ограничений нет."; exit 0
fi

FOLDER=$(awk -F, -v u="${LOGIN,,}" 'NR>1 && tolower($1)==u {gsub(/\r/,"",$2); print $2}' .classroom/students.csv)
if [[ -z "$FOLDER" ]]; then
  echo "❌ $LOGIN нет в .classroom/students.csv"; exit 1
fi

if [[ "$BRANCH" != "student/$FOLDER" ]]; then
  echo "❌ PR должен идти из ветки student/$FOLDER, а не из $BRANCH"; exit 1
fi

BAD=0
while IFS= read -r -d '' f; do
  [[ -z "$f" ]] && continue
  if [[ "$f" != "$FOLDER/"* ]]; then
    echo "❌ Чужой файл: $f"; BAD=1
  fi
done < <(git -c core.quotePath=false diff --name-only -z "$BASE...$HEAD")

if [[ $BAD -eq 1 ]]; then
  echo "Можно менять только файлы в папке $FOLDER/"; exit 1
fi
echo "✅ $LOGIN меняет только $FOLDER/"
