#!/usr/bin/env bash
# Puts together what the plugin compiles against, in the layout its .csproj expects (VALHEIM_DIR):
#   <dest>/valheim_server_Data/Managed/   the game's assemblies, from the free Valheim dedicated server
#                                         (steamcmd, anonymous login; only Managed is kept: ~35 MB)
#   <dest>/BepInEx/core/                  BepInExPack Valheim (Thunderstore)
# Usage: tools/ci/fetch-deps.sh <dest>   (VALHEIM_SERVER_DIR=<installed server> skips the download)
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
source "$here/versions.env"
dest="$(mkdir -p "$1" && cd "$1" && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

if [ ! -f "$dest/valheim_server_Data/Managed/assembly_valheim.dll" ]; then
  server="${VALHEIM_SERVER_DIR:-}"
  if [ -z "$server" ]; then
    case "$(uname -s)" in
      Darwin) url=https://steamcdn-a.akamaihd.net/client/installer/steamcmd_osx.tar.gz ;;
      *) url=https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz ;;
    esac
    mkdir -p "$work/steamcmd" && curl -sSL "$url" | tar xz -C "$work/steamcmd"
    server="$work/server"
    "$work/steamcmd/steamcmd.sh" +@sSteamCmdForcePlatformType linux +force_install_dir "$server" \
      +login anonymous +app_update "$VALHEIM_SERVER_APP" validate +quit
  fi
  mkdir -p "$dest/valheim_server_Data"
  rm -rf "$dest/valheim_server_Data/Managed"
  cp -R "$server/valheim_server_Data/Managed" "$dest/valheim_server_Data/Managed"
fi

if [ ! -f "$dest/BepInEx/core/BepInEx.dll" ]; then
  curl -sSL -o "$work/bepinex.zip" "https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/$BEPINEX_PACK_VERSION/"
  unzip -q -o "$work/bepinex.zip" -d "$work/bepinex"
  mkdir -p "$dest/BepInEx"
  cp -R "$work/bepinex/BepInExPack_Valheim/BepInEx/core" "$dest/BepInEx/"
fi

echo "VALHEIM_DIR=$dest"
