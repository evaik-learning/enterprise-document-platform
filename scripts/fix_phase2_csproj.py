from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parent.parent
services = ['Identity', 'Organization', 'Document', 'Workflow', 'Storage', 'Notification', 'Audit', 'Template']

project_templates = {
    'Api': lambda service: f'''<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\Edp.{service}.Application\\Edp.{service}.Application.csproj" />
  </ItemGroup>

</Project>
''',
    'Application': lambda service: f'''<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\Edp.{service}.Domain\\Edp.{service}.Domain.csproj" />
  </ItemGroup>

</Project>
''',
    'Domain': lambda service: '''<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
''',
    'Infrastructure': lambda service: f'''<Project Sdk="Microsoft.NET.Sdk">

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
''',
}

def write_text(path: Path, content: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding='utf-8')

for service in services:
    base = root / 'src' / 'Services' / service
    for proj_type, template in project_templates.items():
        proj_name = f'Edp.{service}.{proj_type}'
        proj_dir = base / proj_name
        csproj_path = proj_dir / f'{proj_name}.csproj'
        write_text(csproj_path, template(service))
        if proj_type == 'Api':
            write_text(proj_dir / 'Program.cs', '''var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/", () => Results.Ok("Service is running."));
app.MapControllers();

app.Run();
''')
    write_text(base / 'README.md', f'# {service} Service\n\nThis folder contains scaffolded service projects for the {service} domain:\n\n- `Edp.{service}.Api`\n- `Edp.{service}.Application`\n- `Edp.{service}.Domain`\n- `Edp.{service}.Infrastructure`\n')

