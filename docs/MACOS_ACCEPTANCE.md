# macOS Acceptance

## Build and launch

1. On macOS 14 or later with Swift 5.9 or later, run `./script/build_and_run.sh`.
2. Confirm `dist/macos/AGYReadyMac.app` opens with the title **AGY CLI-CLASP 安裝小幫手（標標版）** and the 「標」 app icon.
3. Confirm the window has **環境健檢**, **AGY CLI**, **Node.js / clasp**, and **作者** tabs, and can be resized.
4. Run `./script/build_and_run.sh --verify` and confirm it exits successfully with the app running.

## Environment and help pages

1. On **環境健檢**, confirm the displayed macOS version and architecture match **About This Mac**.
2. Confirm installed AGY, Git, Node.js, npm, and clasp are listed with useful status details. Use **重新檢查** after changing the environment.
3. On **作者**, confirm the author name and that the Facebook and email links open the supplied destinations.
4. On **Node.js / clasp**, confirm the Node.js download action opens `https://nodejs.org/en/download` when setup is needed. Do not proceed with installation unless the Mac owner intends to install it.

## AGY install and PATH recovery

Perform these steps only on a disposable macOS account or after saving a copy of `~/.zprofile`.

1. When AGY is absent, select **安裝 AGY CLI** and confirm the dialog identifies the Google-hosted installer. Cancel if you do not want to install AGY.
2. When `~/.local/bin/agy` exists but a fresh zsh login shell cannot resolve it, select **修復 zsh PATH** and confirm the dialog before changing `~/.zprofile`.
3. Confirm the change adds only the managed PATH block, keeps the previous profile in `~/Library/Application Support/AGYReadyMac/path-backup.json`, and the fresh-shell AGY check passes.
4. Select **還原本次 PATH** and confirm the original file content is restored. If another process changes `.zprofile` after repair, restoration should stop and preserve the file and backup.
5. Confirm symbolic-link profiles and pre-existing managed blocks are not overwritten.

## clasp

1. With Node.js 20+ and npm available and clasp missing, select **安裝 clasp**.
2. Confirm the prompt describes `npm install @google/clasp -g`; cancel to leave the system unchanged, or continue and verify clasp with **重新檢查**.

## Distribution notes

- The development build targets the build host architecture. The current verified host build is Apple Silicon/arm64.
- The app bundle is unsigned and not notarized. For broad distribution, sign with a Developer ID certificate, notarize, and validate the distributed bundle on a clean Mac.
- Download progress is shown when the official installer response provides a content length; command-line install stages use indeterminate progress.
