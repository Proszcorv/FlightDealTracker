# Flight Deal Tracker ✈️
An automated serverless application built with C# and Azure Functions that monitors and alerts on cheap flight deals.

## Features
- **Automated Scanning:** Runs on a daily schedule using Azure TimerTrigger.
- **Global Reach:** Scans European and international destinations using external APIs.
- **Smart Filtering:** Automatically filters flights under a specific price threshold.
- **Email Notifications:** Sends formatted daily reports with direct booking links via SMTP.

## Tech Stack
- C# / .NET 8 (Isolated Worker Model)
- Microsoft Azure Functions
- RESTful APIs & JSON Deserialization

## Update:
- **Targeted Search:** Only searches for specific routes where I would like to go and notifies me if the ticket is cheaper than before.
