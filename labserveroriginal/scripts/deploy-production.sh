#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="${1:-/home/alexey/trspo-3lab}"
APP_VERSION="${2:-local}"
PUBLIC_HOST="${PUBLIC_HOST:-zenrot.root.sx}"
ADMIN_PORT="${ADMIN_PORT:-18080}"
DASHBOARD_PORT="${DASHBOARD_PORT:-18081}"
GITLAB_HTTP_PORT="${GITLAB_HTTP_PORT:-18082}"
GITLAB_SSH_PORT="${GITLAB_SSH_PORT:-18022}"
POSTGRES_HOST_PORT="${POSTGRES_HOST_PORT:-15432}"
PLAGIARISM_PORT="${PLAGIARISM_PORT:-15289}"
MAILPIT_WEB_PORT="${MAILPIT_WEB_PORT:-18083}"
OLLAMA_BASE_URL="${OLLAMA_BASE_URL:-http://host.docker.internal:11434}"

DATA_DIR="$APP_ROOT/data"
SHARED_DIR="$APP_ROOT/shared"
SECRETS_FILE="$SHARED_DIR/deploy.env"

mkdir -p "$DATA_DIR" "$SHARED_DIR"
touch "$SECRETS_FILE"
chmod 600 "$SECRETS_FILE"

ensure_secret() {
  local name="$1"
  local command="$2"

  if ! grep -q "^${name}=" "$SECRETS_FILE"; then
    printf '%s=%s\n' "$name" "$(eval "$command")" >> "$SECRETS_FILE"
  fi
}

ensure_secret POSTGRES_PASSWORD "openssl rand -base64 36 | tr -d '\n'"
ensure_secret JWT_SECURITY_KEY "openssl rand -base64 64 | tr -d '\n'"
ensure_secret GITLAB_TOKEN "printf 'glpat-%s' \"\$(openssl rand -hex 20)\""

set -a
. "$SECRETS_FILE"
set +a

cat > .env <<ENVEOF
APP_VERSION=$APP_VERSION
PUBLIC_HOST=$PUBLIC_HOST
ADMIN_PORT=$ADMIN_PORT
DASHBOARD_PORT=$DASHBOARD_PORT
GITLAB_HTTP_PORT=$GITLAB_HTTP_PORT
GITLAB_SSH_PORT=$GITLAB_SSH_PORT
POSTGRES_HOST_PORT=$POSTGRES_HOST_PORT
PLAGIARISM_PORT=$PLAGIARISM_PORT
MAILPIT_WEB_PORT=$MAILPIT_WEB_PORT
LABSERVER_DATA_DIR=$DATA_DIR
POSTGRES_DB=labs
POSTGRES_PASSWORD=$POSTGRES_PASSWORD
JWT_SECURITY_KEY=$JWT_SECURITY_KEY
GITLAB_TOKEN=$GITLAB_TOKEN
SMTP_DOMAIN=mailpit
SMTP_EMAIL=noreply@$PUBLIC_HOST
SMTP_USERNAME=labserver
SMTP_PASSWORD=labserver
SMTP_SSL=false
SMTP_PORT=1025
DISABLE_BACKGROUND_SERVICES=false
DATABASE_ENSURE_CREATED_ON_STARTUP=true
OLLAMA_BASE_URL=$OLLAMA_BASE_URL
ENVEOF

mkdir -p Server
cat > Server/appsettings.docker.json <<JSONEOF
{
  "ConnectionStrings": {
    "PgSQL": "Host=db;Port=5432;Database=labs;Username=postgres;Password=$POSTGRES_PASSWORD"
  },
  "JwtSecurityKey": "$JWT_SECURITY_KEY",
  "JwtIssuer": "http://$PUBLIC_HOST:$ADMIN_PORT",
  "JwtAudience": "http://$PUBLIC_HOST:$ADMIN_PORT",
  "JwtExpiryInDays": 7,
  "PublicUrls": {
    "Admin": "http://$PUBLIC_HOST:$ADMIN_PORT",
    "Dashboard": "http://$PUBLIC_HOST:$DASHBOARD_PORT",
    "GitLab": "http://$PUBLIC_HOST:$GITLAB_HTTP_PORT"
  },
  "Database": {
    "EnsureCreatedOnStartup": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "GitLabClient": {
    "url": "http://gitlab",
    "secret_token": "$GITLAB_TOKEN"
  },
  "Services": {
    "TestRunner": {
      "period": "00:00:10"
    },
    "MergeRequestMonitor": {
      "period": "00:00:15"
    }
  },
  "SMTP": {
    "domain": "mailpit",
    "email": "noreply@$PUBLIC_HOST",
    "username": "labserver",
    "password": "labserver",
    "ssl": false,
    "port": 1025
  }
}
JSONEOF

docker compose -p trspo-lab -f compose.deploy.yml up -d db mailpit gitlab

echo "Waiting for GitLab to accept rails runner commands..."
for attempt in $(seq 1 90); do
  if docker exec trspo-gitlab gitlab-rails runner "puts 'ready'" >/dev/null 2>&1; then
    break
  fi

  if [ "$attempt" -eq 90 ]; then
    echo "GitLab did not become ready in time" >&2
    docker compose -p trspo-lab -f compose.deploy.yml logs --tail=200 gitlab >&2
    exit 1
  fi

  sleep 10
done

docker exec trspo-gitlab gitlab-rails runner "user = User.find_by_username('root'); token = user.personal_access_tokens.find_or_initialize_by(name: 'labserver-ci-token'); token.scopes = [:api]; token.expires_at = 365.days.from_now; token.set_token('$GITLAB_TOKEN'); token.save!; puts 'LabServer GitLab token is ready'"

docker compose -p trspo-lab -f compose.deploy.yml --profile build run --rm admin-builder
docker compose -p trspo-lab -f compose.deploy.yml --profile build run --rm dashboard-builder
docker compose -p trspo-lab -f compose.deploy.yml up -d --build
docker compose -p trspo-lab -f compose.deploy.yml ps
