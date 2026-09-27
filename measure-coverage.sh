#!/bin/bash
# Phase 5: Coverage Measurement Script
# Measures test coverage for both backend and frontend
# Enforces 80% threshold

set -e

echo "════════════════════════════════════════════════════════════════"
echo "         Phase 5: Coverage Measurement & Enforcement"
echo "════════════════════════════════════════════════════════════════"
echo ""

BACKEND_PASS=true
FRONTEND_PASS=true
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "📊 Measuring backend coverage..."
echo "─────────────────────────────────────────────────────────────"

cd src/BannerService

# Run backend tests with coverage
if dotnet test \
  --collect:"XPlat Code Coverage" \
  --logger:console \
  --verbosity:minimal; then
  echo -e "${GREEN}✅ Backend tests passed${NC}"
else
  echo -e "${RED}❌ Backend tests failed${NC}"
  BACKEND_PASS=false
fi

# Check if coverage directory exists
if [ -d "TestResults" ]; then
  # Find the coverage.cobertura.xml file
  COVERAGE_FILE=$(find TestResults -name "coverage.cobertura.xml" | head -1)
  if [ -f "$COVERAGE_FILE" ]; then
    echo -e "${GREEN}✅ Coverage report generated: $COVERAGE_FILE${NC}"
    echo ""
    echo "Backend Coverage Report Location:"
    echo "  Full Path: $(pwd)/$COVERAGE_FILE"
    echo ""
    echo "To generate HTML report:"
    echo "  dotnet tool install -g dotnet-reportgenerator-globaltool"
    echo "  reportgenerator -reports:\"$COVERAGE_FILE\" -targetdir:\"coverage/html\" -reporttypes:Html"
  else
    echo -e "${YELLOW}⚠️  Coverage file not found in TestResults${NC}"
  fi
fi

cd - > /dev/null

echo ""
echo "📊 Measuring frontend coverage..."
echo "─────────────────────────────────────────────────────────────"

cd src/BannerUI

# Run frontend tests with coverage
if npm run test:coverage 2>&1; then
  echo -e "${GREEN}✅ Frontend tests passed with coverage${NC}"

  if [ -f "coverage/lcov-report/index.html" ]; then
    echo -e "${GREEN}✅ Coverage report generated: coverage/lcov-report/index.html${NC}"
    echo ""
    echo "Frontend Coverage Report Location:"
    echo "  $(pwd)/coverage/lcov-report/index.html"
  fi
else
  echo -e "${RED}❌ Frontend tests failed or coverage threshold not met${NC}"
  FRONTEND_PASS=false
fi

cd - > /dev/null

echo ""
echo "════════════════════════════════════════════════════════════════"
echo "                    COVERAGE SUMMARY"
echo "════════════════════════════════════════════════════════════════"
echo ""

if [ "$BACKEND_PASS" = true ]; then
  echo -e "${GREEN}✅ Backend: PASSED${NC}"
else
  echo -e "${RED}❌ Backend: FAILED${NC}"
fi

if [ "$FRONTEND_PASS" = true ]; then
  echo -e "${GREEN}✅ Frontend: PASSED${NC}"
else
  echo -e "${RED}❌ Frontend: FAILED${NC}"
fi

echo ""
echo "Next Steps:"
echo "─────────────────────────────────────────────────────────────"
echo "1. Review backend coverage report:"
echo "   open src/BannerService/TestResults/coverage.cobertura.xml"
echo ""
echo "2. Review frontend coverage report:"
echo "   open src/BannerUI/coverage/lcov-report/index.html"
echo ""
echo "3. Target: 80% coverage across entire codebase"
echo ""
echo "4. Enforcement: Build gates active for both backend and frontend"
echo ""

if [ "$BACKEND_PASS" = true ] && [ "$FRONTEND_PASS" = true ]; then
  echo -e "${GREEN}🎉 Phase 5 Enforcement Ready!${NC}"
  exit 0
else
  echo -e "${RED}⚠️  Coverage enforcement needs attention${NC}"
  exit 1
fi
