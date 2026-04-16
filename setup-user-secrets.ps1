# Run this script once to populate .NET User Secrets for local development.
# Values are sourced from docker-compose.yml and appsettings.Development.json.

$project = "src/PayFlow.API"

Write-Host "Setting up User Secrets for PayFlow.API..."

dotnet user-secrets set "ConnectionStrings:DefaultConnection" `
  "Server=localhost,1433;Database=PayFlow;User Id=sa;Password=PayFlow@123!;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True" `
  --project $project

dotnet user-secrets set "JwtSettings:SecretKey" `
  'z%u>bAey>RdkRdlkHG$s(vyW+tU<f&^Jw' `
  --project $project

dotnet user-secrets set "RabbitMQ:UserName" `
  "payflow" `
  --project $project

dotnet user-secrets set "RabbitMQ:Password" `
  "payflow123" `
  --project $project

Write-Host "Done. Verify with: dotnet user-secrets list --project $project"
