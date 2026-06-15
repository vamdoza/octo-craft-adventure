#!/usr/bin/env bash
# Deploy Unity Cloud Build WebGL output to a GitHub Pages repo.
# Unity Build Automation runs bash scripts on ALL builders (including Windows).
# Expects env vars: GITHUB_TOKEN, GITHUB_USER, GITHUB_REPO, GITHUB_EMAIL
# Optional: USER or USERNAME (git author), UCB_BUILD_NUMBER (commit message)

set -euo pipefail

echo "====================DEPLOYMENT_TO_GITHUB_PAGES_START============================="
set -x

DEPLOY_PATHS=(index.html TemplateData Build StreamingAssets WebGL)

require_env() {
  local name="$1"
  if [ -z "${!name:-}" ]; then
    echo "Required environment variable is not set: $name" >&2
    exit 1
  fi
}

for name in GITHUB_TOKEN GITHUB_USER GITHUB_REPO GITHUB_EMAIL; do
  require_env "$name"
done

GIT_AUTHOR_NAME="${USER:-${USERNAME:-unity-cloud-build}}"
BUILD_NUMBER="${UCB_BUILD_NUMBER:-${GITHUB_RUN_NUMBER:-unknown}}"
COMMIT_MESSAGE="${DEPLOY_COMMIT_MESSAGE:-CI build ${BUILD_NUMBER}}"

resolve_build_folder() {
  if [ -n "${UNITY_PLAYER_PATH:-}" ]; then
    if [ -d "$UNITY_PLAYER_PATH" ]; then
      echo "$UNITY_PLAYER_PATH"
      return 0
    fi
    if [ -f "$UNITY_PLAYER_PATH" ]; then
      dirname "$UNITY_PLAYER_PATH"
      return 0
    fi
  fi

  if [ -n "${OUTPUT_DIRECTORY:-}" ] && [ -d "$OUTPUT_DIRECTORY" ]; then
    echo "$OUTPUT_DIRECTORY"
    return 0
  fi

  find . -maxdepth 3 -type d -regex '.*/temp[^/]*/default-webgl.*' -print -quit
}

clean_deploy_artifacts() {
  local path
  for path in "${DEPLOY_PATHS[@]}"; do
    rm -rf "$path"
  done
}

# game-ci uses buildName: WebGL, which can export a nested WebGL/ folder.
# GitHub Pages must serve index.html at the repository root.
hoist_nested_webgl_build() {
  if [ ! -f WebGL/index.html ]; then
    return 0
  fi

  echo "Promoting nested WebGL/ output to repository root..."
  clean_deploy_artifacts
  shopt -s dotglob nullglob
  mv WebGL/* .
  shopt -u dotglob
  rmdir WebGL 2>/dev/null || rm -rf WebGL
}

buildfolder="$(resolve_build_folder)"
if [ -z "$buildfolder" ]; then
  echo "Could not find build folder." >&2
  echo "Checked UNITY_PLAYER_PATH, OUTPUT_DIRECTORY, and ./temp*/default-webgl*" >&2
  exit 1
fi

buildfolder="$(cd "$buildfolder" && pwd)"
echo "Build folder: $buildfolder"

if [ ! -d ./tmp ]; then
  git clone "https://${GITHUB_TOKEN}@github.com/${GITHUB_USER}/${GITHUB_REPO}" ./tmp
fi

cd ./tmp
clean_deploy_artifacts
cp -r "$buildfolder/." .
hoist_nested_webgl_build
ls -la

git config --global user.email "$GITHUB_EMAIL"
git config --global user.name "$GIT_AUTHOR_NAME"

git add -A index.html TemplateData Build StreamingAssets
git add -u .

if git diff --cached --quiet; then
  echo "No changes to commit; skipping push."
  exit 0
fi

git commit -m "${COMMIT_MESSAGE}"
git log -1
git push --force

echo "====================DEPLOYMENT_TO_GITHUB_PAGES_END============================="
exit 0
