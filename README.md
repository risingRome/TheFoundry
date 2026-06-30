# The Foundry

A Business Intelligence Platform built with ASP.NET Core MVC, SQL Server, Entity Framework Core, Chart.js, and Bootstrap.

---

## Overview

The Foundry is a full-stack Business Intelligence platform that enables organizations to upload business datasets, automatically profile and analyze data, generate dashboards, create executive reports, and interact with a rule-based Executive Assistant.

The platform supports role-based access control, dashboard persistence, reporting, filtering, and business intelligence recommendations.

---

## Features

### Dataset Management
- CSV Upload
- Excel Upload (.xlsx)
- Dataset Registry
- Dataset Validation
- Domain Classification
- Dataset Profiling

### Data Exploration
- Dataset Preview
- Schema Explorer
- Column Statistics
- Data Quality Metrics

### Business Intelligence Engine
- Business Domain Detection
- KPI Detection
- Insight Generation
- Recommendation Engine
- Trend Analysis

### Generated Dashboards
- Automatic Dashboard Generation
- KPI Cards
- Trend Charts
- Comparison Charts
- Interactive Filters
- Dashboard Persistence

### Dashboard Library
- Save Dashboard
- Rename Dashboard
- Archive Dashboard
- Restore Dashboard
- Dashboard Catalog

### Executive Reports
- Executive Summary
- KPI Analysis
- Trend Analysis
- Business Recommendations
- PDF Export
- DOCX Export

### Executive Assistant
- Dataset Summaries
- KPI Explanations
- Business Insights
- Dashboard Explanations
- Recommendation Queries
- Conversation History Persistence

### Security & RBAC
- ASP.NET Identity
- Admin
- Analyst
- Executive
- Viewer

---

## Technology Stack

### Backend
- ASP.NET Core MVC
- C#
- Entity Framework Core

### Database
- SQL Server

### Frontend
- Razor Views
- Bootstrap 5
- Chart.js

### Authentication
- ASP.NET Identity

---

## Architecture

```text
User
 │
 ▼
Upload Dataset
 │
 ▼
Dataset Profiling
 │
 ▼
Business Intelligence Engine
 │
 ├── Insights
 ├── KPI Detection
 ├── Recommendations
 │
 ▼
Generated Dashboard
 │
 ├── Filters
 ├── Charts
 ├── KPI Cards
 │
 ▼
Executive Reports
 │
 ▼
Executive Assistant
```

---

## Screenshots

Add screenshots here:

- Login Page
- Dataset Registry
- Insights
- Generated Dashboard
- Dashboard Library
- Reports
- Executive Assistant

---

## Demo Accounts

| Role | Email |
|--------|--------|
| Admin | admin@foundry.local |
| Analyst | analyst@foundry.local |
| Executive | executive@foundry.local |
| Viewer | viewer@foundry.local |

Password:

```text
Foundry!234
```

---

## Local Setup

```powershell
git clone <repository-url>

cd OpsDashboardMvp

dotnet restore

dotnet run --project src/OpsDashboard.Web
```

---

## Current Version

```text
The Foundry v1.9
```

Implemented:

- Dataset Management
- Dataset Profiling
- Business Intelligence Engine
- Generated Dashboards
- Interactive Analytics
- Dashboard Library
- Executive Reports
- Executive Assistant
- Authentication & RBAC

---

## Future Roadmap

### v2.0
- Azure Deployment
- Production Hosting
- Public Demo Environment

### v2.1
- AI-Powered Executive Assistant
- Natural Language Querying

---

## Author

Rahul Kumar Yadav

Computer Engineering Graduate  
Data Analytics & Business Intelligence Enthusiast