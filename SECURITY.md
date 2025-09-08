# Security Policy

## Security Features

OneTime Share implements several security measures to protect user data and ensure safe file sharing:

### Authentication & Authorization
- **Google OAuth 2.0**: Secure authentication using Google's OpenID Connect
- **Secure Cookies**: HttpOnly, Secure, SameSite protection
- **Session Management**: Sliding expiration with configurable timeouts

### Token Security
- **Cryptographically Strong Tokens**: 256-bit random tokens for download links
- **Salted Hashing**: SHA-256 with per-token salt for storage
- **Constant-Time Verification**: Protection against timing attacks
- **One-Time Use**: Tokens are invalidated after first use

### File Security
- **Secure Storage**: Files stored with GUID names, not original filenames
- **Path Traversal Protection**: Controlled directory structure
- **Automatic Cleanup**: Files deleted after download or expiration
- **Size Limits**: Configurable upload size restrictions

### Web Security
- **HTTPS Enforcement**: Automatic redirection in production
- **HSTS Headers**: HTTP Strict Transport Security
- **Content Security**: Proper Content-Disposition and X-Content-Type-Options headers
- **CSRF Protection**: Anti-forgery tokens on forms
- **Input Validation**: Server-side validation of all inputs

### Database Security
- **Optimistic Concurrency**: Protection against race conditions
- **No Sensitive Data**: Only hashed tokens stored, never plain text
- **Parameterized Queries**: Protection against SQL injection

## Security Configuration

### Required Security Settings

```bash
# Production settings
ASPNETCORE_ENVIRONMENT=Production
COOKIE_SECURE=true
APP_BASE_URL=https://yourdomain.com  # Must use HTTPS

# Google OAuth (keep secret)
GOOGLE_CLIENT_ID=your_client_id
GOOGLE_CLIENT_SECRET=your_client_secret
```

### Recommended Security Practices

1. **Use HTTPS**: Always deploy with valid SSL certificates
2. **Secure Secrets**: Use environment variables or secret management systems
3. **Regular Updates**: Keep dependencies updated
4. **Monitor Logs**: Watch for suspicious activity
5. **Backup Strategy**: Regular backups of database and configuration

## Threat Model

### Threats Mitigated
- **Token Guessing**: Cryptographically strong random tokens
- **Replay Attacks**: One-time use tokens
- **Session Hijacking**: Secure cookie configuration
- **CSRF Attacks**: Anti-forgery token validation
- **Path Traversal**: Controlled file storage paths
- **Timing Attacks**: Constant-time token verification
- **Race Conditions**: Database optimistic concurrency

### Potential Risks
- **Social Engineering**: Users sharing links inappropriately
- **Insider Threats**: Server administrators have access to files
- **Physical Access**: Local file system access bypasses application security
- **Network Interception**: Requires HTTPS for protection

## Reporting Security Issues

If you discover a security vulnerability, please report it responsibly:

1. **Do not** create public GitHub issues for security vulnerabilities
2. Email security concerns to: [your-security-email@domain.com]
3. Include detailed information about the vulnerability
4. Allow reasonable time for response and fix

## Security Checklist for Deployment

### Before Deployment
- [ ] Valid SSL certificate configured
- [ ] HTTPS redirection enabled
- [ ] Secure environment variables set
- [ ] Google OAuth redirect URIs configured correctly
- [ ] File storage directory permissions restricted
- [ ] Database file permissions restricted
- [ ] Log files secured and rotated

### Regular Maintenance
- [ ] Monitor application logs for suspicious activity
- [ ] Review and rotate Google OAuth credentials periodically
- [ ] Update dependencies regularly
- [ ] Monitor disk space for storage directory
- [ ] Backup database and configuration
- [ ] Test disaster recovery procedures

## Security Headers

The application automatically sets these security headers:

```
X-Content-Type-Options: nosniff
Cache-Control: no-store (for downloads)
Strict-Transport-Security: max-age=31536000 (in production)
```

## Data Protection

### Data Retention
- Files are automatically deleted after first download
- Files expire after 30 days (configurable)
- Database records are cleaned up after retention period + 7 days
- User accounts persist but contain minimal information

### Data Minimization
- Only essential user information stored (Google sub, email, name)
- No file content analysis or indexing
- Minimal logging of user activities
- No tracking or analytics by default

## Compliance Considerations

While OneTime Share implements security best practices, organizations should evaluate compliance requirements:

- **GDPR**: Minimal data collection, automatic deletion, user control
- **HIPAA**: Additional controls may be needed for healthcare data
- **SOX**: Audit logging and access controls may need enhancement
- **Industry Standards**: Evaluate against specific regulatory requirements