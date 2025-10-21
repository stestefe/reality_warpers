import base64
from openai import OpenAI
from PIL import Image
import json
import sys
import re
import io

client = OpenAI()

def encode_image(image_path):
    with open(image_path, "rb") as f:
        return base64.b64encode(f.read()).decode("utf-8")

def extract_items_from_image(image_path):
    image_base64 = encode_image(image_path)

    prompt = (
        "Look at this image and identify up to 5 distinct items or objects. "
        "Return ONLY a JSON array of simple English nouns, all lowercase. "
        "Example: [\"person\", \"bicycle\", \"tree\"]"
    )

    response = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[
            {"role": "user", "content": [
                {"type": "text", "text": prompt},
                {"type": "image_url", "image_url": {
                    "url": f"data:image/jpeg;base64,{image_base64}"
                }}
            ]}
        ],
        max_tokens=300
    )

    raw_output = response.choices[0].message.content.strip()
    raw_output = re.sub(r"^```(json)?|```$", "", raw_output.strip())
    try:
        items = json.loads(raw_output)
        if not isinstance(items, list):
            raise ValueError
    except Exception:
        print(raw_output)
        return []

    return items

if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(1)

    image_path = sys.argv[1]
    items = extract_items_from_image(image_path)
    print("items:", items)
