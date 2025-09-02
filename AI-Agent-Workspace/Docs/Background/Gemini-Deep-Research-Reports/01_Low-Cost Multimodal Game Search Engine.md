

# **A Strategic Blueprint for an Ultra-Low-Cost Multimodal Game Search Engine**

---

### **User Prompt**

I have a Bioinformatics Rig custom-build desktop PC in my home that carries 128GB of RAM, a few TB of storage, and an nVidia RTX 3090\. I have, in the past, attempted to create a semantic search engine of games primarily from public steam review data (I am a developer of two published Steam games and have API access and follow the API access agreement). The project became too expensive with Zilliz and I'd like to see how much I can accomplish on my local machine. I'm interested in trying again, but with the \*\*multimodal embeddings\*\* that can be constructed against reviews \*\*and game screenshots\*\*.

This time, I need to seriously seriously reduce/optimize 2 costs:

\- Embeddings generation (preferring open source embedding models, higher accuracy, lower cost, and either Azure, Cloudflare, or the container itself running the embeddings)

\- DB/search (Would Azure Search Service, Cosmos DB, or some other lower-tech mechanism be more cost effective than the 300/month Zilliz bill I used to incur? DiskANN on Cosmos is quite attractive and probably the prefered route for comfort and simplicity, but I also quite like Cloudflare or options which just use LibSQL or Sqlite and run the whole dang search on the container itself).

I would expect to hold no more than 200 high-quality reviews for each game and no more than 10 screenshots.

It is possible that clever creation of our object models (i.e. NoSQL JSON payload for a document DB) could be constructed in a clever way to contain many smaller elements inside, writing a single embedding for, say, all reviews of a single game and its description. Consider ways to reduce costs with programmatic options.

