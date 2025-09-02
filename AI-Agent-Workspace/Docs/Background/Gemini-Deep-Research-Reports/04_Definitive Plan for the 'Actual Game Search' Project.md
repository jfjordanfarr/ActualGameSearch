

# **Definitive Plan for the 'Actual Game Search' Project**

---

## **User Prompt**

Attached are the history of a game search engine I've tried to develop (outputs from python notebooks from 2023\) as well as a reimagining of that game search engine at ultra-low-cost using modern tech which I've begun issuing Gemini Deep Research reports about. You'll see prior Gemini Deep Research reports attached to this chat.

I want to continue pursuing the sqlite-based reimaging.

In my last prompt, I gave a follow-up to report 3, and I prompted:

"""

Show me the whole of Path C: Client-Side Wasm. It's fine if we show loading screens. Telling people that they are not sending any text or image data to our servers would be unbelievable. Creating a world-class project template for \*\*cost-free semantic search\*\* and providing it as an open source project to the rest of the world would be earthshaking. You can use .NET 8 for this but you know what would get you really ahead of the curve? Using .NET 10\. Expecting to be the first to pop out an absolute cost-cutting juggernaut of an enterprise secure template, proven on a nontrivial testbed dataset of a huge trove of well-selected Steam reviews and game screenshots for hundreds of thousands of games.

"""

Report 3.5 came back as a brief follow-up response, and it was not given the full depth it deserved. I want to see the whole system envisioned top to bottom. Show me the backend, the frontend, the deployment units. You have in front of you a lot of information that has internal conflicts which must be resolved with wise decisions. Use your previous reports to take this home: prove that this can be done from top to bottom, backend-to-frontend, with the assumption that you will inevitably be building out a template for performing \*\*any\*\* kind of low-cost multimodal search with Dotnet Aspire/Dotnet 10\.

## 

## ---

## 

## **Report 3.5**

## **Section 1: The Vision—A New Paradigm for Search**

The prevailing architecture for AI-powered semantic search involves a costly and complex server-side infrastructure. User queries are sent to a server, processed by expensive GPU-powered machines, and compared against a server-hosted database. This model incurs significant compute costs, introduces latency, and requires users to trust a third party with their search data. This document outlines a revolutionary alternative: a world-class, enterprise-secure project template for delivering high-performance, multimodal semantic search with virtually zero server-side operational cost.

By harnessing the power of.NET 10, Blazor WebAssembly (WASM), and the ONNX Runtime, we shift the entire computational workload of query processing to the end-user's device.1 This client-side architecture is not merely a cost-cutting measure; it is a fundamental paradigm shift with profound implications:

* **Absolute Cost-Efficiency:** With no server-side compute required for inference, the operational cost per query approaches zero. The only expenses are for static file hosting, which are orders of magnitude lower than maintaining an active inference server.  
* **Unbreakable User Privacy:** User queries—whether text or images—are never transmitted over the network. All embedding calculations happen within the secure sandbox of the user's browser, offering an unparalleled level of privacy that is simply impossible with server-side architectures.1  
* **Infinite Scalability:** The application's ability to scale is no longer constrained by server capacity. As the user base grows, the computational load is naturally distributed across the users themselves, making the system infinitely scalable from the provider's perspective.

This document provides a comprehensive blueprint for building this next-generation search engine, leveraging a forward-looking.NET 10 technology stack to create an open-source juggernaut poised to redefine cost-effective, secure AI applications.

## **Section 2: The Architectural Blueprint**

The elegance of this architecture lies in its simplicity and the strategic offloading of responsibilities. The "backend" is reduced to a simple, highly-scalable static file hosting solution, while the "frontend" becomes a powerful, self-contained compute engine.

### **2.1 Core Components**

1. Application Framework:.NET 10 Blazor WebAssembly  
   The foundation is a standalone Blazor WASM application. By targeting.NET 10, we leverage cutting-edge performance enhancements, including improved JIT compilation, advanced garbage collection (WasmGC), and more efficient JavaScript interop, which are critical for running complex.NET code in the browser..3NET 10's focus on AOT compilation and IL trimming is essential for minimizing the initial application payload, a key challenge in WASM applications.5  
2. AI Inference Engine: ONNX Runtime Web  
   The onnxruntime-web library is the engine that makes client-side inference possible. It is a JavaScript library that uses WebAssembly to execute ONNX models directly in the browser with near-native performance.7 It can harness advanced browser features like multi-threading and SIMD (Single Instruction, Multiple Data) to dramatically accelerate computation on the user's CPU.7  
3. Static Asset Hosting: Cloudflare Pages & R2 Storage  
   To achieve the "ultra-low-cost" goal, the entire application is treated as a collection of static assets.  
   * **Cloudflare Pages:** Provides a robust, globally distributed platform for deploying the Blazor WASM application. Its free and paid tiers are exceptionally affordable for hosting static sites.9  
   * **Cloudflare R2:** A zero-egress-fee object storage solution perfect for hosting the large files required by the application: the ONNX models and the pre-compiled SQLite database. This eliminates the punitive bandwidth costs that typically plague data-heavy applications.10  
4. Data and Model Caching: IndexedDB  
   The primary user experience challenge is the initial download of the large ONNX models and the SQLite database. To solve this, we use the browser's IndexedDB API. IndexedDB is a client-side NoSQL database capable of storing large amounts of data (up to several gigabytes) persistently.11 After the first visit, all necessary assets are loaded directly from the user's local storage, making subsequent interactions instantaneous.

### **2.2 The User Experience and Data Flow**

The application is engineered to manage its large asset dependencies intelligently, ensuring a smooth user experience despite the client-side workload.

1. **Initial Load:** The user's browser downloads the core Blazor WASM application from Cloudflare Pages. This initial payload is kept as small as possible using.NET 10's AOT and trimming features.5 A loading screen is immediately displayed, informing the user that the search engine is preparing for first use.  
2. **Asynchronous Asset Fetching:** In the background, the application initiates downloads for the required assets from Cloudflare R2:  
   * The onnxruntime-web WebAssembly binaries.  
   * The ONNX model files (all-MiniLM-L6-v2.onnx and MobileNetV2.onnx).  
   * The games.sqlite database file.  
3. **Client-Side Caching:** As each asset is downloaded, it is stored in the browser's IndexedDB. The UI provides progress indicators for this one-time setup process.  
4. **Ready State:** Once all assets are cached in IndexedDB, the loading screen is replaced by the main search interface. On all subsequent visits, the application checks IndexedDB first, bypassing the network download entirely.  
5. **Search Execution:**  
   * A user enters a text query or uploads an image.  
   * The Blazor application calls the onnxruntime-web library via JavaScript interop.  
   * The appropriate ONNX model is loaded from IndexedDB into the WebAssembly runtime.  
   * The user's query is pre-processed (tokenized for text, transformed for images) in JavaScript and an embedding vector is generated.  
   * This vector is passed back to the C\# code.  
   * The application then executes a query against the SQLite database (loaded from IndexedDB) to find the most similar vectors.  
   * The search results are rendered in the Blazor UI.

## **Section 3: Implementation Deep Dive**

This section details the critical implementation steps for creating the project template.

### **3.1 Project Setup and Configuration**

1. Create a.NET 10 Blazor WASM Project:  
   Use the.NET CLI to create a new, standalone Blazor WebAssembly project.  
   Bash  
   dotnet new blazorwasm \-o GameSearch.Client

2. Configure for Performance:  
   In the GameSearch.Client.csproj file, enable AOT compilation, IL trimming, and other performance-enhancing features available in.NET 10.13  
   XML  
   \<PropertyGroup\>  
     \<TargetFramework\>net10.0\</TargetFramework\>  
     \<RunAOTCompilation\>true\</RunAOTCompilation\>  
     \<WasmStripILAfterAOT\>true\</WasmStripILAfterAOT\>  
     \<WasmEnableSIMD\>true\</WasmEnableSIMD\>  
   \</PropertyGroup\>

