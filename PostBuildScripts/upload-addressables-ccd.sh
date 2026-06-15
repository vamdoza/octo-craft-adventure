#!/usr/bin/env bash
# Sync Addressables build output to Unity Cloud Content Delivery (CCD).
# Uses the UGS CLI entries sync workflow, then creates a release.
# https://services.docs.unity.com/content-delivery-management/v1/#section/Workflow-for-Uploading-Content-to-a-Bucket
#
# Required env:
#   UGS_CLI_SERVICE_KEY_ID
#   UGS_CLI_SERVICE_SECRET_KEY
#   UGS_CLI_PROJECT_ID
#   UGS_CLI_ENVIRONMENT_NAME
#   UGS_CLI_BUCKET_NAME
#
# Optional env:
#   ADDRESSABLES_PATH (default: ServerData/WebGL)
#   GITHUB_RUN_NUMBER, GITHUB_SHA

set -euo pipefail

echo "====================UPLOAD_ADDRESSABLES_TO_CCD_START============================="

require_env() {
  local name="$1"
  if [ -z "${!name:-}" ]; then
    echo "Required environment variable is not set: $name" >&2
    exit 1
  fi
}

for name in UGS_CLI_SERVICE_KEY_ID UGS_CLI_SERVICE_SECRET_KEY UGS_CLI_PROJECT_ID UGS_CLI_ENVIRONMENT_NAME UGS_CLI_BUCKET_NAME; do
  require_env "$name"
done

ADDRESSABLES_PATH="${ADDRESSABLES_PATH:-ServerData/WebGL}"
RUN_NUMBER="${GITHUB_RUN_NUMBER:-manual}"
COMMIT_SHA="${GITHUB_SHA:-local}"

if [ ! -d "$ADDRESSABLES_PATH" ]; then
  echo "Addressables output directory not found: $ADDRESSABLES_PATH" >&2
  exit 1
fi

echo "Addressables path: $ADDRESSABLES_PATH"

curl -sL -o ugs "https://github.com/Unity-Technologies/unity-gaming-services-cli/releases/latest/download/ugs-linux-x64"
chmod +x ugs
./ugs --version

export UGS_CLI_SERVICE_KEY_ID
export UGS_CLI_SERVICE_SECRET_KEY

./ugs config set project-id "$UGS_CLI_PROJECT_ID"
./ugs config set environment-name "$UGS_CLI_ENVIRONMENT_NAME"
./ugs config set bucket-name "$UGS_CLI_BUCKET_NAME"

./ugs status

echo "Syncing local Addressables output to CCD bucket..."
./ugs ccd entries sync "$ADDRESSABLES_PATH" -b "$UGS_CLI_BUCKET_NAME"

RELEASE_NOTES="GitHub Actions run ${RUN_NUMBER} (${COMMIT_SHA:0:7})"
echo "Creating CCD release: ${RELEASE_NOTES}"
./ugs ccd releases create -n "$RELEASE_NOTES"

echo "====================UPLOAD_ADDRESSABLES_TO_CCD_END============================="
