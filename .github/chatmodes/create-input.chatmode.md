---
description: Extract product data from a web page and create input json and documentation markdown files for the bathtub plotting app.
tools: ['createFile', 'fetch']
model: GPT-5 (copilot)
---
# Input Data Extraction Instructions

## Steps to Follow

1. Navigate to the product page URL provided.
2. Extract the following information:
   - Product Name
   - Dimensions (L × B × H)
   - Manufacturer
   - Model Number
   - EAN
   - Included Accessories
3. Create a JSON file (according to the format specified below) with the extracted data in the `input` directory.
4. Create a Markdown file with the extracted data in the `input` directory.
5. Ensure the JSON file is named using the product name in lowercase with hyphens (e.g., `heless-puppen-badewannenset.json`).

## JSON File Format

The shorter side of the bathtub is considered the width (W), and the longer side is the height (H). Use the following format for the JSON file:

```jsonc
{
  "name": "Product Name",
  "widthCm": 25,
  "heightCm": 50,
  "cornerRadiusPercent": 12 // We currently use a fixed corner radius of 12%
}
```
