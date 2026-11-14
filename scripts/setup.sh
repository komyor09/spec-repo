#!/usr/bin/env bash
# Разовая настройка. Запускать учителю из клона репозитория.
# Нужен gh (GitHub CLI) и вход: gh auth login
# Можно запускать повторно: новые логины из students.csv получат приглашения.
set -euo pipefail
REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner)
git checkout master

# 1. Папки студентов
tail -n +2 .classroom/students.csv | while IFS=, read -r login folder full; do
  folder=${folder//$'\r'/}; full=${full//$'\r'/}
  [[ -z "$folder" ]] && continue
  mkdir -p "$folder"
  [[ -f "$folder/README.md" ]] || printf "# %s\n\nТема: \nОписание: \n" "${full:-${folder//_/ }}" > "$folder/README.md"
done
git add -A && git commit -m "Папки студентов" || true
git push origin master

# 2. Ветки и доступ
tail -n +2 .classroom/students.csv | while IFS=, read -r login folder full; do
  folder=${folder//$'\r'/}; login=${login//$'\r'/}
  [[ -z "$folder" ]] && continue
  git ls-remote --exit-code --heads origin "student/$folder" >/dev/null \
    || git push origin "master:refs/heads/student/$folder"
  if [[ -z "$login" ]]; then echo "Пропуск приглашения: $folder (нет логина)"; continue; fi
  gh api -X PUT "repos/$REPO/collaborators/$login" -f permission=push >/dev/null \
    && echo "Приглашён: $login → student/$folder"
done

# 3. Защита master: только через PR, одобрение учителя + зелёный guard
gh api -X PUT "repos/$REPO/branches/master/protection" --input - <<JSON
{
  "required_status_checks": { "strict": false, "contexts": ["guard"] },
  "enforce_admins": false,
  "required_pull_request_reviews": {
    "required_approving_review_count": 1,
    "require_code_owner_reviews": true
  },
  "restrictions": null
}
JSON
echo "Готово."
