using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Parent.Web.Services;

public class LanguageOption
{
    public string Code { get; set; } = "en";
    public string DisplayName { get; set; } = "English";
    public string NativeName { get; set; } = "English";
}

public class ParentLocalizationService
{
    public static readonly IReadOnlyList<LanguageOption> SupportedLanguages = new[]
    {
        new LanguageOption { Code = "en", DisplayName = "English", NativeName = "English" },
        new LanguageOption { Code = "ta", DisplayName = "Tamil", NativeName = "தமிழ்" },
        new LanguageOption { Code = "te", DisplayName = "Telugu", NativeName = "తెలుగు" },
        new LanguageOption { Code = "hi", DisplayName = "Hindi", NativeName = "हिन्दी" },
        new LanguageOption { Code = "kn", DisplayName = "Kannada", NativeName = "ಕನ್ನಡ" }
    };

    public string CurrentLanguage { get; private set; } = "en";

    public event Action? OnLanguageChanged;

    public void SetLanguage(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return;
        var normalized = code.ToLowerInvariant();
        if (Translations.ContainsKey(normalized) && CurrentLanguage != normalized)
        {
            CurrentLanguage = normalized;
            OnLanguageChanged?.Invoke();
        }
    }

    public string T(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        if (Translations.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out var val))
        {
            return val;
        }

