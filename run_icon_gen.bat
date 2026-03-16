@echo off
cd /d "d:\MyWork\SunEyeVision\SunEyeVision"
python generate_icon_correct.py
if errorlevel 1 (
    echo Error occurred!
) else (
    echo Success!
)
pause
