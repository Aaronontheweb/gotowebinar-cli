# GoToWebinar CLI Development Loop

Execute the following verification steps after making changes to the codebase:

## 1. Build Verification
```bash
# Clean and build the solution
dotnet clean
dotnet build -c Release
```

## 2. Run Tests
```bash
# Run all unit tests
dotnet test --no-build -c Release

# Run with coverage (if configured)
dotnet test --collect:"XPlat Code Coverage" --no-build -c Release
```

## 3. Code Quality Checks
```bash
# Format verification
dotnet format --verify-no-changes --verbosity diagnostic

# Run analyzers
dotnet build -c Release /p:TreatWarningsAsErrors=true /p:EnforceCodeStyleInBuild=true
```

## 4. AOT Compilation Test
```bash
# Test AOT compilation for current platform
dotnet publish src/GoToWebinarCLI -c Release -r linux-x64 /p:PublishAot=true

# Verify AOT compatibility
./src/GoToWebinarCLI/bin/Release/net9.0/linux-x64/publish/gotowebinar --test-aot

# Check binary size (should be <10MB)
du -h ./src/GoToWebinarCLI/bin/Release/net9.0/linux-x64/publish/gotowebinar
```

## 5. Integration Tests (when API client is ready)
```bash
# Test configuration
gotowebinar config test

# Test help system
gotowebinar --help
gotowebinar webinar --help
```

## Success Criteria
- [ ] All builds complete without errors
- [ ] All tests pass
- [ ] No formatting issues
- [ ] No analyzer warnings
- [ ] AOT compilation succeeds
- [ ] Binary size <10MB
- [ ] Startup time <50ms

## On Failure
1. Fix compilation errors first
2. Address failing tests
3. Fix formatting issues with `dotnet format`
4. Resolve analyzer warnings
5. Check AOT compatibility annotations
6. Review binary size if >10MB

## Continuous Verification
Run this loop:
1. After each significant code change
2. Before committing
3. Before creating pull requests
4. After merging branches