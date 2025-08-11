#!/bin/bash

echo "Setting up comprobantes tool environment..."

# Check if tesseract is installed and functioning
echo "Checking Tesseract installation..."
if ! command -v tesseract &> /dev/null; then
    echo "Error: Tesseract is not installed or not in PATH"
    echo "Please install Tesseract using: brew install tesseract"
    exit 1
fi

# Check tesseract version
TESSERACT_VERSION=$(tesseract --version 2>&1 | head -n 1)
echo "Detected $TESSERACT_VERSION"

# Check available languages
TESSERACT_LANGS=$(tesseract --list-langs 2>/dev/null)
if [[ $TESSERACT_LANGS == *"eng"* && $TESSERACT_LANGS == *"spa"* ]]; then
    echo "Required languages (eng, spa) are installed."
else
    echo "Warning: Required languages (eng, spa) may not be installed."
    echo "Available languages: $TESSERACT_LANGS"
    echo "You may need to install language data: brew install tesseract-lang"
fi

# No native library path setup needed; using Tesseract CLI only.

# Ensure local tessdata directory exists
TESSDATA_DIR="./tessdata"
mkdir -p "$TESSDATA_DIR"

# Get system tessdata directory
SYSTEM_TESSDATA_DIR=$(tesseract --list-params 2>&1 | grep -oE "datapath[[:space:]]+[[:alnum:]/]+" | awk '{print $2}')

if [ -z "$SYSTEM_TESSDATA_DIR" ]; then
    # Try common locations
    for loc in "/opt/homebrew/share/tessdata" "/usr/local/share/tessdata" "/usr/share/tessdata"; do
        if [ -d "$loc" ]; then
            SYSTEM_TESSDATA_DIR="$loc"
            break
        fi
    done
fi

if [ -n "$SYSTEM_TESSDATA_DIR" ]; then
    echo "System tessdata directory found: $SYSTEM_TESSDATA_DIR"
else
    echo "Warning: Could not locate system tessdata directory"
fi

# Check if tessdata directory exists and contains required files
TESSDATA_DIR="./tessdata"
mkdir -p "$TESSDATA_DIR"
# Check and copy required language files (eng and spa)
for lang in "eng" "spa"; do
    lang_file="${TESSDATA_DIR}/${lang}.traineddata"
    
    if [ ! -f "$lang_file" ]; then
        echo "Language file $lang.traineddata not found in local tessdata directory"
        
        # Copy from system directory if available
        if [ -n "$SYSTEM_TESSDATA_DIR" ] && [ -f "${SYSTEM_TESSDATA_DIR}/${lang}.traineddata" ]; then
            echo "Copying $lang.traineddata from system directory"
            cp "${SYSTEM_TESSDATA_DIR}/${lang}.traineddata" "$lang_file"
            
        # Try to download if not found in system
        elif command -v curl &> /dev/null; then
            echo "Downloading $lang.traineddata from GitHub..."
            curl -s -L "https://github.com/tesseract-ocr/tessdata/raw/main/${lang}.traineddata" -o "$lang_file"
        fi
        
        # Verify copy/download success
        if [ -f "$lang_file" ]; then
            echo "Successfully added $lang.traineddata to local tessdata directory"
        else
            echo "Warning: Failed to add $lang.traineddata. OCR functionality may be limited."
        fi
    else
        echo "$lang.traineddata is already present in local tessdata directory"
    fi
done

# Show tessdata contents briefly
echo "Local tessdata directory contents (top-level):"
ls -l "$TESSDATA_DIR" | head -n 20 || true

# Create logs directory and clean old logs
mkdir -p logs
echo "Created logs directory"

# Remove logs older than 30 days
find logs -name "*.log" -type f -mtime +30 -exec rm {} \;
echo "Cleaned up old log files"

# Clear any stale LastUsedPath from the config
# sed -i.bak -e 's/"LastUsedPath": "[^"]*"/"LastUsedPath": ""/' appsettings.json 2>/dev/null || true
# rm -f appsettings.json.bak 2>/dev/null || true

# Copy appsettings.json to output directory if it exists
if [ -f "appsettings.json" ]; then
    mkdir -p FileContentRenamer/bin/Debug/net9.0
    cp -f appsettings.json FileContentRenamer/bin/Debug/net9.0/appsettings.json
    echo "Configuration file copied to output directory"
fi

# Run the application
echo "Starting comprobantes tool..."
dotnet run --project FileContentRenamer/FileContentRenamer.csproj "$@"

# Copy back any updated appsettings.json from the output directory
if [ -f "FileContentRenamer/bin/Debug/net9.0/appsettings.json" ]; then
    cp -f FileContentRenamer/bin/Debug/net9.0/appsettings.json appsettings.json
    echo "Updated configuration file copied back from output directory"
fi

# Show log location
echo "Log files are available in: $(pwd)/logs/"
ls -la logs/
