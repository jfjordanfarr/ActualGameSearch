

# **A Unified Architecture for a Low-Cost, Multimodal Game Search Engine**

---

## **User Prompt**

Attached are the history of a game search engine I've tried to develop (outputs from python notebooks from 2023\) as well as a reimagining of that game search engine at ultra-low-cost using modern tech which I've begun issuing Gemini Deep Research reports about. You'll see at least one prior Gemini Deep Research report attached to this chat.

I want to continue pursuing the sqlite-based reimaging.

The last 2 deep research reports based on this sqlite reimaging have a fundamental conflict which we must now resolve: what embedding model do we use?

To highlight why this is a conflict, note that the "Low-Cost Multimodal Game Search Engine" doc describes doing all of the embedding generation (functionally, DB generation) offline. I agree with this wholeheartedly and think it's a great idea. But then we reach the second report of the reimagining, and it describes a bit a about performing the embedding calculations as well. And there we see the conflict: user queries, text or images, \*\*must be embedded\*\*. Therefore, \*\*we must ensure that we are using an identical embedding model for user queries as we do for the offline DB generation\*\*. And now comes the trickiest part of all: do we tank our entire idea of doing this cheaply by provisioning expensive hardware to run our container app, do we spring for an embedding API service (i.e. Azure AI foundry), or do we do something absolutely bananas and use Blazor WebAssembly to bring the embedding model \*\*to the end user's device, having their own hardware perform the embedding\*\*?

Imagine that we are restricting ourselves to just, say, an Azure App Service instance and a Blob Storage, as an example (you could propose this with Cloudflare Containers and R2 storage as well). Help unify this design and resolve the contradictions we've accumulated thus far.

---

## **Section 1: The Technological Foundation: Selecting Performant, Low-Cost Embedding Models**

The central architectural challenge facing the reimagined game search engine is the selection of a consistent and cost-effective method for generating vector embeddings for user queries. This method must be identical to the one used for the offline generation of the primary search database to ensure semantic consistency. The project's core tenet of being "ultra-low-cost" dictates that any chosen solution must prioritize efficiency, minimal resource consumption, and scalability without incurring prohibitive expenses. This section establishes the foundational technology choices—the interchange format and the specific embedding models—that will serve as the common components for the three architectural paths evaluated in this report.

### **1.1 The Strategic Imperative of ONNX**

The Open Neural Network Exchange (ONNX) format is not merely an optimization but a foundational requirement for this project. The decision to evaluate a client-side embedding solution using WebAssembly is the primary driver for this mandate. This approach is only feasible through the use of onnxruntime-web, a specialized version of the ONNX Runtime engine designed to execute models directly in a web browser.1 As the user query must be embedded using the exact same model as the offline database, this constraint propagates across all potential architectures. Therefore, any selected model must be available in, or convertible to, the ONNX format.

This universal requirement is advantageous. ONNX provides a standardized, interoperable format that decouples the model from the training framework (such as PyTorch or TensorFlow), allowing it to be deployed across a wide variety of platforms and hardware.4 This is achieved through the ONNX Runtime, a high-performance inference engine optimized for both cloud and edge deployments.6 The runtime leverages hardware-specific execution providers and techniques like model quantization to accelerate inference and reduce the computational footprint, which is critical for achieving the project's cost and performance goals.3 By standardizing on ONNX, the project ensures maximum flexibility and performance, regardless of whether the final architecture places the inference workload on a server, a third-party API, or the user's own device.

### **1.2 Text Embedding Model Selection: sentence-transformers/all-MiniLM-L6-v2**

For text embedding, the ideal model must balance three competing factors: semantic accuracy, inference speed, and resource footprint (both file size and memory usage). After analyzing several lightweight candidates, the sentence-transformers/all-MiniLM-L6-v2 model emerges as the optimal choice.

This model is a distilled version of a larger transformer, specifically engineered for efficiency.8 It maps sentences and short paragraphs to a 384-dimensional dense vector space, which is a compact representation that reduces storage requirements and speeds up similarity calculations compared to larger 768-dimensional vectors.10 While it may sacrifice a small amount of nuance compared to larger models like

all-mpnet-base-v2, its performance is more than sufficient for general-purpose semantic search, and its speed is significantly higher.9

The most critical characteristic for this project is its size in the ONNX format. The LightEmbed/sbert-all-MiniLM-L6-v2-onnx variant, which is optimized for the ONNX Runtime, has a file size of just 80 MB.10 This small footprint is paramount for two reasons:

1. For the client-side WebAssembly path, a smaller model file means a faster initial download for the user, which is crucial for a positive user experience.  
2. For the server-side provisioned compute path, a smaller model reduces the memory overhead, allowing the application to run on lower-cost, resource-constrained virtual machine instances.

Real-world performance data underscores the importance of resource allocation for this model. When this exact model was moved from a high-end local machine (8-core CPU, 32 GB RAM) to a resource-constrained Docker container (1-core CPU, 4 GB RAM), the response time for generating a cosine similarity score increased by a factor of 15-20x, from 2-3 seconds to over 45 seconds.12 This data provides a critical performance baseline for accurately modeling the hardware requirements and associated costs of a server-side deployment. Libraries specifically designed for ONNX, such as

