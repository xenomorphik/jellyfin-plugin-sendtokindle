# Implementation Plan: MVP SendToKindle

## Phase 1: Setup & Configuration Model
- [x] Task: Set up the basic C# class library project for the Jellyfin plugin.
- [x] Task: Define the `PluginConfiguration` class.
  - [x] Write unit tests for configuration serialization/deserialization.
  - [x] Implement properties: SmtpServer, SmtpPort, SmtpUsername, SmtpPassword, TargetKindleEmail.
- [x] Task: Implement the main `Plugin` class inheriting from `BasePlugin<PluginConfiguration>`.
  - [x] Write tests ensuring plugin ID, Name, and Description are set correctly.
  - [x] Implement the `Plugin` class.
- [x] Task: Phase Verification & Checkpoint (Refer to workflow.md) [checkpoint: 9027be7]

## Phase 2: Email Delivery Service (SMTP)
- [x] Task: Create `ISmtpDeliveryService` interface.
- [x] Task: Implement `SmtpDeliveryService`.
  - [x] Write unit tests (mocking SMTP) to verify email construction (subject, body, attachments) and configuration usage.
  - [x] Implement the service using `MailKit` or standard .NET SMTP clients to send emails with attachments.
  - [x] Add robust error handling and logging for connection failures or invalid credentials.
- [x] Task: Phase Verification & Checkpoint (Refer to workflow.md) [checkpoint: 137e47f]

## Phase 3: Book Extraction & Core Logic
- [ ] Task: Create a service to handle the "Send to Kindle" action.
  - [ ] Write unit tests for the core logic: Verify it only processes Book/Document items.
  - [ ] Write unit tests for format selection: Ensure it prioritizes EPUB, then MOBI, and fails for unsupported formats (PDF, AZW3).
  - [ ] Implement the logic: Check item type, find the physical file path of the prioritized format, and pass it to `ISmtpDeliveryService`.
  - [ ] Add logging for successful extractions and format errors.
- [ ] Task: Phase Verification & Checkpoint (Refer to workflow.md)

## Phase 4: UI Integration
- [ ] Task: Inject the "Send to Kindle" action into the Jellyfin Web UI.
  - [ ] Create the necessary JavaScript/HTML patches or custom endpoints to add the context menu item.
  - [ ] Create the necessary JavaScript/HTML patches to add the button to the item detail page.
  - [ ] Wire the UI elements to call the plugin's backend service (Phase 3).
  - [ ] Implement UI feedback (toasts) based on the backend response.
- [ ] Task: Phase Verification & Checkpoint (Refer to workflow.md)
