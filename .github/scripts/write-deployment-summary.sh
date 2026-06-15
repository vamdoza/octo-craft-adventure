#!/usr/bin/env bash
# Write deployment status to the GitHub Actions job summary.
# Invoked only from .github/workflows/unity-ci-reusable.yml.

set -euo pipefail

webgl_status_label() {
  if [ "${BUILD_WEBGL:-false}" != "true" ]; then
    echo "Not built"
    return
  fi
  if [ "${DEPLOY_WEBGL:-false}" != "true" ]; then
    echo "Built (artifact only)"
    return
  fi
  case "${DEPLOY_WEBGL_OUTCOME:-skipped}" in
    success) echo "Published" ;;
    failure) echo "Failed" ;;
    skipped) echo "Skipped" ;;
    cancelled) echo "Cancelled" ;;
    *) echo "Not deployed" ;;
  esac
}

addressables_status_label() {
  if [ "${BUILD_ADDRESSABLES:-false}" != "true" ]; then
    echo "Not built"
    return
  fi
  if [ "${UPLOAD_ADDRESSABLES:-false}" != "true" ]; then
    echo "Built (artifact only)"
    return
  fi
  case "${UPLOAD_ADDRESSABLES_OUTCOME:-skipped}" in
    success) echo "Deployed to Unity CCD" ;;
    failure) echo "Failed" ;;
    skipped) echo "Skipped" ;;
    cancelled) echo "Cancelled" ;;
    *) echo "Not deployed" ;;
  esac
}

PAGES_URL=""
if [ -n "${WEBGL_DEPLOY_USER:-}" ] && [ -n "${WEBGL_DEPLOY_REPO:-}" ]; then
  PAGES_URL="https://${WEBGL_DEPLOY_USER}.github.io/${WEBGL_DEPLOY_REPO}/"
fi

CCD_URL=""
if [ -n "${UGS_CLI_PROJECT_ID:-}" ]; then
  CCD_URL="https://dashboard.unity3d.com/content-delivery/${UGS_CLI_PROJECT_ID}"
fi

WEBGL_STATUS="$(webgl_status_label)"
ADDRESSABLES_STATUS="$(addressables_status_label)"
RELEASE_NOTES="GitHub Actions run ${GITHUB_RUN_NUMBER:-unknown} (${GITHUB_SHA:0:7})"

{
  echo "## Deployment summary"
  echo ""
  echo "### WebGL (GitHub Pages)"
  echo ""
  echo "| | |"
  echo "|---|---|"
  echo "| Status | ${WEBGL_STATUS} |"
  if [ "${BUILD_WEBGL:-false}" = "true" ] && [ -n "${GITHUB_RUN_NUMBER:-}" ]; then
    echo "| Build | #${GITHUB_RUN_NUMBER} |"
  fi
  if [ -n "$PAGES_URL" ] && [ "${DEPLOY_WEBGL:-false}" = "true" ]; then
    echo "| Play | [${PAGES_URL}](${PAGES_URL}) |"
  fi
  echo ""
  echo "### Addressables (Unity CCD)"
  echo ""
  echo "| | |"
  echo "|---|---|"
  echo "| Status | ${ADDRESSABLES_STATUS} |"
  if [ -n "${UGS_CLI_ENVIRONMENT_NAME:-}" ]; then
    echo "| Environment | ${UGS_CLI_ENVIRONMENT_NAME} |"
  fi
  if [ -n "${UGS_CLI_BUCKET_NAME:-}" ]; then
    echo "| Bucket | ${UGS_CLI_BUCKET_NAME} |"
  fi
  if [ "${UPLOAD_ADDRESSABLES:-false}" = "true" ] && [ "${UPLOAD_ADDRESSABLES_OUTCOME:-skipped}" = "success" ]; then
    echo "| Release | ${RELEASE_NOTES} |"
  fi
  if [ -n "$CCD_URL" ] && [ "${UPLOAD_ADDRESSABLES:-false}" = "true" ]; then
    echo "| Dashboard | [Unity CCD](${CCD_URL}) |"
  fi
  echo ""
} >> "${GITHUB_STEP_SUMMARY}"
