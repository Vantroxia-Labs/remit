# EInvoice Integrator - Implementation Roadmap

This document outlines the implementation roadmap for the EInvoice Integrator application based on the provided user stories.

## Phase 1: Foundation & Core Infrastructure (Weeks 1-3)

### ✅ Completed
- [x] FIRS Access Point integration with tenant-agnostic architecture
- [x] Domain entities (Invoice, Business, DigitalCertificate, etc.)
- [x] Value objects (TIN, IRN, Address, QRCode, DigitalSignature)
- [x] Domain events and enums
- [x] Basic Clean Architecture structure

### 🔄 In Progress
- [ ] Complete persistence layer configuration
- [ ] Entity Framework configurations for new entities
- [ ] Database migrations
- [ ] Application services interfaces

## Phase 2: Business Onboarding (Week 4) - US001

### Core Features
- [x] ConnectToFIRSCommand implementation
- [x] BusinessOnboardingController with FIRS credential validation
- [ ] OAuth 2.0 token management
- [ ] Secure credential storage (Key Vault integration)
- [ ] Connection status monitoring

### Acceptance Criteria Mapping
- ✅ Admin can input and save TIN, ServiceID, and secret key
- ⏳ OAuth 2.0 token generation and secure storage  
- ⏳ Connection status display ("Connected", "Disconnected")

## Phase 3: Invoice Creation & UBL Compliance (Weeks 5-6) - US002

### Core Features
- [x] CreateInvoiceCommand with IRN generation
- [x] InvoiceController with validation
- [ ] UBL (Universal Business Language) integration
- [ ] Invoice validation service implementation
- [ ] UBL schema parser and validator

### Acceptance Criteria Mapping
- ⏳ Mandatory fields validation (TIN, buyer/supplier, tax total)
- ⏳ UBL structure validation before submission
- ✅ Automatic IRN generation (INV001-XXXXXX-YYYYMMDD format)

## Phase 4: Digital Signing (Weeks 7-8) - US003

### Core Features
- [ ] Digital certificate management
- [ ] ECDSA signing service implementation
- [ ] JAdES (JSON Advanced Electronic Signatures) support
- [ ] Key vault integration for secure key storage
- [ ] Certificate lifecycle management

### Acceptance Criteria Mapping
- ⏳ Secure cryptographic key storage (HSM/encrypted vault)
- ⏳ ECC signature conforming to JAdES format
- ⏳ Signing error logging and alerting

## Phase 5: QR Code Generation (Week 9) - US004

### Core Features
- [ ] QR code generation service
- [ ] IRN and timestamp encryption
- [ ] Base64 encoding for printable QR codes
- [ ] MBS360 app compatibility validation

### Acceptance Criteria Mapping
- ⏳ Base64-encoded, printable QR codes
- ⏳ QR contains encrypted IRN + timestamp
- ⏳ Scanner validation compatibility

## Phase 6: FIRS Submission & Validation (Weeks 10-11) - US005

### Core Features
- [ ] FIRS submission workflow
- [ ] Retry logic with exponential backoff
- [ ] Status tracking and updates
- [ ] Error handling and user notification

### Acceptance Criteria Mapping
- ⏳ Retry logic for FIRS downtime
- ⏳ Success status updates to "Approved"
- ⏳ Failure alerts with error reasons

## Phase 7: Role-Based Access Control (Week 12) - US006

### Core Features
- [ ] User management system
- [ ] Role assignment and revocation
- [ ] Middleware for role validation
- [ ] Permission audit logging

### Roles Implementation
- **Admin**: Full system access, user management, business setup
- **Manager**: Invoice approval workflows
- **Accountant**: Invoice creation, submission, monitoring
- **Auditor**: Read-only access to reports and logs
- **Viewer**: Read-only access to invoice records

### Acceptance Criteria Mapping
- ⏳ Admin role and permission assignment
- ⏳ Feature access restriction by role
- ⏳ Permission change audit logging

## Phase 8: Compliance Reporting (Week 13) - US007

### Core Features
- [ ] Report generation engine
- [ ] Filtering capabilities (date, buyer, tax type)
- [ ] Multiple export formats (CSV, JSON, PDF)
- [ ] Error log inclusion with timestamps

