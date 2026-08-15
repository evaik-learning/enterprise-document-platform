from pathlib import Path

root = Path(__file__).resolve().parent.parent
services = ['Identity', 'Organization', 'Document', 'Workflow', 'Storage', 'Notification', 'Audit']

# Helper for writing if not exists

def write_text(path: Path, content: str):
    if not path.exists():
        path.write_text(content, encoding='utf-8')

# Service scaffolding
for service in services:
    base = root / 'src' / 'Services' / service
    for proj in ['Api', 'Application', 'Domain', 'Infrastructure']:
        proj_name = f'Edp.{service}.{proj}'
        proj_dir = base / proj_name
        proj_dir.mkdir(parents=True, exist_ok=True)
        csproj_path = proj_dir / f'{proj_name}.csproj'
        if not csproj_path.exists():
            if proj == 'Api':
                content = f'''<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\Edp.{service}.Application\\Edp.{service}.Application.csproj" />
  </ItemGroup>

</Project>
'''
            elif proj == 'Application':
                content = f'''<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\Edp.{service}.Domain\\Edp.{service}.Domain.csproj" />
  </ItemGroup>

</Project>
'''
            elif proj == 'Domain':
                content = '''<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
'''
            else:
                content = f'''<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\Edp.{service}.Application\\Edp.{service}.Application.csproj" />
    <ProjectReference Include="..\\Edp.{service}.Domain\\Edp.{service}.Domain.csproj" />
  </ItemGroup>

</Project>
'''
            csproj_path.write_text(content, encoding='utf-8')
        if proj == 'Api':
            write_text(proj_dir / 'Program.cs', '''var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/", () => Results.Ok("Service is running."));
app.MapControllers();

app.Run();
''')
    readme = base / 'README.md'
    write_text(readme, f'# {service} Service\n\nThis folder contains scaffolded service projects for the {service} domain:\n\n- `Edp.{service}.Api`\n- `Edp.{service}.Application`\n- `Edp.{service}.Domain`\n- `Edp.{service}.Infrastructure`\n')

# Add placeholder directories / files
write_text(root / 'infra' / 'README.md', '# Infrastructure\n\nAzure infrastructure as code and deployment templates for the Enterprise Document Platform.\n')
write_text(root / 'infra' / 'main.bicep', """targetScope = 'resourceGroup'

param location string = resourceGroup().location
param storageAccountName string = 'edpplatform' + uniqueString(resourceGroup().id)

resource storageAccount 'Microsoft.Storage/storageAccounts@2024-06-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
  }
}
""")
write_text(root / 'pipelines' / 'README.md', '# CI/CD pipelines\n\nPipeline definitions for the Enterprise Document Platform.\n')
write_text(root / 'pipelines' / 'ci.yml', """trigger:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: 'windows-latest'

steps:
  - task: UseDotNet@2
    displayName: 'Install .NET SDK'
    inputs:
      packageType: 'sdk'
      version: '10.0.x'

  - script: dotnet restore EnterpriseDocumentPlatform.sln
    displayName: 'Restore NuGet packages'

  - script: dotnet build EnterpriseDocumentPlatform.sln --configuration Debug --no-restore
    displayName: 'Build solution'

  - script: dotnet test EnterpriseDocumentPlatform.sln --configuration Debug --no-build
    displayName: 'Run tests'
""")
write_text(root / 'pipelines' / 'cd.yml', """trigger: none

pr: none

pool:
  vmImage: 'windows-latest'

stages:
  - stage: Deploy
    displayName: 'Deploy to Azure'
    jobs:
      - job: Deploy
        steps:
          - script: echo 'Deployment pipeline placeholder for Enterprise Document Platform.'
            displayName: 'Deploy placeholder'
""")
write_text(root / 'scripts' / 'README.md', '# Scripts\n\nLocal helper scripts for building, testing, and running the platform.\n')
write_text(root / 'scripts' / 'build.ps1', 'dotnet build ..\\EnterpriseDocumentPlatform.sln --configuration Debug\n')
write_text(root / 'scripts' / 'test.ps1', 'dotnet test ..\\EnterpriseDocumentPlatform.sln --configuration Debug\n')
write_text(root / 'scripts' / 'run-local.ps1', 'dotnet run --project ..\\src\\Gateway\\Edp.Gateway --configuration Debug\n')
write_text(root / 'docker' / 'README.md', '# Docker\n\nDockerfiles and local container configuration for the Enterprise Document Platform.\n')
write_text(root / 'docker' / 'Dockerfile.gateway', """FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ../src/Gateway/Edp.Gateway/Edp.Gateway.csproj ./
RUN dotnet restore "Edp.Gateway.csproj"
COPY ../src/Gateway/Edp.Gateway/. ./
RUN dotnet publish "Edp.Gateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Edp.Gateway.dll"]
""")
write_text(root / 'docker' / 'Dockerfile.template.api', """FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ../src/Services/Template/Edp.Template.Api/Edp.Template.Api.csproj ./
RUN dotnet restore "Edp.Template.Api.csproj"
COPY ../src/Services/Template/Edp.Template.Api/. ./
RUN dotnet publish "Edp.Template.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Edp.Template.Api.dll"]
""")
write_text(root / 'docker' / 'docker-compose.yml', """version: '3.9'
services:
  gateway:
    build:
      context: ..
      dockerfile: docker/Dockerfile.gateway
    ports:
      - '5000:80'
    depends_on:
      - template-api
  template-api:
    build:
      context: ..
      dockerfile: docker/Dockerfile.template.api
    ports:
      - '5001:80'
""")
write_text(root / 'tests' / 'README.md', '# Tests\n\nUnit and integration test projects for the Enterprise Document Platform.\n')

for test_proj in ['Edp.Gateway.Tests', 'Edp.Template.Tests']:
    test_dir = root / 'tests' / test_proj
    test_dir.mkdir(parents=True, exist_ok=True)
    csproj = test_dir / f'{test_proj}.csproj'
    write_text(csproj, f'''<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.4.0" />
    <PackageReference Include="xunit" Version="2.5.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
    <PackageReference Include="coverlet.collector" Version="3.2.0" />
  </ItemGroup>

</Project>
''')
    write_text(test_dir / 'UnitTest1.cs', f'''using Xunit;

namespace {test_proj};

public class UnitTest1
{{
    [Fact]
    public void Test1()
    {{
        Assert.True(true);
    }}
}}
''')

write_text(root / 'docs' / 'Phase-2-Foundation-Status.md', '# Phase 2 Foundation Implementation Status\n\nThis document tracks the completion of Phase 2 foundation scaffolding.\n\n- Service scaffolding created for Identity, Organization, Document, Workflow, Storage, Notification, Audit\n- Infrastructure, pipelines, scripts, docker, and tests scaffolding added\n- Gateway middleware will be enabled in bootstrap\n')

print('Scaffolding files created')
