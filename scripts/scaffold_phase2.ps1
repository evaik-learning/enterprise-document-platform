function Write-IfMissing($Path, $Content) {
    if (-not (Test-Path $Path)) {
        $Parent = Split-Path $Path
        if (-not (Test-Path $Parent)) {
            New-Item -ItemType Directory -Path $Parent -Force | Out-Null
        }
        Set-Content -Path $Path -Value $Content -Encoding UTF8
    }
}

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
$root = Split-Path -Path $scriptDir -Parent
$services = @('Identity','Organization','Document','Workflow','Storage','Notification','Audit')

foreach ($service in $services) {
    foreach ($proj in @('Api','Application','Domain','Infrastructure')) {
        $projName = "Edp.$service.$proj"
        $projDir = Join-Path $root "src\Services\$service\$projName"
        if (-not (Test-Path $projDir)) {
            New-Item -ItemType Directory -Path $projDir -Force | Out-Null
        }

        $csprojPath = Join-Path $projDir "$projName.csproj"
        if (-not (Test-Path $csprojPath)) {
            switch ($proj) {
                'Api' {
                    $content = '<Project Sdk="Microsoft.NET.Sdk.Web">`n`n  <PropertyGroup>`n    <TargetFramework>net10.0</TargetFramework>`n    <Nullable>enable</Nullable>`n    <ImplicitUsings>enable</ImplicitUsings>`n  </PropertyGroup>`n`n  <ItemGroup>`n    <ProjectReference Include="..\\Edp.' + $service + '.Application\\Edp.' + $service + '.Application.csproj" />`n  </ItemGroup>`n`n</Project>`n'
                }
                'Application' {
                    $content = '<Project Sdk="Microsoft.NET.Sdk">`n`n  <PropertyGroup>`n    <TargetFramework>net10.0</TargetFramework>`n    <Nullable>enable</Nullable>`n    <ImplicitUsings>enable</ImplicitUsings>`n  </PropertyGroup>`n`n  <ItemGroup>`n    <ProjectReference Include="..\\Edp.' + $service + '.Domain\\Edp.' + $service + '.Domain.csproj" />`n  </ItemGroup>`n`n</Project>`n'
                }
                'Domain' {
                    $content = '<Project Sdk="Microsoft.NET.Sdk">`n`n  <PropertyGroup>`n    <TargetFramework>net10.0</TargetFramework>`n    <Nullable>enable</Nullable>`n    <ImplicitUsings>enable</ImplicitUsings>`n  </PropertyGroup>`n`n</Project>`n'
                }
                'Infrastructure' {
                    $content = '<Project Sdk="Microsoft.NET.Sdk">`n`n  <PropertyGroup>`n    <TargetFramework>net10.0</TargetFramework>`n    <Nullable>enable</Nullable>`n    <ImplicitUsings>enable</ImplicitUsings>`n  </PropertyGroup>`n`n  <ItemGroup>`n    <ProjectReference Include="..\\Edp.' + $service + '.Application\\Edp.' + $service + '.Application.csproj" />`n    <ProjectReference Include="..\\Edp.' + $service + '.Domain\\Edp.' + $service + '.Domain.csproj" />`n  </ItemGroup>`n`n</Project>`n'
                }
            }
            Set-Content -Path $csprojPath -Value $content -Encoding UTF8
            if ($proj -eq 'Api') {
                $programPath = Join-Path $projDir 'Program.cs'
                if (-not (Test-Path $programPath)) {
                    $programContent = 'var builder = WebApplication.CreateBuilder(args)`n`nbuilder.Services.AddControllers();`n`nvar app = builder.Build();`n`napp.MapGet("/", () => Results.Ok("Service is running."));`napp.MapControllers();`n`napp.Run();`n'
                    Set-Content -Path $programPath -Value $programContent -Encoding UTF8
                }
            }
        }
    }

    $readmePath = Join-Path $root "src\Services\$service\README.md"
    $readmeContent = "# $service Service`n`nThis folder contains scaffolded service projects for the $service domain:`n`n- `Edp.$service.Api`n- `Edp.$service.Application`n- `Edp.$service.Domain`n- `Edp.$service.Infrastructure`n"
    Write-IfMissing $readmePath $readmeContent
}

