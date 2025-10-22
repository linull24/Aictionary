[CmdletBinding()]
Param(
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$BuildArguments
)

$ScriptDir = Split-Path $MyInvocation.MyCommand.Path -Parent

& dotnet run --project "$ScriptDir/build/build.csproj" -- $BuildArguments

exit $LASTEXITCODE
