namespace EnjoyEveryday.Domain.Constants;

/// <summary>
/// Pipeline stages for child enrollment and admissions workflow.
/// </summary>
public static class EnrollmentStages
{
    public const string Inquiry = "Inquiry";
    public const string Enquiry = "Enquiry";
    public const string TourScheduled = "TourScheduled";
    public const string ApplicationSubmitted = "ApplicationSubmitted";
    public const string Review = "Review";
    public const string Waitlist = "Waitlist";
    public const string OfferSent = "OfferSent";
    public const string Enrolled = "Enrolled";
    public const string Withdrawn = "Withdrawn";
    public const string Graduated = "Graduated";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Inquiry,
        TourScheduled,
        ApplicationSubmitted,
        Waitlist,
        OfferSent,
        Enrolled,
        Withdrawn,
        Graduated
    };
}

/// <summary>
/// Active status for child enrollment contracts.
/// </summary>
public static class EnrollmentStatuses
{
    public const string Enrolled = "Enrolled";
    public const string Withdrawn = "Withdrawn";
    public const string Graduated = "Graduated";
    public const string Pending = "Pending";
}
