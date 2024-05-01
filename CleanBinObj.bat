@echo off
setlocal enabledelayedexpansion

REM Löscht "bin" und "obj" Ordner im aktuellen Verzeichnis
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

REM Durchläuft alle Unterordner und löscht "bin" und "obj" Ordner
for /d %%d in (*) do (
    if exist "%%d\bin" (
        echo Lösche %%d\bin
        rmdir /s /q "%%d\bin"
    )
    if exist "%%d\obj" (
        echo Lösche %%d\obj
        rmdir /s /q "%%d\obj"
    )
)

echo Fertig.
pause