fastembed and light-embed, further enhance its performance by leveraging the ONNX Runtime, making it demonstrably faster than standard PyTorch or Hugging Face Transformers implementations.7

### **1.3 Image Embedding Model Selection: MobileNetV2**

For image embedding, the selection criteria are analogous to text: a lightweight model that performs well on CPU-bound hardware and has a small file size. The ONNX Model Zoo provides several excellent candidates designed for mobile and embedded vision applications, including MobileNet, SqueezeNet, and ShuffleNet.4

The recommended model for this project is **MobileNetV2**. It is explicitly designed as a "Light-weight deep neural network best suited for mobile and embedded vision applications".4 Its architecture utilizes depth-wise separable convolutions to dramatically reduce the number of parameters and computational complexity while maintaining high accuracy, making it ideal for devices with limited resources.14 SqueezeNet is a viable alternative, offering "AlexNet level accuracy with 50x fewer parameters," which also translates to a very small model size.4 However, MobileNet is a more modern and widely adopted architecture for efficient on-device inference.

A key advantage of selecting MobileNet is the extensive support and optimization available within the ONNX Runtime ecosystem. For the client-side WebAssembly path, onnxruntime-web can accelerate MobileNetV2 inference by up to 3.4x on a CPU by enabling multi-threading and SIMD (Single Instruction, Multiple Data) features available in modern browsers.3 These same optimizations benefit a server-side deployment running on a standard CPU.

Regardless of the final architecture, a consistent image pre-processing pipeline is essential. Both the offline database generation process and the online query embedding function must perform the exact same transformations: decoding the image, resizing it to the model's expected input dimensions (e.g., 224×224), center cropping, normalizing pixel values, and reordering color channels.14 Failure to maintain this consistency will result in mismatched embeddings and poor search results.

### **Table 1: Recommended Embedding Models and Specifications**

The following table summarizes the foundational model selections for the game search engine. These models represent the best-in-class options for balancing performance, size, and cost, and will be the common components used in the architectural analysis that follows.

| Modality | Model Name | ONNX Variant | Embedding Dimensions | Max Sequence Length | ONNX File Size | Key Rationale |
| :---- | :---- | :---- | :---- | :---- | :---- | :---- |
| Text | all-MiniLM-L6-v2 | LightEmbed/sbert-all-MiniLM-L6-v2-onnx | 384 | 256 | 80 MB | Excellent balance of speed and accuracy; extremely small file size is critical for both client-side and server-side deployments. |
| Image | MobileNetV2 | Available in ONNX Model Zoo | 1000+ (feature vector) | N/A | \~14 MB | Designed for high-performance, low-resource environments; benefits significantly from ONNX Runtime CPU optimizations. |

## **Section 2: Architectural Deep Dive: The Three Paths to Query-Time Embedding**

With the foundational models selected, the analysis now turns to the three distinct architectural patterns for performing embedding generation on user queries. Each path presents a unique set of trade-offs regarding cost, performance, complexity, and user experience. A thorough examination of each is necessary to make a well-informed final recommendation.

### **2.1 Path A: Server-Side Inference on Provisioned Compute**

This path represents the most traditional and straightforward architecture. It involves deploying a self-contained web service that packages the ONNX models and the ONNX Runtime, exposing an API endpoint to generate embeddings for incoming user queries.

#### **2.1.1 Architecture and Implementation**

The proposed implementation consists of a containerized Python web application built with a modern, high-performance framework such as FastAPI. The application would have two primary endpoints: one for text embedding and one for image embedding. Upon receiving a request, the service would load the query data, perform any necessary pre-processing (e.g., tokenization for text, transformations for images), and then execute an inference session using the onnxruntime Python package to generate the vector embedding.5

The application would be packaged as a Docker container, which includes the Python source code, a requirements.txt file specifying dependencies like fastapi, uvicorn, and onnxruntime, and the .onnx model files themselves.18 This container would then be deployed to a provisioned compute service. The primary candidate for this is an Azure App Service running on a Linux App Service Plan, which provides a fully managed environment for hosting containerized applications.19 An alternative could be Cloudflare Containers, which offers a similar container hosting model.21

The deployment process to Azure App Service is well-documented. It involves creating an App Service Plan and the App Service instance, pushing the Docker image to a container registry (like Azure Container Registry), and configuring the App Service to pull and run that image. A custom startup command would be used to launch the Gunicorn server with Uvicorn workers to run the FastAPI application efficiently.19

#### **2.1.2 Performance and Scalability Analysis**

For a real-time, interactive search application, latency is a paramount concern. This is where a critical distinction must be made between "serverless" and "provisioned" compute. Many modern container platforms operate on a scale-to-zero model, where the container is shut down after a period of inactivity to save costs. While economically attractive, this introduces the problem of "cold starts"—the significant delay incurred while the platform provisions resources and starts the container to serve the first request.

Observed cold start times for such platforms are unacceptably high for this use case. Cloudflare Containers have demonstrated cold starts of around 13 seconds.21 Similarly, Azure Functions, another serverless platform, has shown a median cold start latency of 3.64 seconds.24 A user performing a search will not tolerate a multi-second delay before their query is even processed.

