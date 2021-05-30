@ECHO OFF
SETLOCAL EnableExtensions
SETLOCAL EnableDelayedExpansion

SET WORKING_DIR=%cd%
SET SCRIPTS_DIR=OpenvibeScripts

REM If not set to PASSIVE, OpenMP (Eigen) boxes may load the cores fully even if there's little to do.
SET "OMP_WAIT_POLICY=PASSIVE"

REM Get the directory location of this script, assume it contains the OpenViBE dist tree. These variables will be used by OpenViBE executables.
SET WD=%~dp0
SET OV_PATH_ROOT=%~2
SET OV_PATH_BIN=%OV_PATH_ROOT%\bin
SET OV_PATH_LIB=%OV_PATH_ROOT%\bin
SET OV_PATH_DATA=%OV_PATH_ROOT%\share\openvibe

REM Default behavior
SET OV_PAUSE=PAUSE
SET OV_RUN_IN_BG=

SET ARGS=--no-gui --play "%WORKING_DIR%\%SCRIPTS_DIR%\%1"
SET EMPTY=

SET OV_RUN_IN_BG=START "openvibe-designer.exe"

REM Set dependency paths etc...
SET "OV_ENVIRONMENT_FILE=%OV_PATH_ROOT%\bin\OpenViBE-set-env.cmd"
IF NOT EXIST "%OV_ENVIRONMENT_FILE%" (
	ECHO Error: "%OV_ENVIRONMENT_FILE%" was not found
	GOTO EndOfScript
)
CALL "%OV_ENVIRONMENT_FILE%"

"%OV_PATH_ROOT%\bin\openvibe-designer.exe" %ARGS%
if %ERRORLEVEL% GEQ 1 pause