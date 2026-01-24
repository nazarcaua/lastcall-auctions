#Local Server Connection

Run in terminal with your specific local server name.

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_LOCAL_SERVER_NAME;Database=LastCallMotorAuctions;Trusted_Connection=true;TrustServerCertificate=true;"

#Set JWT Key

Run in terminal with the secret key from Chase

dotnet user-secrets set "JWT:Key" "your-super-secret-jwt-key-here-make-it-at-least-32-characters-long"

#Set VINAudi API Key

Run in terminal with our apikey

dotnet user-secrets set "VINAudit:ApiKey" "your-vinaudit-api-key-here"