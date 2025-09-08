# OneTime Share

A secure ASP.NET Core MVC application for one-time file sharing with automatic cleanup.

## Features

- **Secure Authentication**: Google OAuth 2.0 / OpenID Connect integration
- **One-Time Downloads**: Files are automatically deleted after first download
- **Automatic Cleanup**: Files expire after 30 days (configurable)
- **Secure Token Generation**: Cryptographically strong tokens with constant-time verification
- **Concurrency Safe**: Multiple simultaneous download attempts are handled safely
- **Docker Support**: Ready for containerized deployment
- **Health Checks**: Built-in health monitoring endpoints

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Google OAuth 2.0 credentials (Client ID and Secret)

### Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd file_exchanger
   ```

2. **Configure Google OAuth**
   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Create a new project or select existing one
   - Enable Google+ API
   - Create OAuth 2.0 credentials
   - Add authorized redirect URI: `https://yourdomain.com/signin-google`

3. **Set Environment Variables**
   ```bash
   cp .env.example .env
   # Edit .env with your Google OAuth credentials
   ```

4. **Run the application**
   ```bash
   cd src/OneTimeShare.Web
   dotnet run
   ```

5. **Access the application**
   - Open browser to `https://localhost:5001`
   - Sign in with Google
   - Upload files and share one-time links

## Environment Variables

### Required
- `GOOGLE_CLIENT_ID`: Google OAuth 2.0 Client ID
- `GOOGLE_CLIENT_SECRET`: Google OAuth 2.0 Client Secret

### Optional
- `APP_BASE_URL`: Base URL for the application (default: auto-detected)
- `STORAGE_ROOT`: Directory for file storage (default: `./storage`)
- `SQLITE_CONN_STRING`: SQLite connection string (default: `Data Source=./App_Data/app.db`)
- `MAX_UPLOAD_BYTES`: Maximum file upload size in bytes (default: `104857600` = 100MB)
- `FILE_RETENTION_DAYS`: Days before files expire (default: `30`)
- `CLEANUP_INTERVAL_MINUTES`: Cleanup job interval (default: `1440` = 24 hours)
- `COOKIE_NAME`: Authentication cookie name (default: `.OneTimeShare.Auth`)
- `COOKIE_SECURE`: Use secure cookies (default: `true` in Production)
- `LOG_LEVEL`: Logging level (default: `Information`)

## Docker Deployment

### Build and Run

```bash
# Build the image
docker build -t onetimeshare .

# Run with environment variables
docker run -d \
  -p 8080:8080 \
  -e GOOGLE_CLIENT_ID=your_client_id \
  -e GOOGLE_CLIENT_SECRET=your_client_secret \
  -e APP_BASE_URL=https://yourdomain.com \
  -v $(pwd)/storage:/app/storage \
  -v $(pwd)/data:/app/App_Data \
  --name onetimeshare \
  onetimeshare
```

### Docker Compose

```yaml
version: '3.8'
services:
  onetimeshare:
    build: .
    ports:
      - "8080:8080"
    environment:
      - GOOGLE_CLIENT_ID=${GOOGLE_CLIENT_ID}
      - GOOGLE_CLIENT_SECRET=${GOOGLE_CLIENT_SECRET}
      - APP_BASE_URL=https://yourdomain.com
    volumes:
      - ./storage:/app/storage
      - ./data:/app/App_Data
    restart: unless-stopped
```

## API Endpoints

### Web UI
- `GET /` - Home page
- `GET /files/upload` - Upload form
- `POST /files/upload` - Handle file upload
- `GET /files/success/{id}` - Upload success page

### Download
- `GET /d/{fileId}?t={token}` - One-time download endpoint

### Authentication
- `GET /auth/login` - Initiate Google login
- `GET /signin-google` - Google OAuth callback
- `POST /auth/logout` - Sign out

### Health Checks
- `GET /health` or `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe

## Security Features

- **HTTPS Enforcement**: Automatic HTTPS redirection in production
- **Secure Cookies**: HttpOnly, Secure, SameSite protection
- **CSRF Protection**: Anti-forgery tokens on upload forms
- **Token Security**: SHA-256 hashed tokens with salt, constant-time verification
- **Input Validation**: File size limits, filename sanitization
- **Content Security**: Proper Content-Disposition headers, X-Content-Type-Options

## Architecture

- **ASP.NET Core MVC**: Web framework with Razor views
- **Entity Framework Core**: ORM with SQLite provider
- **Google Authentication**: OAuth 2.0 / OpenID Connect
- **Background Services**: Automated cleanup with hosted service
- **Local File Storage**: Configurable storage root directory

## Development

### Project Structure
```
src/OneTimeShare.Web/
├── Controllers/         # MVC controllers
├── Views/              # Razor views
├── Models/             # Data models and view models
├── Services/           # Business logic services
├── Data/               # Entity Framework context
├── Hosted/             # Background services
└── wwwroot/            # Static files
```

### Running Tests
```bash
cd src/OneTimeShare.Tests
dotnet test
```

### Database Migrations
```bash
cd src/OneTimeShare.Web
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Monitoring

### Health Checks
- **Liveness**: `/health/live` - Always returns 200 if app is running
- **Readiness**: `/health/ready` - Returns 200 if app can serve traffic (DB connected)

### Logging
- Structured logging with Microsoft.Extensions.Logging
- Key events: uploads, downloads, deletions, authentication
- Configurable log levels via `LOG_LEVEL` environment variable

## Troubleshooting

### Common Issues

1. **Google OAuth Error**
   - Verify `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET`
   - Check authorized redirect URIs in Google Console
   - Ensure callback URL matches: `{APP_BASE_URL}/signin-google`

2. **File Upload Fails**
   - Check file size against `MAX_UPLOAD_BYTES` limit
   - Verify storage directory permissions
   - Check available disk space

3. **Database Issues**
   - Ensure SQLite file directory exists and is writable
   - Check `SQLITE_CONN_STRING` format
   - Verify Entity Framework migrations are applied

4. **Download Links Don't Work**
   - Verify `APP_BASE_URL` is correctly set
   - Check if file has already been downloaded (one-time use)
   - Confirm file hasn't expired (check `FILE_RETENTION_DAYS`)

## License

This project is licensed under the MIT License - see the LICENSE file for details.