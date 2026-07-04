public record SendEmailConfirmationPayload(
    string ToEmail,
    string FirstName,
    string ConfirmationCode,
    string Language
);
