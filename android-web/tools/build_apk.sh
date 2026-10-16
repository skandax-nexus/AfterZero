#!/usr/bin/env bash
# Builds the installable Android APK: copies the game into the WebView shell, assembles with Apktool,
# then zipaligns + signs (v1-v3) with uber-apk-signer. Output: dist/AfterZero_FirstCycle.apk
set -euo pipefail
cd "$(dirname "$0")/.."
VERSION_NAME="${VERSION_NAME:-1.0.0}"
VERSION_CODE="${VERSION_CODE:-1}"
rm -rf build && mkdir -p build dist
# Fetch the build tools on first use (kept out of git).
[ -f tools/apktool.jar ] || curl -sSL -o tools/apktool.jar https://github.com/iBotPeaches/Apktool/releases/download/v2.10.0/apktool_2.10.0.jar
[ -f tools/signer.jar ] || curl -sSL -o tools/signer.jar https://github.com/patrickfav/uber-apk-signer/releases/download/v1.3.0/uber-apk-signer-1.3.0.jar
cp -r android build/app
mkdir -p build/app/assets
cp -r assets/* build/app/assets/
sed -i "s/versionCode: .*/versionCode: $VERSION_CODE/; s/versionName: .*/versionName: $VERSION_NAME/" build/app/apktool.yml
java -jar tools/apktool.jar b build/app -o build/unsigned.apk >/dev/null
KS=tools/afterzero-release.jks
if [ ! -f "$KS" ]; then
  keytool -genkeypair -keystore "$KS" -alias afterzero -keyalg RSA -keysize 2048 -validity 10000 \
    -storepass afterzero -keypass afterzero -dname "CN=AfterZero, O=Skandax, C=US" >/dev/null 2>&1
fi
java -jar tools/signer.jar -a build/unsigned.apk -o build/signed --ks "$KS" --ksAlias afterzero --ksPass afterzero --ksKeyPass afterzero >/dev/null
cp build/signed/*.apk dist/AfterZero_FirstCycle.apk
java -jar tools/signer.jar -a dist/AfterZero_FirstCycle.apk --onlyVerify | grep -E "VERIFY|signature|Signature" | head -5 || true
ls -la dist/AfterZero_FirstCycle.apk
