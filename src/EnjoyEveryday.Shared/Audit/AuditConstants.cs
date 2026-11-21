namespace EnjoyEveryday.Shared.Audit;

/// <summary>
/// Centralized audit action constants for structured audit logging.
/// </summary>
public static class AuditActions
{
    public static class Journey
    {
        public const string Created = "JourneyCreated";
        public const string Updated = "JourneyUpdated";
        public const string Approved = "JourneyApproved";
        public const string MilestoneCaptured = "MilestoneCaptured";
    }

    public static class DailyCare
    {
        public const string Recorded = "CHILD_CARE_RECORDED";
        public const string BatchRecorded = "BATCH_CHILD_CARE_RECORDED";
        public const string Corrected = "CHILD_CARE_CORRECTED";
        public const string NapStarted = "NAP_STARTED";
        public const string NapEnded = "NAP_ENDED";
        public const string PlanUpdated = "CARE_PLAN_UPDATED";
        public const string SupplyUpdated = "CARE_SUPPLY_UPDATED";
        public const string HandoverSubmitted = "TeacherHandoverSubmitted";
        public const string HandoverAcknowledged = "TeacherHandoverAcknowledged";
    }

    public static class Attendance
    {
        public const string ChildRecorded = "ChildAttendanceRecorded";
        public const string ChildDepartureVerified = "ChildDepartureVerified";
        public const string AbsenceReported = "ParentReportedAbsence";
    }

    public static class Auth
    {
        public const string RoleAssigned = "Role.Assigned";
        public const string UserDeactivated = "User.DeactivatedFromOrganization";
        public const string TeacherPromotedToLead = "Teacher.PromotedToLead";
        public const string TeacherClassroomsUpdated = "Teacher.ClassroomsUpdated";
        public const string AuthorizationDenied = "Authorization.Denied";
        public const string TenantDataAccess = "Support.TenantDataAccess";
    }

    public static class Parent
    {
        public const string DraftCreated = "ParentCommunication.DraftCreated";
        public const string DraftUpdated = "ParentCommunication.DraftUpdated";
        public const string CommunicationSent = "ParentCommunication.Sent";
        public const string MessageSent = "ParentMessageSent";
        public const string ProfileUpdated = "ParentProfileUpdated";
        public const string PrivacyApproved = "PrivacyAndTrustApproved";
        public const string EventRsvpSubmitted = "EventRsvpSubmitted";
    }

    public static class Operations
    {
        public const string BranchOpenDay = "Branch.OpenDay";
        public const string BranchCloseDay = "Branch.CloseDay";
        public const string ClassroomOpenDay = "Classroom.OpenDay";
        public const string ClassroomCloseDay = "Classroom.CloseDay";
        public const string EndOfDayProcess = "Daycare.EndOfDayProcess";
    }

    public static class Commercial
    {
        public const string BillingModeChanged = "BillingMode.Changed";
        public const string EnrollmentInquiryCreated = "EnrollmentInquiryCreated";
        public const string EnrollmentStageUpdated = "EnrollmentStageUpdated";
        public const string EnrollmentFinalized = "EnrollmentFinalized";
        public const string InvoiceDraftCreated = "Invoice.DraftCreated";
        public const string InvoiceDraftUpdated = "Invoice.DraftUpdated";
        public const string InvoicePublished = "Invoice.Published";
        public const string InvoiceVoided = "Invoice.Voided";
        public const string PaymentRecorded = "Payment.Recorded";
        public const string CashRecorded = "Payment.CashRecorded";
        public const string PaymentSubmitted = "ParentPayment.Submitted";
        public const string PaymentVerified = "ParentPayment.Verified";
        public const string PaymentRejected = "ParentPayment.Rejected";
    }

    public static class Event
    {
        public const string Created = "Event.Created";
        public const string Updated = "Event.Updated";
        public const string Deleted = "Event.Deleted";
    }
}

/// <summary>
/// Centralized audit entity type constants.
/// </summary>
public static class AuditEntityTypes
{
    public const string ChildJourneyEntry = "ChildJourneyEntry";
    public const string ChildMilestone = "ChildMilestone";
    public const string ChildCareEvent = "ChildCareEvent";
    public const string ChildNapSession = "ChildNapSession";
    public const string ChildCarePlan = "ChildCarePlan";
    public const string ChildCareSupply = "ChildCareSupply";
    public const string ChildAttendance = "ChildAttendance";
    public const string ChildAttendanceAbsenceNotice = "ChildAttendanceAbsenceNotice";
    public const string ChildConsent = "ChildConsent";
    public const string Child = "Child";
    public const string Person = "Person";
    public const string User = "User";
    public const string TeacherHandover = "TeacherHandover";
    public const string ParentCommunication = "ParentCommunication";
    public const string EnrollmentApplication = "EnrollmentApplication";
    public const string Invoice = "Invoice";
    public const string Payment = "Payment";
    public const string Event = "Event";
    public const string EventRsvp = "EventRsvp";
    public const string BranchDailyOperations = "BranchDailyOperations";
    public const string ClassroomDailyOperations = "ClassroomDailyOperations";
    public const string DaycareOperations = "DaycareOperations";
}