### Acceptance Criteria Mapping
- ⏳ Multi-criteria filtering
- ⏳ Multiple export format support
- ⏳ Comprehensive error logs and timestamps

## Phase 9: Notifications & Alerts (Week 14) - US008

### Core Features
- [ ] Notification service implementation
- [ ] Multiple notification channels (in-app, email, SMS)
- [ ] Human-readable error messages
- [ ] Real-time UI status updates

### Acceptance Criteria Mapping
- ⏳ Multi-channel alert system (in-app + email)
- ⏳ User-friendly error messages
- ⏳ Visual status indicators in UI

## Phase 10: Advanced Features (Weeks 15-18)

### US013: Service Provider Gateway (APP Layer)
- [ ] ERP integration API endpoints
- [ ] OAuth 2.0 API authentication
- [ ] Comprehensive API documentation
- [ ] End-to-end integration testing

### US014: Invoice Approval Workflow  
- [x] ApprovalWorkflow entity created
- [ ] Workflow state management
- [ ] Manager approval requirements
- [ ] Approval audit trail

### US015: Merchant Data Ingestion
- [ ] Bulk import functionality (CSV/XML/JSON)
- [ ] Data validation with line-number error reporting
- [ ] Import audit trail for compliance

## Phase 11: Multi-tenancy & Scalability (Weeks 19-20)

### Core Features
- [ ] Tenant isolation middleware
- [ ] Multi-branch support (US011)
- [ ] Invoice templates by business type (US012)
- [ ] Business profile sharing for consultants (US010)

## Technical Architecture Decisions

### Database Design
- **Multi-tenant**: Tenant isolation at row level using TenantId
- **Audit Trail**: Complete audit logging for compliance
- **Event Sourcing**: Domain events for critical business operations

### Security Implementation
- **Key Management**: Azure Key Vault for certificate storage
- **Authentication**: JWT Bearer tokens with role claims
- **Authorization**: Policy-based with role and tenant validation
- **Encryption**: AES-256 for sensitive data at rest

### Integration Architecture
- **FIRS Integration**: Tenant-agnostic service layer
- **ERP Integration**: RESTful API with OAuth 2.0
- **Notification**: Multi-channel with template support

### Performance & Scalability
- **Background Jobs**: Hangfire for invoice submission and retry logic
- **Caching**: Redis for frequently accessed reference data
- **Message Queue**: Service Bus for reliable event processing

## Testing Strategy

### Unit Testing
- ✅ Domain entities and value objects
- ✅ FIRS Access Point components
- ⏳ Application services and commands
- ⏳ API controllers and validation

### Integration Testing
- ⏳ FIRS API integration
- ⏳ Database operations
- ⏳ Background job processing

### End-to-End Testing
- ⏳ Complete invoice lifecycle
- ⏳ Multi-tenant scenarios
- ⏳ Error handling and recovery

## Deployment Strategy

### Environment Setup
- **Development**: Local SQL Server, local Key Vault emulator
- **Staging**: Azure SQL Database, Azure Key Vault
- **Production**: Azure with high availability, backup strategies

### CI/CD Pipeline
- **Build**: Automated testing, code quality gates
- **Deploy**: Blue-green deployment strategy
- **Monitor**: Application Insights, custom dashboards

## Success Metrics

### Business Metrics
- Invoice processing time: < 2 minutes end-to-end
- FIRS submission success rate: > 99%
- User adoption rate by role
- Compliance audit pass rate: 100%

### Technical Metrics
- API response time: < 200ms (95th percentile)
- System availability: > 99.9%
- Test coverage: > 90%
- Security vulnerability score: A-grade

## Risk Mitigation

### High-Risk Items
1. **FIRS API Changes**: Maintain versioned integration layer
2. **Certificate Expiry**: Automated monitoring and renewal alerts
3. **Multi-tenant Data Leakage**: Comprehensive access control testing
4. **Performance Under Load**: Load testing and horizontal scaling

### Contingency Plans
- **FIRS Downtime**: Queue-based retry with exponential backoff
- **Certificate Issues**: Multiple certificate support and fallbacks
- **Data Loss**: Point-in-time recovery with audit trail preservation

---

**Next Steps**: Begin Phase 2 implementation with business onboarding features, focusing on secure credential management and FIRS connectivity validation.