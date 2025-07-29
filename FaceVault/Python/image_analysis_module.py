from enum import Enum
from typing import Optional, List
from datetime import datetime
from pydantic import BaseModel, Field
import openai


# -----------------------------
# Schema Definitions
# -----------------------------
class ImageCategory(str, Enum):
    screenshot = "screenshot"
    document_scan = "documentScan"
    photo = "photo"
    illustration = "illustration"
    diagram = "diagram"
    graphic = "graphic"
    other = "other"

class PhotoSubcategory(str, Enum):
    landscape = "landscape"
    portrait = "portrait"
    food = "food"
    product = "product"
    architecture = "architecture"
    vehicle = "vehicle"
    event = "event"
    artwork = "artwork"
    animal = "animal"
    nature = "nature"
    people = "people"
    selfie = "selfie"
    group = "group"
    macro = "macro"
    street = "street"
    night = "night"
    sports = "sports"
    abstract = "abstract"
    other = "other"

class Place(BaseModel):
    name: Optional[str]
    description: Optional[str]
    latitude: Optional[float]
    longitude: Optional[float]

class ImageMetadata(BaseModel):
    file_name: str = Field(..., description="Name of the image file on disk")
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

class OpenAISettings(BaseModel):
    api_key: str
    api_base: str
    model: str


def extract_image_metadata(
    image_binary: bytes,
    file_name: str,
    settings: OpenAISettings,
) -> ImageMetadata:
    """
    Analyze an image and return structured metadata using an OpenAI-compatible model.

    :param image_binary: Raw image data (bytes)
    :param file_name: Filename to include in metadata
    :param settings: OpenAISettings with api_key, api_base, and model
    :return: ImageMetadata instance
    """
    # Configure OpenAI client
    openai.api_key = settings.api_key
    openai.api_base = settings.api_base

    # Prepare schema JSON for prompt
    schema_json = ImageMetadata.schema_json(indent=2)
    system_prompt = (
        "You are an expert in image analysis assisting a private user to organize their personal image library. "
        "Extract comprehensive metadata—technical details, descriptive tags, subjects, and categorization. "
        "Return EXACTLY one JSON object matching this Pydantic schema (no extra keys):\n" + schema_json
    )

    user_prompt = (
        f"Analyze the provided image in detail and fill all schema fields. "
        f"Include the file name '{file_name}'."
    )

    # Note: OpenAI's Python SDK currently doesn't accept direct binary in ChatCompletion; you must host the image or provide its description.
    # For demonstration, we assume the model has image processing capability.

    response = openai.ChatCompletion.create(
        model=settings.model,
        messages=[
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_prompt}
        ],
        temperature=0,
        top_p=1,
    )

    content = response.choices[0].message.content.strip()
    metadata = ImageMetadata.parse_raw(content)
    return metadata


# Example usage
def main():
    import os
    from pprint import pprint

    # Load image binary
    with open("example.jpg", "rb") as f:
        img_data = f.read()

    settings = OpenAISettings(
        api_key=os.getenv("OPENAI_API_KEY"),
        api_base="https://api.openai.com/v1",
        model="gpt-4o-mini"
    )

    result = extract_image_metadata(img_data, "example.jpg", settings)
    pprint(result.dict(by_alias=True, exclude_none=True))


if __name__ == "__main__":
    main()
