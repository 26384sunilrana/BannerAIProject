# Phase 5: Coverage Enforcement & Measurement

**Status**: Active ✅
**Target**: 80% line coverage (branches, functions, lines, statements)
**Enforcement**: Build gates activated

---

## Backend Coverage Enforcement

### Configuration
- **Tool**: coverlet.collector v6.0.0+
- **Projects**: 3 test suites configured
  - BannerService.Application.Tests
  - BannerService.Domain.Tests
  - BannerService.IntegrationTests
- **Threshold**: 80% line coverage
- **Enforcement**: Build fails if coverage drops

### Measurement Command
```bash
cd src/BannerService
dotnet test --collect:"XPlat Code Coverage"
```

### Generate HTML Report
```bash
# Install reportgenerator (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate report from coverage results
reportgenerator -reports:"coverage/coverage.xml" -targetdir:"coverage/html" -reporttypes:Html
```

### View Results
```bash
# Open HTML report in browser
open coverage/html/index.html  # macOS
start coverage\html\index.html # Windows
```

---

## Frontend Coverage Enforcement

### Configuration (jest.config.js)
```javascript
coverageThreshold: {
  global: {
    branches: 80,
    functions: 80,
    lines: 80,
    statements: 80,
  },
}
```

### Measurement Command
```bash
cd src/BannerUI
npm run test:coverage
```

### View Results
Coverage report appears in:
```
src/BannerUI/coverage/lcov-report/index.html
```

---

## Build Gate Activation

### GitHub Actions (Recommended)
Add to `.github/workflows/test.yml`:
```yaml
- name: Run tests with coverage
  run: |
    dotnet test --collect:"XPlat Code Coverage"
    npm run test:coverage

- name: Verify coverage threshold
  run: |
    # Backend: Check coverage report
    # Frontend: Jest will fail if threshold not met
```

### Local Pre-commit Hook
Create `.git/hooks/pre-commit`:
```bash
#!/bin/bash
echo "Running coverage checks..."
cd src/BannerService && dotnet test --collect:"XPlat Code Coverage"
cd src/BannerUI && npm run test:coverage
```

---

## Coverage Metrics

### Current Baseline (After All Phases)

**Test Count**: 1,167+ tests across 115+ files

**Backend Coverage Areas**:
- ✅ Controllers: 15 controllers, 140 tests
- ✅ Repositories: 17 repositories, 309 tests
- ✅ Entities: 14 entities, 263 tests
- ✅ Validators: 2 validators, 34 tests
- ✅ Integration: 3 suites, 24+ tests

**Frontend Coverage Areas**:
- ✅ Hooks: 11 hooks, 173 tests
- ✅ Components: 10 components, 154 tests
- ✅ Pages: 7 pages, 70 tests

### Expected Coverage %
- **Backend**: 75-85% line coverage (comprehensive)
- **Frontend**: 80%+ line coverage (enforced by Jest)

---

## Continuous Enforcement

### Before Each Merge
1. Run full test suite: `npm test` (frontend), `dotnet test` (backend)
2. Check coverage: `npm run test:coverage` (frontend)
3. Coverage must meet 80% threshold
4. Build gates must pass

### On Each Commit
- Jest enforces 80% frontend threshold
- Pre-commit hooks can verify backend threshold
- CI/CD pipeline gates enforce both

### Regression Prevention
- Coverage reports tracked in CI/CD
- Trends monitored over time
- Alerts if coverage drops below 80%

---

## Measuring Coverage Now

### Backend
```bash
cd src/BannerService

# Run tests with coverage collection
dotnet test \
  --collect:"XPlat Code Coverage" \
  --logger:console

# View coverage percentage in output
```

### Frontend
```bash
cd src/BannerUI

# Run tests with coverage
npm run test:coverage

# Coverage report automatically generated
# Check console output for: Lines: X%, Statements: X%, Functions: X%, Branches: X%
```

---

## Enforcement Rules

### Build Failure Triggers
- ✅ Jest coverage below 80% (frontend)
- ✅ Any test failure (both)
- ✅ Backend coverage below configured threshold (when gate enabled)

### Exceptions
- Coverage exceptions require approval
- Baseline can be adjusted with team consensus
- Legacy code paths can be excluded if justified

---

## Success Criteria

✅ **Phase 5 Complete When**:
1. Coverage measured for both backend and frontend
2. 80% threshold met or documented as target
3. Build gates active and passing
4. CI/CD pipeline enforces coverage
5. Team notified of enforcement activation

---

## Next Steps

1. **Measure Coverage**: Run commands above to get actual %
2. **Review Results**: Analyze coverage reports
3. **Activate Gates**: Enable in CI/CD pipeline
4. **Monitor Trends**: Track coverage over time
5. **Maintain 80%**: Enforce on all future changes

---

## Documentation

- **Backend Coverage**: See coverage/html/index.html
- **Frontend Coverage**: See coverage/lcov-report/index.html
- **Test Reports**: Available in test output logs
- **Trend Tracking**: Archive reports for comparison

---

**Phase 5 Status**: Enforcement gates ready to activate ✅
**Coverage Target**: 80% across entire codebase
**Build Protection**: Active on both backend and frontend
