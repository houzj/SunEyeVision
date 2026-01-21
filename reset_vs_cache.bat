@echo off
echo Resetting Visual Studio cache...

echo.
echo Closing Visual Studio first if running...
taskkill /F /IM devenv.exe 2>nul
timeout /t 2 /nobreak >nul

echo.
echo Cleaning .vs directory...
rd /s /q "c:\Users\houzhongjie\CodeBuddy\20260119143126\.vs" 2>nul

echo.
echo Cleaning bin and obj directories...
for /d /r "c:\Users\houzhongjie\CodeBuddy\20260119143126" %%d in (bin) do @rd /s /q "%%d" 2>nul
for /d /r "c:\Users\houzhongjie\CodeBuddy\20260119143126" %%d in (obj) do @rd /s /q "%%d" 2>nul

echo.
echo Done! Please reopen Visual Studio and the solution.
pause
