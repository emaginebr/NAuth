# NAuth.API - Authentication Framework

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=landim32_NAuth.API&metric=alert_status)](https://sonarcloud.io/project/overview?id=landim32_NAuth.API)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=landim32_NAuth.API&metric=coverage)](https://sonarcloud.io/project/overview?id=landim32_NAuth.API)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=landim32_NAuth.API&metric=code_smells)](https://sonarcloud.io/project/overview?id=landim32_NAuth.API)
![License](https://img.shields.io/badge/License-MIT-green)

## Overview

**NAuth.API** is the central backend of the NAuth ecosystem — a complete, modular authentication framework designed for fast and secure user management in modern web applications. Built using **.NET 8** and **PostgreSQL**, it provides a robust REST API for user registration, login, password recovery, role management, and profile updates.

This is the **main project** of the NAuth ecosystem. All other NAuth packages ([NAuth.DTO](https://github.com/landim32/NAuth.DTO), [NAuth.ACL](https://github.com/landim32/NAuth.ACL), [NAuth.React](https://github.com/landim32/NAuth.React), [NAuth.App](https://github.com/landim32/NAuth.APP)) integrate with or consume this API.

The project follows a clean architecture approach with separated layers for API, Application, Domain, Infrastructure, and comprehensive test coverage.

---

## 🚀 Features

- 🔐 **User Registration** - Complete registration flow with email confirmation
- 🔑 **JWT Authentication** - Secure token-based authentication
- 🔄 **Password Recovery** - Secure password reset via email with token validation
- ✏️ **Profile Management** - User profile update and password change
- 👥 **Role-Based Access Control** - User roles and permissions management
- 📧 **Email Integration** - Email templates and SMTP support
- 🗄️ **PostgreSQL Database** - Schema and migrations included
- 📦 **Modular Architecture** - Reusable across multiple projects
- 🌐 **REST API** - Complete RESTful API with Swagger documentation
- 🐳 **Docker Support** - Production-ready Docker configurations
- ✅ **Health Checks** - Built-in health check endpoints
- 🔒 **Security** - Non-root containers, encrypted passwords, token validation

---

## 🛠️ Technologies Used

### Core Framework
- **.NET 8.0** - Modern, cross-platform framework for building web APIs
- **ASP.NET Core** - Web framework for building HTTP services
- **Entity Framework Core 9.0.8** - ORM with proxy support

### Database
- **PostgreSQL** - Robust relational database
- **Npgsql.EntityFrameworkCore.PostgreSQL 9.0.8** - PostgreSQL provider for EF Core

### Security
- **JWT (JSON Web Tokens)** - Secure authentication mechanism
- **PBKDF2/BCrypt** - Strong password hashing algorithms
- **Token-based Email Verification** - Secure email confirmation and password reset

### Frontend Integration
- **React** - Modern UI library
- **Bootstrap** - Responsive UI components
- **React Hooks** - Custom authentication hooks

### Additional Libraries
- **Swashbuckle.AspNetCore 9.0.4** - Swagger/OpenAPI documentation
- **MailerSend Integration** - Email delivery service

### Testing
- **xUnit** - Unit testing framework
- Comprehensive test coverage across all layers

### DevOps
- **Docker** - Containerization with multi-stage builds
- **Docker Compose** - Multi-container orchestration
- **GitHub Actions** - CI/CD pipeline

---

## 📁 Project Structure

```
NAuth.API/
├── NAuth.API/                # Web API layer with controllers
│   ├── Controllers/          # API endpoints (User, Role, etc.)
│   ├── appsettings.*.json   # Configuration files
│   └── Startup.cs           # Application configuration
├── NAuth.Application/        # Application layer with DI setup
│   └── Initializer.cs       # Dependency injection configuration
├── NAuth.Domain/            # Domain layer with business logic
│   ├── Models/              # Domain models
│   ├── Services/            # Business logic services
│   ├── Factory/             # Domain factories
│   └── LocalAuthHandler.cs  # Local authentication handler
├── NAuth.Infra/             # Infrastructure layer
│   ├── Context/             # Database context
│   └── Repository/          # Data access repositories
├── NAuth.Infra.Interfaces/  # Repository interfaces
├── NAuth.Test/              # Comprehensive test suite
│   ├── Domain/              # Domain tests
│   ├── Infra/               # Infrastructure tests
│   └── ACL/                 # ACL tests
├── Dockerfile               # Production-ready Docker image
├── docker-compose.yml       # Docker Compose configuration
├── postgres.Dockerfile      # PostgreSQL container
└── README.md                # This file
```

### Ecosystem

NAuth is a modular ecosystem. This repository (**NAuth.API**) is the central backend — the other packages integrate with it:

| Project | Type | Package | Description |
|---------|------|---------|-------------|
| **[NAuth.DTO](https://github.com/landim32/NAuth.DTO)** | NuGet | [![NuGet](https://img.shields.io/nuget/v/NAuth.DTO.svg)](https://www.nuget.org/packages/NAuth.DTO/) | Shared Data Transfer Objects used across all .NET projects |
| **[NAuth.ACL](https://github.com/landim32/NAuth.ACL)** | NuGet | [![NuGet](https://img.shields.io/nuget/v/NAuth.ACL.svg)](https://www.nuget.org/packages/NAuth.ACL/) | HTTP client library for consuming the NAuth API from .NET apps |
| **[NAuth.React](https://github.com/landim32/NAuth.React)** | NPM | [![npm](https://img.shields.io/npm/v/nauth-react.svg)](https://www.npmjs.com/package/nauth-react) | React component library (login, register, user management) |
| **[NAuth.App](https://github.com/landim32/NAuth.APP)** | App | — | Frontend web application built with NAuth.React |

#### Dependency graph

```
NAuth.App (React frontend)
  └─ nauth-react (NPM)
       └─ NAuth.API (HTTP) ← you are here
            ├─ NAuth.ACL (NuGet)
            │   └─ NAuth.DTO (NuGet)
            └─ NAuth.DTO (NuGet)
```

---

## ⚙️ Environment Configuration

Before running the application, you need to configure the environment variables:

### 1. Copy the environment template

```bash
cp .env.example .env
```

### 2. Edit the `.env` file

```bash
# PostgreSQL Database Configuration
POSTGRES_DB=nauth_db
POSTGRES_USER=nauth
POSTGRES_PASSWORD=your_secure_password_here_change_this
POSTGRES_PORT=5432

# Connection String
# Use 'nauth-postgres' when running with Docker Compose
CONNECTION_STRING=Host=nauth-postgres;Port=5432;Database=nauth_db;Username=nauth;Password=your_secure_password_here_change_this

# JWT Configuration (minimum 64 characters)
JWT_SECRET=your_jwt_secret_with_at_least_64_characters_for_maximum_security_change_this_value

# NAuth API Configuration
API_HTTP_PORT=5004
API_HTTPS_PORT=5005
CERTIFICATE_PASSWORD=your_certificate_password_here
```

⚠️ **IMPORTANT**: 
- Never commit the `.env` file with real credentials
- Only the `.env.example` should be version controlled
- Change all default passwords and secrets before deployment
- JWT_SECRET must be at least 64 characters for security

---

## 🐳 Docker Setup

### Quick Start with Docker Compose

#### 1. Create Docker Network

```bash
docker network create emagine-network
```

Or remove the `external: true` configuration from `docker-compose.yml` if you don't need an external network.

#### 2. Build and Start Services

```bash
docker-compose up -d --build
```

This command will:
- Build the Docker images for both API and PostgreSQL
- Create and start the containers
- Set up networking between containers
- Apply health checks

#### 3. Verify Deployment

Check container status:
```bash
docker-compose ps
```

View logs:
```bash
# All services
docker-compose logs -f

# API only
docker-compose logs -f nauth-api

# PostgreSQL only
docker-compose logs -f postgres
```

### Accessing the Application

After deployment, the services will be available at:

- **Frontend App**: http://localhost:5006
- **API HTTP**: http://localhost:5004
- **API HTTPS**: https://localhost:5005
- **Swagger UI**: http://localhost:5004/swagger
- **Health Check**: http://localhost:5004/ (returns JSON with application status)
- **PostgreSQL**: localhost:5432 (accessible from host machine)

### Docker Compose Commands

| Action | Command |
|--------|---------|
| Start services | `docker-compose up -d` |
| Start with rebuild | `docker-compose up -d --build` |
| Stop services | `docker-compose stop` |
| Restart services | `docker-compose restart` |
| View status | `docker-compose ps` |
| View all logs | `docker-compose logs -f` |
| View API logs | `docker-compose logs -f nauth-api` |
| View DB logs | `docker-compose logs -f postgres` |
| Remove containers | `docker-compose down` |
| Remove containers and volumes (⚠️ deletes data) | `docker-compose down -v` |

### Production Deployment

For production environments, use the production configuration:

```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

The production configuration includes:
- Resource limits and reservations
- Rolling update strategy
- Enhanced security settings
- Optimized logging
- Health check configurations

---

## 🔧 Manual Setup (Without Docker)

### Prerequisites
- .NET 8.0 SDK
- PostgreSQL 12+
- Node.js 16+ (for frontend)

### Backend Setup

#### 1. Configure Database

Create a PostgreSQL database and update the connection string in `NAuth.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "NAuthContext": "Host=localhost;Port=5432;Database=nauth_db;Username=nauth;Password=your_password"
  }
}
```

#### 2. Configure JWT Settings

Update JWT configuration in `NAuth.API/appsettings.Development.json`:

```json
{
  "NAuth": {
    "JwtSecret": "your_jwt_secret_at_least_64_characters_long"
  }
}
```

#### 3. Run Database Migrations

```bash
cd NAuth.Infra
dotnet ef database update --startup-project ../NAuth.API
```

#### 4. Start the API

```bash
cd NAuth.API
dotnet restore
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5004
- HTTPS: https://localhost:5005
- Swagger: http://localhost:5004/swagger

### Frontend Setup

#### 1. Install Dependencies

```bash
cd Frontend/nauth-app
npm install
```

#### 2. Configure API URL

Update the API URL in your frontend configuration to point to your backend.

#### 3. Start the Frontend

```bash
npm start
```

### Updating nauth-core Hook

When making changes to the `Frontend/nauth-core` library:

```bash
cd Frontend/nauth-app
npm install --legacy-peer-deps ../nauth-core
```

---

## 📚 API Documentation

The NAuth API provides comprehensive endpoints for user authentication and management.

### Authentication Flow

```
1. Register → 2. Verify Email → 3. Login → 4. Access Protected Resources
```

### User Controller

Manages user registration, authentication, and profile operations.

#### Endpoints

**POST** `/User/register`

Register a new user account.

- **Request Body**: `UserInfo` object
  ```json
  {
    "name": "John Doe",
    "email": "john@example.com",
    "password": "SecurePassword123!",
    "cpf": "12345678900",
    "phone": "5511999999999"
  }
  ```
- **Returns**:
  - `200 OK` - User registered successfully (returns user ID)
  - `400 Bad Request` - Validation errors
  - `500 Internal Server Error` - Registration failed
- **Notes**: 
  - Sends verification email to user
  - Password is hashed before storage
  - CPF is validated

**POST** `/User/login`

Authenticate user and receive JWT token.

- **Request Body**: `LoginParam` object
  ```json
  {
    "email": "john@example.com",
    "password": "SecurePassword123!"
  }
  ```
- **Returns**:
  - `200 OK` - Returns JWT token and user info
    ```json
    {
      "token": "eyJhbGciOiJIUzI1NiIs...",
      "user": {
        "id": 1,
        "name": "John Doe",
        "email": "john@example.com",
        "roles": ["User"]
      },
      "expiresAt": "2024-01-16T10:00:00Z"
    }
    ```
  - `401 Unauthorized` - Invalid credentials
  - `500 Internal Server Error` - Login failed

**POST** `/User/verifyEmail`

Verify user email with token.

- **Request Body**: `EmailVerificationParam` object
  ```json
  {
    "userId": 1,
    "token": "verification-token-from-email"
  }
  ```
- **Returns**:
  - `200 OK` - Email verified successfully
  - `400 Bad Request` - Invalid or expired token
  - `500 Internal Server Error` - Verification failed

**POST** `/User/requestPasswordReset`

Request password reset token.

- **Request Body**:
  ```json
  {
    "email": "john@example.com"
  }
  ```
- **Returns**:
  - `200 OK` - Reset email sent
  - `404 Not Found` - User not found
  - `500 Internal Server Error` - Request failed
- **Notes**: Sends password reset email with token

**POST** `/User/resetPassword`

Reset password with token.

- **Request Body**: `PasswordResetParam` object
  ```json
  {
    "email": "john@example.com",
    "token": "reset-token-from-email",
    "newPassword": "NewSecurePassword123!"
  }
  ```
- **Returns**:
  - `200 OK` - Password reset successfully
  - `400 Bad Request` - Invalid or expired token
  - `500 Internal Server Error` - Reset failed

**PUT** `/User/{id}`

Update user profile.

- **Parameters**:
  - `id` (int, path): User ID
- **Request Body**: `UserInfo` object (partial update supported)
- **Returns**:
  - `200 OK` - User updated successfully
  - `401 Unauthorized` - Not authenticated
  - `403 Forbidden` - Not authorized to update this user
  - `404 Not Found` - User not found
  - `500 Internal Server Error` - Update failed
- **Authorization**: Requires JWT token

**GET** `/User/{id}`

Get user profile by ID.

- **Parameters**:
  - `id` (int, path): User ID
- **Returns**:
  - `200 OK` - Returns user profile
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - User not found
- **Authorization**: Requires JWT token

**GET** `/User/email/{email}`

Get user by email address.

- **Parameters**:
  - `email` (string, path): User email
- **Returns**:
  - `200 OK` - Returns user profile
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - User not found
- **Authorization**: Requires JWT token

**POST** `/User/changePassword`

Change user password.

- **Request Body**: `ChangePasswordParam` object
  ```json
  {
    "userId": 1,
    "currentPassword": "OldPassword123!",
    "newPassword": "NewSecurePassword123!"
  }
  ```
- **Returns**:
  - `200 OK` - Password changed successfully
  - `401 Unauthorized` - Not authenticated or invalid current password
  - `500 Internal Server Error` - Change failed
- **Authorization**: Requires JWT token

### Role Controller

Manages user roles and permissions.

#### Endpoints

**GET** `/Role/list`

Get all available roles.

- **Returns**:
  - `200 OK` - Array of role objects
    ```json
    [
      {
        "id": 1,
        "name": "Admin",
        "description": "Administrator role"
      },
      {
        "id": 2,
        "name": "User",
        "description": "Standard user role"
      }
    ]
    ```
  - `401 Unauthorized` - Not authenticated
- **Authorization**: Requires JWT token

**POST** `/Role/assignRole`

Assign a role to a user.

- **Request Body**: `UserRoleParam` object
  ```json
  {
    "userId": 1,
    "roleId": 2
  }
  ```
- **Returns**:
  - `200 OK` - Role assigned successfully
  - `401 Unauthorized` - Not authenticated
  - `403 Forbidden` - Not authorized
  - `404 Not Found` - User or role not found
  - `500 Internal Server Error` - Assignment failed
- **Authorization**: Requires JWT token with admin role

**DELETE** `/Role/removeRole/{userId}/{roleId}`

Remove a role from a user.

- **Parameters**:
  - `userId` (int, path): User ID
  - `roleId` (int, path): Role ID
- **Returns**:
  - `200 OK` - Role removed successfully
  - `401 Unauthorized` - Not authenticated
  - `403 Forbidden` - Not authorized
  - `404 Not Found` - User role assignment not found
  - `500 Internal Server Error` - Removal failed
- **Authorization**: Requires JWT token with admin role

### Health Check Endpoint

**GET** `/`

Application health check endpoint.

- **Returns**:
  - `200 OK` - Application is healthy
    ```json
    {
      "currentTime": "2024-01-15 10:30:00",
      "statusApplication": "Healthy"
    }
    ```
  - `503 Service Unavailable` - Application is unhealthy

---

## 🔒 Security Features

### Authentication
- **JWT Tokens** - Secure, stateless authentication
- **Token Expiration** - Configurable token lifetime
- **Refresh Tokens** - Support for token refresh (configurable)

### Password Security
- **Strong Hashing** - PBKDF2 or BCrypt algorithms
- **Salt** - Unique salt for each password
- **Password Validation** - Minimum complexity requirements
- **Password Reset** - Secure token-based flow with expiration

### Email Verification
- **Token-based Verification** - Secure email confirmation
- **Token Expiration** - Tokens expire after configured time
- **One-time Use** - Tokens are invalidated after use

### CORS Configuration
- **Configurable Origins** - Control allowed origins
- **Secure Headers** - Proper CORS headers
- **Credentials Support** - Optional credential support

### Docker Security
- **Non-root User** - Containers run as non-root user (`appuser`)
- **Security Capabilities** - Minimal capabilities assigned
- **Read-only Filesystem** - Where possible
- **Secret Management** - Environment-based secrets

### Database Security
- **Parameterized Queries** - Protection against SQL injection
- **Connection Encryption** - SSL/TLS support
- **User Isolation** - Separate database users for different operations

---

## 💾 Backup and Restore

### Database Backup

**Manual Backup:**
```bash
docker exec nauth-postgres pg_dump -U nauth nauth_db > backup_$(date +%Y%m%d_%H%M%S).sql
```

**Compressed Backup:**
```bash
docker exec nauth-postgres pg_dump -U nauth nauth_db | gzip > backup_$(date +%Y%m%d_%H%M%S).sql.gz
```

### Database Restore

**Manual Restore:**
```bash
docker exec -i nauth-postgres psql -U nauth -d nauth_db < backup_20240115_120000.sql
```

**Restore Compressed Backup:**
```bash
gunzip < backup_20240115_120000.sql.gz | docker exec -i nauth-postgres psql -U nauth -d nauth_db
```

### Automated Backup

For production, set up automated backups using cron (Linux/Mac):

```bash
# Add to crontab (crontab -e)
0 2 * * * cd /path/to/nauth && docker exec nauth-postgres pg_dump -U nauth nauth_db > backup_$(date +\%Y\%m\%d_\%H\%M\%S).sql
```

Or Windows Task Scheduler:

```powershell
# Create scheduled task
schtasks /create /tn "NAuth Backup" /tr "docker exec nauth-postgres pg_dump -U nauth nauth_db > C:\backups\nauth\backup_%date:~-4,4%%date:~-10,2%%date:~-7,2%.sql" /sc daily /st 02:00
```

---

## 🧪 Testing

The project includes comprehensive test coverage across all layers.

### Running Tests

**All Tests:**
```bash
dotnet test
```

**Specific Project:**
```bash
dotnet test NAuth.Test/NAuth.Test.csproj
```

**With Coverage:**
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Test Structure

```
NAuth.Test/
├── Domain/
│   ├── Models/          # Domain model tests
│   ├── Services/        # Business logic tests
│   └── LocalAuthHandlerTests.cs
├── Infra/
│   └── Repository/      # Data access tests
└── ACL/
    ├── UserClientTests.cs
    ├── RoleClientTests.cs
    └── RemoteAuthHandlerTests.cs
```

---

## 🔍 Troubleshooting

### Common Issues

#### API Not Starting

**Check logs:**
```bash
docker-compose logs nauth-api
```

**Common causes:**
- Database connection failed (check CONNECTION_STRING)
- Port already in use (change API_HTTP_PORT in .env)
- Missing environment variables (check .env file)

#### Database Connection Failed

**Verify PostgreSQL is running:**
```bash
docker-compose ps postgres
```

**Check PostgreSQL logs:**
```bash
docker-compose logs postgres
```

**Test connection:**
```bash
docker exec nauth-postgres pg_isready -U nauth -d nauth_db
```

**Common solutions:**
- Wait for PostgreSQL to fully start (check health status)
- Verify CONNECTION_STRING uses correct host (`nauth-postgres` for Docker)
- Check POSTGRES_PASSWORD matches in .env and CONNECTION_STRING

#### Health Check Failing

**Test health endpoint:**
```bash
curl http://localhost:5000/
```

**Check application logs:**
```bash
docker-compose logs nauth-api
```

#### Ports Already in Use

**Find process using port:**
```bash
# Linux/Mac
lsof -i :5000

# Windows
netstat -ano | findstr :5000
```

**Solution:**
- Kill the process using the port
- Or change port in .env file:
  ```bash
  API_HTTP_PORT=6000
  API_HTTPS_PORT=6001
  ```

#### HTTPS Certificate Issues

**Symptoms:**
- HTTPS not working
- Certificate errors in logs

**Solutions:**
1. Verify `NAuth.API/emagine.pfx` exists
2. Check CERTIFICATE_PASSWORD in .env
3. For development, use HTTP only (port 5000)

#### Docker Build Fails

**Clear Docker cache:**
```bash
docker-compose build --no-cache
```

**Check disk space:**
```bash
docker system df
```

**Clean up Docker:**
```bash
docker system prune -af
```

### Getting Help

1. **Check logs**: `docker-compose logs -f`
2. **Check container status**: `docker-compose ps`
3. **Open an issue**: [GitHub Issues](https://github.com/landim32/NAuth/issues)

---

## 📦 Integration

### Using NAuth in Your Application

#### Option 1: API Integration

Connect to NAuth API from any application:

```javascript
// Example: Login request
const response = await fetch('http://localhost:5000/User/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'password123'
  })
});

const data = await response.json();
const token = data.token;

// Use token in subsequent requests
const userResponse = await fetch('http://localhost:5000/User/1', {
  headers: { 'Authorization': `Bearer ${token}` }
});
```

#### Option 2: Frontend Components

Reuse NAuth React components:

```jsx
import { UserProvider, useUser } from 'nauth-core';

function App() {
  return (
    <UserProvider apiUrl="http://localhost:5000">
      <YourApp />
    </UserProvider>
  );
}

function LoginPage() {
  const { loginWithEmail, user, loading } = useUser();
  
  const handleLogin = async (email, password) => {
    const result = await loginWithEmail(email, password);
    if (result.sucesso) {
      // Login successful
    }
  };
  
  return (
    // Your login form
  );
}
```

#### Option 3: Module Integration

Import NAuth services directly (coming soon via NuGet):

```csharp
// Add NAuth to your project
services.AddNAuth(Configuration);

// Use in your controllers
public class MyController : Controller
{
    private readonly IUserService _userService;
    
    public MyController(IUserService userService)
    {
        _userService = userService;
    }
}
```

---

## 🚀 Deployment

### Development Environment

Using Docker Compose:
```bash
docker-compose up -d --build
```

Or manually without Docker:
```bash
dotnet run --project NAuth.API
```

### Staging Environment

```bash
# Use staging configuration
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d --build
```

### Production Environment

```bash
# Use production configuration
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

### Cloud Deployment

#### Azure Container Instances

1. Build and push image:
   ```bash
   docker build -t your-registry.azurecr.io/nauth-api:latest .
   docker push your-registry.azurecr.io/nauth-api:latest
   ```

2. Deploy to Azure:
   ```bash
   az container create --resource-group myResourceGroup \
     --name nauth-api \
     --image your-registry.azurecr.io/nauth-api:latest \
     --dns-name-label nauth-api \
     --ports 80 443
   ```

#### AWS ECS

1. Create task definition with NAuth and PostgreSQL containers
2. Create ECS service
3. Configure Application Load Balancer
4. Set up RDS for PostgreSQL

#### Kubernetes

```yaml
# Example deployment
apiVersion: apps/v1
kind: Deployment
metadata:
  name: nauth-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: nauth-api
  template:
    metadata:
      labels:
        app: nauth-api
    spec:
      containers:
      - name: nauth-api
        image: your-registry/nauth-api:latest
        ports:
        - containerPort: 80
        env:
        - name: ConnectionStrings__NAuthContext
          valueFrom:
            secretKeyRef:
              name: nauth-secrets
              key: connection-string
```

---

## 🔄 CI/CD

### GitHub Actions

The project includes a GitHub Actions workflow for automated Docker builds.

**Workflow triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main`
- Tags matching `v*` pattern

**Workflow steps:**
1. Build Docker images for API and PostgreSQL
2. Push images to GitHub Container Registry (ghcr.io)
3. Create image tags based on:
   - Branch name
   - Semver version
   - Commit SHA

**Using the images:**
```bash
docker pull ghcr.io/landim32/nauth/nauth-api:main
docker pull ghcr.io/landim32/nauth/nauth-postgres:main
```

---

## 🧩 Roadmap

### Planned Features

- [ ] **Two-Factor Authentication (2FA)** - TOTP and SMS support
- [ ] **OAuth2 Integration** - Social login providers
  - [ ] Google
  - [ ] GitHub
  - [ ] Facebook
  - [ ] Microsoft
- [ ] **Admin Dashboard** - Web interface for user management
- [ ] **Advanced RBAC** - Fine-grained permissions
- [ ] **Audit Logging** - Track user actions
- [ ] **Rate Limiting** - API request throttling
- [ ] **Session Management** - Multiple device support
- [ ] **Account Lockout** - Brute force protection
- [ ] **Password Policies** - Configurable complexity rules
- [ ] **User Invitations** - Admin-initiated registration
- [ ] **NuGet Package** - Easy integration via NuGet
- [ ] **Localization** - Multi-language support

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Make your changes
4. Run tests (`dotnet test`)
5. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
6. Push to the branch (`git push origin feature/AmazingFeature`)
7. Open a Pull Request

### Coding Standards

- Follow C# coding conventions
- Write unit tests for new features
- Update documentation as needed
- Keep commits atomic and well-described

---

## 👨‍💻 Author

Developed by **[Rodrigo Landim Carneiro](https://github.com/landim32)**

---

## 📄 License

This project is licensed under the **MIT License** - see the LICENSE file for details.

---

## 🙏 Acknowledgments

- Built with [.NET 8](https://dotnet.microsoft.com/)
- Database powered by [PostgreSQL](https://www.postgresql.org/)
- Frontend with [React](https://reactjs.org/)
- Containerization with [Docker](https://www.docker.com/)
- Documentation inspired by best practices in open source

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/landim32/NAuth/issues)
- **Discussions**: [GitHub Discussions](https://github.com/landim32/NAuth/discussions)

---

**⭐ If you find this project useful, please consider giving it a star!**
