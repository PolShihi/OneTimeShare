# Operations Guide

## Deployment

### Prerequisites
- .NET 8.0 Runtime
- Google OAuth 2.0 credentials
- SSL certificate (for production)
- Sufficient disk space for file storage

### Environment Setup

1. **Create Google OAuth Application**
   ```
   1. Go to Google Cloud Console
   2. Create new project or select existing
   3. Enable Google+ API
   4. Create OAuth 2.0 credentials
   5. Add authorized redirect URI: https://yourdomain.com/signin-google
   ```

2. **Set Environment Variables**
   ```bash
   export GOOGLE_CLIENT_ID="your_google_client_id"
   export GOOGLE_CLIENT_SECRET="your_google_client_secret"
   export APP_BASE_URL="https://yourdomain.com"
   export ASPNETCORE_ENVIRONMENT="Production"
   ```

### Docker Deployment

```bash
# Build and run with Docker
docker build -t onetimeshare .
docker run -d \
  -p 8080:8080 \
  -e GOOGLE_CLIENT_ID=your_client_id \
  -e GOOGLE_CLIENT_SECRET=your_client_secret \
  -e APP_BASE_URL=https://yourdomain.com \
  -v /path/to/storage:/app/storage \
  -v /path/to/data:/app/App_Data \
  --name onetimeshare \
  onetimeshare
```

### Docker Compose Deployment

```bash
# Copy environment file
cp .env.example .env
# Edit .env with your configuration

# Start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

## Monitoring

### Health Checks

The application provides health check endpoints:

- **Liveness**: `GET /health/live` - Returns 200 if application is running
- **Readiness**: `GET /health/ready` - Returns 200 if application can serve traffic

### Logging

Application logs structured events:

```json
{
  "timestamp": "2024-01-01T12:00:00Z",
  "level": "Information",
  "message": "File uploaded successfully",
  "userId": "google_sub_id",
  "fileId": "guid",
  "sizeBytes": 1024
}
```

Key events logged:
- User authentication (login/logout)
- File uploads (success/failure)
- Download attempts (success/failure/already used)
- File deletions (manual/automatic cleanup)
- Cleanup operations
- Errors and exceptions

### Metrics to Monitor

1. **Application Metrics**
   - Upload success/failure rate
   - Download success/failure rate
   - Authentication success/failure rate
   - Cleanup job execution time

2. **System Metrics**
   - Disk space usage (storage directory)
   - Database size
   - Memory usage
   - CPU usage
   - Network I/O

3. **Business Metrics**
   - Number of active users
   - Files uploaded per day
   - Files downloaded per day
   - Average file size
   - Storage utilization

## Maintenance

### Regular Tasks

1. **Daily**
   - Check application logs for errors
   - Monitor disk space usage
   - Verify cleanup job execution

2. **Weekly**
   - Review security logs
   - Check database size growth
   - Verify backup integrity

3. **Monthly**
   - Update dependencies
   - Review and rotate secrets
   - Analyze usage patterns
   - Capacity planning review

### Database Maintenance

```bash
# Check database size
ls -lh App_Data/app.db

# Backup database
cp App_Data/app.db App_Data/app.db.backup.$(date +%Y%m%d)

# View database statistics (requires sqlite3)
sqlite3 App_Data/app.db "SELECT COUNT(*) as total_files FROM StoredFiles;"
sqlite3 App_Data/app.db "SELECT COUNT(*) as deleted_files FROM StoredFiles WHERE DeletedAtUtc IS NOT NULL;"
```

### Storage Maintenance

```bash
# Check storage usage
du -sh storage/

# Count files in storage
find storage/ -type f | wc -l

