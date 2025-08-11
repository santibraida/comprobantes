# Logging Implementation with Serilog

This document describes the logging implementation in the File Content Renamer application.

## Overview

The application uses Serilog for structured logging. Logs are output to:

1. Console - for immediate feedback
2. Log files - for persistent storage and troubleshooting

## Log File Location

Log files are stored in the `logs` directory in the application root folder. Files are named using the pattern:

```plaintext
app_YYYYMMDD.log
```

Where YYYYMMDD is the date (e.g., app_20250808.log)

## Log Levels

The application uses the following log levels:

- **Verbose**: Detailed debugging information
- **Debug**: Information useful for debugging
- **Information**: General information about application progress
- **Warning**: Potential issues that don't prevent the application from working
- **Error**: Issues that prevent a specific operation from completing
- **Fatal**: Critical errors that may cause the application to terminate

## Log Format

Each log entry includes:

- Timestamp
- Log level
- Application name
- Machine name
- Username
- Message
- Exception details (if applicable)

## Debugging

For detailed troubleshooting, check the log files which contain complete information about application execution. The console output provides a simplified view focused on the most important information.

## Tesseract OCR Logging

Special attention is given to logging the interaction with the Tesseract OCR engine:

- Available languages
- Command execution
- Processing results
- Error conditions and recovery attempts

## Example Log Entries

```plaintext
2025-08-08 14:30:22.123 +00:00 [INF] [FileContentRenamer/MacBook/santibraida] Processing file: invoice.jpg
2025-08-08 14:30:23.456 +00:00 [DBG] [FileContentRenamer/MacBook/santibraida] Using processor AlternativeImageProcessor for file invoice.jpg
2025-08-08 14:30:25.789 +00:00 [WRN] [FileContentRenamer/MacBook/santibraida] Tesseract process failed with language 'eng': Cannot open lang model
2025-08-08 14:30:26.012 +00:00 [INF] [FileContentRenamer/MacBook/santibraida] Retry successful! Text extracted (preview): INVOICE #12345...
```
