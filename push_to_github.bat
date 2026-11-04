@echo off
echo ========================================================
echo   Pushing EarbudsController v1.0.0 to GitHub
echo ========================================================
echo.
echo Make sure you have created an empty repository named 'EarbudsController'
echo on your GitHub account (https://github.com/new).
echo.
pause

git branch -M main
git push -u origin main --tags

echo.
if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] Successfully pushed v1.0.0 to GitHub!
) else (
    echo [NOTE] If you haven't created the repository yet, go to:
    echo        https://github.com/new
    echo        Create 'EarbudsController' and run this script again.
)
echo.
pause