3. Install ONNX Runtime Web:  
   While onnxruntime-web is a JavaScript library, it needs to be included in the project. The simplest way is to use a package manager like npm.  
   Bash  
   npm install onnxruntime-web

   The necessary .js and .wasm files from node\_modules/onnxruntime-web/dist/ must then be copied into the project's wwwroot folder so they can be served as static assets.15

### **3.2 JavaScript Interop Service**

The bridge between C\# and the ONNX Runtime is a dedicated JavaScript interop service. This involves creating a JavaScript module and a corresponding C\# service to invoke it.

1. Create the JavaScript Module (wwwroot/js/onnxInterop.js):  
   This module will encapsulate all interactions with the onnxruntime-web library. It will expose functions to initialize the runtime, load models, and run inference.  
   JavaScript  
   import \* as ort from '../lib/onnxruntime-web/ort.min.js';

   let textSession;  
   let imageSession;

   export async function initializeOnnx() {  
       // Configure the path to the WASM binaries  
       ort.env.wasm.wasmPaths \= '/lib/onnxruntime-web/';  
       // Pre-load the models from URLs (or IndexedDB)  
       const textModelUrl \= 'https://\<your-r2-bucket\>/all-MiniLM-L6-v2.onnx';  
       const imageModelUrl \= 'https://\<your-r2-bucket\>/mobilenetv2.onnx';

       textSession \= await ort.InferenceSession.create(textModelUrl);  
       imageSession \= await ort.InferenceSession.create(imageModelUrl);  
       return true;  
   }

   export async function embedText(text) {  
       // NOTE: A JS tokenizer like 'bert-tokenizer' would be needed here  
       // For simplicity, this is a placeholder for tokenization logic.  
       const encoded \= await myTokenizer.encode(text);

       const feeds \= {  
           input\_ids: new ort.Tensor('int64', BigInt64Array.from(encoded.input\_ids.map(BigInt)), \[1, encoded.input\_ids.length\]),  
           attention\_mask: new ort.Tensor('int64', BigInt64Array.from(encoded.attention\_mask.map(BigInt)), \[1, encoded.attention\_mask.length\])  
       };

       const results \= await textSession.run(feeds);  
       return results.last\_hidden\_state.data;  
   }

2. Create the C\# Wrapper Service (OnnxInteropService.cs):  
   This C\# class uses IJSRuntime to call the functions in the JavaScript module..NET 10 introduces new JS interop features that can make this more efficient.16  
   C\#  
   public class OnnxInteropService : IAsyncDisposable  
   {  
       private readonly Lazy\<Task\<IJSObjectReference\>\> \_moduleTask;

       public OnnxInteropService(IJSRuntime jsRuntime)  
       {  
           \_moduleTask \= new(() \=\> jsRuntime.InvokeAsync\<IJSObjectReference\>(  
               "import", "./js/onnxInterop.js").AsTask());  
       }

       public async Task\<bool\> InitializeOnnxAsync()  
       {  
           var module \= await \_moduleTask.Value;  
           return await module.InvokeAsync\<bool\>("initializeOnnx");  
       }

       public async Task\<float\> EmbedTextAsync(string text)  
       {  
           var module \= await \_moduleTask.Value;  
           return await module.InvokeAsync\<float\>("embedText", text);  
       }

       public async ValueTask DisposeAsync()  
       {  
           if (\_moduleTask.IsValueCreated)  
           {  
               var module \= await \_moduleTask.Value;  
               await module.DisposeAsync();  
           }  
       }  
   }

3. Register the Service:  
   In Program.cs, register the service for dependency injection.  
   C\#  
   builder.Services.AddScoped\<OnnxInteropService\>();

### **3.3 Asset Caching with IndexedDB**

To avoid re-downloading multi-megabyte models, we use IndexedDB. While Blazor doesn't have a built-in API for this, it's easily accomplished via JS interop or by using a community library.12

1. **JavaScript Caching Logic (wwwroot/js/cacheManager.js):**  
   JavaScript  
   export async function cacheAsset(url, key) {  
       const response \= await fetch(url);  
       const blob \= await response.blob();  
       // Logic to open IndexedDB and store the blob with the given key  
       //...  
   }

   export async function getAssetFromCache(key) {  
       // Logic to retrieve the blob from IndexedDB  
       //...  
   }

2. Blazor Component Logic:  
   In the main component, on the first load, check if the assets exist in IndexedDB. If not, fetch them from R2, display progress, and store them in the cache for future use.

## **Section 4: Deployment and The Future**

### **4.1 Deployment to Cloudflare**

Deploying the application is a straightforward process for static sites 9:

1. **Build the Project:** Run dotnet publish \-c Release to generate the optimized, AOT-compiled output in the wwwroot directory.  
2. **Upload to Cloudflare Pages:** Connect a GitHub repository to a Cloudflare Pages project. The build command will be ./build.sh (a script to install the.NET SDK and run the publish command), and the output directory will be output/wwwroot.9  
3. **Upload Assets to R2:** Upload the games.sqlite file and the .onnx model files to a Cloudflare R2 bucket. Ensure the bucket is configured for public access or accessed via a custom domain so the Blazor application can fetch the files.10

### **4.2 The Road Ahead with WebAssembly**

The future of WebAssembly is bright, with upcoming standards that will further enhance the capabilities of this architecture 8:

* **WasmGC (Garbage Collection):** As WasmGC becomes standard across all browsers, the performance and memory management of.NET applications in Blazor will improve significantly, reducing the overhead of shipping a.NET garbage collector with the app.19  
* **Enhanced SIMD and Multithreading:** Broader support and more advanced instructions will allow client-side AI tasks to achieve performance even closer to native applications.  
* **.NET 10 and Beyond:** Future versions of.NET will continue to deepen their integration with WebAssembly, offering better performance, smaller payloads, and more direct access to browser APIs, further solidifying the viability of this cost-free semantic search template.

By open-sourcing this template, we provide the community with a powerful, secure, and economically disruptive alternative to traditional AI infrastructure, empowering developers worldwide to build the next generation of intelligent applications.

#### **Works cited**

