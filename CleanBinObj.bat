@echo off
setlocal enabledelayedexpansion

REM L�scht "bin" und "obj" Ordner im aktuellen Verzeichnis
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

REM Durchl�uft alle Unterordner und l�scht "bin" und "obj" Ordner
for /d %%d in (*) do (
    if exist "%%d\bin" (
        echo L�sche %%d\bin
        rmdir /s /q "%%d\bin"
    )
    if exist "%%d\obj" (
        echo L�sche %%d\obj
        rmdir /s /q "%%d\obj"
    )
)

echo Fertig.
pause