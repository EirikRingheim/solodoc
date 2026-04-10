namespace Solodoc.Domain.Enums;

public enum AbsenceStatus
{
    Registered,  // Past absence, logged directly
    Pending,     // Future ferie/avspasering, awaiting approval (søknad)
    Approved,
    Rejected
}