# Find orphaned files (files without database records)
# This requires custom script to cross-reference with database
```

### Cleanup Operations

The cleanup service runs automatically, but you can also trigger manual cleanup:

1. **Expired Files**: Files older than retention period
2. **Orphaned Files**: Files on disk without database records
3. **Old Database Records**: Deleted file records older than retention + 7 days

## Backup and Recovery

### Backup Strategy

1. **Database Backup**
   ```bash
   # Create backup
   cp App_Data/app.db backups/app.db.$(date +%Y%m%d_%H%M%S)
   
   # Automated backup script
   #!/bin/bash
   BACKUP_DIR="/path/to/backups"
   DATE=$(date +%Y%m%d_%H%M%S)
   cp App_Data/app.db "$BACKUP_DIR/app.db.$DATE"
   
   # Keep only last 30 days of backups
   find "$BACKUP_DIR" -name "app.db.*" -mtime +30 -delete
   ```

2. **Configuration Backup**
   ```bash
   # Backup environment configuration
   env | grep -E "(GOOGLE_|APP_|STORAGE_|SQLITE_|MAX_|FILE_|CLEANUP_|COOKIE_)" > config.backup
   ```

3. **Storage Backup** (Optional - files are temporary)
   ```bash
   # Only if you need to backup active files
   tar -czf storage.backup.tar.gz storage/
   ```

### Recovery Procedures

1. **Database Recovery**
   ```bash
   # Stop application
   docker-compose down
   
   # Restore database
   cp backups/app.db.YYYYMMDD_HHMMSS App_Data/app.db
   
   # Start application
   docker-compose up -d
   ```

2. **Full System Recovery**
   ```bash
   # Restore application files
   # Restore database
   # Restore configuration
   # Restart services
   ```

## Troubleshooting

### Common Issues

1. **Google OAuth Errors**
   ```
   Problem: "OAuth error" or "Invalid client"
   Solution: 
   - Verify GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET
   - Check authorized redirect URIs in Google Console
   - Ensure APP_BASE_URL matches OAuth configuration
   ```

2. **File Upload Failures**
   ```
   Problem: "File upload failed"
   Solution:
   - Check disk space in storage directory
   - Verify storage directory permissions
   - Check MAX_UPLOAD_BYTES setting
   - Review application logs for specific errors
   ```

3. **Database Connection Issues**
   ```
   Problem: "Database connection failed"
   Solution:
   - Check SQLITE_CONN_STRING format
   - Verify database file permissions
   - Ensure App_Data directory exists
   - Check disk space
   ```

4. **Download Link Issues**
   ```
   Problem: "Download link not working"
   Solution:
   - Verify APP_BASE_URL is correct
   - Check if file has already been downloaded
   - Confirm file hasn't expired
   - Review token format and encoding
   ```

### Log Analysis

```bash
# View recent errors
docker-compose logs onetimeshare | grep -i error

# Monitor real-time logs
docker-compose logs -f onetimeshare

# Search for specific events
docker-compose logs onetimeshare | grep "File uploaded successfully"
```

### Performance Tuning

1. **Database Optimization**
   - Regular VACUUM operations for SQLite
   - Monitor query performance
   - Consider connection pooling settings

2. **Storage Optimization**
   - Monitor I/O patterns
   - Consider SSD storage for better performance
   - Implement storage quotas if needed

3. **Application Optimization**
   - Adjust cleanup interval based on usage
   - Monitor memory usage patterns
   - Consider horizontal scaling for high load

## Security Operations

### Security Monitoring

1. **Failed Authentication Attempts**
   ```bash
   # Monitor failed logins
   docker-compose logs onetimeshare | grep "authentication failed"
   ```

2. **Suspicious Download Patterns**
   ```bash
   # Monitor download attempts
   docker-compose logs onetimeshare | grep "Download attempt"
   ```

3. **File Access Patterns**
   ```bash
   # Monitor file operations
   docker-compose logs onetimeshare | grep -E "(uploaded|downloaded|deleted)"
   ```

### Incident Response

1. **Suspected Breach**
   - Immediately rotate Google OAuth credentials
   - Review access logs for suspicious activity
   - Consider temporary service shutdown
   - Notify affected users if necessary

2. **Data Loss**
   - Assess scope of data loss
   - Restore from backups if available
   - Communicate with affected users
   - Review backup procedures

### Compliance Auditing

1. **Access Logs**: Review who accessed what files when
2. **Retention Compliance**: Verify automatic deletion is working
3. **Data Minimization**: Confirm only necessary data is stored
4. **Security Controls**: Regular review of security configurations