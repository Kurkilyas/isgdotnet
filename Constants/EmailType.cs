namespace InvoiceTrackingSystemBackend.Constants;

public enum EmailType
{
    EmailVerification,
    PasswordReset,
    LoginTwoFactor,
    AccountLocked,
    InvoiceAssigned,
    SlaReminder,
    SlaEscalated,
    InvoiceRejected,
    InvoiceReturned,
    InvoiceMissingDocument
}