This latency sensitivity effectively disqualifies any scale-to-zero hosting model for this architecture. The only viable implementation of Path A is to use a **provisioned compute tier** with an "Always On" feature. This ensures that at least one instance of the container is constantly running, ready to accept requests with minimal latency. This decision fundamentally reframes the cost model for Path A from a transactional, pay-per-use structure to a fixed, recurring monthly expense. Scalability can be handled by configuring auto-scale rules on the App Service Plan to add more "warm" instances based on CPU load or request count, but the baseline cost for having at least one instance active 24/7 is non-negotiable.

#### **2.1.3 Cost Modeling**

Given the requirement for an "Always On" instance, the cost model for Path A is determined by the monthly price of the selected Azure App Service Plan. The hardware resource requirements are guided by the performance characteristics of the chosen models. The analysis of all-MiniLM-L6-v2 showed significant performance degradation when constrained to a single CPU core and limited RAM.12 Therefore, selecting a plan with adequate resources is crucial, but must be balanced against the "ultra-low-cost" mandate.

Examining Azure's pricing for Linux App Service plans reveals several potential tiers 25:

* **Basic B1 Plan:** Provides 1 vCPU and 1.75 GB of RAM for approximately $13.14 per month. This is the lowest-cost tier that offers a dedicated vCPU and avoids the daily CPU minute quotas of the Free/Shared tiers, but the RAM may be insufficient.  
* **Standard S1 Plan:** Provides 1 vCPU and 1.75 GB of RAM for approximately $69.35 per month.26 While offering more features, the core resources are similar to the B1 plan.  
* **Premium v3 P0v3 Plan (Windows):** Provides 1 vCPU and 4 GB of RAM for approximately $120.45 per month.27 This tier would comfortably run the models but is likely too expensive for the project's goals.

The most pragmatic starting point would be the **Basic B1** plan. While the 1.75 GB of RAM is less than the 4 GB used in the container test case 12, it may be sufficient given that only one model would be loaded into memory at a time. The monthly cost of \~$13 provides a predictable, fixed expense for a performant, low-latency embedding service. If performance proves inadequate, scaling up to a higher tier is straightforward but would significantly increase the operating cost.

### **2.2 Path B: Server-Side Inference via Managed API**

This architecture seeks to minimize operational complexity by outsourcing the embedding generation to a specialized, third-party AI service.

#### **2.2.1 Architecture and Implementation**

In this model, the project's backend would be a "thin proxy." It could be implemented as an extremely lightweight application, perhaps even on a free tier of Azure App Service or using a serverless function like Cloudflare Workers, as its own resource needs are negligible.28 Its sole responsibility would be to receive the user's query (text or image data), securely authenticate with a managed AI service (e.g., Azure AI Vision), forward the query to that service's embedding endpoint, and then relay the resulting vector back to the client. This approach entirely offloads the burden of hosting, scaling, and maintaining the embedding models and their runtime environment.

#### **2.2.2 Performance and Dependency Analysis**

The performance of this architecture is governed by network latency. The total time for a query includes multiple network hops: from the user's client to the thin proxy, from the proxy to the managed API, and then the return journey. While the inference itself on the managed service's powerful hardware is likely very fast, the cumulative network round-trip time could be a significant factor.

However, the most significant issue with this path is a fundamental conflict with the project's core requirements. Managed AI services typically provide access to their own proprietary, highly optimized models. They do not generally allow users to upload and run a specific, custom ONNX model like all-MiniLM-L6-v2. This creates a critical inconsistency: the model used for online user queries would be different from the model used for the offline database generation. This semantic mismatch would lead to a severe degradation in search quality, as the vector spaces would not be aligned.

Therefore, Path B is only viable if the *entire embedding process*, both offline and online, is performed using the same managed API. While technically possible, this introduces a strong vendor lock-in and makes the cost of the initial, large-scale database generation dependent on the API's pricing, which could be substantial. This violation of the "same model" principle, as originally conceived, presents a major architectural disqualifier.

#### **2.2.3 Cost Modeling and The Crossover Point**

Assuming for the sake of analysis that the "same model" constraint could be met by using the managed API for all embedding tasks, the cost model becomes purely transactional. The cost is calculated on a per-query basis. A comparison of leading services reveals the following approximate prices:

* **Azure AI Vision (Image Embeddings):** $0.10 per 1,000 transactions.30  
* **OpenAI text-embedding-3-small:** $0.02 per 1,000,000 tokens.31  
* **Cohere Embed 4:** $0.12 per 1,000,000 tokens.33  
* **Google Gemini Embedding:** $0.15 per 1,000,000 tokens.34

This transactional model can be compared to the fixed monthly cost of Path A to determine a "crossover point" where one becomes more economical than the other.

* **Image Embedding Crossover:**  
  * Path A Fixed Cost (B1 Plan): \~$13.14/month.  
  * Path B Cost: $0.10 per 1,000 queries.  
  * Crossover Calculation: ($13.14/$0.10)×1,000=131,400 queries per month.  
  * Below \~131k image queries per month, the managed API is cheaper. Above this volume, the provisioned server becomes more cost-effective.  
