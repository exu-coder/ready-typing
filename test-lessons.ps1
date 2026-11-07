[CmdletBinding()]
param(
    # Pass the .NET SDK path when no SDK is on PATH.
    [string]$Dotnet = "dotnet"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$lessonFile = Join-Path $root "BijoyTrainer\Data\lessons.txt"
$testProject = Join-Path $root "LessonTests\LessonTests.csproj"

Write-Host "Testing all 118 lessons..."
& $Dotnet run --project $testProject -c Release -- $lessonFile
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    throw "Lesson tests failed with exit code $exitCode."
}

Write-Host "Lesson tests passed."
