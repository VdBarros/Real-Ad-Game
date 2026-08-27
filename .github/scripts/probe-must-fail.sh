#!/usr/bin/env bash
#
# Builds a probe project that is EXPECTED to fail, and asserts it failed for the
# stated reason. A probe pins a ceiling the fast loop must not drift past —
# LangVersionProbe pins C# 9, NUnitApiProbe pins the NUnit 3.5 API surface — so
# a probe that starts compiling means the ceiling is gone and the fast loop can
# go green where Unity 6000.5.9f1 would be red.
#
# Usage: probe-must-fail.sh <project-dir> <expected-error-code> <ceiling> <drift-message>

set -euo pipefail

project=$1
expected=$2
ceiling=$3
drift=$4

# GITHUB_ACTIONS is unset for this build only, so the SDK does not turn the
# expected compiler error into a red ::error:: annotation on a passing pull request.
set +e
output=$(env -u GITHUB_ACTIONS dotnet build "$project" --nologo 2>&1)
status=$?
set -e

echo "$output" | sed 's/^/    probe| /'

if [ "$status" -eq 0 ]; then
  echo "::error::$project compiled. $drift"
  exit 1
fi

if ! grep -q "$expected" <<<"$output"; then
  echo "::error::$project failed for the wrong reason — expected $expected, got the output above."
  exit 1
fi

echo "OK — $project failed to compile with $expected, which is the expected result."
echo "$ceiling"