* **Text Embedding Crossover (using OpenAI pricing):**  
  * Path A Fixed Cost (B1 Plan): \~$13.14/month.  
  * Path B Cost: $0.02 per 1M tokens. Assuming an average of 30 tokens per search query.  
  * Cost per 1,000 queries: (1,000 queries×30 tokens/query)/1,000,000 tokens×$0.02=$0.0006.  
  * Crossover Calculation: ($13.14/$0.0006)×1,000≈21,900,000 queries per month.  
  * The economics for text embedding are vastly different. The provisioned server only becomes more cost-effective at an extremely high volume of over 21 million text queries per month.

This analysis reveals that the financial viability of a managed API is highly dependent on the modality (image vs. text) and the projected usage scale. However, the fundamental issue of model inconsistency remains the primary obstacle for this architectural path.

### **2.3 Path C: Client-Side Inference with Blazor WebAssembly**

This path represents the most innovative and potentially the most cost-effective solution at scale. It proposes to shift the computational workload of embedding generation from the server entirely to the end-user's device by leveraging Blazor WebAssembly and the ONNX Runtime for the web.

#### **2.3.1 Architecture and Implementation**

The implementation of this architecture is the most complex of the three. It requires integrating a JavaScript-based ML runtime (onnxruntime-web) into a.NET-based Blazor WebAssembly application. The key engineering tasks are:

1. **Serving ONNX Runtime Binaries:** The onnxruntime-web library consists of JavaScript code and one or more WebAssembly (.wasm) binary files.35 The application's web server must be configured to serve these  
   .wasm files correctly as static assets. This has been a noted point of difficulty in web development environments and requires careful configuration of the static file pipeline.35  
2. **Serving ONNX Model Files:** The all-MiniLM-L6-v2.onnx (80 MB) and MobileNetV2.onnx (\~14 MB) files must also be hosted as static assets. Given their size, they should be stored in a scalable object storage service like Azure Blob Storage or Cloudflare R2 and delivered via a CDN for performance.  
3. **Blazor JS Interop:** The Blazor application will need to use its JavaScript interop capabilities to communicate with the onnxruntime-web library. This involves writing C\# code that calls JavaScript functions to:  
   * Initialize the ONNX Runtime environment.  
   * Create an InferenceSession by providing the URL to the .onnx model file.  
   * Prepare the input data (user query) as a tensor.  
   * Execute the session.run() method to perform inference.  
   * Return the resulting embedding vector from JavaScript back to the C\# code.1

#### **2.3.2 Performance and User Experience Analysis**

The primary challenge for this path is the user experience, particularly on the first visit. Blazor WebAssembly applications inherently have a larger initial payload size because they must download a.NET runtime compiled to WebAssembly.38 Adding the ONNX Runtime

.wasm files and the large .onnx model files further exacerbates this issue.

To make this viable, a hybrid, lazy-loading strategy is essential. The user experience flow must be carefully orchestrated:

1. The user first downloads the minimal Blazor application, allowing the UI to render quickly. Techniques like Ahead-of-Time (AOT) compilation and IL trimming should be used to minimize this initial app size.39  
2. Once the UI is interactive, the application asynchronously and in the background begins downloading the required onnxruntime-web binaries and the specific .onnx model needed for the user's first query.  
3. Upon successful download, these large assets must be aggressively cached on the client-side using IndexedDB.35 This ensures that on subsequent visits or page loads, the models and runtime can be loaded from local storage instantly, bypassing the network entirely.

This strategy decouples the perceived application load time from the machine learning model load time, making the initial experience tolerable and subsequent experiences very fast.

However, a second, more fundamental challenge arises: the "performance lottery." By offloading computation to the client, the application's performance becomes entirely dependent on the user's hardware.1 A user on a modern desktop with a multi-core CPU will experience fast, near-native inference speeds, especially with browser features like SIMD and multi-threading enabled.41 Conversely, a user on an older, low-power mobile device may experience slow and sluggish performance. This creates an unpredictable and unequal user experience. The application design must account for this variability, perhaps by displaying a loading indicator during inference and implementing a timeout mechanism to handle exceptionally slow devices.

#### **2.3.3 Cost Modeling**

The cost model for Path C is radically different from the others. The concept of "compute cost" is virtually eliminated, as the user's own device provides the CPU cycles. The primary costs are shifted to storage and bandwidth:

* **Storage:** A recurring, low monthly cost for storing the .onnx model files in Azure Blob Storage or Cloudflare R2.  
* **Egress/Bandwidth:** A variable cost associated with delivering the Blazor application, the onnxruntime-web binaries, and the .onnx models to each new user on their first visit.

This model is exceptionally inexpensive at scale. After the initial download, subsequent queries from the same user incur zero cost to the service provider. The cost structure is front-loaded to user acquisition rather than being tied to user activity. For an application with a high ratio of repeat users to new users, this architecture offers the lowest possible long-term operational cost, perfectly aligning with the "ultra-low-cost" principle.

## **Section 3: Comparative Analysis and Decision Framework**

The detailed exploration of the three architectural paths reveals a complex landscape of trade-offs. To distill these findings into a clear, actionable comparison, this section synthesizes the key advantages and disadvantages of each approach and presents a decision matrix that scores them against the project's core requirements.

