#Local Server Connection

Run in terminal with your specific local server name.

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_LOCAL_SERVER_NAME;Database=LastCallMotorAuctions;Trusted_Connection=true;TrustServerCertificate=true;"