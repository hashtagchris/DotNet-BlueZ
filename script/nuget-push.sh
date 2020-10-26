#!/usr/bin/env bash

# Usage:
# 0. Create an API key at https://www.nuget.org/account/apikeys and store it in ~/nuget.org.key
# 1. Increment the package version number in src/DotNetBlueZ.csproj
# 2. Run `dotnet build src` or `dotnet pack src`
# 3. Run this script, which will publish the latest nuget package.

SCRIPT_PATH=`dirname $0`

NUGET_PKG=$1
if [ -z "$NUGET_PKG" ]; then
  NUGET_PKG=`find $SCRIPT_PATH/.. -name "HashtagChris.DotNetBlueZ.*.nupkg" | sort | tail -1`
fi

if [ -z "$NUGET_PKG" ]; then
  echo "No nuget package found. Be sure to run dotnet build src or supply a path."
  exit 0
fi

echo "Publishing $NUGET_PKG"
dotnet nuget push -s https://api.nuget.org/v3/index.json -k `cat ~/nuget.org.key` $NUGET_PKG
