# Secure Student API - JWT, Roles, and Policies

This repository is the secured continuation of my original [Student API 3-Tier with SQL Server](https://github.com/seifelabed21/Student-API-3-Tier-SQL-Server) project.

The original project was an educational REST API that used ASP.NET Core, SQL Server, ADO.NET, stored procedures, and a three-tier architecture but intentionally had no authentication or authorization. This project applies security layers to that API after completing the **Secure Your APIs: JWT, Roles & Policies in ASP.NET Core** course at [Programming Advices Academy](https://programmingadvices.com/courses/enrolled/2537475).

## Security layers implemented

The course roadmap is implemented from the foundation through application-level protections:

1. **HTTPS configuration**
   - HTTP redirection
   - Development HTTPS and Swagger setup
   - JWT bearer metadata validation

2. **CORS protection**
   - An allow-list replaces an unrestricted browser policy
   - Development origins are limited to the API, Swagger host, and local frontend port

3. **Credential registration and password security**
   - Public `Register` and `Login` endpoints
   - BCrypt password hashing with a per-password salt and work factor
   - No plaintext password storage

4. **JWT authentication**
   - Signed access tokens
   - Issuer, audience, signature, and lifetime validation
   - Short-lived access tokens
   - JWT claims for identity, email, role, and optional student ownership

5. **Role-based authorization**
   - `Admin` and `Student` roles
   - Read, write, and administrative boundaries
   - `401 Unauthorized` for missing or invalid authentication
   - `403 Forbidden` for authenticated but insufficient roles

6. **Ownership and policy-based authorization**
   - A reusable `StudentOwnerOrAdmin` requirement and handler
   - Students can access their own resource ID
   - Administrators can access every student resource

7. **Refresh-token sessions**
   - Short-lived access tokens
   - Longer-lived refresh tokens
   - Refresh-token rotation
   - Logout revocation

8. **Rate limiting**
   - Login and refresh endpoints are limited per remote IP
   - Excess requests receive `429 Too Many Requests`
   - The controller is not run after the middleware blocks the request

9. **Security logging and auditing**
   - Successful and failed logins are logged
   - Account registration and logout are audited
   - Administrator student mutations are audited
   - A protected admin-only audit endpoint exposes recent in-memory entries

## Technologies

- C# and .NET 8
- ASP.NET Core Web API
- SQL Server and SQL Server Management Studio
- ADO.NET and stored procedures
- JWT Bearer authentication
- BCrypt password hashing
- ASP.NET Core authorization policies and resource handlers
- Built-in fixed-window rate limiting
- Swagger/OpenAPI with Bearer authorization
- CORS middleware
- C# console `HttpClient`

## Architecture

```text
Client / Swagger
       |
       v
HTTPS + CORS + Rate Limiter
       |
       v
JWT Authentication Middleware
       |
       v
Role and Policy Authorization
       |
       v
Student API Controller
       |
       v
Business and Data Access Layers
       |
       v
SQL Server / StudentsDB
```

Security services remain in the presentation/API project so authentication and authorization stay separate from database student operations.

## Repository structure

```text
Secure-Student-API-JWT-Roles-Policies/
├── 01 StudentAPIServer with DB/
│   ├── StudentApi.sln
│   ├── MyFirstRestAPI_Porject/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── AuditController.cs
│   │   │   └── StudentAPIController.cs
│   │   ├── Security/
│   │   │   ├── AuthModels.cs
│   │   │   ├── AuthorizationHandlers.cs
│   │   │   ├── AuditService.cs
│   │   │   ├── TokenService.cs
│   │   │   └── UserStore.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── StudentAPIBusinessLayer/
│   └── StudentDataAccessLayer/
├── 02 StudentApiClient with DB/
├── Database/
│   └── StudentsDB.bak
└── README.md
```

## Authorization boundaries

| Operation | Anonymous | Student | Admin |
| --- | --- | --- | --- |
| Register | Allowed | Allowed | Allowed |
| Login | Allowed | Allowed | Allowed |
| Refresh | Allowed | Allowed | Allowed |
| Read all students | Denied | Allowed | Allowed |
| Read passed students | Denied | Allowed | Allowed |
| Read average grade | Denied | Allowed | Allowed |
| Read one student | Denied | Own ID only | Any ID |
| Add student | Denied | Denied (`403`) | Allowed |
| Update student | Denied | Denied (`403`) | Allowed |
| Delete student | Denied | Denied (`403`) | Allowed |
| Read audit log | Denied | Denied (`403`) | Allowed |

`401` means the caller did not present a valid access token. `403` means authentication succeeded but the caller lacks the required role or ownership rule.

## Demo identities

These identities are seeded for local course testing. Replace them before a real deployment.

| Role | Email | Password | Ownership |
| --- | --- | --- | --- |
| Admin | `admin@studentapi.local` | `Admin123!` | All student resources |
| Student | `alia.maher@student.com` | `Student123!` | Student ID `1` |

The user and refresh-token stores are in-memory for this learning implementation. A production version should persist users, password hashes, refresh-token hashes, and audit entries in a database and revoke sessions securely.

## Prerequisites

- .NET 8 SDK
- SQL Server or SQL Server Express
- SQL Server Management Studio
- Visual Studio 2022 or VS Code
- The database backup from `Database\StudentsDB.bak`

Restore the backup as `StudentsDB` using the instructions in the [original project README](https://github.com/seifelabed21/Student-API-3-Tier-SQL-Server).

## Database setup

Restore `Database\StudentsDB.bak` to a database named `StudentsDB`.

Update the SQL connection string in:

`01 StudentAPIServer with DB\StudentDataAccessLayer\StudentData.cs`

The security demo keeps its demo users in `UserStore.cs` and does not require an additional Users table.

## JWT configuration

The development configuration is in `appsettings.json`:

```json
"Jwt": {
  "Issuer": "StudentApi",
  "Audience": "StudentApiClients",
  "Key": "development-only-key-change-before-production-32chars",
  "AccessTokenMinutes": 15
}
```

Before deployment:

- Replace the key with a cryptographically random secret.
- Store it in user secrets or environment variables.
- Keep the issuer and audience values consistent in the token service and validator.
- Never commit a real signing key.

## Run locally

From the API project directory:

```powershell
cd "01 StudentAPIServer with DB\MyFirstRestAPI_Porject"
dotnet restore
dotnet build
dotnet run --launch-profile https
```

Open:

```text
https://localhost:7217/swagger
```

If the development certificate is untrusted, run:

```powershell
dotnet dev-certs https --trust
```

## Use Swagger

1. Open Swagger.
2. Call `POST /api/Auth/Login`.
3. Log in with an admin or student demo identity.
4. Copy `accessToken` from the JSON response.
5. Select **Authorize**.
6. Enter `Bearer <access-token>`.
7. Execute protected endpoints.

Swagger accepts the bearer scheme directly. Do not include literal quotes around the token.

## Test with PowerShell

Login as admin:

```powershell
$baseUrl = "https://localhost:7217"

$loginBody = @{
    email = "admin@studentapi.local"
    password = "Admin123!"
} | ConvertTo-Json

$auth = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/Auth/Login" `
    -ContentType "application/json" `
    -Body $loginBody `
    -SkipCertificateCheck

$headers = @{
    Authorization = "Bearer $($auth.accessToken)"
}

Invoke-RestMethod `
    -Uri "$baseUrl/api/Students/All" `
    -Headers $headers `
    -SkipCertificateCheck
```

Test ownership as the student:

```powershell
$studentLogin = @{
    email = "alia.maher@student.com"
    password = "Student123!"
} | ConvertTo-Json

$studentAuth = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/Auth/Login" `
    -ContentType "application/json" `
    -Body $studentLogin `
    -SkipCertificateCheck

$studentHeaders = @{
    Authorization = "Bearer $($studentAuth.accessToken)"
}

# Allowed for this student: the JWT student_id claim is 1
Invoke-RestMethod `
    -Uri "$baseUrl/api/Students/1" `
    -Headers $studentHeaders `
    -SkipCertificateCheck

# Forbidden for this student
Invoke-RestMethod `
    -Uri "$baseUrl/api/Students/2" `
    -Headers $studentHeaders `
    -SkipCertificateCheck
```

Test the login rate limit with six requests:

```powershell
for ($i = 1; $i -le 6; $i++) {
    try {
        $result = Invoke-WebRequest `
            -Method Post `
            -Uri "$baseUrl/api/Auth/Login" `
            -ContentType "application/json" `
            -Body '{"email":"not-found@example.com","password":"wrongPassword"}' `
            -SkipCertificateCheck
        "Attempt $i`: $($result.StatusCode)"
    } catch {
        "Attempt $i`: $($_.Exception.Response.StatusCode.value__)"
    }
}
```

The sixth request should be blocked with `429 Too Many Requests`. Do not reveal permit counts or internal partition names to normal API clients.

## Refresh and logout

Refresh the access token:

```powershell
$refreshBody = @{
    refreshToken = $auth.refreshToken
} | ConvertTo-Json

Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/Auth/Refresh" `
    -ContentType "application/json" `
    -Body $refreshBody `
    -SkipCertificateCheck
```

Refresh tokens rotate: after one successful rotation, the old refresh token is revoked. Call `POST /api/Auth/Logout` with the current refresh token and a valid access token to revoke the session.

## Console client

The console client logs in as the demo administrator, attaches the access token, and demonstrates the existing CRUD operations.

```powershell
cd "02 StudentApiClient with DB\StudentApiClient"
dotnet restore
dotnet run
```

Review the client’s sample student mutations before running it against a database you want to preserve.

## Deployment notes

1. Restore `StudentsDB` on the target SQL Server.
2. Configure the SQL and JWT secrets using environment variables or a secret store.
3. Replace demo in-memory users and add persistent password/user storage.
4. Persist and hash refresh tokens rather than storing raw demo tokens.
5. Add persistent audit storage and centralized secure logging.
6. Restrict CORS origins to the exact deployed frontend origins.
7. Enable HTTPS at the reverse proxy or host and configure trusted certificates.
8. Publish with `dotnet publish -c Release`.
9. Keep Swagger disabled or restricted outside development.

## Security notes and limitations

- This is a course learning implementation, not a complete production identity platform.
- Refresh sessions and audit entries are held in memory and are lost when the process restarts.
- Demo users are defined in code to match the course exercise.
- A production system should use persistent users and refresh-token hashes.
- Password registration can create Student accounts but is not linked to a student-table ID; ownership therefore remains null until mapped.
- HTTPS certificates and CORS are environment-dependent.
- JWT signing keys must be kept secret.
- `POST`, `PUT`, and `DELETE` require the Admin role.

## Original project

This project secures the earlier, intentionally unsecured [Student API 3-Tier with SQL Server](https://github.com/seifelabed21/Student-API-3-Tier-SQL-Server). It preserves the original student CRUD API and three-tier database architecture while adding the authentication and authorization layers taught by the second Programming Advices course.
