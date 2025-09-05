# Security Policy

## Supported Versions

We actively support the following versions of Vortex Programming with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

The Vortex Programming team takes security vulnerabilities seriously. We appreciate your efforts to responsibly disclose your findings.

### How to Report

**Please DO NOT report security vulnerabilities through public GitHub issues.**

Instead, please report security vulnerabilities by emailing: **security@vortexprogramming.com**

Include the following information in your report:
- Type of issue (buffer overflow, SQL injection, cross-site scripting, etc.)
- Full paths of source file(s) related to the manifestation of the issue
- The location of the affected source code (tag/branch/commit or direct URL)
- Any special configuration required to reproduce the issue
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

### What to Expect

After submitting a report, you can expect:

1. **Acknowledgment**: We'll acknowledge receipt of your vulnerability report within 2 business days
2. **Investigation**: We'll investigate and validate the vulnerability within 5 business days
3. **Resolution Timeline**: We'll provide an estimated timeline for resolution
4. **Updates**: We'll keep you informed of our progress throughout the process
5. **Credit**: If you'd like, we'll publicly credit you for the responsible disclosure

### Security Update Process

1. **Patch Development**: We develop and test a fix for the vulnerability
2. **Release Planning**: We coordinate the release of the security patch
3. **Notification**: We notify users through:
   - GitHub Security Advisories
   - Release notes
   - Email notifications (for critical vulnerabilities)
4. **Public Disclosure**: After users have had time to update, we may publish details about the vulnerability

## Security Best Practices

When using Vortex Programming in production:

### Multi-Tenant Security
- Always validate `TenantId` values before processing
- Use proper tenant isolation in your data layer
- Implement tenant-specific access controls
- Regularly audit tenant separation

### Process Security
- Validate all input data before processing
- Use secure serialization practices for DSL processes
- Implement proper error handling to avoid information disclosure
- Monitor process execution for anomalies

### Context Security
- Protect context properties that contain sensitive information
- Use secure channels for distributed execution
- Implement proper authentication for process chains
- Regularly rotate any credentials used in contexts

### Event Stream Security
- Sanitize event data before logging
- Implement access controls for event subscriptions
- Use secure transport for event streaming
- Monitor for unusual event patterns

## Vulnerability Disclosure Timeline

We follow a coordinated disclosure timeline:

- **Day 0**: Vulnerability reported
- **Day 2**: Acknowledgment sent to reporter
- **Day 7**: Initial assessment completed
- **Day 30**: Target date for patch development
- **Day 37**: Security advisory published (if applicable)
- **Day 90**: Full public disclosure (if patch is available)

## Security Contact

For security-related questions or concerns:

- **Email**: security@vortexprogramming.com
- **PGP Key**: Available at https://vortexprogramming.com/.well-known/pgp-key.asc
- **Response Time**: Within 2 business days

## Acknowledgments

We thank the following security researchers for their responsible disclosure:

*No vulnerabilities reported yet - be the first to help make Vortex Programming more secure!*

---

**Note**: This security policy applies to the Vortex Programming Framework and associated documentation. For security issues in applications built with Vortex Programming, please contact the respective application maintainers. 