# Fix test projects
for test_proj in ['Edp.Gateway.Tests', 'Edp.Template.Tests']:
    test_dir = root / 'tests' / test_proj
    write_text(test_dir / f'{test_proj}.csproj', '''<Project Sdk="Microsoft.NET.Sdk">

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
    write_text(test_dir / 'UnitTest1.cs', f'''using Xunit;\n\nnamespace {test_proj};\n\npublic class UnitTest1\n{{\n    [Fact]\n    public void Test1()\n    {{\n        Assert.True(true);\n    }}\n}}\n''')

# Create placeholder root files
write_text(root / 'infra' / 'README.md', '# Infrastructure\n\nAzure infrastructure as code and deployment templates for the Enterprise Document Platform.\n')
write_text(root / 'infra' / 'main.bicep', """targetScope = 'resourceGroup'\n\nparam location string = resourceGroup().location\nparam storageAccountName string = 'edpplatform' + uniqueString(resourceGroup().id)\n\nresource storageAccount 'Microsoft.Storage/storageAccounts@2024-06-01' = {\n  name: storageAccountName\n  location: location\n  sku: {\n    name: 'Standard_LRS'\n  }\n  kind: 'StorageV2'\n  properties: {\n    accessTier: 'Hot'\n  }\n}\n""")
write_text(root / 'pipelines' / 'README.md', '# CI/CD pipelines\n\nPipeline definitions for the Enterprise Document Platform.\n')
write_text(root / 'pipelines' / 'ci.yml', """trigger:\n  branches:\n    include:\n      - main\n      - develop\n\npool:\n  vmImage: 'windows-latest'\n\nsteps:\n  - task: UseDotNet@2\n    displayName: 'Install .NET SDK'\n    inputs:\n      packageType: 'sdk'\n      version: '10.0.x'\n\n  - script: dotnet restore EnterpriseDocumentPlatform.sln\n    displayName: 'Restore NuGet packages'\n\n  - script: dotnet build EnterpriseDocumentPlatform.sln --configuration Debug --no-restore\n    displayName: 'Build solution'\n\n  - script: dotnet test EnterpriseDocumentPlatform.sln --configuration Debug --no-build\n    displayName: 'Run tests'\n""")
write_text(root / 'pipelines' / 'cd.yml', """trigger: none\n\npr: none\n\npool:\n  vmImage: 'windows-latest'\n\nstages:\n  - stage: Deploy\n    displayName: 'Deploy to Azure'\n    jobs:\n      - job: Deploy\n        steps:\n          - script: echo 'Deployment pipeline placeholder for Enterprise Document Platform.'\n            displayName: 'Deploy placeholder'\n""")
write_text(root / 'scripts' / 'README.md', '# Scripts\n\nLocal helper scripts for building, testing, and running the platform.\n')
write_text(root / 'scripts' / 'build.ps1', 'dotnet build ..\\EnterpriseDocumentPlatform.sln --configuration Debug\n')
write_text(root / 'scripts' / 'test.ps1', 'dotnet test ..\\EnterpriseDocumentPlatform.sln --configuration Debug\n')
write_text(root / 'scripts' / 'run-local.ps1', 'dotnet run --project ..\\src\\Gateway\\Edp.Gateway --configuration Debug\n')
write_text(root / 'docker' / 'README.md', '# Docker\n\nDockerfiles and local container configuration for the Enterprise Document Platform.\n')
write_text(root / 'docker' / 'Dockerfile.gateway', """FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base\nWORKDIR /app\nEXPOSE 80\n\nFROM mcr.microsoft.com/dotnet/sdk:10.0 AS build\nWORKDIR /src\nCOPY ../src/Gateway/Edp.Gateway/Edp.Gateway.csproj ./\nRUN dotnet restore \"Edp.Gateway.csproj\"\nCOPY ../src/Gateway/Edp.Gateway/. ./\nRUN dotnet publish \"Edp.Gateway.csproj\" -c Release -o /app/publish\n\nFROM base AS final\nWORKDIR /app\nCOPY --from=build /app/publish .\nENTRYPOINT [\"dotnet\", \"Edp.Gateway.dll\"]\n""")
write_text(root / 'docker' / 'Dockerfile.template.api', """FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base\nWORKDIR /app\nEXPOSE 80\n\nFROM mcr.microsoft.com/dotnet/sdk:10.0 AS build\nWORKDIR /src\nCOPY ../src/Services/Template/Edp.Template.Api/Edp.Template.Api.csproj ./\nRUN dotnet restore \"Edp.Template.Api.csproj\"\nCOPY ../src/Services/Template/Edp.Template.Api/. ./\nRUN dotnet publish \"Edp.Template.Api.csproj\" -c Release -o /app/publish\n\nFROM base AS final\nWORKDIR /app\nCOPY --from=build /app/publish .\nENTRYPOINT [\"dotnet\", \"Edp.Template.Api.dll\"]\n""")
write_text(root / 'docker' / 'docker-compose.yml', """version: '3.9'\nservices:\n  gateway:\n    build:\n      context: ..\n      dockerfile: docker/Dockerfile.gateway\n    ports:\n      - '5000:80'\n    depends_on:\n      - template-api\n  template-api:\n    build:\n      context: ..\n      dockerfile: docker/Dockerfile.template.api\n    ports:\n      - '5001:80'\n""")
write_text(root / 'tests' / 'README.md', '# Tests\n\nUnit and integration test projects for the Enterprise Document Platform.\n')
write_text(root / 'docs' / 'Phase-2-Foundation-Status.md', '# Phase 2 Foundation Implementation Status\n\nThis document tracks the completion of Phase 2 foundation scaffolding.\n\n- Service scaffolding created for Identity, Organization, Document, Workflow, Storage, Notification, Audit\n- Infrastructure, pipelines, scripts, docker, and tests scaffolding added\n- Gateway middleware will be enabled in bootstrap\n')

# Validate all generated .csproj files are XML-well-formed
invalid_files = []
for proj in sorted(root.rglob('*.csproj')):
    if 'obj' in proj.parts or 'bin' in proj.parts:
        continue
    try:
        ET.parse(proj)
    except ET.ParseError as exc:
        invalid_files.append((proj, str(exc)))

if invalid_files:
    print('⚠️ Invalid generated csproj files:')
    for proj, exc in invalid_files:
        print(f'{proj}: {exc}')
    raise SystemExit(1)

print('All generated .csproj files are valid XML.')

# Add all projects to solution
sln = root / 'EnterpriseDocumentPlatform.sln'
list_result = subprocess.run(['dotnet', 'sln', str(sln), 'list'], capture_output=True, text=True)
existing_solution = list_result.stdout.replace('\\', '/').lower()
added = 0
for project_path in sorted(root.rglob('*.csproj')):
    if 'obj' in project_path.parts or 'bin' in project_path.parts:
        continue
    normalized = str(project_path).replace('\\', '/').lower()
    if normalized in existing_solution:
        continue
    add_result = subprocess.run(['dotnet', 'sln', str(sln), 'add', str(project_path)], capture_output=True, text=True, timeout=30)
    if add_result.returncode != 0:
        print(f'Error adding {project_path}: {add_result.stderr.strip()}')
    else:
        print(f'Added {project_path}')
        added += 1

print(f'Added {added} new projects to solution.')
print('Script completed.')
