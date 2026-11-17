from pydantic import BaseModel, Field, field_validator
from typing import List, Optional, Dict, Any

class TopExperienceDto(BaseModel):
    id: Optional[str] = None
    title: str
    description: Optional[str] = None
    category: Optional[str] = None

class IdeaRequest(BaseModel):
    idea: str = Field(..., min_length=2, max_length=1000, description="The starting concept or learning theme")
    ageGroup: Optional[str] = Field(default=None, description="Target age group e.g. 3-5 years, Toddler 18-36m")
    environment: Optional[str] = Field(default=None, description="Indoor, Outdoor, or Indoor / Outdoor")
    focusArea: Optional[str] = Field(default=None, description="Pedagogical focus area e.g. Sensory, Motor, Language, STEM")
    availableMaterials: List[str] = Field(default_factory=list, description="Available classroom materials to incorporate")
    topExperiences: List[TopExperienceDto] = Field(default_factory=list, description="Reference top-performing tenant experiences")

class ImproveRequest(BaseModel):
    experienceDna: Dict[str, Any] = Field(..., description="The current Experience DNA representation")
    requestContext: str = Field(..., min_length=2, max_length=1000, description="The requested improvement")

class MaterialsRequest(BaseModel):
    experienceDna: Dict[str, Any] = Field(..., description="The current Experience DNA representation")

class InSessionPivotRequest(BaseModel):
    experienceTitle: str
    situation: str
    materials: Optional[str] = None

class RecommendationCandidateDto(BaseModel):
    id: str
    title: str
    description: Optional[str] = None
    category: Optional[str] = None
    ageRange: Optional[str] = None
    materials: List[str] = Field(default_factory=list)

class EvidenceMemoryItemDto(BaseModel):
    id: str
    sourceId: str
    sourceType: str
    eventType: str
    title: str
    description: Optional[str] = None
    eventDate: Optional[str] = None

class RecommendationPreferencesDto(BaseModel):
    preferredDurationMinutes: Optional[int] = Field(default=None, ge=5, le=120)
    environment: Optional[str] = None
    format: Optional[str] = None
    category: Optional[str] = None
    additionalNotes: Optional[str] = None

class RecommendationRequest(BaseModel):
    scope: str = Field("Classroom", description="'Classroom' or 'Child'")
    targetName: str = Field(..., min_length=1, max_length=200)
    candidateExperiences: List[RecommendationCandidateDto] = Field(default_factory=list)
    evidenceMemories: List[EvidenceMemoryItemDto] = Field(default_factory=list)
    preferences: Optional[RecommendationPreferencesDto] = None

class JourneyInsightRequest(BaseModel):
    childId: str
    childName: str
    periodType: str = "Last30Days"
    periodStartDate: str
    periodEndDate: str
    evidenceMemories: List[EvidenceMemoryItemDto] = Field(default_factory=list)
    candidateExperiences: List[RecommendationCandidateDto] = Field(default_factory=list)

class WeeklyPlanPreferencesDto(BaseModel):
    focusAreas: Optional[str] = None
    indoorOutdoorPreference: str = "Balanced"
    materialsNotes: Optional[str] = None

class WeeklyPlanRequest(BaseModel):
    classroomId: str
    classroomName: str
    ageGroup: Optional[str] = None
    weekStartDate: str
    weekEndDate: str
    candidateExperiences: List[RecommendationCandidateDto] = Field(default_factory=list)
    recentExperiences: List[str] = Field(default_factory=list)
    preferences: Optional[WeeklyPlanPreferencesDto] = None

class ParentCommunicationRequest(BaseModel):
    childName: str
    classroomName: str = "Classroom"
    recipientName: str = "Parent"
    recipientRelationship: str = "Parent"
    communicationType: str = "DailyUpdate"
    reportingPeriod: str = "Today"
    attendanceSummary: str = ""
    completedExperiences: List[str] = Field(default_factory=list)
    approvedMoments: List[str] = Field(default_factory=list)
    careHighlights: List[str] = Field(default_factory=list)
    milestoneHighlights: List[str] = Field(default_factory=list)
    relevantEvents: List[str] = Field(default_factory=list)
    teacherNotes: str = ""

class OrganizationIntelligenceRequest(BaseModel):
    scopeName: str = "Organization"
    reportingPeriod: str = "Last7Days"
    attendanceRate: float = 100.0
    planningCoverage: float = 0.0
    completedExperiences: int = 0
    upcomingExperiences: int = 0
    uniqueExperiences: int = 0
    varietyDiversityRate: float = 100.0
    domainBreakdown: Dict[str, int] = Field(default_factory=dict)
    classroomsNeedingPlansCount: int = 0
    classroomsNeedingPlans: List[str] = Field(default_factory=list)
    missingDeparturesCount: int = 0
    pendingDraftsCount: int = 0
    dataCompletenessScore: float = 0.0