### **3.1 Synthesizing the Trade-offs**

* **Path A: Server-Side Inference on Provisioned Compute**  
  * **Pros:** This path offers the most predictable and consistent performance. By using an "Always On" provisioned instance, it eliminates cold starts and ensures that every user receives the same low-latency experience, regardless of their device. The architecture is traditional, well-understood, and provides the developer with full control over the execution environment.  
  * **Cons:** It carries a fixed monthly cost, which can be inefficient at very low usage scales. It requires management of the server infrastructure, including scaling, security, and updates. It represents the highest baseline cost of the three options.  
* **Path B: Server-Side Inference via Managed API**  
  * **Pros:** This path boasts the lowest implementation complexity. It outsources all the operational burdens of model hosting and maintenance. The pay-per-transaction model means there is zero cost if the service is not used, making it financially attractive for projects with uncertain or sporadic traffic.  
  * **Cons:** It is fundamentally incompatible with the project's requirement to use a specific, consistent embedding model for both offline and online processing. This semantic mismatch is a likely deal-breaker. Furthermore, it introduces vendor lock-in, and its transactional costs can become prohibitively expensive at high scale, particularly for image embeddings.  
* **Path C: Client-Side Inference with Blazor WebAssembly**  
  * **Pros:** This path offers the lowest possible operational cost at scale, approaching zero per query. It enhances user privacy by ensuring that query data never leaves the client's device. The architecture is infinitely scalable from the provider's perspective, as the computational load is fully distributed among the users.  
  * **Cons:** It has the highest implementation complexity, requiring expertise in Blazor, JavaScript interop, and browser performance optimization. The user experience is subject to the "performance lottery," leading to inconsistent and unpredictable inference times. The initial load time for new users is a significant hurdle that requires careful engineering to mitigate.

### **3.2 Table 2: Architectural Decision Matrix**

The following matrix provides a quantitative and qualitative scoring of each architectural path against the critical success factors for the game search engine. The scoring is based on a 1-5 scale, where 5 is the most favorable.

| Criterion | Path A: Provisioned Compute | Path B: Managed API | Path C: Client-Side Wasm | Rationale |
| :---- | :---- | :---- | :---- | :---- |
| **Cost (Low Scale: \<100k queries/mo)** | 2 | 4 | 5 | Path A has a fixed monthly cost (\~$13). Path B is transactional and cheaper at this scale. Path C has only minimal storage/egress costs. |
| **Cost (High Scale: \>1M queries/mo)** | 4 | 1 | 5 | Path A's fixed cost becomes very efficient. Path B's transactional costs become extremely high. Path C's per-query cost remains near zero. |
| **Performance (Latency & Consistency)** | 5 | 3 | 2 | Path A offers consistent low latency. Path B adds network hops. Path C's performance is variable and dependent on user hardware. |
| **Implementation Complexity** | 4 | 5 | 1 | Path B is the simplest (a thin proxy). Path A is a standard container deployment. Path C is highly complex, requiring JS interop and advanced client-side optimization. |
| **Scalability (Provider Perspective)** | 3 | 4 | 5 | Path A requires active management of scaling rules. Path B relies on the provider's scalability. Path C is inherently scalable as the load is distributed. |
| **Alignment with "Same Model" Principle** | 5 | 1 | 5 | Path A and C allow for the use of the exact same custom ONNX model. Path B forces the use of a different, provider-specific model, violating the principle. |
| **Alignment with "Ultra-Low-Cost" Principle** | 3 | 2 | 5 | Path A is low-cost but not "ultra-low." Path B becomes expensive. Path C embodies the principle by minimizing provider-side operational costs. |
| **Overall Score** | **26** | **20** | **28** |  |

The decision matrix clearly illustrates the strengths and weaknesses of each path. Path C scores the highest overall, primarily due to its unparalleled cost-effectiveness at scale and perfect alignment with the project's core principles. However, its low scores in performance consistency and implementation complexity represent significant risks. Path A is a strong, balanced contender, offering excellent performance at the cost of a fixed monthly fee. Path B is largely disqualified due to its failure to meet the "same model" requirement and its poor cost-scaling for image-heavy workloads.

## **Section 4: Final Recommendation: A Unified and Cost-Optimized Architecture**

Based on the comprehensive analysis of the three architectural paths and the scoring in the decision matrix, a definitive recommendation can be made to resolve the project's core conflict. The optimal strategy is not to select a single path but to adopt a phased approach that mitigates risk while still achieving the long-term vision of an ultra-low-cost system.

### **4.1 The Recommended Path: A Phased Implementation**

**Phase 1: Launch with Path A (Server-Side Inference on Provisioned Compute)**

The immediate priority should be to launch a reliable, performant, and functional version of the search engine. For this, **Path A is the recommended starting point.**

* **Rationale:** This approach provides the best balance of implementation feasibility and user experience for an initial release. It guarantees consistent, low-latency query performance for all users, which is critical for establishing the product's viability. The implementation, while not trivial, involves standard practices for containerized web application development and deployment, minimizing the risk of unforeseen technical hurdles. By selecting the lowest-cost "Always On" tier, such as the Azure App Service **Basic B1 plan (\~$13/month)**, the initial fixed cost is contained and predictable.