1. Web | onnxruntime, accessed August 31, 2025, [https://onnxruntime.ai/docs/tutorials/web/](https://onnxruntime.ai/docs/tutorials/web/)  
2. onnxruntime-web \- NPM, accessed August 31, 2025, [https://www.npmjs.com/package/onnxruntime-web](https://www.npmjs.com/package/onnxruntime-web)  
3. The New Features and Enhancements in .NET 10 \- CODE Magazine, accessed August 31, 2025, [https://www.codemag.com/Article/2507051/The-New-Features-and-Enhancements-in-.NET-10](https://www.codemag.com/Article/2507051/The-New-Features-and-Enhancements-in-.NET-10)  
4. What's new in .NET 10 | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)  
5. Blazor performance tuning for Production: Insights and lessons learned \- Reddit, accessed August 31, 2025, [https://www.reddit.com/r/Blazor/comments/1guc532/blazor\_performance\_tuning\_for\_production\_insights/](https://www.reddit.com/r/Blazor/comments/1guc532/blazor_performance_tuning_for_production_insights/)  
6. ASP.NET Core Blazor Best Practices — Architecture and Performance Optimization, accessed August 31, 2025, [https://blog.devart.com/asp-net-core-blazor-best-practices-architecture-and-performance-optimization.html](https://blog.devart.com/asp-net-core-blazor-best-practices-architecture-and-performance-optimization.html)  
7. ONNX Runtime Web—running your machine learning model in browser, accessed August 31, 2025, [https://opensource.microsoft.com/blog/2021/09/02/onnx-runtime-web-running-your-machine-learning-model-in-browser/](https://opensource.microsoft.com/blog/2021/09/02/onnx-runtime-web-running-your-machine-learning-model-in-browser/)  
8. The State of WebAssembly – 2024 and 2025 \- Uno Platform, accessed August 31, 2025, [https://platform.uno/blog/state-of-webassembly-2024-2025/](https://platform.uno/blog/state-of-webassembly-2024-2025/)  
9. Blazor · Cloudflare Pages docs, accessed August 31, 2025, [https://developers.cloudflare.com/pages/framework-guides/deploy-a-blazor-site/](https://developers.cloudflare.com/pages/framework-guides/deploy-a-blazor-site/)  
10. Overview · Cloudflare R2 docs, accessed August 31, 2025, [https://developers.cloudflare.com/r2/](https://developers.cloudflare.com/r2/)  
11. IndexedDB Storage in Blazor WebAssembly .NET 7 \- Blazor School, accessed August 31, 2025, [https://blazorschool.com/tutorial/blazor-wasm/dotnet7/indexeddb-storage-261461](https://blazorschool.com/tutorial/blazor-wasm/dotnet7/indexeddb-storage-261461)  
12. Client-Side Cache in Blazor using Index DB (Part 3\) \- Chandradev's Blog, accessed August 31, 2025, [https://chandradev819.in/2025/02/02/client-side-cache-in-blazor-using-index-db-part-3/](https://chandradev819.in/2025/02/02/client-side-cache-in-blazor-using-index-db-part-3/)  
13. Blazor Basics: Optimizing Performance in Blazor WebAssembly \- Telerik.com, accessed August 31, 2025, [https://www.telerik.com/blogs/blazor-basics-optimizing-performance-blazor-webassembly-applications](https://www.telerik.com/blogs/blazor-basics-optimizing-performance-blazor-webassembly-applications)  
14. ASP.NET Core Blazor performance best practices | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/?view=aspnetcore-9.0)  
15. onnxruntime-web fails to find ort-wasm-simd.wasm, doesn't use my /static folder in SvelteKit, accessed August 31, 2025, [https://stackoverflow.com/questions/77179151/onnxruntime-web-fails-to-find-ort-wasm-simd-wasm-doesnt-use-my-static-folder](https://stackoverflow.com/questions/77179151/onnxruntime-web-fails-to-find-ort-wasm-simd-wasm-doesnt-use-my-static-folder)  
16. What's new in ASP.NET Core in .NET 10 | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-9.0)  
17. Deploy Blazor to Cloudflare \- YouTube, accessed August 31, 2025, [https://www.youtube.com/watch?v=ABkmHTMgkbc](https://www.youtube.com/watch?v=ABkmHTMgkbc)  
18. Use R2 as static asset storage with Cloudflare Pages, accessed August 31, 2025, [https://developers.cloudflare.com/pages/tutorials/use-r2-as-static-asset-storage-for-pages/](https://developers.cloudflare.com/pages/tutorials/use-r2-as-static-asset-storage-for-pages/)  
19. Why Blazor WASM is so slow compared to other WASM frameworks? : r/dotnet \- Reddit, accessed August 31, 2025, [https://www.reddit.com/r/dotnet/comments/13dsibj/why\_blazor\_wasm\_is\_so\_slow\_compared\_to\_other\_wasm/](https://www.reddit.com/r/dotnet/comments/13dsibj/why_blazor_wasm_is_so_slow_compared_to_other_wasm/)

---

## **Report 04**

## **The Low-Cost Architecture Blueprint**

This section establishes the foundational architectural strategy for the 'Actual Game Search' project, driven by the core requirement of being "extraordinarily low-cost." The system's components and the rationale behind the technology stack are detailed, emphasizing how each choice contributes to minimizing operational expenditure and resolving prior project inconsistencies.

### **Core Principles: Offloading Compute to the Client**

The central pillar of this architecture is the strategic decision to offload the computationally expensive task of machine learning inference—specifically, the generation of embedding vectors—to the client's browser. This approach represents a fundamental departure from traditional server-centric AI application models, where inference workloads necessitate powerful and costly server infrastructure. By leveraging modern web technologies, the computational burden is shifted from the provider to the consumer, drastically altering the economic model of the service.

This client-side computation is made feasible and performant through the combination of Blazor WebAssembly and ONNX Runtime Web. Blazor WebAssembly (WASM) allows for the execution of.NET C\# code directly in the browser, providing a rich, interactive application environment without constant server communication.1 ONNX Runtime Web complements this by compiling a high-performance, native machine learning inference engine into a WebAssembly module.2 This enables the execution of complex neural network models at near-native speeds directly on the end-user's CPU, utilizing features like multi-threading and SIMD where available to further accelerate processing.2

The primary implication of this architectural choice is a dramatic reduction in server-side costs. The project entirely sidesteps the need for expensive, GPU-enabled cloud instances or scalable serverless compute services that are typically required for ML model inference. The server's role is consequently relegated to that of a simple data custodian and query executor. It becomes responsible only for serving the initial application payload and performing lightweight database lookups, functions that can be reliably handled by the most basic, low-cost commodity web hosting tiers available. This strategic offloading is the single most critical decision in achieving the project's "extraordinarily low-cost" mandate.

### **System Overview: The Hosted Blazor WASM and Lightweight API Model**

The system will be structured as a **Hosted Blazor WebAssembly application**. This deployment model provides a cohesive and streamlined development experience by combining the frontend and backend into a single, deployable ASP.NET Core project.4 This unified structure simplifies configuration, deployment, and maintenance, directly addressing the need for a clear and consistent project plan.

The operational flow is as follows:

1. **Initial Load:** Upon the first visit, the user's browser downloads the complete Blazor WASM application. This payload includes the application's.NET assemblies, the.NET WebAssembly runtime, and the required ONNX model files.5  
2. **Client-Side Interaction:** The user provides input, either text (a search query) or an image (an uploaded file).  
3. **In-Browser Inference:** The Blazor WASM application, running entirely on the user's machine, preprocesses the input and uses the ONNX Runtime Web library to generate a high-dimensional embedding vector. This entire process occurs without any communication with the server.  
4. **API Request:** Once the embedding vector is generated, the Blazor application makes a single, lightweight HTTP request to its hosting ASP.NET Core backend. The payload of this request contains only the computed vector.  
5. **Backend Search:** The ASP.NET Core API receives the vector and executes a similarity search against a pre-populated vector database.  
6. **API Response:** The backend returns a small, efficient response, typically a ranked list of game identifiers and their corresponding similarity scores.  
7. **Result Visualization:** The Blazor application receives the list of game IDs, fetches any additional metadata required for display, and renders the results to the user.

This model ensures that the server is never involved in the complex and resource-intensive ML computation, thereby maintaining its lightweight and low-cost nature.

### **Technology Stack and Rationale**

The selection of each technology has been carefully considered to align with the project's primary constraints of minimal cost and architectural clarity. The stack is designed to be robust, maintainable, and highly efficient from both a performance and cost perspective.

* **Frontend Framework: Blazor WebAssembly**  
  * **Role:** To build the interactive client-side user interface.  
  * **Rationale:** Blazor WASM enables the development of the entire frontend using C\# and the.NET ecosystem, which promotes code sharing with the backend and allows the team to leverage existing C\# skills.1 Its ability to run.NET code in the browser is the foundational technology that makes this client-centric architecture possible.  
* **Client-Side ML Runtime: ONNX Runtime Web**  
  * **Role:** To execute the machine learning model within the browser for embedding generation.  
  * **Rationale:** ONNX Runtime Web is a high-performance inference engine specifically designed for this purpose. It supports the open ONNX model format, ensuring compatibility with models trained in popular frameworks like PyTorch or TensorFlow.2 Its WebAssembly backend provides near-native speed, which is essential for a responsive user experience, and it is the key enabler for offloading compute from the server.3  
* **Backend Framework: ASP.NET Core Web API**  
  * **Role:** To serve the Blazor WASM application and provide the similarity search API endpoint.  
  * **Rationale:** ASP.NET Core is a high-performance, lightweight, and cross-platform framework. Using its minimal API feature set allows for the creation of an extremely lean backend with minimal boilerplate code. The seamless integration within the Hosted Blazor WASM model simplifies deployment into a single, cohesive unit.4  
* **Vector Database: SQLite with sqlite-vss Extension**  
  * **Role:** To store and perform K-Nearest Neighbor (KNN) search on the pre-computed game embedding vectors.  
  * **Rationale:** SQLite is a serverless, file-based database engine that incurs zero additional infrastructure or licensing costs. It is embedded directly within the application process, eliminating the complexity and expense of managing a separate database server.9 The  
    sqlite-vss extension (or a similar alternative like sqlite-vec) adds powerful and efficient vector search capabilities directly to SQLite, providing a robust, no-cost solution for the project's core backend task.10  
* **Machine Learning Model: CLIP (openai/clip-vit-base-patch32)**  
  * **Role:** To convert user-provided text and images into meaningful numeric representations (embedding vectors).  
  * **Rationale:** The CLIP model is a state-of-the-art, multi-modal model renowned for its ability to create a shared embedding space for both text and images.13 This allows the application to perform powerful cross-modal searches (e.g., searching for games using a descriptive phrase or a reference image). Sourcing a pre-trained version in the ONNX format from a repository like Hugging Face eliminates the costly and time-consuming process of training a model from scratch.15

The following table provides a consolidated view of the technology stack, explicitly linking each component to the project's low-cost objective.

| Technology | Role in Architecture | Rationale for Low-Cost Implementation |
| :---- | :---- | :---- |
| **Blazor WebAssembly** | Client-Side UI Framework | Enables rich, interactive applications that run on the user's hardware, forming the foundation for offloading compute tasks. |
| **ONNX Runtime Web** | In-Browser ML Inference Engine | Executes complex ML models on the client device, eliminating the primary cost driver: server-side GPU/CPU compute for inference.2 |
| **ASP.NET Core (Minimal API)** | Backend API & Web Server | Provides a highly efficient, low-resource-footprint server that can be hosted on the cheapest commodity hardware or PaaS tiers. |
| **SQLite with sqlite-vss** | Embedded Vector Database | A serverless, file-based solution that requires no separate database server, incurring zero additional infrastructure, licensing, or maintenance costs.10 |
| **CLIP (ONNX Format)** | Pre-trained Embedding Model | Utilizes a powerful, freely available pre-trained model, avoiding the significant expense associated with custom model development and training.15 |

## **Frontend Implementation: Blazor WebAssembly and In-Browser Inference**

This section provides a detailed plan for developing the Blazor WebAssembly frontend. The primary focus is on the technical implementation of in-browser machine learning inference, which involves integrating the JavaScript-based ONNX Runtime, managing the ML model on the client, and processing user input to generate embedding vectors.

### **Project Setup and ONNX Runtime Web Integration**

The project will be initiated using the "Blazor Web App" template with the "WebAssembly" interactivity mode, configured for a hosted deployment. This standard.NET template creates a solution with three projects: a Client project for the Blazor WASM components, a Server project for the ASP.NET Core backend, and a Shared project for common data models.8

The integration of ONNX Runtime Web, a JavaScript library, requires careful management of its static assets. The onnxruntime-web npm package will be installed within the Server project's directory. This allows the build process to place the necessary JavaScript (.js) and WebAssembly (.wasm) files into the Server project's wwwroot folder. These files will then be served to the client as static assets alongside the Blazor application itself.7

A reference to the main onnxruntime-web.min.js script will be added to the App.razor file (or a similar root component) within the Client project. It is critical to ensure that the associated .wasm binary files (e.g., ort-wasm-simd-threaded.wasm) are correctly located and served. If these files are not in the same directory as the main JavaScript bundle, their path must be explicitly configured in the client-side code using the ort.env.wasm.wasmPaths property. This can point to a relative path on the server or even a public CDN for optimized delivery.20

### **The JavaScript Interop Bridge: Calling the ONNX Runtime from C\#**

As ONNX Runtime Web exposes a JavaScript API, all interactions from the Blazor application's C\# code must be channeled through Blazor's JavaScript interoperability layer, which is accessible via the IJSRuntime service.21

A direct and scattered use of IJSRuntime.InvokeAsync throughout the application's components would lead to brittle, hard-to-maintain code. To prevent this and establish a robust architectural pattern, a dedicated C\# service, OnnxInferenceService, will be created and registered for dependency injection. This service will act as an abstraction layer, encapsulating all JS interop logic and exposing a clean, strongly-typed C\# API to the rest of the application.

To support this service, a corresponding JavaScript module (onnx-interop.js) will be created in the Server project's wwwroot and referenced by the client. This module will contain wrapper functions that simplify the interaction with the ONNX Runtime Web library:

* **initOnnxSession(modelPath):** This asynchronous function will be responsible for creating an ort.InferenceSession. It will fetch the .onnx model from the provided path, create the session, and cache it in a JavaScript variable to avoid redundant initializations on subsequent inference calls.2  
* **runInference(session, inputTensors):** This function will take the pre-processed input tensors passed from C\#, execute the core session.run(inputTensors) method, and return the resulting output tensor data. This isolates the Blazor application from the specifics of the ONNX Runtime API.2

The OnnxInferenceService in C\# will use JSRuntime.InvokeAsync\<TValue\> to call these JavaScript functions. It will handle the serialization of complex C\# objects (such as pre-processed image data) into JSON, which Blazor's interop layer manages automatically, and deserialize the results returned from JavaScript.23 This pattern isolates the "seam" between the C\# and JavaScript worlds, making the system more modular and easier to update if the underlying JavaScript library evolves.

### **Model Selection and Management: Acquiring and Caching the CLIP ONNX Model**

The project will utilize the openai/clip-vit-base-patch32 model, which will be sourced from the Hugging Face Hub in its pre-converted ONNX format.15 As a multi-modal model, CLIP is composed of two distinct parts: a text encoder and an image encoder. The pipeline must therefore acquire and manage two separate

.onnx files.15

These .onnx model files will be treated as static assets and placed within the Server project's wwwroot/models directory. From there, they will be served to the client upon request. Given that these model files can be several hundred megabytes in size, downloading them on every application visit would result in a poor user experience and excessive bandwidth consumption.

To mitigate this, a client-side caching strategy is essential. The initOnnxSession JavaScript function will be enhanced to use the browser's IndexedDB API. Before attempting to fetch a model from the network, the function will first check if the model file already exists in IndexedDB. If it does, the model will be loaded directly from the local cache. If not, it will be fetched from the server, used to create the inference session, and then stored in IndexedDB for future use. This ensures that the large model download occurs only once, significantly improving the performance of subsequent visits.20

### **Client-Side Embedding Generation: Processing User Input**

The OnnxInferenceService will expose two primary methods for generating embeddings, one for text and one for images. Both will orchestrate the necessary preprocessing, JS interop calls, and post-processing.

#### **For Text Input**

When a user submits a search query, the following steps will occur:

1. The C\# service receives the raw search string.  
2. It passes this string to a dedicated JS interop function. This function will use a JavaScript-based tokenizer compatible with the CLIP model, such as a lightweight bert-tokenizer library, to convert the text into a sequence of token IDs.3  
3. The tokenizer's output is used to construct the input tensors required by the CLIP text encoder: input\_ids and attention\_mask.3  
4. The C\# service then calls the runInference JS function, passing a reference to the text encoder session and the newly created tensors.  
5. The JS function executes the model and returns the output embedding vector, which is deserialized back into a float in C\#.

#### **For Image Input**

When a user uploads an image for a similarity search:

1. The image file is read into a byte stream within the Blazor application.  
2. This data is passed to a series of preprocessing functions, which can be implemented in either C\# or through JS interop for performance. These functions will perform the transformations required by the CLIP vision model: resizing the image to a 224x224 resolution, performing a center crop, rescaling pixel values to a 0−1 range, and normalizing them with the specific mean and standard deviation values the model was trained on.13  
3. The processed image data is arranged into the final pixel\_values input tensor, with the shape \[batch\_size, num\_channels, height, width\].  
4. The C\# service calls the runInference JS function, passing a reference to the image encoder session and the pixel\_values tensor.  
5. The resulting embedding vector is returned and deserialized into a float in C\#.

### **State Management and API Communication**

Once the OnnxInferenceService successfully returns an embedding vector, the responsible Blazor component will update its state to reflect that it is now performing a search (e.g., displaying a loading spinner). It will then use a standard, dependency-injected HttpClient to issue a POST request to the backend API endpoint (/api/search). The request body will be a simple JSON object containing the float embedding vector. The component will asynchronously await the response from the API before proceeding to the result display phase.

## **Backend Implementation: Lightweight Similarity Search with ASP.NET Core and SQLite**

This section details the design and implementation of the minimalist backend API. The architecture is intentionally simple, focusing on a single responsibility: performing an efficient, low-cost vector similarity search. This simplicity is a direct result of offloading all complex machine learning logic to the client.

### **Database Strategy: Leveraging SQLite with a Vector Search Extension**

The backend's data storage and query needs will be met by SQLite. This choice is fundamental to the low-cost architecture. As a serverless, embedded database engine, SQLite eliminates the need for a separate database server process, which in turn removes associated costs for hosting, administration, licensing, and maintenance.9 The entire database will be contained within a single file (

games.db) deployed alongside the ASP.NET Core application, simplifying the deployment package and operational footprint.

Standard SQLite does not natively support vector similarity search. To enable this crucial functionality, a specialized native extension will be integrated. The sqlite-vss extension, which is based on the high-performance Faiss library, is an excellent candidate.11 Alternatives like

sqlite-vec also provide the necessary capabilities.10 These extensions augment SQLite's SQL dialect with functions and virtual table modules specifically designed for storing vector embeddings and executing efficient K-Nearest Neighbor (KNN) searches.10

### **Database Schema and Vector Indexing**

The database schema will be straightforward, consisting of two primary tables. The first will be a standard SQL table for storing essential game metadata.

**Games Table Schema:**

* GameId (INTEGER, PRIMARY KEY)  
* SteamAppId (INTEGER, UNIQUE)  
* Title (TEXT)  
* HeaderImageUrl (TEXT)

The second, and more critical, table will be a virtual table created using the syntax provided by the sqlite-vss extension. This virtual table is optimized for storing and indexing the high-dimensional embedding vectors.

**vss\_game\_embeddings Virtual Table Schema (using vss0 module):**

SQL

CREATE VIRTUAL TABLE vss\_game\_embeddings USING vss0(  
    embedding(512)  
);

This command defines a virtual table where each row's rowid implicitly corresponds to a GameId and contains a single column named embedding designed to hold a 512-dimensional floating-point vector, matching the output dimension of the openai/clip-vit-base-patch32 model.11 The offline data ingestion pipeline, described in the subsequent section, will be responsible for populating both of these tables.

### **Implementing the Similarity Search API Endpoint**

A single, focused API endpoint will be created within the ASP.NET Core Server project using the minimal API syntax for brevity and performance. This endpoint will serve as the sole point of interaction for the Blazor frontend when executing a search.

The following table formally defines the contract for this endpoint.

| Endpoint | HTTP Method | Request Payload | Response Payload | Description |
| :---- | :---- | :---- | :---- | :---- |
| /api/search | POST | {"vector": \[0.1, 0.2,... \]} | \[{"gameId": 123, "distance": 0.85},...\] | Accepts a 512-dimension float array (embedding vector) and returns a ranked list of the top N most similar game IDs and their similarity distance scores. |

The C\# implementation for this endpoint will perform the following logic:

1. Define the endpoint route and accept the request body, deserializing the JSON payload into a C\# model containing a float vector.  
2. Establish a connection to the games.db SQLite database file.  
3. Construct a SQL query designed to leverage the sqlite-vss extension's KNN search functionality. The query will use the vss\_search function, passing the user's query vector as a parameterized blob to prevent SQL injection and ensure correct data handling.  
4. Execute the query against the database.  
5. Map the results (which consist of rowid and distance) to a list of response objects.  
6. Return the list of results as a JSON array with a 200 OK status code.

An example of the core SQL query to be executed is as follows:

SQL

SELECT  
    rowid,  
    distance  
FROM  
    vss\_game\_embeddings  
WHERE  
    vss\_search(embedding, @query\_vector)  
LIMIT 20;

Here, @query\_vector is a parameter that will be bound to the byte representation of the user's float vector.12

### **C\# Data Access: Loading the Native Extension with EF Core Interceptors**

While raw ADO.NET (Microsoft.Data.Sqlite) can be used for data access, integrating Entity Framework Core can provide a more robust and maintainable data layer, especially if the application's data model grows in complexity. A significant challenge when using an ORM with SQLite extensions is ensuring that the native library file (.dll on Windows, .so on Linux) for the extension is loaded into the connection before any queries are executed.

The most elegant and non-invasive solution to this problem is to implement a DbConnectionInterceptor from Entity Framework Core.29 This pattern allows for the interception of database operations at various points in their lifecycle.

A custom interceptor, SqliteVssInterceptor, will be created. This class will override the ConnectionCreated method. Inside this method, the code will:

1. Receive the newly created DbConnection object.  
2. Safely cast it to a SqliteConnection.  
3. Call sqliteConnection.EnableExtensions(true) to permit the loading of extensions.  
4. Call sqliteConnection.LoadExtension("path/to/vss0.dll") to dynamically load the vector search extension library.29

This interceptor is then registered with the application's DbContext during its configuration in Program.cs by using the AddInterceptors method on the DbContextOptionsBuilder. This ensures that every database connection pooled or created by EF Core for this DbContext will automatically have the sqlite-vss extension loaded and ready for use, without cluttering the application's business logic with data access infrastructure concerns.30

## **Data Ingestion and Processing Pipeline**

The success of the 'Actual Game Search' project is critically dependent on a robust, offline data ingestion and processing pipeline. This pipeline is responsible for populating the SQLite vector database that powers the backend's search functionality. Although this component is not part of the live, user-facing application, its correctness and consistency are paramount to the entire system's utility. It is the process that generates the "ground truth" against which all user queries are compared.

### **Sourcing Game Data: A Strategy for Using the Steam Web API**

A dedicated, standalone console application will be developed to orchestrate the data ingestion process. This application will interact with various Steam data sources to collect the raw information needed for building the search index.

The primary data source will be the Steam Web API. The pipeline will begin by making a request to an endpoint that provides a comprehensive list of all applications available on the platform, retrieving their names and unique AppIDs.31 With this master list, the pipeline will iterate through each game to gather more detailed information.

For textual data, the pipeline will utilize the appreviews endpoint. This endpoint allows for the retrieval of user reviews for a given AppID.33 The process will filter for highly-rated, substantive reviews in English, as the text from these reviews serves as an excellent source for generating representative embeddings that capture the essence of the gameplay experience and community sentiment.35

Acquiring image data, specifically game screenshots, presents a greater challenge as there is no official, publicly documented Web API for this purpose. The ISteamScreenshots API is designed for in-game client integration and is not suitable for this pipeline.36 Therefore, the pipeline will employ a more pragmatic approach by leveraging the unofficial but widely used

appdetails storefront API endpoint. This endpoint, when queried with an AppID, returns a JSON object that includes URLs to the game's store page header image and screenshots.37 As a fallback, direct web scraping of the game's store page using libraries like BeautifulSoup or lxml can be implemented.38 The pipeline must be designed with robust error handling and respect for rate limits to ensure stable and reliable data collection from these unofficial sources.

### **Offline Data Processing: Generating and Storing Embeddings**

Once the raw text and image data have been collected, the pipeline will process them to generate the embedding vectors for the database. A critical requirement for this stage is to maintain absolute consistency with the client-side inference process. The pipeline must use the **exact same version of the ONNX CLIP model** (openai/clip-vit-base-patch32 text and image encoders) and implement the **identical preprocessing logic** as the Blazor WASM frontend. Any discrepancy in the model, tokenization method, or image normalization parameters would result in the database vectors and query vectors occupying different, incompatible vector spaces, rendering the similarity search functionally useless.

The processing workflow will be as follows:

1. For each game, concatenate the text from its top-rated reviews and official description into a single document.  
2. Process this document through the CLIP text encoder to generate a text-based embedding vector.  
3. For each game, download its primary screenshots and header image.  
4. Process each image through the CLIP image encoder to generate multiple image-based embedding vectors.  
5. To create a single, representative vector for each game, a fusion strategy will be employed. This will likely involve averaging the vectors generated from the text and the top images. This combined vector captures both the textual description and visual aesthetic of the game.  
6. The final, aggregated embedding vector for each game is then inserted into the vss\_game\_embeddings virtual table in the SQLite database. Correspondingly, the game's metadata (SteamAppId, Title, etc.) is inserted into the Games table, ensuring the rowid of the vector entry matches the GameId of the metadata entry.

### **Pipeline Automation and Maintenance**

The data ingestion pipeline is designed to be executed periodically—for example, on a weekly or bi-weekly schedule—to keep the database current with new game releases and recent reviews. Each execution of the pipeline will generate a completely new SQLite database file (e.g., games\_YYYY-MM-DD.db).

This approach aligns with the principles of immutable infrastructure. Instead of performing complex, in-place updates on the live database, the deployment process is simplified to an atomic file replacement. The newly generated and validated database file is uploaded to the server, and a symbolic link or application configuration is updated to point to the new file. The web application is then gracefully restarted to begin using the updated data. This method is exceptionally reliable, simplifies rollbacks (by simply pointing back to the previous file), and ensures that the live API is always serving a complete and consistent dataset.

## **User Experience and High-Performance Result Visualization**

This section addresses the user-facing aspects of the project, focusing on delivering a fast, responsive, and intuitive interface for displaying search results. The implementation will strictly adhere to the "not too heavy on the browser" constraint by employing modern Blazor features and lightweight design principles.

### **Designing a Lightweight Results Display**

The primary interface for displaying search results will be a clean, uncluttered grid or list view. Each item in the view will present essential game information in a visually digestible format, such as the game's capsule image, title, and perhaps a concise representation of its similarity score. The design philosophy will prioritize simplicity and performance over complex, resource-intensive animations or heavy graphical elements.

To accelerate development and ensure a professional appearance, the project can leverage a third-party Blazor component library such as Radzen, Syncfusion, or Blazor Bootstrap.39 However, components will be selected judiciously, favoring those that are lightweight and optimized for performance. For instance, when displaying screenshots on a game's detail page, a simple image gallery or carousel component would be appropriate, configured to avoid pre-loading all images at once.42

### **Implementing Component Virtualization for Efficient Scrolling**

A key challenge in displaying search results is handling a potentially large number of items without degrading browser performance. Rendering hundreds of complex components simultaneously can create thousands of DOM elements, leading to high memory consumption and a sluggish, unresponsive user interface ("jank").

To preemptively solve this problem, the application will make extensive use of Blazor's built-in \<Virtualize\> component.45 Virtualization is a rendering technique that dramatically improves perceived performance by rendering only the items that are currently visible within the user's viewport, plus a small buffer of items above and below. As the user scrolls, the

\<Virtualize\> component intelligently renders new items as they come into view and removes items that have scrolled out of view from the DOM.

This approach is perfectly suited for the 'Actual Game Search' application. It ensures that the memory footprint and DOM complexity remain low and constant, regardless of whether the result set contains fifty or five hundred games. This results in a consistently smooth and fluid scrolling experience, directly fulfilling the requirement for a UI that is not "heavy on the browser".47

### **Optimizing Performance with Lazy Loading of Assets and Assemblies**

The initial load time of a Blazor WebAssembly application can be a significant performance consideration, as the browser must download the.NET runtime, application assemblies, and other assets like the ONNX model. To create a faster initial experience, the application will implement a lazy loading strategy for non-essential components and assemblies.49

Functionality that is not required for the main search page—such as a detailed game view, user profile pages, or administrative panels—will be encapsulated within separate Razor Class Libraries (RCLs). In the main application's project file, these RCLs will be marked for lazy loading using the \<BlazorWebAssemblyLazyLoad\> item group.51

The application's Router component will be configured with an OnNavigateAsync event handler. This handler will inspect the requested route and, if it corresponds to a feature in a lazy-loaded assembly, it will use the LazyAssemblyLoader service to dynamically fetch and load the required assembly from the server. This "on-demand" approach can significantly reduce the initial download size, improving the critical Time to Interactive metric for first-time visitors.51 A similar lazy-loading principle will be applied to high-resolution images within the results list, ensuring they are only fetched from the network as they are about to scroll into the user's view.

### **Presenting Similarity: From Raw Scores to Intuitive Visuals**

The backend API returns a raw numerical value for each result, typically a cosine distance, which represents the mathematical similarity between the query vector and the game's vector. This score, while precise, is not intuitive for the average user.

The user interface will be responsible for translating this abstract score into a more meaningful and visually accessible format. Instead of displaying a raw number like "0.893", the UI could present this information as:

* A "Similarity" or "Match" progress bar.  
* A percentage value (e.g., "89% Match").  
* A qualitative label (e.g., "Very Similar").  
* An implicit ranking where the score is used only to order the results from most to least similar.

Simple and effective data visualization techniques will be employed, adhering to principles of clarity and minimalism. For example, color gradients (e.g., from green for high similarity to yellow for moderate similarity) or simple icons can be used to convey the strength of a match at a glance, without introducing performance overhead from complex charting libraries.53 The goal is to make the similarity results immediately understandable to the user, enhancing the overall usability of the application.

## **Deployment and Operational Strategy**

This final section outlines the comprehensive plan for publishing, hosting, and maintaining the 'Actual Game Search' application in a production environment. Every aspect of this strategy is aligned with the project's core tenet of minimizing operational costs while ensuring reliability and scalability.

### **Publishing the Hosted Blazor WebAssembly Application**

The deployment artifact will be generated using the standard.NET publishing process. Executing the dotnet publish \-c Release command from the solution's root directory will produce a self-contained, optimized set of files ready for deployment.4 This command compiles the ASP.NET Core

Server project and intelligently places all the published assets of the Blazor WASM Client project—including its.NET assemblies, the ONNX model files, and the JavaScript interop libraries—into the wwwroot directory of the Server project's output. The result is a single, coherent deployment package that can be hosted on any web server capable of running an ASP.NET Core application.5

### **Hosting Environment Recommendations for Minimal Cost**

The architecture's minimal server-side resource requirements open up a wide range of extremely cost-effective hosting options. The backend's primary tasks are serving static files and executing fast, localized SQLite queries, both of which have a very low CPU and memory footprint.

* **Option 1 (Lowest Cost \- IaaS):** A basic, budget-tier Linux Virtual Private Server (VPS). The published application files can be copied to the server, and the ASP.NET Core application can be configured to run as a persistent background service using systemd. A lightweight reverse proxy, such as Nginx, will be configured to handle incoming HTTP requests, manage SSL/TLS termination, and forward traffic to the running application. This approach offers the most control and the lowest possible monthly cost.4  
* **Option 2 (Low-Cost \- PaaS):** A Platform-as-a-Service (PaaS) offering, such as the Azure App Service Free (F1) or Basic (B1) tier, or an equivalent from another major cloud provider. This option abstracts away the underlying operating system management, simplifying the deployment process. While potentially slightly more expensive than a bare VPS, it reduces administrative overhead.4  
* **Option 3 (Containerization):** The application can be packaged into a Docker container. This creates a portable and consistent environment that can be deployed to any container-compatible host, including Docker Swarm, Kubernetes, or various container app services.5 This is an excellent choice for ensuring environment parity between development and production.

### **Monitoring and Scaling Considerations**

Given the simplicity of the backend, the monitoring and scaling strategies can also be straightforward and low-cost.

* **Monitoring:** Initial monitoring needs can be fully met by the built-in capabilities of ASP.NET Core. Structured logging can be configured to output to the console or a file, and the framework's built-in health check endpoints can be used by external services to verify application liveness and readiness.  
* **Scaling:** The backend API is stateless and performs only read operations against a local database file. This makes it exceptionally easy to scale horizontally. Should user traffic increase beyond the capacity of a single instance, the application can be scaled out by simply deploying more instances behind a load balancer. Each instance would have its own identical copy of the games.db SQLite file. This "shared-nothing" architecture for reads avoids the complexity and cost associated with a distributed database system, as there is no central database server to become a bottleneck. When the database is updated via the offline pipeline, the new games.db file is simply deployed to all running instances during the next release cycle. This simple yet powerful scaling model ensures that the application can handle significant read traffic while maintaining its low operational cost.

## **Conclusion**

This document presents a definitive and internally consistent plan for the 'Actual Game Search' project, designed to meet the explicit requirements of a low-cost architecture, a Blazor WebAssembly frontend with client-side inference, and a lightweight ASP.NET Core backend. The proposed architecture fundamentally re-engineers the economic model of a machine learning-powered application by strategically offloading the most computationally expensive task—ML inference—to the end-user's device. This single decision enables the entire system to operate on minimal, commodity-level server infrastructure, directly fulfilling the "extraordinarily low-cost" mandate.

The plan provides a clear, actionable blueprint for the development team, covering the full technology stack and implementation patterns. Key technical challenges, such as the integration of a JavaScript-based ML runtime into a C\# Blazor application and the loading of native SQLite extensions, are addressed with robust and maintainable architectural patterns like the C\# Interop Service and the EF Core DbConnectionInterceptor. Furthermore, the plan emphasizes high-performance UI/UX techniques, such as component virtualization and assembly lazy loading, to ensure the application remains responsive and lightweight in the browser.

By adhering to this plan, the development team can proceed with confidence, equipped with a clear understanding of the system's components, their interactions, and the rationale behind each architectural decision. The resulting application will be not only functionally powerful, offering advanced multi-modal similarity search, but also operationally efficient and economically sustainable.

#### **Works cited**

1. ASP.NET Core Blazor hosting models | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-9.0)  
2. ONNX Runtime Web—running your machine learning model in browser, accessed August 31, 2025, [https://opensource.microsoft.com/blog/2021/09/02/onnx-runtime-web-running-your-machine-learning-model-in-browser/](https://opensource.microsoft.com/blog/2021/09/02/onnx-runtime-web-running-your-machine-learning-model-in-browser/)  
3. Run AI Models Entirely in the Browser Using WebAssembly \+ ONNX Runtime (No Backend Required) \- DEV Community, accessed August 31, 2025, [https://dev.to/hexshift/run-ai-models-entirely-in-the-browser-using-webassembly-onnx-runtime-no-backend-required-4lag](https://dev.to/hexshift/run-ai-models-entirely-in-the-browser-using-webassembly-onnx-runtime-no-backend-required-4lag)  
4. Different Methods to Host and Deploy Blazor WebAssembly | by The Tech Platform | Medium, accessed August 31, 2025, [https://thetechplatform.medium.com/different-methods-to-host-and-deploy-blazor-webassembly-5433d99e973b](https://thetechplatform.medium.com/different-methods-to-host-and-deploy-blazor-webassembly-5433d99e973b)  
5. Host and deploy ASP.NET Core Blazor WebAssembly | Microsoft ..., accessed August 31, 2025, [https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/?view=aspnetcore-9.0)  
6. New Open Source ONNX Runtime Web Does Machine Learning Modeling in Browser, accessed August 31, 2025, [https://visualstudiomagazine.com/articles/2021/09/13/onnx-runtime-web.aspx](https://visualstudiomagazine.com/articles/2021/09/13/onnx-runtime-web.aspx)  
7. onnxruntime-web \- NPM, accessed August 31, 2025, [https://www.npmjs.com/package/onnxruntime-web](https://www.npmjs.com/package/onnxruntime-web)  
8. Net 8 Blazor with Interactive Webassembly Rendermode and Microservices in .Net Aspire, accessed August 31, 2025, [https://www.reddit.com/r/Blazor/comments/1itabx8/net\_8\_blazor\_with\_interactive\_webassembly/](https://www.reddit.com/r/Blazor/comments/1itabx8/net_8_blazor_with_interactive_webassembly/)  
9. Using the Semantic Kernel SQLite Vector Store connector (Preview ..., accessed August 31, 2025, [https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/sqlite-connector](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/sqlite-connector)  
10. How sqlite-vec Works for Storing and Querying Vector Embeddings ..., accessed August 31, 2025, [https://medium.com/@stephenc211/how-sqlite-vec-works-for-storing-and-querying-vector-embeddings-165adeeeceea](https://medium.com/@stephenc211/how-sqlite-vec-works-for-storing-and-querying-vector-embeddings-165adeeeceea)  
11. asg017/sqlite-vss: A SQLite extension for efficient vector search, based on Faiss\! \- GitHub, accessed August 31, 2025, [https://github.com/asg017/sqlite-vss](https://github.com/asg017/sqlite-vss)  
12. Introducing sqlite-vss: A SQLite Extension for Vector Search / Alex Garcia | Observable, accessed August 31, 2025, [https://observablehq.com/@asg017/introducing-sqlite-vss](https://observablehq.com/@asg017/introducing-sqlite-vss)  
13. CLIP \- Hugging Face, accessed August 31, 2025, [https://huggingface.co/docs/transformers/v4.34.1/model\_doc/clip](https://huggingface.co/docs/transformers/v4.34.1/model_doc/clip)  
14. CLIP \- Hugging Face, accessed August 31, 2025, [https://huggingface.co/docs/transformers/v4.22.1/en/model\_doc/clip](https://huggingface.co/docs/transformers/v4.22.1/en/model_doc/clip)  
15. mlunar/clip-variants \- Hugging Face, accessed August 31, 2025, [https://huggingface.co/mlunar/clip-variants](https://huggingface.co/mlunar/clip-variants)  
16. Xenova/clip-vit-base-patch32 · Hugging Face, accessed August 31, 2025, [https://huggingface.co/Xenova/clip-vit-base-patch32](https://huggingface.co/Xenova/clip-vit-base-patch32)  
17. How to add machine learning to your web application with ONNX Runtime, accessed August 31, 2025, [https://onnxruntime.ai/docs/tutorials/web/](https://onnxruntime.ai/docs/tutorials/web/)  
18. Models \- ONNX Runtime, accessed August 31, 2025, [https://onnxruntime.ai/models](https://onnxruntime.ai/models)  
19. How to Run Machine-Learning Models in the Browser using ONNX | HackerNoon, accessed August 31, 2025, [https://hackernoon.com/how-to-run-machine-learning-models-in-the-browser-using-onnx](https://hackernoon.com/how-to-run-machine-learning-models-in-the-browser-using-onnx)  
20. Deploying ONNX Runtime Web | onnxruntime, accessed August 31, 2025, [https://onnxruntime.ai/docs/tutorials/web/deploy.html](https://onnxruntime.ai/docs/tutorials/web/deploy.html)  
21. Guide to Blazor JavaScript Interop \- Imaginet Blog, accessed August 31, 2025, [https://imaginet.com/2021/guide-blazor-javascript-interop/](https://imaginet.com/2021/guide-blazor-javascript-interop/)  
22. ASP.NET Core Blazor JavaScript interoperability (JS interop) | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-9.0)  
23. Calling JavaScript from .NET \- Blazor University, accessed August 31, 2025, [https://blazor-university.com/javascript-interop/calling-javascript-from-dotnet/](https://blazor-university.com/javascript-interop/calling-javascript-from-dotnet/)  
24. Call JavaScript functions from .NET methods in ASP.NET Core Blazor | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet?view=aspnetcore-9.0)  
25. 11.3 ONNX Pipeline Models : CLIP Multi-Modal Embedding \- Oracle Help Center, accessed August 31, 2025, [https://docs.oracle.com/en/database/oracle/machine-learning/oml4py/2-23ai/mlpug/onnx-pipeline-models-multi-modal-embedding.html](https://docs.oracle.com/en/database/oracle/machine-learning/oml4py/2-23ai/mlpug/onnx-pipeline-models-multi-modal-embedding.html)  
26. Clip Vit Base Patch32 · Models \- Dataloop, accessed August 31, 2025, [https://dataloop.ai/library/model/openai\_clip-vit-base-patch32/](https://dataloop.ai/library/model/openai_clip-vit-base-patch32/)  
27. SQLite Vector Similarity Search \- Thought Eddies, accessed August 31, 2025, [https://www.danielcorin.com/til/sqlite/sqlite-vss/](https://www.danielcorin.com/til/sqlite/sqlite-vss/)  
28. CLIP \- Hugging Face, accessed August 31, 2025, [https://huggingface.co/docs/transformers/v4.48.0/model\_doc/clip](https://huggingface.co/docs/transformers/v4.48.0/model_doc/clip)  
29. c\# \- How to load extension for SQLite (SQLite \+ Entity Framework ..., accessed August 31, 2025, [https://stackoverflow.com/questions/77904865/how-to-load-extension-for-sqlite-sqlite-entity-framework-core](https://stackoverflow.com/questions/77904865/how-to-load-extension-for-sqlite-sqlite-entity-framework-core)  
30. Entity Framework Core Extension tips & tricks \- Injecting dependencies into EF Interceptors, accessed August 31, 2025, [https://maciejz.dev/entity-framework-core-extension-tips-tricks-interceptors/](https://maciejz.dev/entity-framework-core-extension-tips-tricks-interceptors/)  
31. Web API Overview \- Sign in to Steamworks, accessed August 31, 2025, [https://partner.steamgames.com/doc/webapi\_overview](https://partner.steamgames.com/doc/webapi_overview)  
32. Scraping information of all games from Steam with Python | by mmmmmm44 \- Medium, accessed August 31, 2025, [https://medium.com/codex/scraping-information-of-all-games-from-steam-with-python-6e44eb01a299](https://medium.com/codex/scraping-information-of-all-games-from-steam-with-python-6e44eb01a299)  
33. User Reviews \- Get List (Steamworks Documentation), accessed August 31, 2025, [https://partner.steamgames.com/doc/store/getreviews](https://partner.steamgames.com/doc/store/getreviews)  
34. Steam API: How to get the recent review sentiment? :: Help and Tips, accessed August 31, 2025, [https://steamcommunity.com/discussions/forum/1/4030224579607523412/](https://steamcommunity.com/discussions/forum/1/4030224579607523412/)  
35. Efficiently Scraping Steam Game Reviews with Python: A Comprehensive Guide \- Medium, accessed August 31, 2025, [https://medium.com/codex/efficiently-scraping-steam-game-reviews-with-python-a-comprehensive-guide-3a5732cb7f0b](https://medium.com/codex/efficiently-scraping-steam-game-reviews-with-python-a-comprehensive-guide-3a5732cb7f0b)  
36. ISteamScreenshots Interface (Steamworks Documentation), accessed August 31, 2025, [https://partner.steamgames.com/doc/api/isteamscreenshots](https://partner.steamgames.com/doc/api/isteamscreenshots)  
37. Looking for a way to grab all Steam header images \- Stack Overflow, accessed August 31, 2025, [https://stackoverflow.com/questions/26505768/looking-for-a-way-to-grab-all-steam-header-images](https://stackoverflow.com/questions/26505768/looking-for-a-way-to-grab-all-steam-header-images)  
38. Scraping Steam for Data using Python \+ BeautifulSoup \- JonLim.ca, accessed August 31, 2025, [https://jonlim.ca/blog/scraping-steam-data-using-python-beautifulsoup/](https://jonlim.ca/blog/scraping-steam-data-using-python-beautifulsoup/)  
39. Blazor Components | 100+ Native UI Controls \- Syncfusion, accessed August 31, 2025, [https://www.syncfusion.com/blazor-components](https://www.syncfusion.com/blazor-components)  
40. Free Blazor Components | 90+ UI controls by Radzen, accessed August 31, 2025, [https://blazor.radzen.com/](https://blazor.radzen.com/)  
41. Blazor Pagination Component, accessed August 31, 2025, [https://demos.blazorbootstrap.com/pagination](https://demos.blazorbootstrap.com/pagination)  
42. Blazor Gallery \- ServiceStack, accessed August 31, 2025, [https://blazor-gallery.servicestack.net/](https://blazor-gallery.servicestack.net/)  
43. Blazor Carousel \- Overview \- Demos, accessed August 31, 2025, [https://demos.telerik.com/blazor-ui/carousel/overview](https://demos.telerik.com/blazor-ui/carousel/overview)  
44. Blast Off with Blazor: Build a responsive image gallery \- Dave Brock, accessed August 31, 2025, [https://www.daveabrock.com/2020/12/16/blast-off-blazor-responsive-gallery/](https://www.daveabrock.com/2020/12/16/blast-off-blazor-responsive-gallery/)  
45. Displaying Lists Efficiently in Blazor \- Visual Studio Magazine, accessed August 31, 2025, [https://visualstudiomagazine.com/articles/2021/01/06/blazor-lists.aspx](https://visualstudiomagazine.com/articles/2021/01/06/blazor-lists.aspx)  
46. ASP.NET Core Razor component virtualization | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/aspnet/core/blazor/components/virtualization?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/virtualization?view=aspnetcore-9.0)  
47. Virtualization in Blazor ListView Component | Syncfusion, accessed August 31, 2025, [https://blazor.syncfusion.com/documentation/listview/virtualization](https://blazor.syncfusion.com/documentation/listview/virtualization)  
48. .NET 7 Update: Blazor Virtualization Update in 10 Minutes or Less \- YouTube, accessed August 31, 2025, [https://www.youtube.com/watch?v=oNZnYNUpu54](https://www.youtube.com/watch?v=oNZnYNUpu54)  
49. Blazor Basics: Lazy Load Assemblies to Boost the Performance of Blazor WebAssembly, accessed August 31, 2025, [https://www.telerik.com/blogs/blazor-basics-lazy-load-assemblies-boost-performance-blazor-webassembly](https://www.telerik.com/blogs/blazor-basics-lazy-load-assemblies-boost-performance-blazor-webassembly)  
50. Lazy Loading Syncfusion Blazor Assemblies in a Blazor WebAssembly Application, accessed August 31, 2025, [https://www.syncfusion.com/blogs/post/lazy-loading-syncfusion-blazor-assemblies-in-a-blazor-webassembly-application](https://www.syncfusion.com/blogs/post/lazy-loading-syncfusion-blazor-assemblies-in-a-blazor-webassembly-application)  
51. Lazy load assemblies in ASP.NET Core Blazor WebAssembly | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-lazy-load-assemblies?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-lazy-load-assemblies?view=aspnetcore-9.0)  
52. How Lazyloading In Blazor Can Increase Your Application Performance\! \- C\# Corner, accessed August 31, 2025, [https://www.c-sharpcorner.com/article/lazyloading-in-blazor/](https://www.c-sharpcorner.com/article/lazyloading-in-blazor/)  
53. Selecting an effective data visualization | Looker \- Google Cloud, accessed August 31, 2025, [https://cloud.google.com/looker/docs/visualization-guide](https://cloud.google.com/looker/docs/visualization-guide)  
54. Data visualization \- Material Design 2, accessed August 31, 2025, [https://m2.material.io/design/communication/data-visualization.html](https://m2.material.io/design/communication/data-visualization.html)  
55. Urban Institute Data Visualization style guide \- GitHub Pages, accessed August 31, 2025, [http://urbaninstitute.github.io/graphics-styleguide/](http://urbaninstitute.github.io/graphics-styleguide/)  
56. How to best Deploy Blazor web assembly standalone app, asp.net core web API and a MySQL database, to a client's local computer. \- Reddit, accessed August 31, 2025, [https://www.reddit.com/r/Blazor/comments/1igphvi/how\_to\_best\_deploy\_blazor\_web\_assembly\_standalone/](https://www.reddit.com/r/Blazor/comments/1igphvi/how_to_best_deploy_blazor_web_assembly_standalone/)