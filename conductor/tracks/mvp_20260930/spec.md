# Specification: Initial Implementation / MVP

## Overview
This track implements the MVP of the SendToKindle Jellyfin plugin. It allows users to configure SMTP credentials and a target Kindle email address, and provides a UI integration to send supported ebooks (EPUB, MOBI) directly to their Kindle.

## Functional Requirements
1. **Plugin Configuration:**
   - Define a configuration class for the plugin to store SMTP server address, port, username, password, and the target Kindle email address.
   - Register the configuration with the Jellyfin Plugin API so it can be managed via the Jellyfin dashboard.
2. **Book Extraction:**
   - Identify when a user triggers the "Send to Kindle" action on a specific media item.
   - Verify that the item is a Book/Document.
   - Scan the item's underlying files to find a supported format (EPUB or MOBI). If both exist, prioritize EPUB.
3. **Email Delivery:**
   - Implement an SMTP client that constructs an email with the extracted ebook file as an attachment.
   - Send the email using the configured SMTP credentials to the target Kindle email address.
4. **UI Integration:**
   - Inject a "Send to Kindle" option into the context menu for Book/Document items in the Jellyfin web interface.
   - Inject a "Send to Kindle" button on the item detail page for Books/Documents.

## Non-Functional Requirements
- **Logging:** All SMTP connection attempts, successful sends, and errors (e.g., unsupported format, SMTP failure) must be logged to the Jellyfin server log.
- **Error Handling:** The plugin must gracefully handle errors (e.g., network issues, invalid credentials) without crashing the server.

## Acceptance Criteria
- A user can install the plugin and see its configuration page in the Jellyfin dashboard.
- A user can enter and save their SMTP credentials and Kindle email address.
- A user sees the "Send to Kindle" option on book items in the Jellyfin UI.
- Clicking "Send to Kindle" successfully emails an EPUB or MOBI file to the configured address.
- If a book only has a PDF or AZW3 format, the plugin logs an error and does not attempt to send it (as per current format constraints).

## Out of Scope
- Automatic conversion of unsupported formats (e.g., converting PDF to EPUB).
- Support for multiple target Kindle email addresses per user (MVP will use a single global target address).