**Phase 2: Evolve to Path C (Client-Side Inference with Blazor WebAssembly)**

Once the search engine has launched and demonstrated market traction, development efforts should shift to implementing **Path C as the long-term, cost-optimized architecture.**

* **Rationale:** Path C perfectly embodies the "ultra-low-cost" principle and offers unmatched scalability and privacy benefits. While the initial engineering investment is high, the long-term operational savings are substantial, especially as user traffic grows. By treating this as a second-phase evolution, the development team can tackle the complexities of Blazor Wasm integration and client-side performance optimization without the pressure of an imminent launch. The live version running on Path A provides a stable baseline for performance comparison and a fallback option if the client-side implementation encounters significant issues.

This phased strategy effectively de-risks the project. It uses a proven, reliable architecture for the initial launch to validate the product, while simultaneously paving the way for a transition to a more innovative and economically efficient architecture in the future.

### **4.2 High-Level Implementation Roadmap**

The following roadmap outlines the key steps for both the common offline pipeline and the two phases of the online application.

#### **4.2.1 Offline Data Pipeline (Common to Both Phases)**

1. **Model Preparation:**  
   * Obtain the sentence-transformers/all-MiniLM-L6-v2 and MobileNetV2 models.  
   * Convert them to the ONNX format if they are not already, ensuring they are optimized and quantized where appropriate.  
2. **Database Generation Script:**  
   * Develop a Python batch processing script that uses the onnxruntime library.  
   * The script will iterate through the entire game dataset (metadata, descriptions, screenshots, etc.).  
   * For each game, it will generate text and image embeddings using the prepared ONNX models.  
   * The script will then populate a SQLite database file with the game data and its corresponding vector embeddings.  
3. **Database Storage:**  
   * Upload the final, comprehensive games.sqlite file to a cloud object storage service (Azure Blob Storage or Cloudflare R2).

#### **4.2.2 Online Application (Phase 1: Path A)**

1. **Develop FastAPI Service:**  
   * Create a Python FastAPI application with endpoints for /embed-text and /embed-image.  
   * Package the ONNX models and the onnxruntime library within the application.  
   * Implement the logic to load models and run inference sessions.  
2. **Containerize the Application:**  
   * Write a Dockerfile to package the FastAPI application and its dependencies into a container image.  
3. **Deploy to Azure App Service:**  
   * Provision an Azure App Service Plan (Linux, Basic B1 tier) and an App Service instance.  
   * Configure the "Always On" setting to prevent cold starts.  
   * Push the Docker image to Azure Container Registry.  
   * Configure the App Service to deploy the container image and set the appropriate Gunicorn startup command.  
4. **Frontend Integration:**  
   * The frontend application will make API calls to the deployed FastAPI service to get embeddings for user queries before querying the SQLite database.

#### **4.2.3 Online Application (Phase 2: Path C)**

1. **Blazor Wasm Project Setup:**  
   * Create the Blazor WebAssembly frontend application.  
   * Configure the project's static file handling to correctly serve .wasm binaries.  
2. **Integrate ONNX Runtime Web:**  
   * Add the onnxruntime-web library via npm.  
   * Write JavaScript interop services in C\# to manage the creation of inference sessions and the execution of the models.  
3. **Implement Model Loading and Caching:**  
   * Host the .onnx model files in cloud storage (Azure Blob Storage/R2).  
   * Implement the lazy-loading strategy: download models on demand after the initial UI load.  
   * Use JavaScript interop to store the downloaded models and runtime binaries in the browser's IndexedDB for fast subsequent loads.  
4. **Frontend Logic:**  
   * Modify the search component to perform embedding generation directly in the browser using the integrated ONNX Runtime before querying the SQLite database.  
   * Implement UI feedback (e.g., loading indicators) to account for variable inference times.

#### **Works cited**

