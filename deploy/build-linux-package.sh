#!/usr/bin/env bash
set -euo pipefail

OUTPUT_DIR=${1:-"dist/linux"}
RUNTIME=${2:-"linux-x64"}
SELF_CONTAINED=${SELF_CONTAINED:-true}

ROOT=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)

echo "==> Nettoyage de ${OUTPUT_DIR}"
rm -rf "${OUTPUT_DIR}"
mkdir -p "${OUTPUT_DIR}"

PUBLISH_ARGS=(publish -c Release -r "${RUNTIME}" --self-contained "${SELF_CONTAINED}")

DAEMON_PUBLISH_SRC="${ROOT}/KyberDaemon/publish/${RUNTIME}"
CLI_PUBLISH_SRC="${ROOT}/KyberCLI/publish/${RUNTIME}"

echo "==> Publication de KyberDaemon (${RUNTIME})"
pushd "${ROOT}/KyberDaemon" > /dev/null
dotnet "${PUBLISH_ARGS[@]}" -o "${DAEMON_PUBLISH_SRC}"
popd > /dev/null

echo "==> Publication de KyberCLI (${RUNTIME})"
pushd "${ROOT}/KyberCLI" > /dev/null
dotnet "${PUBLISH_ARGS[@]}" -o "${CLI_PUBLISH_SRC}"
popd > /dev/null

echo "==> Compilation du module PowerShell"
pushd "${ROOT}/KyberModule" > /dev/null
dotnet build -c Release
popd > /dev/null

PACKAGE_ROOT="${OUTPUT_DIR}/KyberModule"
mkdir -p "${PACKAGE_ROOT}"
cp "${ROOT}/KyberModule/KyberModule.psd1" "${PACKAGE_ROOT}/"
cp "${ROOT}/KyberModule/bin/Release/netstandard2.0/KyberModule.dll" "${PACKAGE_ROOT}/"
cp "${ROOT}/KyberLibrary/bin/Release/netstandard2.0/KyberLibrary.dll" "${PACKAGE_ROOT}/"
BC_PATH="$HOME/.nuget/packages/bouncycastle.cryptography/2.6.2/lib/netstandard2.0/BouncyCastle.Cryptography.dll"
if [[ ! -f "${BC_PATH}" ]]; then
  echo "Erreur: BouncyCastle.Cryptography.dll introuvable." >&2
  exit 1
fi
cp "${BC_PATH}" "${PACKAGE_ROOT}/"
cp "${ROOT}/KyberModule/Examples.ps1" "${PACKAGE_ROOT}/" 2>/dev/null || true
cp "${ROOT}/KyberModule/DilithiumExamples.ps1" "${PACKAGE_ROOT}/" 2>/dev/null || true

echo "==> Copie de KyberDaemon"
DAEMON_TARGET_ROOT="${OUTPUT_DIR}/KyberDaemon"
DAEMON_PUBLISH_TARGET="${DAEMON_TARGET_ROOT}/publish/${RUNTIME}"
mkdir -p "${DAEMON_PUBLISH_TARGET}"
cp -R "${DAEMON_PUBLISH_SRC}/"* "${DAEMON_PUBLISH_TARGET}/"
cp -R "${ROOT}/KyberDaemon/config" "${DAEMON_TARGET_ROOT}/config"
PASS=$(tr -dc 'A-Za-z0-9!@#%+=' < /dev/urandom | head -c 24)
sed -i "s|auth_config = .*|auth_config = /etc/kyberd/auth.conf|" "${DAEMON_TARGET_ROOT}/config/kyberd.conf"
sed -i "s|keys_directory = .*|keys_directory = /etc/kyberd/keys|" "${DAEMON_TARGET_ROOT}/config/kyberd.conf"
sed -i "s|key_encryption_mode = .*|key_encryption_mode = passphrase          # dpapi (Windows) ou passphrase|" "${DAEMON_TARGET_ROOT}/config/kyberd.conf"
sed -i "s|key_encryption_passphrase = .*|key_encryption_passphrase = ${PASS}  # obligatoire si key_encryption_mode=passphrase|" "${DAEMON_TARGET_ROOT}/config/kyberd.conf"
cp -R "${ROOT}/KyberDaemon/tools" "${DAEMON_TARGET_ROOT}/tools"
cp -R "${ROOT}/KyberDaemon/deploy" "${DAEMON_TARGET_ROOT}/deploy"

echo "==> Copie de KyberCLI"
CLI_TARGET_ROOT="${OUTPUT_DIR}/KyberCLI"
CLI_PUBLISH_TARGET="${CLI_TARGET_ROOT}/publish/${RUNTIME}"
mkdir -p "${CLI_PUBLISH_TARGET}"
cp -R "${CLI_PUBLISH_SRC}/"* "${CLI_PUBLISH_TARGET}/"

ARCHIVE="${OUTPUT_DIR}/KyberModule-${RUNTIME}.tar.gz"
echo "==> Création de l'archive ${ARCHIVE}"
tar -C "${OUTPUT_DIR}" -czf "${ARCHIVE}" .

echo "✅ Package Linux disponible dans ${ARCHIVE}"
