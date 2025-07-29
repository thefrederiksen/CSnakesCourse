# Photo Analysis Specifications for Large Language Model

## Overview

A structured routine for analyzing images via an OpenAI-compatible LLM. This routine extracts comprehensive metadata—technical, descriptive, contextual, and categorical—to enable efficient organization and search in a personal image library.

## Objectives

- **Automate** extraction of rich metadata from images.
- **Standardize** output using a shared Pydantic model schema.
- **Categorize** images into `screenshot`, `documentScan`, `photo`, `illustration`, `diagram`, `graphic`, or `other`.
- **Subcategorize** real-world photos (e.g. `landscape`, `portrait`, `food`, etc.).
- **Support** any OpenAI-compatible model endpoint (key, base URL, model name).

## Inputs

1. **Image file**: Provided as a local filename or path. The LLM sees only prompts, not raw binary.
2. **Settings**:
   - `api_key`: API credential for the LLM.
   - `api_base`: Base URL for the API (e.g. `https://api.openai.com/v1`).
   - `model`: Model identifier (e.g. `gpt-4o-mini`).

## Outputs

A JSON object matching the `ImageMetadata` Pydantic model:

```python
class ImageMetadata(BaseModel):
    file_name: str
    description: str
    imageCategory: ImageCategory
    photoSubcategory: Optional[PhotoSubcategory]
    screenshot: bool
    encodingFormat: Optional[str]
    width: Optional[int]
    height: Optional[int]
    orientation: Optional[str]
    colorSpace: Optional[str]
    dateTaken: Optional[datetime]
    timeOfDay: Optional[str]
    season: Optional[str]
    captureDevice: Optional[str]
    contentLocation: Optional[Place]
    primarySubjects: Optional[List[str]]
    secondarySubjects: Optional[List[str]]
    environment: Optional[List[str]]
    recreationGear: Optional[List[str]]
    imageProperties: Optional[List[str]]
    keywords: Optional[List[str]]
    extractionTimeUTC: datetime
    modelUsed: str
```

## Workflow

1. **Initialize** OpenAI client with provided settings.
2. **Generate** a `system` prompt embedding the full JSON schema via `ImageMetadata.schema_json()`.
3. **Issue** a `ChatCompletion.create` call:
   - `messages`: system prompt + a user prompt framing the image analysis task.
   - `temperature=0`, `top_p=1` for deterministic output.
4. **Parse** the LLM’s JSON response into the Pydantic `ImageMetadata` model.
5. **Return** the model instance for downstream use (e.g. database ingestion, search indexing).

## Prompt Design

- **System Prompt**: Establishes persona (expert in image analysis), outlines purpose (personal photo library organization), and provides exact JSON schema.
- **User Prompt**: Indicates a single image to analyze (content only, not binary) and instructs to fill all schema fields.

### Example Prompts

```text
SYSTEM: You are an expert in image analysis assisting a private user who wants to organize their personal image library. For each image, extract as much metadata as possible—technical details, descriptive tags, subjects, and categorization. Return EXACTLY one JSON object matching this Pydantic schema (no extra keys):
<insert schema JSON>

USER: The user has provided an image. Analyze its contents and metadata in detail. Include all fields in the schema.
```

## Categories & Subcategories

- **imageCategory**: `screenshot`, `documentScan`, `photo`, `illustration`, `diagram`, `graphic`, `other`
- **photoSubcategory** (if `photo`): `landscape`, `portrait`, `food`, `product`, `architecture`, `vehicle`, `event`, `artwork`, `animal`, `nature`, `people`, `selfie`, `group`, `macro`, `street`, `night`, `sports`, `abstract`, `other`

## Error Handling

- **Invalid JSON**: Raise parsing error with raw content for diagnostics.
- **Missing Keys**: Reject output and retry prompt or log warning.
- **API Failures**: Implement retries with exponential backoff.

## Extensibility

- Swap in any OpenAI-API-compatible backend by replacing the `openai` client methods.
- Extend `ImageCategory` or `PhotoSubcategory` enums as new use cases arise.
- Add custom fields (e.g. face recognition confidence) by updating the Pydantic model and prompts.

## Testing & Validation

- **Unit tests**: Mock `openai.ChatCompletion` responses and validate `ImageMetadata.parse_raw`.
- **Integration tests**: Sample images covering each category and subcategory.
- **Schema validation**: Ensure no extra keys and all required fields return.

---

*End of specification.*

