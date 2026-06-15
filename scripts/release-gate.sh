#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APPSETTINGS="$ROOT_DIR/src/Presentation/AegisEInvoicing.Portal.API/appsettings.json"
DOCKER_COMPOSE="$ROOT_DIR/docker-compose.yml"
PARITY_REPORT="${PRICING_PARITY_REPORT:-$ROOT_DIR/artifacts/pricing-parity-report.json}"
MAX_PARITY_DRIFT="${MAX_PARITY_DRIFT:-0}"
MAX_BENCHMARK_STALENESS_MINUTES="${MAX_BENCHMARK_STALENESS_MINUTES:-15}"

errors=()

fail_if_pattern() {
  local file="$1"
  local pattern="$2"
  local message="$3"

  if grep -E -q "$pattern" "$file"; then
    errors+=("$message ($file)")
  fi
}

fail_if_pattern "$APPSETTINGS" "sk_test_|pk_test_|AKIA[0-9A-Z]{16}|ASIA[0-9A-Z]{16}" "Detected leaked test key or cloud credential marker"
fail_if_pattern "$APPSETTINGS" "localhost|127\\.0\\.0\\.1" "Detected localhost reference in API appsettings"
fail_if_pattern "$DOCKER_COMPOSE" "sk_test_|pk_test_|AKIA[0-9A-Z]{16}|ASIA[0-9A-Z]{16}" "Detected leaked credential marker in docker-compose"
fail_if_pattern "$DOCKER_COMPOSE" "Password=[^$][^}]" "Detected hardcoded connection-string password default"
fail_if_pattern "$DOCKER_COMPOSE" "SMTP_PASSWORD:-[^}]|SFTP_PASSWORD:-[^}]|ENCRYPTION_KEY:-[^}]|PGADMIN_DEFAULT_PASSWORD:-[^}]|DB_CONNECTION_STRING:-[^}]" "Detected hardcoded secret fallback in docker-compose"

if [[ "${STRICT_PARITY_GATE:-false}" == "true" ]]; then
  if [[ ! -f "$PARITY_REPORT" ]]; then
    errors+=("Pricing parity report missing: $PARITY_REPORT")
  else
    drift=$(grep -Eo '"maxDrift"[[:space:]]*:[[:space:]]*[0-9]+' "$PARITY_REPORT" | head -n1 | grep -Eo '[0-9]+' || true)
    stale=$(grep -Eo '"benchmarkStalenessMinutes"[[:space:]]*:[[:space:]]*[0-9]+' "$PARITY_REPORT" | head -n1 | grep -Eo '[0-9]+' || true)
    status=$(grep -Eo '"status"[[:space:]]*:[[:space:]]*"[^"]+"' "$PARITY_REPORT" | head -n1 | sed -E 's/.*"([^"]+)"/\1/' || true)

    if [[ -z "$drift" || -z "$stale" || -z "$status" ]]; then
      errors+=("Pricing parity report is malformed: $PARITY_REPORT")
    else
      if (( drift > MAX_PARITY_DRIFT )); then
        errors+=("Pricing drift gate failed. maxDrift=$drift threshold=$MAX_PARITY_DRIFT")
      fi

      if (( stale > MAX_BENCHMARK_STALENESS_MINUTES )); then
        errors+=("Benchmark freshness gate failed. staleness=$stale threshold=$MAX_BENCHMARK_STALENESS_MINUTES")
      fi

      if [[ "$status" != "pass" ]]; then
        errors+=("Pricing parity status is not pass. status=$status")
      fi
    fi
  fi
fi

if [[ ${#errors[@]} -gt 0 ]]; then
  echo "Release gate failed:"
  for issue in "${errors[@]}"; do
    echo "- $issue"
  done
  exit 1
fi

echo "Release gate passed."