My dream is to create a website which lets users enter text, paste images, and link to other existing (Steam) games to help them find their dream game. Attached is a bunch of context from the time I last tried this, back in 2023 (it's not August 31, 2025). The technology has come a long way since and costs and processes have become much simpler.

If I could build my application with dotnet Aspire and run it in a container, that would be preferable. If I need python, I could be convinced to use it. I'm also open to using workers/azure function apps as a method to do some offline work (i.e. hydrating a DB periodically with embeddings and reviews).

Help me serve genuine honest true game search to the masses at \_extraordinarily\_ low cost, so that I don't have to ever pull the plug on it. Indeed, you could perhaps attempt to price this against the expected revenue, of, say, a single google banner ad. We want to create a game search that pays for itself with a single google sidebar or banner ad. How can we deliver this?

Oh, and we can absolutely use my bioinformatics rig to do offline processing jobs as well. If me generating a giant-ass CSV or JSON blob on my rig and then uploading it to some bucket storage somewhere is the right move, I'm up for it. Just research and think deeply about our options.

---

### **Executive Summary: The Blueprint for an Ultra-Low-Cost Multimodal Search Engine**

The challenge of building powerful, AI-driven applications on an independent developer's budget is significant, often leading to projects being abandoned due to escalating operational costs. This report provides a definitive, actionable strategy to construct a sophisticated, multimodal game search engine that is not only technologically advanced but also financially sustainable. The core of this strategy is a paradigm shift away from expensive, real-time managed services towards a **"Pre-computed Offline Index"** model. This approach strategically leverages the formidable local processing power of the provided Bioinformatics Rig for computationally intensive tasks—namely, the generation of vector embeddings—while utilizing ultra-low-cost cloud object storage and an embedded, in-container database for serving search queries. This architecture effectively neutralizes the primary cost drivers that plagued previous attempts, paving the way for a service that can genuinely pay for itself.

The key recommendations forming the foundation of this blueprint are as follows:

* **Embedding Model:** The analysis identifies RzenEmbed-v1-2B as the optimal open-source multimodal embedding model. It strikes a superior balance between state-of-the-art performance on relevant benchmarks and a manageable size, making it ideal for efficient local processing on an NVIDIA RTX 3090\.1  
* **Data Pipeline & Architecture:** The recommended architecture is a polyglot system orchestrated by.NET Aspire. A Python-based script will run on the local machine to handle the heavy lifting of data ingestion and embedding generation. The resulting index is then served by a lightweight.NET web application, providing a unified and modern development experience.2  
* **Vector Database:** To achieve near-zero query costs, the report recommends sqlite-vec, a high-performance vector search extension for SQLite. This allows the entire search index to be encapsulated in a single database file, loaded and queried directly within the application container, thus eliminating the need for a dedicated, managed database service.4  
* **Cloud Infrastructure:** The financial model is anchored by two key services: Cloudflare R2 for object storage, selected for its critical zero-egress fee policy 6, and a minimal container hosting service, such as Azure Container Apps on a consumption plan, for the web application.

The culmination of these strategic decisions is a projected Total Cost of Ownership (TCO) of **under $5.00 per month**. This remarkably low operational expenditure makes the goal of achieving self-sufficiency through the revenue of a single website banner ad not just a distant possibility, but a highly probable outcome.

## **Section 1: Selecting the Heart of the System \- The Multimodal Embedding Model**

The choice of the multimodal embedding model is the most critical technical decision in this project. It is the engine that powers the semantic understanding of game reviews and screenshots, directly influencing the quality of search results, the computational cost of the offline data pipeline, and the resource requirements of the entire deployed system. A data-driven selection process, grounded in public benchmarks, is essential to identify a model that is both high-performing and perfectly suited for local hardware capabilities.

### **1.1 The Multimodal Landscape: Beyond CLIP**

Multimodal models like CLIP (Contrastive Language-Image Pre-training) revolutionized the field by learning to map images and text into a shared semantic vector space.8 In this space, the vector for a picture of a dog is numerically close to the vector for the text "a photo of a dog." This enables powerful cross-modal search. However, the field has evolved rapidly. Successor architectures like SigLIP (Sigmoid Language-Image Pre-training) have refined the training methodology, employing a pairwise sigmoid loss function instead of CLIP's global contrastive loss.10 This technical improvement allows for more efficient training with larger batch sizes and often results in more robust and performant open-source models.

In this "Cambrian explosion" of Multimodal Large Language Models (MLLMs) 11, navigating the landscape to find the best open-source option can be daunting. Relying on standardized, objective benchmarks is the only way to make an informed decision. The

**Massive MultiModal Embedding Benchmark (MMEB)** serves as the ground truth for this analysis. MMEB evaluates models across 36 diverse datasets spanning classification, visual question answering (VQA), and retrieval tasks.1 This comprehensive evaluation is far more indicative of a model's real-world capabilities for understanding nuanced game content than a single-task benchmark would be. The existence of such public benchmarks is a direct result of the proliferation of models and provides an invaluable resource, allowing for enterprise-grade, data-driven decisions without the need for costly, bespoke evaluations.

### **1.2 Candidate Model Analysis: Balancing Performance and Practicality**

The local bioinformatics rig, equipped with an NVIDIA RTX 3090 with 24GB of VRAM, is a significant asset for offline processing.12 This hardware can comfortably run models up to approximately 7-8 billion parameters, particularly when using quantization techniques. However, for the sake of faster processing cycles and reduced complexity, models in the 2-3 billion parameter range represent a sweet spot, offering an excellent trade-off between performance and computational efficiency.

Filtering the MMEB leaderboard for high-performing, open-source models under 3 billion parameters yields a clear set of top contenders.1

| Model Name | Parameters (Billions) | MMEB Overall Score | MMEB Image Score | Notes |
| :---- | :---- | :---- | :---- | :---- |
| **RzenEmbed-v1-2B** | 2.21 | **64.36** | 68.53 | **Recommended Starting Point.** Top performer in its size class. |
| Ops-MM-embedding-v1-2B | 2.21 | 63.44 | 69.03 | Strong alternative with slightly better image performance. |
| VLM2Vec-V2.0-Qwen2VL-2B | 2.21 | 58.02 | 64.85 | Viable, but noticeably lower overall score. |
| interestFM-UIR-CAFe-0.5B | 0.894 | 49.68 | 55.43 | Extremely lightweight; a good fallback if processing time is critical. |

Based on this data, **RzenEmbed-v1-2B is the clear recommendation**. It offers the highest overall performance in its size class, providing a state-of-the-art foundation for search quality while remaining well within the processing capabilities of the local hardware.

The choice of a smaller, high-performance model like this has a cascading cost-saving effect that extends far beyond VRAM usage. A smaller model implies faster inference times during the offline batch job, which can save dozens of hours of processing when indexing tens of thousands of games. Furthermore, smaller models often produce lower-dimensional embeddings (e.g., 768 dimensions instead of 1536). This directly reduces the file size of the final search index, leading to lower storage costs in the cloud and, most importantly, a smaller memory footprint inside the production container. This smaller memory requirement allows the application to run on a cheaper hosting plan, directly minimizing the primary monthly operational cost. Therefore, optimizing for model size at the outset is a critical lever for achieving the project's financial goals.

### **1.3 Embedding Strategy: From Raw Data to Searchable Vectors**

Representing an entire game—with its official description, up to 200 reviews, and 10 screenshots—poses a strategic challenge. Embedding every single piece of content individually would create an explosion in the number of vectors, leading to a large, slow, and expensive index. A more sophisticated approach is required to distill the essence of each game into a manageable set of vectors.

The proposed strategy is to create **"Game Facet" Embeddings**. Instead of a single, monolithic embedding or a multitude of individual ones, this approach generates a small, fixed set of vectors for each game, representing its core facets. This balances semantic richness with search efficiency.

1. **Text Facet Embedding:** The game's official description is concatenated with the top-rated 50-100 user reviews. This composite text document, capturing both the developer's intent and the player community's consensus on gameplay, narrative, and mechanics, is then passed through the model to generate a single text embedding.  
2. **Gameplay Image Facet Embedding:** Each of the 10 screenshots is passed through the model's image encoder to generate 10 individual image embeddings. These 10 vectors are then mathematically averaged to create a single "visual gestalt" vector. This composite vector represents the game's overall art style, user interface design, color palette, and typical in-game scenes.

At query time, a user's input (whether text or an image) is first converted into a query vector. This single vector is then compared against *both* the Text Facet and the Gameplay Image Facet for every game in the index. The final relevance score for a game is calculated as a weighted average of the two resulting similarity scores. This "late fusion" approach is computationally simple yet powerful.15 It allows a textual query like "deep crafting system" to match strongly against a game's Text Facet, while an image query of a dark, atmospheric screenshot would match against the Gameplay Image Facet, enabling truly multimodal discovery.

## **Section 2: The Bedrock of Your System \- Vector Storage and Search**

This section addresses the single largest cost driver of the previous project iteration: the vector database. The analysis evaluates a spectrum of solutions, from fully managed enterprise services to lightweight embedded libraries, to architect a system that reduces the monthly search infrastructure bill from hundreds of dollars to virtually zero.

### **2.1 Comparative Analysis of Vector Database Solutions**

The market for vector search solutions has bifurcated into two main categories: highly scalable (and often expensive) managed services, and hyper-efficient (and extremely cheap) embedded libraries. This evolution is a direct result of the open-sourcing and commoditization of the underlying Approximate Nearest Neighbor (ANN) search algorithms like HNSW, which are now foundational technologies in libraries like Faiss.17 This gives developers a critical choice that was not widely available just a few years ago.

* **The High-Cost Incumbents (Azure):**  
  * **Azure AI Search:** This is a powerful, feature-rich platform, but its pricing is based on provisioned "Search Units" (SUs) billed at a fixed hourly rate. The entry-level "Basic" tier starts at approximately $74 per month, a cost that is prohibitive for this project's budget.20  
  * **Azure Cosmos DB with Vector Search:** While offering a more flexible serverless model based on Request Unit (RU) consumption 22, this option introduces significant cost uncertainty. Vector search queries can be RU-intensive, and the serverless tier has a minimum billable storage amount, which can lead to unexpected charges even for small datasets.24 The complexity and cost risk make it unsuitable.  
* **The Low-Cost Managed Contender (Cloudflare):**  
  * **Cloudflare Vectorize:** A purpose-built, globally distributed vector database with an exceptionally competitive pricing model. Costs are calculated based on "Vector Dimensions Stored" and "Vector Dimensions Queried".25 A production-scale workload with 50,000 vectors and 200,000 queries per month is estimated to cost just  
    **$1.94 per month**.25 As a managed service, it also simplifies operations, making it a very strong candidate.27  
* **The Zero-Cost Embedded Powerhouses (Local Libraries):**  
  * **FAISS & HNSWlib:** These are the seminal C++ libraries that power many commercial vector databases. They are not databases themselves but rather toolkits for building, saving, and loading high-performance ANN indexes.17 Using them directly offers maximum performance but requires significant engineering effort to manage index files and integrate with an application.18  
  * **SQLite with sqlite-vec:** This represents the ultimate low-tech, high-impact solution. sqlite-vec is a modern SQLite extension written in C with no external dependencies. It seamlessly adds vector search capabilities to a standard SQLite database file.4 It supports K-Nearest Neighbor (KNN) search through SQL virtual tables, allows for metadata filtering, and is highly optimized for brute-force search, which is surprisingly performant for datasets in the low millions of vectors.5

| Solution | Type | Estimated Monthly Cost | Performance Profile | Developer Experience/Complexity | Recommendation |
| :---- | :---- | :---- | :---- | :---- | :---- |
| Azure AI Search | Managed Service | \> $74.00 | High | Medium | Not Recommended (Cost) |
| Azure Cosmos DB | Managed Service | Variable (\> $5.00) | High | High | Not Recommended (Cost/Complexity) |
| Cloudflare Vectorize | Managed Service | \~ $2.00 | High | Low | Strong Alternative |
| FAISS (Self-Managed) | Library | $0.00 | Very High | Very High | Not Recommended (Complexity) |
| **sqlite-vec** | **Embedded Library** | **$0.00** | **High** | **Low (with.NET wrapper)** | **Strongly Recommended** |

### **2.2 The Recommended Architecture: The "Index-as-a-File" Pattern**

To achieve the project's core financial objective, the architecture must eliminate *all* variable costs associated with search queries. While Cloudflare Vectorize is remarkably inexpensive, it still has a per-query cost component. The sqlite-vec approach, however, has **zero per-query cost**. The search operation runs on the CPU and memory of the application container, resources that are already allocated and paid for. This strategic decision is the key to unlocking the "extraordinarily low cost" mandate.

This enables the **"Index-as-a-File"** architectural pattern. For read-heavy workloads with periodic batch updates, like this game search engine, the entire paradigm of a distributed, stateful, always-on database service is an unnecessary and expensive overhead. By shifting the "state"—the vector index—to a simple file hosted in object storage, an expensive, active service is transformed into a cheap, passive one.

The architectural flow is as follows:

1. **Offline Processing (Local Rig):** The Python script generates the "Game Facet" embeddings for the entire game catalog.  
2. **Index Creation:** The script creates a new SQLite database file (games.db). It populates a standard table with game metadata (ID, name, links to screenshots) and uses sqlite-vec's vec0 virtual table functionality to store the Text Facet and Gameplay Image Facet vectors.4  
3. **Deployment to Object Storage:** The completed games.db file is uploaded to a public Cloudflare R2 bucket. R2 is chosen specifically over alternatives like Azure Blob Storage because of its zero egress fee policy, a critical feature since the application container will download this file on every startup.6  
4. **Online Serving (In-Container):** The.NET application, upon starting, downloads the games.db file from R2 to its local container filesystem. It then opens a standard SQLite connection to this local file. All incoming search requests are executed as SQL queries against this local database file.  
5. **Periodic Updates:** The offline pipeline can be re-run on a schedule (e.g., weekly) to incorporate new games and reviews. This process generates a new games.db file, which is then uploaded to R2, overwriting the old one. The running application instances can be configured to periodically check for a new index version and trigger a graceful restart to pull the latest data.

Crucially, the existence of a high-quality.NET connector, Microsoft.SemanticKernel.Connectors.SqliteVec, makes this architecture practical and elegant for a.NET developer.30 Interfacing a C-based library like

sqlite-vec with.NET would traditionally require complex and error-prone P/Invoke (Platform Invoke) code.34 This connector abstracts away all the low-level interop details, bridging the gap between the ideal low-cost infrastructure choice and the preferred development environment.

## **Section 3: System Implementation and.NET Architecture**

This section provides a concrete implementation plan, translating the architectural decisions into a project structure that leverages the.NET Aspire stack for orchestration and development, while embracing a polyglot approach for the data processing pipeline.

### **3.1 The.NET Aspire AppHost: Orchestrating a Polyglot System**

.NET Aspire is designed to simplify the development and orchestration of distributed applications.37 Its

AppHost project acts as a single, code-first source of truth for defining all the services, containers, and resources that comprise the application.

* **Project Structure:** The solution will be organized as follows:  
  * GameSearch.AppHost: The.NET Aspire orchestration project.  
  * GameSearch.WebApp: The user-facing ASP.NET Core web application.  
  * GameSearch.DataProcessor: A directory containing the Python embedding and indexing script.  
* **Orchestrating Python:** A key feature of modern.NET Aspire is its native support for orchestrating Python projects via the AddPythonProject method in the AppHost.2 This is more than a simple convenience; it represents a strategic embrace of polyglot microservice architectures. For this project, it means the  
  AppHost can be configured to launch the Python data processing script during local development. This provides a unified dotnet run experience, allowing a.NET developer to seamlessly manage a Python component—complete with integrated logging and service discovery—without leaving their preferred tooling. This dramatically lowers the friction of using the best tool for each job: Python for ML inference and.NET for the web backend.

### **3.2 The Offline Data Pipeline (Python)**

The machine learning ecosystem, particularly for running state-of-the-art models from hubs like Hugging Face, is overwhelmingly Python-native.8 It is therefore far more practical and efficient to perform the embedding generation in Python.

The script, executed on the local bioinformatics rig, will perform the following steps:

1. **Setup:** Install necessary Python packages via pip, including transformers, torch (with CUDA support), datasets, sqlite-vec, and the Cloudflare R2 client library.  
2. **Data Ingestion:** Use the Steamworks Web API to fetch the list of all games, and for each game, retrieve its description, screenshots, and a corpus of user reviews.  
3. **Model Loading:** Load the recommended RzenEmbed-v1-2B model and its associated processor from the Hugging Face Hub. The model will be explicitly moved to the CUDA device to leverage the RTX 3090's GPU acceleration.10  
4. **Embedding Generation Loop:** Iterate through each game, applying the "Game Facet" strategy. Text and images will be pre-processed and passed through the model in batches to efficiently generate the two embedding vectors for each game.  
5. **Index Building:** Connect to a new SQLite file (games.db). Using the sqlite-vec library, create a standard table for metadata and a vec0 virtual table for the vectors. Insert the game metadata and the corresponding Text and Image Facet vectors into their respective tables.4  
6. **Asset Upload:** Upload the final games.db index file and a simple version.txt file to the designated Cloudflare R2 bucket.

### **3.3 The Online Web Application (.NET)**

The GameSearch.WebApp is a standard ASP.NET Core application responsible for serving the user interface and handling search API requests.

* **Startup Logic:** On application startup, the service will perform a check against the version.txt file in the R2 bucket. If the remote version is newer than the local games.db file (or if no local file exists), it will download the latest index from R2 to its local filesystem. This ensures the application always serves queries from the most recent data.  
* **Vector Search with Semantic Kernel:** The application will integrate the Microsoft.SemanticKernel.Connectors.SqliteVec NuGet package, which provides a high-level, developer-friendly API for interacting with the sqlite-vec enabled database.30  
  * A service layer will be created to encapsulate the search logic. When a search request is received, the input query (text or image) will be vectorized.  
  * This query vector will then be used to execute two parallel SQL queries against the local games.db file. These queries will use sqlite-vec's custom functions to find the K-nearest neighbors in the Text Facet and Gameplay Image Facet columns, respectively.  
  * The results from both queries—each a list of game IDs and their similarity scores—will be merged and re-ranked in C\# using a weighted scoring formula to produce the final, unified search results.  
* **API Endpoints:** The web application will expose several key endpoints:  
  * /search/text: Accepts a text query, vectorizes it (potentially using a small, fast model running in the container or via a serverless function), and executes the search logic.  
  * /search/image: Accepts an uploaded image, vectorizes it, and executes the search.  
  * /search/similar: Accepts a Steam App ID, retrieves its pre-computed facet vectors from the local database, and uses them to find visually and textually similar games.

## **Section 4: Financial Analysis and Path to Self-Sufficiency**

This final section provides a quantitative analysis to validate that the proposed architecture meets the project's core financial objective. A detailed cost model is presented and benchmarked against realistic advertising revenue projections, demonstrating a clear path to self-sufficiency.

### **4.1 Total Cost of Ownership (TCO) Breakdown**

The cost model is based on a hypothetical but realistic scale of 50,000 indexed games, each with two 768-dimension facet vectors, 10 screenshots, and aggregated review text.

A critical architectural enabler for this entire model is the zero-egress fee policy of Cloudflare R2.6 The "Index-as-a-File" pattern requires the application container to download the index file from object storage on every startup. With traditional cloud providers, this egress traffic would be a variable and potentially significant cost, especially in a serverless environment where instances are frequently created and destroyed. R2's policy transforms this unpredictable operational expenditure into a fixed, zero cost, thereby de-risking the entire architecture and making it financially predictable.

| Cost Item | Service Provider | Unit of Measure | Estimated Monthly Usage | Free Tier Allocation | Billable Usage | Unit Cost | Estimated Monthly Cost |
| :---- | :---- | :---- | :---- | :---- | :---- | :---- | :---- |
| Index File Storage | Cloudflare R2 | GB-months | 0.5 GB | 10 GB | 0 GB | $0.015 / GB | $0.00 |
| Raw Asset Storage | Cloudflare R2 | GB-months | 102.0 GB | (10 \- 0.5) GB | 92.5 GB | $0.015 / GB | $1.39 |
| Data Write Ops | Cloudflare R2 | Million Requests | \< 0.01 | 1 Million | 0 | $4.50 / Million | $0.00 |
| Data Read Ops | Cloudflare R2 | Million Requests | \< 0.01 | 10 Million | 0 | $0.36 / Million | $0.00 |
| Container Compute | Azure Container Apps | vCPU/GiB-seconds | Low/Intermittent | Varies | Varies | Consumption | \~$3.00 |
| **Total** |  |  |  |  |  |  | **\~$4.39** |

* **Cloudflare R2 Storage Costs:** The index file is estimated at \~307 MB (50,000 games × 2 vectors/game × 768 dimensions × 4 bytes/dimension), rounded up to 0.5 GB. Raw assets (10 screenshots at 200KB each \+ 50KB text per game) total \~102 GB. The total storage is 102.5 GB. R2's pricing is $0.015/GB/month, with the first 10 GB free.6 The monthly storage cost is therefore  
  (102.5−10)×$0.015=$1.39.  
* **Cloudflare R2 Operations Costs:** The initial population of the bucket involves a one-time write cost. Monthly operations, primarily the container downloading the index file on startup and users' browsers fetching images, fall well within the generous free tiers for Class A (1 million) and Class B (10 million) operations, resulting in a monthly cost of **$0.00**.41  
* **Container Hosting Costs:** A consumption-based plan, such as Azure Container Apps, is ideal. A 1 vCPU / 2 GiB RAM instance is sufficient to hold the index in memory and run the application. For a low-traffic niche site, the cost based on active vCPU-seconds and GiB-seconds is conservatively estimated at **$3.00 per month**.

The total estimated monthly operational cost for the entire service is approximately **$4.39**.

### **4.2 Benchmarking Against Ad Revenue**

The final step is to determine the feasibility of covering this cost with revenue from a single banner advertisement. A Cost-Per-Click (CPC) model is most appropriate for a new, lower-traffic website.42

* **Revenue Assumptions:** Based on industry averages for display ads, we assume an average CPC of $0.58 and an average Click-Through Rate (CTR) of 0.44%.42  
* **Breakeven Calculation:**  
  * Clicks needed per month to cover costs: $4.39/$0.58 per click≈8 clicks.  
  * Page views needed to generate those clicks: 8 clicks/0.0044 CTR≈1,818 page views.

### **Conclusion and Recommendations**

The analysis confirms that the proposed architecture is not only technically sound but also extraordinarily cost-effective. The system can achieve financial self-sufficiency and profitability after generating just over **1,800 page views per month**, which translates to roughly 60 visits per day. This is an exceptionally low and achievable threshold for a niche project, directly fulfilling the core requirement of building a service that can be sustained indefinitely without significant financial burden.

By strategically combining offline processing on powerful local hardware with an "Index-as-a-File" pattern served from a lightweight container, this blueprint provides a clear and viable path to delivering a genuine, honest, and powerful game search experience to the masses at an unprecedentedly low cost.

#### **Works cited**

1. MMEB Leaderboard \- a Hugging Face Space by TIGER-Lab, accessed August 31, 2025, [https://huggingface.co/spaces/TIGER-Lab/MMEB-Leaderboard](https://huggingface.co/spaces/TIGER-Lab/MMEB-Leaderboard)  
2. Orchestrate Python apps in .NET Aspire, accessed August 31, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-python](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-python)  
3. Cloud-Native .NET Aspire 8.1 Targets Building Containers, Orchestrating Python, accessed August 31, 2025, [https://visualstudiomagazine.com/articles/2024/07/25/net-aspire-8-1.aspx](https://visualstudiomagazine.com/articles/2024/07/25/net-aspire-8-1.aspx)  
4. How sqlite-vec Works for Storing and Querying Vector Embeddings | by Stephen Collins, accessed August 31, 2025, [https://medium.com/@stephenc211/how-sqlite-vec-works-for-storing-and-querying-vector-embeddings-165adeeeceea](https://medium.com/@stephenc211/how-sqlite-vec-works-for-storing-and-querying-vector-embeddings-165adeeeceea)  
5. Introducing sqlite-vec v0.1.0: a vector search SQLite extension that runs everywhere, accessed August 31, 2025, [https://alexgarcia.xyz/blog/2024/sqlite-vec-stable-release/index.html](https://alexgarcia.xyz/blog/2024/sqlite-vec-stable-release/index.html)  
6. Cloudflare R2 | Zero Egress Fee Object Storage, accessed August 31, 2025, [https://www.cloudflare.com/developer-platform/products/r2/](https://www.cloudflare.com/developer-platform/products/r2/)  
7. R2 Pricing Calculator \- Cloudflare, accessed August 31, 2025, [https://r2-calculator.cloudflare.com/](https://r2-calculator.cloudflare.com/)  
8. mlfoundations/open\_clip: An open source implementation ... \- GitHub, accessed August 31, 2025, [https://github.com/mlfoundations/open\_clip](https://github.com/mlfoundations/open_clip)  
9. CLIP embeddings to improve multimodal RAG with GPT-4 Vision | OpenAI Cookbook, accessed August 31, 2025, [https://cookbook.openai.com/examples/custom\_image\_embedding\_search](https://cookbook.openai.com/examples/custom_image_embedding_search)  
10. SigLIP \- Hugging Face, accessed August 31, 2025, [https://huggingface.co/docs/transformers/en/model\_doc/siglip](https://huggingface.co/docs/transformers/en/model_doc/siglip)  
11. BradyFU/Awesome-Multimodal-Large-Language-Models: :sparkles \- GitHub, accessed August 31, 2025, [https://github.com/BradyFU/Awesome-Multimodal-Large-Language-Models](https://github.com/BradyFU/Awesome-Multimodal-Large-Language-Models)  
12. Houdini Minimal OpenCL Solver (Sparse) Review with RTX3090 \! \- YouTube, accessed August 31, 2025, [https://www.youtube.com/watch?v=GbhPtTEv858](https://www.youtube.com/watch?v=GbhPtTEv858)  
13. Running WAN 2.1 on a high-performance NVIDIA RTX 3090 with ComfyUI \- YouTube, accessed August 31, 2025, [https://www.youtube.com/watch?v=4Ybvi5djoCA](https://www.youtube.com/watch?v=4Ybvi5djoCA)  
14. How to enable RTX 3090 support in fast.ai once and for all with Docker | by Enrico Rampazzo | This time is different: my journey towards machine learning | Medium, accessed August 31, 2025, [https://medium.com/this-time-is-different-my-journey-towards-machine/how-to-enable-rtx-3090-support-in-fast-ai-once-and-for-all-with-docker-48d8d9e5247d](https://medium.com/this-time-is-different-my-journey-towards-machine/how-to-enable-rtx-3090-support-in-fast-ai-once-and-for-all-with-docker-48d8d9e5247d)  
15. What are best practices for combining text and image embeddings? \- Zilliz Vector Database, accessed August 31, 2025, [https://zilliz.com/ai-faq/what-are-best-practices-for-combining-text-and-image-embeddings](https://zilliz.com/ai-faq/what-are-best-practices-for-combining-text-and-image-embeddings)  
16. \[D\]\[P\] How to combine aligned embeddings for cosine similarity search? \- Reddit, accessed August 31, 2025, [https://www.reddit.com/r/MachineLearning/comments/193ylih/dp\_how\_to\_combine\_aligned\_embeddings\_for\_cosine/](https://www.reddit.com/r/MachineLearning/comments/193ylih/dp_how_to_combine_aligned_embeddings_for_cosine/)  
17. The faiss library \- arXiv, accessed August 31, 2025, [https://arxiv.org/pdf/2401.08281](https://arxiv.org/pdf/2401.08281)  
18. Welcome to Faiss Documentation — Faiss documentation, accessed August 31, 2025, [https://faiss.ai/](https://faiss.ai/)  
19. nmslib/hnswlib: Header-only C++/python library for fast approximate nearest neighbors, accessed August 31, 2025, [https://github.com/nmslib/hnswlib](https://github.com/nmslib/hnswlib)  
20. Azure AI Search Pricing 2025 \- TrustRadius, accessed August 31, 2025, [https://www.trustradius.com/products/azure-ai-search/pricing](https://www.trustradius.com/products/azure-ai-search/pricing)  
21. Azure AI Search pricing, accessed August 31, 2025, [https://azure.microsoft.com/en-us/pricing/details/search/](https://azure.microsoft.com/en-us/pricing/details/search/)  
22. Pricing Model for Azure Cosmos DB | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/azure/cosmos-db/how-pricing-works](https://learn.microsoft.com/en-us/azure/cosmos-db/how-pricing-works)  
23. Pricing Model for Azure Cosmos DB, accessed August 31, 2025, [https://docs.azure.cn/en-us/cosmos-db/how-pricing-works](https://docs.azure.cn/en-us/cosmos-db/how-pricing-works)  
24. Azure Cosmos DB Serverless Pricing \- Reddit, accessed August 31, 2025, [https://www.reddit.com/r/AZURE/comments/16zr94d/azure\_cosmos\_db\_serverless\_pricing/](https://www.reddit.com/r/AZURE/comments/16zr94d/azure_cosmos_db_serverless_pricing/)  
25. Pricing · Cloudflare Vectorize docs, accessed August 31, 2025, [https://developers.cloudflare.com/vectorize/platform/pricing/](https://developers.cloudflare.com/vectorize/platform/pricing/)  
26. Vectorize \- Cloudflare, accessed August 31, 2025, [https://www.cloudflare.com/developer-platform/products/vectorize/](https://www.cloudflare.com/developer-platform/products/vectorize/)  
27. Overview · Cloudflare Vectorize docs, accessed August 31, 2025, [https://developers.cloudflare.com/vectorize/](https://developers.cloudflare.com/vectorize/)  
28. Introduction to Facebook AI Similarity Search (Faiss) \- Pinecone, accessed August 31, 2025, [https://www.pinecone.io/learn/series/faiss/faiss-tutorial/](https://www.pinecone.io/learn/series/faiss/faiss-tutorial/)  
29. Faiss \- Python LangChain, accessed August 31, 2025, [https://python.langchain.com/docs/integrations/vectorstores/faiss/](https://python.langchain.com/docs/integrations/vectorstores/faiss/)  
30. Using the Semantic Kernel SQLite Vector Store connector (Preview) | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/sqlite-connector](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/sqlite-connector)  
31. Azure Blob Storage Pricing Breakdown: What You Need to Know \- Cloudchipr, accessed August 31, 2025, [https://cloudchipr.com/blog/azure-blob-storage-pricing](https://cloudchipr.com/blog/azure-blob-storage-pricing)  
32. Azure Blob Storage pricing, accessed August 31, 2025, [https://azure.microsoft.com/en-us/pricing/details/storage/blobs/](https://azure.microsoft.com/en-us/pricing/details/storage/blobs/)  
33. Microsoft.SemanticKernel.Connectors.Sqlite 1.51.0-preview \- NuGet, accessed August 31, 2025, [https://www.nuget.org/packages/Microsoft.SemanticKernel.Connectors.Sqlite/](https://www.nuget.org/packages/Microsoft.SemanticKernel.Connectors.Sqlite/)  
34. Integrating C/C++ Libraries to .NET Applications with P/Invoke \- DEV Community, accessed August 31, 2025, [https://dev.to/turalsuleymani/integrating-cc-libraries-to-net-applications-with-pinvoke-1n4c](https://dev.to/turalsuleymani/integrating-cc-libraries-to-net-applications-with-pinvoke-1n4c)  
35. Integrating C/C++ Libraries to .NET Applications with P/Invoke \- C\# Corner, accessed August 31, 2025, [https://www.c-sharpcorner.com/article/integrating-ccpp-libraries-to-net-applications-with-pinvoke/](https://www.c-sharpcorner.com/article/integrating-ccpp-libraries-to-net-applications-with-pinvoke/)  
36. Platform Invoke (P/Invoke) \- .NET | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)  
37. Aspire overview \- .NET Aspire | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)  
38. OpenCLIP \- open-clip-torch · PyPI, accessed August 31, 2025, [https://pypi.org/project/open-clip-torch/2.6.1/](https://pypi.org/project/open-clip-torch/2.6.1/)  
39. Projects based on SigLIP (Zhai et. al, 2023\) and Hugging Face transformers integration \- GitHub, accessed August 31, 2025, [https://github.com/merveenoyan/siglip](https://github.com/merveenoyan/siglip)  
40. open\_clip/README.md · hamacojr/CAT-Seg at aff8d56cbd0128a128569c1468a746ad0a824fc2 \- Hugging Face, accessed August 31, 2025, [https://huggingface.co/spaces/hamacojr/CAT-Seg/blob/aff8d56cbd0128a128569c1468a746ad0a824fc2/open\_clip/README.md](https://huggingface.co/spaces/hamacojr/CAT-Seg/blob/aff8d56cbd0128a128569c1468a746ad0a824fc2/open_clip/README.md)  
41. Pricing \- R2 \- Cloudflare Docs, accessed August 31, 2025, [https://developers.cloudflare.com/r2/pricing/](https://developers.cloudflare.com/r2/pricing/)  
42. Online Advertising Costs: How Much Should I Charge for Ad Rates? \- SmartyAds, accessed August 31, 2025, [https://smartyads.com/blog/web-site-ad-rates-how-much-should-i-charge](https://smartyads.com/blog/web-site-ad-rates-how-much-should-i-charge)