# Application and API

## 1. Frontend Application (.NET Blazor)

The frontend will be a .NET Blazor Web App hosted on Cloudflare Pages. It will utilize the "Static SSR with Islands of Interactivity" model to ensure a fast, SEO-friendly experience.

### 1.1. User Interface

*   **Search Interface:** A clean and intuitive search interface with a text input bar, faceted filtering controls (e.g., Genre, Year), and support for multimodal inputs (text and image).
*   **Results Display:** A responsive grid/list view for displaying search results. Blazor's `<Virtualize>` component will be used for efficient scrolling of large result sets.
*   **Graph Visualization:** An interactive, force-directed graph visualization for exploring related games. This will be implemented using D3.js, controlled via Blazor's JS Interop capabilities.

### 1.2. Key Features

*   **Hybrid Search:** A single search box that seamlessly handles both keyword and semantic queries.
*   **Faceted Filtering:** The ability to filter search results by various criteria (e.g., genre, release year, platform).
*   **"More Like This" Recommendations:** A feature on game detail pages that allows users to find semantically similar games.
*   **Interactive Graph Exploration:** A visual way to navigate the relationships between games.

## 2. Backend API (Cloudflare Worker)

The backend API will be implemented as a TypeScript-based Cloudflare Worker.

### 2.1. Endpoints

*   `POST /api/search`: The primary endpoint for handling search requests.
*   `GET /api/games/{id}/related`: An endpoint for retrieving pre-computed nearest neighbors for a given game, used to power the graph visualization.

### 2.2. Hybrid Search Algorithm

The core of the backend API will be the hybrid search algorithm, which will be executed in the following steps:

1.  **Input Processing:** Sanitize the user's query and parse any filters.
2.  **Real-time Embedding:** Use the Workers AI binding to embed the user's query string into a vector.
3.  **Stage 1 (D1 Filtering):**
    *   Construct and execute a dynamic SQL query against the D1 database.
    *   Apply `WHERE` clauses for structured filters and `MATCH` against the `games_fts` table for keywords.
    *   Retrieve a list of candidate `game_id`s.
4.  **Stage 2 (Vectorize Semantic Ranking):**
    *   Execute a vector similarity search against the Vectorize index using the query embedding.
    *   Apply a filter to restrict the search to the `game_id`s retrieved from Stage 1.
5.  **Ranking and Fusion:**
    *   Rank the results based on their semantic similarity scores.
    *   If both keyword and semantic results are present, use a fusion algorithm (e.g., Reciprocal Rank Fusion) to combine the rankings.
6.  **Hydration and Response:**
    *   Fetch the full metadata for the top N results from the D1 database.
    *   Return the ranked and hydrated results to the frontend.
