UPDATE experiences
SET dna_payload = dna_payload || '{
    "Challenge": "Can you find three different natural textures on our outdoor nature walk?",
    "Create": "A nature collage displaying the natural leaves and twigs collected on our walk.",
    "Together": "Children work in pairs to compare their nature discoveries and describe textures to each other.",
    "Environment": "Outdoor",
    "Materials": ["Magnifying glass", "Cloth pouches", "Nature card"],
    "DevelopmentalGoals": ["Tactile exploration and sensory vocabulary", "Outdoor gross motor navigation", "Peer observation and descriptive sharing"],
    "ExecutionGuide": {
        "Adaptations": ["Provide magnifying viewers with large handles for easy grip", "Pair children with a sensory buddy"]
    }
}'::jsonb
WHERE title = 'Forest Sensory Scavenger Hunt';