Write-IfMissing "$root\infra\README.md" '# Infrastructure`n`nAzure infrastructure as code and deployment templates for the Enterprise Document Platform.`n'
Write-IfMissing "$root\infra\main.bicep" "targetScope = 'resourceGroup'`n`nparam location string = resourceGroup().location`nparam storageAccountName string = 'edpplatform' + uniqueString(resourceGroup().id)`n`nresource storageAccount 'Microsoft.Storage/storageAccounts@2024-06-01' = {`n  name: storageAccountName`n  location: location`n  sku: {`n    name: 'Standard_LRS'`n  }`n  kind: 'StorageV2'`n  properties: {`n    accessTier: 'Hot'`n  }`n}`n"
Write-IfMissing "$root\pipelines\README.md" '# CI/CD pipelines`n`nPipeline definitions for the Enterprise Document Platform.`n'
Write-IfMissing "$root\pipelines\ci.yml" "trigger:`n  branches:`n    include:`n      - main`n      - develop`n`npool:`n  vmImage: 'windows-latest'`n`nsteps:`n  - task: UseDotNet@2`n    displayName: 'Install .NET SDK'`n    inputs:`n      packageType: 'sdk'`n      version: '10.0.x'`n`n  - script: dotnet restore EnterpriseDocumentPlatform.sln`n    displayName: 'Restore NuGet packages'`n`n  - script: dotnet build EnterpriseDocumentPlatform.sln --configuration Debug --no-restore`n    displayName: 'Build solution'`n`n  - script: dotnet test EnterpriseDocumentPlatform.sln --configuration Debug --no-build`n    displayName: 'Run tests'`n"
Write-IfMissing "$root\pipelines\cd.yml" "trigger: none`n`npr: none`n`npool:`n  vmImage: 'windows-latest'`n`nstages:`n  - stage: Deploy`n    displayName: 'Deploy to Azure'`n    jobs:`n      - job: Deploy`n        steps:`n          - script: echo 'Deployment pipeline placeholder for Enterprise Document Platform.'`n            displayName: 'Deploy placeholder'`n"
Write-IfMissing "$root\scripts\README.md" '# Scripts`n`nLocal helper scripts for building, testing, and running the platform.`n'
Write-IfMissing "$root\scripts\build.ps1" 'dotnet build ..\\EnterpriseDocumentPlatform.sln --configuration Debug`n'
Write-IfMissing "$root\scripts\test.ps1" 'dotnet test ..\\EnterpriseDocumentPlatform.sln --configuration Debug`n'
Write-IfMissing "$root\scripts\run-local.ps1" 'dotnet run --project ..\\src\\Gateway\\Edp.Gateway --configuration Debug`n'
Write-IfMissing "$root\docker\README.md" '# Docker`n`nDockerfiles and local container configuration for the Enterprise Document Platform.`n'
Write-IfMissing "$root\docker\Dockerfile.gateway" "FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base`nWORKDIR /app`nEXPOSE 80`n`nFROM mcr.microsoft.com/dotnet/sdk:10.0 AS build`nWORKDIR /src`nCOPY ../src/Gateway/Edp.Gateway/Edp.Gateway.csproj ./`nRUN dotnet restore \"Edp.Gateway.csproj\"`nCOPY ../src/Gateway/Edp.Gateway/. ./`nRUN dotnet publish \"Edp.Gateway.csproj\" -c Release -o /app/publish`n`nFROM base AS final`nWORKDIR /app`nCOPY --from=build /app/publish .`nENTRYPOINT [\"dotnet\", \"Edp.Gateway.dll\"]`n"
Write-IfMissing "$root\docker\Dockerfile.template.api" "FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base`nWORKDIR /app`nEXPOSE 80`n`nFROM mcr.microsoft.com/dotnet/sdk:10.0 AS build`nWORKDIR /src`nCOPY ../src/Services/Template/Edp.Template.Api/Edp.Template.Api.csproj ./`nRUN dotnet restore \"Edp.Template.Api.csproj\"`nCOPY ../src/Services/Template/Edp.Template.Api/. ./`nRUN dotnet publish \"Edp.Template.Api.csproj\" -c Release -o /app/publish`n`nFROM base AS final`nWORKDIR /app`nCOPY --from=build /app/publish .`nENTRYPOINT [\"dotnet\", \"Edp.Template.Api.dll\"]`n"
Write-IfMissing "$root\docker\docker-compose.yml" "version: '3.9'`nservices:`n  gateway:`n    build:`n      context: ..`n      dockerfile: docker/Dockerfile.gateway`n    ports:`n      - '5000:80'`n    depends_on:`n      - template-api`n  template-api:`n    build:`n      context: ..`n      dockerfile: docker/Dockerfile.template.api`n    ports:`n      - '5001:80'`n"
Write-IfMissing "$root\tests\README.md" '# Tests`n`nUnit and integration test projects for the Enterprise Document Platform.`n'

function Write-TestProject($name) {
    $testDir = Join-Path $root "tests\$name"
    if (-not (Test-Path $testDir)) {
        New-Item -ItemType Directory -Path $testDir -Force | Out-Null
    }
    $csproj = Join-Path $testDir "$name.csproj"
    $content = '<Project Sdk="Microsoft.NET.Sdk">`n`n  <PropertyGroup>`n    <TargetFramework>net10.0</TargetFramework>`n    <IsPackable>false</IsPackable>`n    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>`n  </PropertyGroup>`n`n  <ItemGroup>`n    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.4.0" />`n    <PackageReference Include="xunit" Version="2.5.0" />`n    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />`n    <PackageReference Include="coverlet.collector" Version="3.2.0" />`n  </ItemGroup>`n`n</Project>`n'
    Write-IfMissing $csproj $content
    Write-IfMissing (Join-Path $testDir 'UnitTest1.cs') "using Xunit;`n`nnamespace $name;`n`npublic class UnitTest1`n{`n    [Fact]`n    public void Test1()`n    {`n        Assert.True(true);`n    }`n}`n"
}

Write-TestProject 'Edp.Gateway.Tests'
Write-TestProject 'Edp.Template.Tests'
Write-IfMissing "$root\docs\Phase-2-Foundation-Status.md" '# Phase 2 Foundation Implementation Status`n`nThis document tracks the completion of Phase 2 foundation scaffolding.`n`n- Service scaffolding created for Identity, Organization, Document, Workflow, Storage, Notification, Audit`n- Infrastructure, pipelines, scripts, docker, and tests scaffolding added`n- Gateway middleware enabled in bootstrap`n'

$projectFiles = Get-ChildItem -Path "$root\src\Services" -Recurse -Filter *.csproj | Select-Object -ExpandProperty FullName
$projectFiles += Get-ChildItem -Path "$root\tests" -Recurse -Filter *.csproj | Select-Object -ExpandProperty FullName
foreach ($proj in $projectFiles) {
    dotnet sln "$root\EnterpriseDocumentPlatform.sln" add $proj | Out-Null
}

Write-Output 'Phase 2 scaffolding created and solution updated.'
