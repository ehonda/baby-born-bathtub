---
description: Extract product data from a web page and create input json and documentation markdown files for the bathtub plotting app.
tools: ['createFile', 'editFiles', 'fetch']
model: GPT-5 (copilot)
---
# Input Data Extraction Instructions

## Steps to Follow

1. Navigate to the product page URL provided.
2. Create a JSON file (according to the format specified below) with the extracted dimensional data in the `input` directory.
3. Create a Markdown file with a summary of the extracted data in the `products` directory.
4. Create a common file name for both files based on the product name, converting it to kebab-case.

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

## Warnings

* Keep the file name short.
* Do not create any other files.
* Fix any linting errors in the generated files.
