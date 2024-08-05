@echo off
setlocal enabledelayedexpansion

if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

for /d %%d in (*) do (
    if exist "%%d\bin" (
        echo Deleting %%d\bin
        rmdir /s /q "%%d\bin"
    )
    if exist "%%d\obj" (
        echo Deleting %%d\obj
        rmdir /s /q "%%d\obj"
    )
)

echo Fertig.
pause