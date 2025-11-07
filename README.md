# Comprobantes - File Content Renamer Tool

This tool allows you to automatically rename files based on their content. It uses OCR (Optical Character Recognition) to extract text from images and PDFs, and then generates file names based on the content.

## Features

- Scans directories for specified file types
- Extracts text content from:
  - PDF files
  - Text files
  - Images (using OCR with Tesseract)
- Intelligently renames files based on detected patterns:
  - Service names (aysa, edenor, metrogas, etc.)
  - Dates from the document
  - Payment methods (santander, galicia, etc.)
- Creates filenames in the format: `service_date_paymentmethod`
- Comprehensive logging with Serilog

## Prerequisites

- .NET 6 SDK or later
- Tesseract OCR engine
- Leptonica library

## Installation

### macOS

1. Install Homebrew if not already installed:

   ```bash
   /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
   ```

2. Install Tesseract OCR and dependencies:

   ```bash
   brew install tesseract leptonica
   ```

3. Run the setup script (creates necessary symbolic links and configures the environment):

   ```bash
   ./run.sh
   ```

### Windows

1. Download and install Tesseract from the UB Mannheim GitHub repository:
   [https://github.com/UB-Mannheim/tesseract/wiki](https://github.com/UB-Mannheim/tesseract/wiki)

2. Make sure to add Tesseract to your PATH.

### Linux

Install using apt:

```bash
sudo apt-get install tesseract-ocr
```

## Usage

Use the run.sh script to start the application:

```bash
./run.sh [directory_path]
```

If you don't specify a directory path, the application will prompt you to enter one.

The application will:

1. Scan the specified directory for files with supported extensions (.pdf, .txt, .jpg, .png, .jpeg)
2. Extract text content using OCR (for images) or direct text extraction (for PDFs and text files)
3. Generate meaningful filenames based on the content
4. Rename the files

## How it Works

1. The application scans the specified directory for supported file types
2. For each file:
   - Text is extracted from the file using the appropriate method
   - The application looks for common patterns in the text (service names, dates, payment methods)
   - A new filename is generated using the pattern: `service_date_paymentmethod`
   - The file is renamed using the new filename

## Configuration

The application uses a configuration file (`appsettings.json`) for customizable settings. The configuration file should be located at the solution root directory.

You can configure:

- File extensions to process
- Default directory paths
- Tesseract OCR settings
- Logging behavior

Example configuration:

```json
{
  "AppConfig": {
    "BasePath": ".",
    "FileExtensions": [".pdf", ".txt", ".jpg", ".png", ".jpeg"],
    "IncludeSubdirectories": true,
    "TesseractDataPath": "tessdata",
    "TesseractLanguage": "eng+spa"
  }
}
```

## Logging

The application uses Serilog for structured logging. Logs are written to:

- Console (for immediate feedback)
- Log files in the `logs` directory (for detailed troubleshooting)

See [LOGGING.md](LOGGING.md) for more information about the logging system.

## Testing & Code Coverage

The project includes comprehensive unit tests with 223 passing tests.

To run tests with code coverage:

```bash
./coverage.sh
```

See [COVERAGE.md](COVERAGE.md) for detailed information about:

- Running coverage analysis
- Generating HTML reports
- Understanding coverage metrics
- CI/CD integration

## Troubleshooting

If you encounter issues with Leptonica or Tesseract libraries:

1. Verify that Tesseract is installed and in your PATH:

   ```bash
   tesseract --version
   ```

2. Check if the necessary language files are installed in the tessdata directory:

   ```bash
   ls -la tessdata/
   ```

3. If language files are missing, you can download them from:
   [https://github.com/tesseract-ocr/tessdata](https://github.com/tesseract-ocr/tessdata)

4. Check the log files in the `logs` directory for detailed information about any errors.

## Alternative Processing

If the standard image processing doesn't work, the application will fall back to using the command-line version of Tesseract, which might be more reliable in some cases.

## Customization

You can modify the `AppConfig.cs` file to customize:

- Supported file extensions
- Tesseract language configuration
- Other application settings
