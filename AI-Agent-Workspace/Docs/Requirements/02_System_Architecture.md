# System Architecture

## 1. Overview

The system is designed as a serverless, composable architecture built entirely on the Cloudflare Developer Platform. It consists of three main components: a Blazor-based frontend, a Cloudflare Worker-powered backend, and a containerized offline data pipeline.

## 2. Technology Stack

| Layer | Technology | Role | Cost-Saving Rationale |
| :--- | :--- | :--- | :--- |
| **Frontend** | .NET Blazor Web App | Client-side UI Framework | Static SSR with interactive islands minimizes client-side load and improves performance. |
| **Hosting/CDN** | Cloudflare Pages | Hosts the static frontend application | Generous free tier for hosting static sites. |
| **Backend (API)** | Cloudflare Workers (TypeScript) | Serverless execution for the search API | 100,000 free requests per day; scales to zero. |
| **Relational DB & FTS** | Cloudflare D1 | Serverless SQLite for metadata and FTS5 | 5 million free reads per day; no server to manage. |
| **Vector Database** | Cloudflare Vectorize | Stores and searches game embeddings | Free tier for initial development and small-scale use. |
| **AI/Embeddings** | Cloudflare Workers AI | Provides embedding models (e.g., BGE) | Consistent, low-cost embeddings without managing models. |
| **Object Storage** | Cloudflare R2 | Stores backups and raw data assets | Zero egress fees, making it ideal for data transfer. |
| **Offline ETL Compute** | Cloudflare Containers (Python) | Runs the data ingestion and indexing pipeline | Pay-per-use compute for long-running tasks. |
| **ETL Orchestration** | Durable Objects & Cron Triggers | Schedules and coordinates the offline pipeline | Efficiently triggers the ETL process with minimal overhead. |
| **Graph Visualization** | D3.js (via Blazor JS Interop) | Renders the interactive related games graph | Leverages a powerful, open-source library for rich visualizations. |

## 3. Component Breakdown

### 3.1. Frontend: Blazor Web App

The frontend will be a .NET Blazor Web App hosted on Cloudflare Pages. It will utilize the "Static SSR with Islands of Interactivity" model to ensure a fast, SEO-friendly experience. Key interactive components, such as the search bar and graph visualization, will be rendered using Blazor's interactive render modes (`@rendermode InteractiveAuto`).

### 3.2. Backend: Cloudflare Worker API

The backend will be a TypeScript-based Cloudflare Worker that serves as the API for the frontend. It will handle all incoming search requests and orchestrate the hybrid search process. The Worker will have bindings to the D1 database, the Vectorize index, and the Workers AI service, allowing it to efficiently query and process data.

### 3.3. Offline Data Pipeline: Cloudflare Container

The ETL process will be encapsulated in a Python-based Docker container and run on Cloudflare Containers. This approach is ideal for the long-running, resource-intensive tasks of data ingestion and processing. The pipeline will be triggered on a schedule by a Cloudflare Worker with a Cron Trigger, which will use a Durable Object to manage the state of the ETL job.