1. Run AI Models Entirely in the Browser Using WebAssembly \+ ONNX ..., accessed August 31, 2025, [https://dev.to/hexshift/run-ai-models-entirely-in-the-browser-using-webassembly-onnx-runtime-no-backend-required-4lag](https://dev.to/hexshift/run-ai-models-entirely-in-the-browser-using-webassembly-onnx-runtime-no-backend-required-4lag)  
2. How to add machine learning to your web application with ONNX Runtime, accessed August 31, 2025, [https://onnxruntime.ai/docs/tutorials/web/](https://onnxruntime.ai/docs/tutorials/web/)  
3. ONNX Runtime Web—running your machine learning model in browser, accessed August 31, 2025, [https://opensource.microsoft.com/blog/2021/09/02/onnx-runtime-web-running-your-machine-learning-model-in-browser/](https://opensource.microsoft.com/blog/2021/09/02/onnx-runtime-web-running-your-machine-learning-model-in-browser/)  
4. A collection of pre-trained, state-of-the-art models in the ONNX format \- GitHub, accessed August 31, 2025, [https://github.com/onnx/models](https://github.com/onnx/models)  
5. ONNX Runtime and models \- Azure Machine Learning \- Microsoft Community, accessed August 31, 2025, [https://learn.microsoft.com/en-us/azure/machine-learning/concept-onnx?view=azureml-api-2](https://learn.microsoft.com/en-us/azure/machine-learning/concept-onnx?view=azureml-api-2)  
6. ONNX Runtime for inferencing machine learning models now in preview \- Microsoft Azure, accessed August 31, 2025, [https://azure.microsoft.com/en-us/blog/onnx-runtime-for-inferencing-machine-learning-models-now-in-preview/](https://azure.microsoft.com/en-us/blog/onnx-runtime-for-inferencing-machine-learning-models-now-in-preview/)  
7. FastEmbed: Fast and Lightweight Embedding Generation for Text \- DEV Community, accessed August 31, 2025, [https://dev.to/qdrant/fastembed-fast-and-lightweight-embedding-generation-for-text-4i6c](https://dev.to/qdrant/fastembed-fast-and-lightweight-embedding-generation-for-text-4i6c)  
8. What are lightweight embedding models? \- Milvus, accessed August 31, 2025, [https://milvus.io/ai-quick-reference/what-are-lightweight-embedding-models](https://milvus.io/ai-quick-reference/what-are-lightweight-embedding-models)  
9. What are some popular pre-trained Sentence Transformer models and how do they differ (for example, all-MiniLM-L6-v2 vs all-mpnet-base-v2)? \- Milvus, accessed August 31, 2025, [https://milvus.io/ai-quick-reference/what-are-some-popular-pretrained-sentence-transformer-models-and-how-do-they-differ-for-example-allminilml6v2-vs-allmpnetbasev2](https://milvus.io/ai-quick-reference/what-are-some-popular-pretrained-sentence-transformer-models-and-how-do-they-differ-for-example-allminilml6v2-vs-allmpnetbasev2)  
10. LightEmbed/sbert-all-MiniLM-L6-v2-onnx \- Hugging Face, accessed August 31, 2025, [https://huggingface.co/LightEmbed/sbert-all-MiniLM-L6-v2-onnx](https://huggingface.co/LightEmbed/sbert-all-MiniLM-L6-v2-onnx)  
11. All MiniLM L6 V2 · Models \- Dataloop, accessed August 31, 2025, [https://dataloop.ai/library/model/sentence-transformers\_all-minilm-l6-v2/](https://dataloop.ai/library/model/sentence-transformers_all-minilm-l6-v2/)  
12. Hardware requirements for using sentence-transformers/all-MiniLM ..., accessed August 31, 2025, [https://stackoverflow.com/questions/76618655/hardware-requirements-for-using-sentence-transformers-all-minilm-l6-v2](https://stackoverflow.com/questions/76618655/hardware-requirements-for-using-sentence-transformers-all-minilm-l6-v2)  
13. qdrant/fastembed: Fast, Accurate, Lightweight Python library to make State of the Art Embedding \- GitHub, accessed August 31, 2025, [https://github.com/qdrant/fastembed](https://github.com/qdrant/fastembed)  
14. MOBILENET for On-Device Inference in Uno Platform Applications, accessed August 31, 2025, [https://platform.uno/blog/mobilenet-for-on-device-inference-in-uno-platform-applications/](https://platform.uno/blog/mobilenet-for-on-device-inference-in-uno-platform-applications/)  
15. ONNX Pipeline Models: Image Embedding \- Oracle Help Center, accessed August 31, 2025, [https://docs.oracle.com/en/database/oracle/oracle-database/23/vecse/onnx-pipeline-models-image-embedding.html](https://docs.oracle.com/en/database/oracle/oracle-database/23/vecse/onnx-pipeline-models-image-embedding.html)  
16. Local inference using ONNX for AutoML image \- Azure Machine Learning, accessed August 31, 2025, [https://docs.azure.cn/en-us/machine-learning/how-to-inference-onnx-automl-image-models?view=azureml-api-2](https://docs.azure.cn/en-us/machine-learning/how-to-inference-onnx-automl-image-models?view=azureml-api-2)  
17. ONNX Runtime | Home, accessed August 31, 2025, [https://onnxruntime.ai/](https://onnxruntime.ai/)  
18. Deploy a Flask or FastAPI web app as a container in Azure App Service \- Python on Azure, accessed August 31, 2025, [https://learn.microsoft.com/en-us/azure/developer/python/tutorial-containerize-simple-web-app-for-app-service](https://learn.microsoft.com/en-us/azure/developer/python/tutorial-containerize-simple-web-app-for-app-service)  
19. Quickstart: Deploy a Python (Django, Flask, or FastAPI) web app to ..., accessed August 31, 2025, [https://learn.microsoft.com/en-us/azure/app-service/quickstart-python](https://learn.microsoft.com/en-us/azure/app-service/quickstart-python)  
20. Configure Linux Python apps \- Azure App Service | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/azure/app-service/configure-language-python](https://learn.microsoft.com/en-us/azure/app-service/configure-language-python)  
21. I just spent the last 8 hours trying to compare Cloudflare Containers ..., accessed August 31, 2025, [https://www.reddit.com/r/CloudFlare/comments/1lv7vsk/i\_just\_spent\_the\_last\_8\_hours\_trying\_to\_compare/](https://www.reddit.com/r/CloudFlare/comments/1lv7vsk/i_just_spent_the_last_8_hours_trying_to_compare/)  
22. Tutorial: Deploy a Python FastAPI web app with PostgreSQL \- Azure App Service \- Azure.cn, accessed August 31, 2025, [https://docs.azure.cn/en-us/app-service/tutorial-python-postgresql-app-fastapi](https://docs.azure.cn/en-us/app-service/tutorial-python-postgresql-app-fastapi)  
23. Quickstart: Deploy a Python (Django, Flask, or FastAPI) web app to Azure App Service, accessed August 31, 2025, [https://docs.azure.cn/en-us/app-service/quickstart-python](https://docs.azure.cn/en-us/app-service/quickstart-python)  
24. Cloudflare Worker vs Azure Functions | serverless comparisons, accessed August 31, 2025, [https://serverlesstalent.com/compare/cloudflare-worker/azure-functions](https://serverlesstalent.com/compare/cloudflare-worker/azure-functions)  
25. Tutorial: Detect objects using an ONNX deep learning model \- ML.NET | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx](https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx)  
26. Pricing – App Service for Linux | Microsoft Azure, accessed August 31, 2025, [https://azure.microsoft.com/en-us/pricing/details/app-service/linux-previous/](https://azure.microsoft.com/en-us/pricing/details/app-service/linux-previous/)  
27. Azure App Service on Windows pricing, accessed August 31, 2025, [https://azure.microsoft.com/en-us/pricing/details/app-service/windows/](https://azure.microsoft.com/en-us/pricing/details/app-service/windows/)  
28. Static Web Apps pricing \- Microsoft Azure, accessed August 31, 2025, [https://azure.microsoft.com/en-us/pricing/details/app-service/static/](https://azure.microsoft.com/en-us/pricing/details/app-service/static/)  
29. Workers & Pages Pricing \- Cloudflare, accessed August 31, 2025, [https://www.cloudflare.com/plans/developer-platform/](https://www.cloudflare.com/plans/developer-platform/)  
30. Azure AI Vision pricing, accessed August 31, 2025, [https://azure.microsoft.com/en-us/pricing/details/cognitive-services/computer-vision/](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/computer-vision/)  
31. OpenAI Embeddings Pricing Calculator \- InvertedStone, accessed August 31, 2025, [https://invertedstone.com/calculators/embedding-pricing-calculator](https://invertedstone.com/calculators/embedding-pricing-calculator)  
32. From Tokens to Costs: Embedding Estimation with OpenAI API | by Vaibhav Pandey, accessed August 31, 2025, [https://mindfulcto.com/from-tokens-to-costs-embedding-estimation-with-openai-api-8c535753a479](https://mindfulcto.com/from-tokens-to-costs-embedding-estimation-with-openai-api-8c535753a479)  
33. Cohere Pricing Guide for the UK (2025) \- Wise, accessed August 31, 2025, [https://wise.com/gb/blog/cohere-pricing](https://wise.com/gb/blog/cohere-pricing)  
34. Gemini Developer API Pricing | Gemini API | Google AI for Developers, accessed August 31, 2025, [https://ai.google.dev/gemini-api/docs/pricing](https://ai.google.dev/gemini-api/docs/pricing)  
35. Deploying ONNX Runtime Web | onnxruntime, accessed August 31, 2025, [https://onnxruntime.ai/docs/tutorials/web/deploy.html](https://onnxruntime.ai/docs/tutorials/web/deploy.html)  
36. Load onnx model in browser, can't find wasm file \- Stack Overflow, accessed August 31, 2025, [https://stackoverflow.com/questions/76185469/load-onnx-model-in-browser-cant-find-wasm-file](https://stackoverflow.com/questions/76185469/load-onnx-model-in-browser-cant-find-wasm-file)  
37. ONNX runtime using Blazor · microsoft onnxruntime · Discussion ..., accessed August 31, 2025, [https://github.com/microsoft/onnxruntime/discussions/14657](https://github.com/microsoft/onnxruntime/discussions/14657)  
38. Why Blazor WASM is so slow compared to other WASM frameworks? : r/dotnet \- Reddit, accessed August 31, 2025, [https://www.reddit.com/r/dotnet/comments/13dsibj/why\_blazor\_wasm\_is\_so\_slow\_compared\_to\_other\_wasm/](https://www.reddit.com/r/dotnet/comments/13dsibj/why_blazor_wasm_is_so_slow_compared_to_other_wasm/)  
39. ASP.NET Core Blazor WebAssembly runtime performance ..., accessed August 31, 2025, [https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/webassembly-runtime-performance?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/webassembly-runtime-performance?view=aspnetcore-9.0)  
40. Blazor performance tuning for Production: Insights and lessons learned \- Reddit, accessed August 31, 2025, [https://www.reddit.com/r/Blazor/comments/1guc532/blazor\_performance\_tuning\_for\_production\_insights/](https://www.reddit.com/r/Blazor/comments/1guc532/blazor_performance_tuning_for_production_insights/)  
41. Performance Diagnosis | onnxruntime, accessed August 31, 2025, [https://onnxruntime.ai/docs/tutorials/web/performance-diagnosis.html](https://onnxruntime.ai/docs/tutorials/web/performance-diagnosis.html)