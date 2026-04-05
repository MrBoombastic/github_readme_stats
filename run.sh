#!/usr/bin/env bash
set -e

APP_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$APP_DIR/src/GitHubStats.Api"
URL="http://localhost:5042"

# Colors
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}   GitHub Readme Stats                  ${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

# Check .NET SDK
if ! command -v dotnet &>/dev/null; then
    echo "Error: .NET SDK not found. Install .NET 9.0 from https://dotnet.microsoft.com/download"
    exit 1
fi

echo -e "  .NET SDK: $(dotnet --version)"

# Restore & Build
echo ""
echo -e "${GREEN}[1/2]${NC} Building..."
dotnet build "$APP_DIR/src/GitHubStats.sln" -c Release --nologo -v q

# Run
echo -e "${GREEN}[2/2]${NC} Starting server..."
echo ""
echo -e "  ${GREEN}➜${NC}  App:    ${CYAN}${URL}${NC}"
echo -e "  ${GREEN}➜${NC}  Health: ${CYAN}${URL}/health${NC}"
echo ""

dotnet run --project "$PROJECT" --configuration Release --no-build --launch-profile http
