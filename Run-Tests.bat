@echo off
:: ============================================================================
:: CXA - Run Unit Tests
:: ============================================================================
:: Runs all unit tests for the CXA solution
:: ============================================================================

title CXA - Running Tests

echo.
echo  ========================================================
echo   CXA - Unit Tests
echo  ========================================================
echo.

:: Run tests
echo [INFO] Running tests...
echo.
dotnet test MedSecureVision.Tests\MedSecureVision.Tests.csproj --configuration Release --verbosity normal

echo.
if %ERRORLEVEL% equ 0 (
    echo  ========================================================
    echo   ALL TESTS PASSED!
    echo  ========================================================
) else (
    echo  ========================================================
    echo   SOME TESTS FAILED
    echo  ========================================================
)
echo.
pause
