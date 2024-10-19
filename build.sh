#!/bin/bash
set -ev
dotnet restore ./Tests
dotnet test ./Tests/Tests.fsproj -c Release
dotnet publish ./HttpApi -c Release -r debian.8-x64 -o Published
zip -r yog-robot.agent.zip ./HttpApi/Published