        if (Translations.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enVal))
        {
            return enVal;
        }

        return key;
    }

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new(StringComparer.OrdinalIgnoreCase)
        {
            // Navigation
            ["Nav.Today"] = "Today",
            ["Nav.Journey"] = "Journey",
            ["Nav.Memories"] = "Memories",
            ["Nav.Messages"] = "Messages",
            ["Nav.Family"] = "Family & Care",
            ["Nav.Actions"] = "Everyday Actions",
            ["Nav.TrustCenter"] = "Privacy & Trust",

            // Daily Summary & Care
            ["Today.Title"] = "Today's Summary",
            ["Today.Subtitle"] = "Real-time updates, learning moments, and daily care routines.",
            ["Today.NotUpdatedYet"] = "Not yet updated today. Care notes and learning moments will appear here once recorded by your child's teachers.",
            ["Today.Meals"] = "Meals & Snacks",
            ["Today.Naps"] = "Nap & Rest",
            ["Today.Diapers"] = "Diapers & Hygiene",
            ["Today.Comfort"] = "Comfort & Mood",
            ["Today.LearningMoments"] = "Today's Learning Moments",
            ["Today.UpcomingEvents"] = "Upcoming Events",
            ["Today.ActionRequired"] = "Action Required",
            ["Today.PendingConsent"] = "Pending Consent Form",
            ["Today.UnpaidInvoice"] = "Tuition Invoice Due",
            ["Today.UnreadMessage"] = "New Teacher Message",

            // Actions & Forms
            ["Action.ReportAbsence"] = "Report Absence",
            ["Action.AuthorizedPickups"] = "Authorized Pickups",
            ["Action.ViewInvoices"] = "View Invoices",
            ["Action.ReviewConsent"] = "Review Consent",
            ["Action.QuickReplies"] = "Quick Replies",
            ["Action.QuietHours"] = "Quiet Hours Active (8:30 AM – 3:30 PM). Non-urgent messages will be reviewed during rest time.",
            ["Action.Close"] = "Close",
            ["Action.Submit"] = "Submit",
            ["Action.Saving"] = "Submitting...",

            // Statuses
            ["Status.NotRecorded"] = "Not recorded yet",
            ["Status.DidNotOccur"] = "Did not occur today",
            ["Status.Verified"] = "Verified & Active",
            ["Status.PendingStaff"] = "Pending Staff Review",

            // Privacy
            ["Privacy.Title"] = "Privacy & Trust Center",
            ["Privacy.PhotoConsent"] = "Photo & Media Consent",
            ["Privacy.DataPolicy"] = "Data Protection & Retention",
            ["Privacy.GuardianAccess"] = "Authorized Caregivers",
            ["Privacy.ContactOfficer"] = "Contact Data Protection Officer"
        },

        ["ta"] = new(StringComparer.OrdinalIgnoreCase)
        {
            // Navigation
            ["Nav.Today"] = "இன்று",
            ["Nav.Journey"] = "பயணம்",
            ["Nav.Memories"] = "நினைவுகள்",
            ["Nav.Messages"] = "செய்திகள்",
            ["Nav.Family"] = "குடும்பம் & பராமரிப்பு",
            ["Nav.Actions"] = "அன்றாட செயல்கள்",
            ["Nav.TrustCenter"] = "தனியுரிமை & நம்பிக்கை",

            // Daily Summary & Care
            ["Today.Title"] = "இன்றைய சுருக்கம்",
            ["Today.Subtitle"] = "நேரலை அறிவிப்புகள், கற்றல் தருணங்கள் மற்றும் அன்றாட பராமரிப்பு.",
            ["Today.NotUpdatedYet"] = "இன்று இன்னும் புதுப்பிக்கப்படவில்லை. ஆசிரியர்கள் பதிவு செய்தவுடன் பராமரிப்பு குறிப்புகள் இங்கே தோன்றும்.",
            ["Today.Meals"] = "உணவு & சிற்றுண்டி",
            ["Today.Naps"] = "தூக்கம் & ஓய்வு",
            ["Today.Diapers"] = "சுத்தம் & சுகாதாரம்",
            ["Today.Comfort"] = "மனநிலை & ஆறுதல்",
            ["Today.LearningMoments"] = "இன்றைய கற்றல் தருணங்கள்",
            ["Today.UpcomingEvents"] = "வரவிருக்கும் நிகழ்வுகள்",
            ["Today.ActionRequired"] = "கவனம் தேவை",
            ["Today.PendingConsent"] = "நிலுவையில் உள்ள சம்மதப் படிவம்",
            ["Today.UnpaidInvoice"] = "நிலுவையில் உள்ள கட்டணம்",
            ["Today.UnreadMessage"] = "புதிய ஆசிரியர் செய்தி",

            // Actions & Forms
            ["Action.ReportAbsence"] = "விடுப்பு தெரிவித்தல்",
            ["Action.AuthorizedPickups"] = "அங்கீகரிக்கப்பட்ட பெற்றோர்",
            ["Action.ViewInvoices"] = "கட்டண ரசீதுகள்",
            ["Action.ReviewConsent"] = "சம்மதம் சரிபார்",
            ["Action.QuickReplies"] = "விரைவு பதில்கள்",
            ["Action.QuietHours"] = "அமைதி நேரம் (8:30 AM – 3:30 PM). அவசரமில்லாத செய்திகள் ஓய்வு நேரத்தில் கவனிக்கப்படும்.",
            ["Action.Close"] = "மூடு",
            ["Action.Submit"] = "சமர்ப்பி",
            ["Action.Saving"] = "சமர்ப்பிக்கிறது...",

            // Statuses
            ["Status.NotRecorded"] = "இன்னும் பதிவு செய்யப்படவில்லை",
            ["Status.DidNotOccur"] = "இன்று நடைபெறவில்லை",
            ["Status.Verified"] = "சரிபார்க்கப்பட்டது",
            ["Status.PendingStaff"] = "ஆசிரியர் பரிசீலனையில் உள்ளது",

            // Privacy
            ["Privacy.Title"] = "தனியுரிமை & நம்பிக்கை மையம்",
            ["Privacy.PhotoConsent"] = "புகைப்பட சம்மதம்",
            ["Privacy.DataPolicy"] = "தரவு பாதுகாப்பு கொள்கை",
            ["Privacy.GuardianAccess"] = "அங்கீகரிக்கப்பட்ட பாதுகாவலர்கள்",
            ["Privacy.ContactOfficer"] = "தனியுரிமை அதிகாரியை தொடர்பு கொள்ளவும்"
        },

        ["te"] = new(StringComparer.OrdinalIgnoreCase)
        {
            // Navigation
            ["Nav.Today"] = "ఈరోజు",
            ["Nav.Journey"] = "ప్రయాణం",
            ["Nav.Memories"] = "జ్ఞాపకాలు",
            ["Nav.Messages"] = "సందేశాలు",
            ["Nav.Family"] = "కుటుంబం & సంరక్షణ",
            ["Nav.Actions"] = "రోజువారీ చర్యలు",
            ["Nav.TrustCenter"] = "గోప్యత & భద్రత",

            // Daily Summary & Care
            ["Today.Title"] = "ఈనాటి సారాంశం",
            ["Today.Subtitle"] = "తాజా నవీకరణలు, అభ్యాస క్షణాలు మరియు రోజువారీ సంరక్షణ వివరాలు.",
            ["Today.NotUpdatedYet"] = "ఇంకా నవీకరించబడలేదు. ఉపాధ్యాయులు నమోదు చేసిన తర్వాత వివరాలు ఇక్కడ కనిపిస్తాయి.",
            ["Today.Meals"] = "భోజనం & అల్పాహారం",
            ["Today.Naps"] = "నిద్ర & విశ్రాంతి",
            ["Today.Diapers"] = "పరిశుభ్రత",
            ["Today.Comfort"] = "మూడ్ & భావోద్వేగం",
            ["Today.LearningMoments"] = "ఈనాటి అభ్యాస క్షణాలు",
            ["Today.UpcomingEvents"] = "రాబోయే కార్యక్రమాలు",
            ["Today.ActionRequired"] = "చర్య అవసరం",
            ["Today.PendingConsent"] = "సమ్మతి పత్రం పెండింగ్‌లో ఉంది",
            ["Today.UnpaidInvoice"] = "ఫీజు రశీదు పెండింగ్‌లో ఉంది",
            ["Today.UnreadMessage"] = "కొత్త సందేశం",

            // Actions & Forms
            ["Action.ReportAbsence"] = "సెలవు నివేదించండి",
            ["Action.AuthorizedPickups"] = "అధికారిక పికప్ వ్యక్తులు",
            ["Action.ViewInvoices"] = "ఇన్‌వాయిస్‌లు చూడండి",
            ["Action.ReviewConsent"] = "సమ్మతిని సమీక్షించండి",
            ["Action.QuickReplies"] = "శీఘ్ర ప్రత్యుత్తరాలు",
            ["Action.QuietHours"] = "ప్రశాంత సమయం (8:30 AM – 3:30 PM). అత్యవసరం కాని సందేశాలు విశ్రాంతి సమయంలో సమీక్షించబడతాయి.",
            ["Action.Close"] = "మూసివేయి",
            ["Action.Submit"] = "సమర్పించు",
            ["Action.Saving"] = "సమర్పిస్తోంది...",

            // Statuses
            ["Status.NotRecorded"] = "ఇంకా నమోదు కాలేదు",
            ["Status.DidNotOccur"] = "ఈరోజు జరగలేదు",
            ["Status.Verified"] = "ధృవీకరించబడింది",
            ["Status.PendingStaff"] = "సిబ్బంది సమీక్షలో ఉంది",

            // Privacy
            ["Privacy.Title"] = "గోప్యత & భద్రతా కేంద్రం",
            ["Privacy.PhotoConsent"] = "ఫోటో సమ్మతి",
            ["Privacy.DataPolicy"] = "డేటా రక్షణ విధానం",
            ["Privacy.GuardianAccess"] = "అధికారిక సంరక్షకులు",
            ["Privacy.ContactOfficer"] = "గోప్యతా అధికారిని సంప్రదించండి"
        },

        ["hi"] = new(StringComparer.OrdinalIgnoreCase)
        {
            // Navigation
            ["Nav.Today"] = "आज",
            ["Nav.Journey"] = "विकास यात्रा",
            ["Nav.Memories"] = "यादें",
            ["Nav.Messages"] = "संदेश",
            ["Nav.Family"] = "परिवार एवं देखभाल",
            ["Nav.Actions"] = "दैनिक क्रियाएं",
            ["Nav.TrustCenter"] = "गोपनीयता एवं विश्वास",

            // Daily Summary & Care
            ["Today.Title"] = "आज का सारांश",
            ["Today.Subtitle"] = "रीयल-टाइम अपडेट, सीखने के क्षण और दैनिक देखभाल।",
            ["Today.NotUpdatedYet"] = "आज अभी तक अपडेट नहीं किया गया है। शिक्षकों द्वारा दर्ज करने के बाद विवरण यहाँ दिखाई देंगे।",
            ["Today.Meals"] = "भोजन एवं नाश्ता",
            ["Today.Naps"] = "नींद और विश्राम",
            ["Today.Diapers"] = "स्वच्छता एवं डायपर",
            ["Today.Comfort"] = "मनोदशा और आराम",
            ["Today.LearningMoments"] = "आज के सीखने के क्षण",
            ["Today.UpcomingEvents"] = "आगामी कार्यक्रम",
            ["Today.ActionRequired"] = "आवश्यक कार्रवाई",
            ["Today.PendingConsent"] = "लंबित सहमति प्रपत्र",
            ["Today.UnpaidInvoice"] = "शुल्क चालान देय",
            ["Today.UnreadMessage"] = "नया शिक्षक संदेश",

            // Actions & Forms
            ["Action.ReportAbsence"] = "अनुपस्थिति दर्ज करें",
            ["Action.AuthorizedPickups"] = "अधिकृत पिकअप सदस्य",
            ["Action.ViewInvoices"] = "चालान और रसीदें",
            ["Action.ReviewConsent"] = "सहमति की समीक्षा करें",
            ["Action.QuickReplies"] = "त्वरित उत्तर",
            ["Action.QuietHours"] = "शांत घंटे (8:30 पूर्वाह्न - 3:30 अपराह्न)। गैर-जरूरी संदेशों को विश्राम समय में देखा जाएगा।",
            ["Action.Close"] = "बंद करें",
            ["Action.Submit"] = "जमा करें",
            ["Action.Saving"] = "जमा हो रहा है...",

            // Statuses
            ["Status.NotRecorded"] = "अभी दर्ज नहीं किया गया",
            ["Status.DidNotOccur"] = "आज घटित नहीं हुआ",
            ["Status.Verified"] = "सत्यापित एवं सक्रिय",
            ["Status.PendingStaff"] = "स्टाफ समीक्षा लंबित",

            // Privacy
            ["Privacy.Title"] = "गोपनीयता एवं विश्वास केंद्र",
            ["Privacy.PhotoConsent"] = "तस्वीर सहमति",
            ["Privacy.DataPolicy"] = "डेटा सुरक्षा नीति",
            ["Privacy.GuardianAccess"] = "अधिकृत अभिभावक",
            ["Privacy.ContactOfficer"] = "डेटा सुरक्षा अधिकारी से संपर्क करें"
        },

        ["kn"] = new(StringComparer.OrdinalIgnoreCase)
        {
            // Navigation
            ["Nav.Today"] = "ಇಂದು",
            ["Nav.Journey"] = "ಪ್ರಯಾಣ",
            ["Nav.Memories"] = "ನೆನಪುಗಳು",
            ["Nav.Messages"] = "ಸಂದೇಶಗಳು",
            ["Nav.Family"] = "ಕುಟುಂಬ ಮತ್ತು ಆರೈಕೆ",
            ["Nav.Actions"] = "ದೈನಂದಿನ ಕ್ರಿಯೆಗಳು",
            ["Nav.TrustCenter"] = "ಗೌಪ್ಯತೆ ಮತ್ತು ನಂಬಿಕೆ",

            // Daily Summary & Care
            ["Today.Title"] = "ಇಂದಿನ ಸಾರಾಂಶ",
            ["Today.Subtitle"] = "ನೈಜ-ಸಮಯದ ನವೀಕರಣಗಳು, ಕಲಿಕೆಯ ಕ್ಷಣಗಳು ಮತ್ತು ದೈನಂದಿನ ಆರೈಕೆ.",
            ["Today.NotUpdatedYet"] = "ಇಂದು ಇನ್ನೂ ನವೀಕರಿಸಲಾಗಿಲ್ಲ. ಶಿಕ್ಷಕರು ದಾಖಲಿಸಿದ ನಂತರ ಆರೈಕೆ ಟಿಪ್ಪಣಿಗಳು ಇಲ್ಲಿ ಗೋಚರಿಸುತ್ತವೆ.",
            ["Today.Meals"] = "ಊಟ ಮತ್ತು ಉಪಹಾರ",
            ["Today.Naps"] = "ನಿದ್ರೆ ಮತ್ತು ವಿಶ್ರಾಂತಿ",
            ["Today.Diapers"] = "ನೈರ್ಮಲ್ಯ",
            ["Today.Comfort"] = "ಮನಸ್ಥಿತಿ ಮತ್ತು ಸಮಾಧಾನ",
            ["Today.LearningMoments"] = "ಇಂದಿನ ಕಲಿಕೆಯ ಕ್ಷಣಗಳು",
            ["Today.UpcomingEvents"] = "ಮುಂಬರುವ ಕಾರ್ಯಕ್ರಮಗಳು",
            ["Today.ActionRequired"] = "ಕ್ರಮ ಅಗತ್ಯವಿದೆ",
            ["Today.PendingConsent"] = "ಬಾಕಿ ಉಳಿದಿರುವ ಒಪ್ಪಿಗೆ ಪತ್ರ",
            ["Today.UnpaidInvoice"] = "ಶುಲ್ಕ ಬಾಕಿ ಇದೆ",
            ["Today.UnreadMessage"] = "ಹೊಸ ಶಿಕ್ಷಕರ ಸಂದೇಶ",

            // Actions & Forms
            ["Action.ReportAbsence"] = "ಗೈರುಹಾಜರಿ ವರದಿ ಮಾಡಿ",
            ["Action.AuthorizedPickups"] = "ಅಧಿಕೃತ ಪಿಕಪ್ ವ್ಯಕ್ತಿಗಳು",
            ["Action.ViewInvoices"] = "ಶುಲ್ಕ ವಿವರಗಳು",
            ["Action.ReviewConsent"] = "ಒಪ್ಪಿಗೆಯನ್ನು ಪರಿಶೀಲಿಸಿ",
            ["Action.QuickReplies"] = "ತ್ವರಿತ ಉತ್ತರಗಳು",
            ["Action.QuietHours"] = "ಶಾಂತ ಸಮಯ (8:30 AM – 3:30 PM). ತುರ್ತು ಅಲ್ಲದ ಸಂದೇಶಗಳನ್ನು ವಿಶ್ರಾಂತಿ ಸಮಯದಲ್ಲಿ ನೋಡಲಾಗುತ್ತದೆ.",
            ["Action.Close"] = "ಮುಚ್ಚಿ",
            ["Action.Submit"] = "ಸಲ್ಲಿಸಿ",
            ["Action.Saving"] = "ಸಲ್ಲಿಸಲಾಗುತ್ತಿದೆ...",

            // Statuses
            ["Status.NotRecorded"] = "ಇನ್ನೂ ದಾಖಲಾಗಿಲ್ಲ",
            ["Status.DidNotOccur"] = "ಇಂದು ಸಂಭವಿಸಿಲ್ಲ",
            ["Status.Verified"] = "ಪರಿಶೀಲಿಸಲಾಗಿದೆ",
            ["Status.PendingStaff"] = "ಸಿಬ್ಬಂದಿ ಪರಿಶೀಲನೆಯಲ್ಲಿದೆ",

            // Privacy
            ["Privacy.Title"] = "ಗೌಪ್ಯತೆ ಮತ್ತು ನಂಬಿಕೆ ಕೇಂದ್ರ",
            ["Privacy.PhotoConsent"] = "ಫೋಟೋ ಒಪ್ಪಿಗೆ",
            ["Privacy.DataPolicy"] = "ಡೇಟಾ ರಕ್ಷಣೆ ನೀತಿ",
            ["Privacy.GuardianAccess"] = "ಅಧಿಕೃತ ಪೋಷಕರು",
            ["Privacy.ContactOfficer"] = "ಗೌಪ್ಯತಾ ಅಧಿಕಾರಿಯನ್ನು ಸಂಪರ್ಕಿಸಿ"
        }
    };
}
