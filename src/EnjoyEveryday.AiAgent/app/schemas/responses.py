from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any

class IdeaResponse(BaseModel):
    suggestions: List[Dict[str, Any]]

class ImproveResponse(BaseModel):
    reason: str
    updatedData: Dict[str, Any]

class MaterialsResponse(BaseModel):
    materials: List[str]

class SupportingEvidenceCitation(BaseModel):
    sourceId: str
    sourceType: str
    title: Optional[str] = None
    summary: Optional[str] = None

class RecommendationItem(BaseModel):
    experienceId: str
    experienceTitle: str
    suggestedFormat: str = "Small Group"
    estimatedDurationMinutes: int = 20
    rationale: str
    supportingEvidence: List[SupportingEvidenceCitation] = Field(default_factory=list)
    materials: List[str] = Field(default_factory=list)
    isEvidenceLimited: bool = False

class RecommendationResponse(BaseModel):
    recommendations: List[Dict[str, Any]]

class JourneyInsightItem(BaseModel):
    category: str
    classification: str
    statement: str
    evidenceStrength: str = "Supported"
    supportingEvidence: List[SupportingEvidenceCitation] = Field(default_factory=list)
    suggestedExperienceId: Optional[str] = None
    suggestedExperienceTitle: Optional[str] = None
    rankOrder: int = 1

class JourneyInsightResponse(BaseModel):
    overallSummary: str
    evidenceGaps: Optional[str] = None
    items: List[Dict[str, Any]] = Field(default_factory=list)

class DailyPlanItem(BaseModel):
    dayOfWeek: str
    experienceId: str
    experienceTitle: str
    learningGoal: Optional[str] = None
    groupFormat: str = "Small Group"
    estimatedDurationMinutes: int = 25
    timeOfDay: str = "Morning"
    requiredMaterials: Optional[str] = None
    rationale: Optional[str] = None

class WeeklyPlanResponse(BaseModel):
    weekSummary: str
    dailyPlans: List[Dict[str, Any]] = Field(default_factory=list)

class ParentCommunicationResponse(BaseModel):
    title: str
    greeting: str
    highlights: str
    growthAndLearning: str
    familyConversationStarter: str
    signOff: str

class OrganizationIntelligenceItem(BaseModel):
    title: str
    category: str
    observation: str
    possibleExplanation: Optional[str] = None
    suggestedNextStep: Optional[str] = None
    dataLimitations: Optional[str] = None
