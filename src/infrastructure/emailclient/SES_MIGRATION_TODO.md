# Amazon SES Migration TODO

## Infrastructure Setup
- [ ] Provision SES infrastructure for the domain (nightingaletrading.com)
  - [ ] Verify domain in SES console
  - [ ] Set up DKIM records for domain authentication
  - [ ] Set up SPF records
  - [ ] Request production access (move out of sandbox)
  - [ ] Configure verified sender identities (noreply@nightingaletrading.com)
- [ ] Provision AWS credentials for SES access
  - [ ] Create IAM user with SES permissions (ses:SendEmail, ses:SendTemplatedEmail)
  - [ ] Generate access key and secret key
  - [ ] Store credentials securely (AWS Secrets Manager or similar)
- [ ] Configure credentials in production environment
  - [ ] Set environment variables: AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION
  - [ ] Update deployment scripts/Dockerfile if needed
  - [ ] Test credentials in staging environment first

## Code Implementation
- [ ] Add AWS SDK NuGet package
  - [ ] Add `AWSSDK.SimpleEmail` to emailclient.fsproj
- [ ] Implement SES client initialization
  - [ ] Initialize AmazonSimpleEmailServiceClient in constructor
  - [ ] Handle credential provider options (IAM role vs explicit credentials)
- [ ] Implement interface methods
  - [ ] Implement `sendRaw` method using SES SendEmail API
    - [ ] Create Message with subject and body (text/html)
    - [ ] Create SendEmailRequest with sender, recipient, message
    - [ ] Handle errors and retries
  - [ ] Implement `sendTemplated` method using SES SendTemplatedEmail API
    - [ ] Serialize template data to JSON
    - [ ] Create SendTemplatedEmailRequest
    - [ ] Handle errors and retries

## Email Templates (Local Management)
Templates will be managed locally and deployed to SES, not stored in SES console.

- [ ] Create email template files in project
  - [ ] UserDeleted template
  - [ ] Welcome template
  - [ ] ContactUs template
  - [ ] Verify template
  - [ ] PasswordReset template
  - [ ] Alerts template
  - [ ] BrokerageTransactions template
  - [ ] SellAlert template
  - [ ] MaxProfits template
- [ ] Create template deployment script
  - [ ] Script to upload/update templates in SES
  - [ ] Validate templates before deployment
- [ ] Version control templates
  - [ ] Define template versioning strategy
  - [ ] Track template changes in git

## Testing
- [ ] Unit tests for SESEmailService
  - [ ] Mock SES client for testing
  - [ ] Test error handling
  - [ ] Test template data serialization
- [ ] Integration tests with SES sandbox
  - [ ] Test sending to verified email addresses
  - [ ] Verify email delivery
  - [ ] Test all template types
- [ ] Load testing
  - [ ] Test SES sending limits and throttling
  - [ ] Implement rate limiting if needed

## Migration Strategy
- [ ] Plan migration approach
  - [ ] Parallel run (SendGrid + SES) with feature flag?
  - [ ] Direct cutover?
  - [ ] Gradual rollout by email type?
- [ ] Create rollback plan
  - [ ] Document steps to revert to SendGrid if needed
  - [ ] Keep SendGrid credentials available during transition period
- [ ] Monitor migration
  - [ ] Set up CloudWatch alarms for SES
  - [ ] Monitor bounce rates and complaints
  - [ ] Track delivery success rates

## Cleanup
- [ ] Remove SendGrid dependencies
  - [ ] Remove SendGrid NuGet packages
  - [ ] Remove SendGrid configuration code
  - [ ] Remove sendgridclient project
- [ ] Update documentation
  - [ ] Update README with SES setup instructions
  - [ ] Document email template management process
  - [ ] Update copilot-instructions.md

## Notes
- Current implementation: Dummy methods with printfn for debugging
- AWS Region default: us-east-1
- Configuration keys: AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION
