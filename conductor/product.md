# SendToKindle for Jellyfin

## Vision
A Jellyfin plugin designed to allow users to seamlessly send ebooks and documents from their Jellyfin library directly to their Kindle email address. It integrates with Jellyfin's plugin architecture to extract ebook files (like EPUB or MOBI) and utilizes an SMTP service to forward the files to user-configured Kindle email addresses.

## Core Features
- **Format Support:** Supports standard Kindle formats including EPUB and MOBI.
- **Email Delivery:** Utilizes user-provided SMTP credentials configurable directly from the Jellyfin plugin settings, ensuring privacy and decentralization.
- **User Feedback:** Provides native Jellyfin UI notifications (toasts/alerts) to inform users of successful deliveries or errors, backed by detailed server logs for troubleshooting.

## Target Audience
Jellyfin users who manage their ebook libraries on the platform and want a frictionless way to read their collection on Amazon Kindle devices.
