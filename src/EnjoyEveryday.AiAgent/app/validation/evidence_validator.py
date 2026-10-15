from typing import List, Set, Dict, Any, Optional

class EvidenceValidator:
    @staticmethod
    def validate_candidate_references(
        recommended_items: List[Dict[str, Any]],
        candidate_experiences: List[Dict[str, Any]],
        id_field: str = "experienceId",
        title_field: str = "experienceTitle"
    ) -> List[Dict[str, Any]]:
        """Ensures that all recommended experience IDs exist in candidate list and matches their authoritative title."""
        candidate_map = {str(c.get("id")): c.get("title") for c in candidate_experiences if c.get("id")}
        validated = []

        for item in recommended_items:
            if not isinstance(item, dict):
                continue
            exp_id = str(item.get(id_field, ""))
            if exp_id in candidate_map:
                # Overwrite/guarantee title matches authoritative catalog
                item[title_field] = candidate_map[exp_id]
                validated.append(item)

        return validated

    @staticmethod
    def validate_evidence_citations(
        supporting_evidence: List[Dict[str, Any]],
        evidence_memories: List[Dict[str, Any]]
    ) -> List[Dict[str, Any]]:
        """Validates that cited evidence source IDs actually exist in authorized memories."""
        authorized_source_ids = {str(m.get("sourceId")) for m in evidence_memories if m.get("sourceId")}
        authorized_memory_ids = {str(m.get("id")) for m in evidence_memories if m.get("id")}
        all_authorized = authorized_source_ids | authorized_memory_ids

        validated_citations = []
        for cite in supporting_evidence:
            if not isinstance(cite, dict):
                continue
            s_id = str(cite.get("sourceId", ""))
            if s_id in all_authorized:
                validated_citations.append(cite)

        return validated_citations

evidence_validator = EvidenceValidator()
