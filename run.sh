#!/usr/bin/env bash
set -e

APP_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$APP_DIR/src/GitHubStats.Api"
SOLUTION="$APP_DIR/src/GitHubStats.sln"
URL="http://localhost:5042"

# Colors
GREEN='\033[0;32m'
CYAN='\033[0;36m'
RED='\033[0;31m'
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
echo -e "${GREEN}[1/3]${NC} Building..."
dotnet build "$SOLUTION" -c Release --nologo -v q

# Run Tests
echo -e "${GREEN}[2/3]${NC} Running tests..."
dotnet test "$SOLUTION" -c Release --no-build --nologo -v q
echo -e "  ${GREEN}Tests passed${NC}"

# Run
echo -e "${GREEN}[3/3]${NC} Starting server..."
echo ""
echo -e "  ${GREEN}➜${NC}  App:    ${CYAN}${URL}${NC}"
echo -e "  ${GREEN}➜${NC}  Health: ${CYAN}${URL}/health${NC}"
echo ""

dotnet run --project "$PROJECT" --configuration Release --no-build --launch-profile http
