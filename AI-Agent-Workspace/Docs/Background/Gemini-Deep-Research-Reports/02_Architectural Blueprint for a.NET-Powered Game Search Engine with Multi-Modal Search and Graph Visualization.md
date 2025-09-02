

# **Architectural Blueprint for a.NET-Powered Game Search Engine with Multi-Modal Search and Graph Visualization**

---

## **User Prompt**

Attached are the history of a game search engine I've tried to develop (outputs from python notebooks from 2023\) as well as a reimagining of that game search engine at ultra-low-cost using modern tech which I've begun issuing Gemini Deep Research reports about. You'll see at least one prior Gemini Deep Research report attached to this chat.

I want to continue pursuing the sqlite-based reimaging. One thing that I want to do is get a grasp on the web application side. How can we make sure that we serve something that is as close-as-possible to a static site with little islands of interactivity? I want to host a public-facing API for folks to do game search and a minimal UI web application (either as part of the API box or as a static site if possible).

We need to be able to let the user enter text searches into a text area, drop screenshots, and pick out existing Steam games from some kind of searchable...list... thing? The UI design decisions are really not set in stone, but one special thing is: the ability for users to click any game (perhaps games are "cards" floating in space) and see which games are related to it, then click a related game and see which games are related to \_it\_.

This positions us as needing to fullfill 2 new particular requirements:

1\. On the backend system, we need to calculate nearest neighbors, either in real time or precomputed. We must provide nearest neighbor-like data as part of the responses which hydrate the UI web application with data.

2\. On the frontend system, we need a really smart way to visualize these graph-like relationships. Cytoscape.js looks like crap, sigma.js is an okay start, but I think that there might be other ways for us to cleverly display Steam games' relatedness to one another without visually overwhelming the user or requiring the entire hundreds-of-thousands-of-games graph to be ferried over to the end user's browser everytime they want to engage with this kind of visualization. What frontend frameworks can we use that will make it simple for us to provide users with a simple search engine-like interface (perhaps not so different, in the input area, to the inputs one finds on an AI chatbot, but likely rather different in the output display?)

Keep carrying the torch on this and make sure that if you find new information with conflicts with the first report, you note it and try to understand and resolve the conflict.

I've used a little bit of Next.js, but I'm really really comfortable with C\#. I don't love JS frameworks, but among the choices you spoke of, Astro would likely be a favored one. Sveltekit is a bit on the move for me. If there's an option which happens within the dotnet ecosystem, I'll be strongly capable of evaluating the quality of the work. Still, if another option truly is better, make that case and I will hear it in good faith.

---

## **Section 1: Frontend Architecture \- Crafting a Modern UI with Blazor**

This section establishes Blazor as the optimal frontend framework for this project. It details its modern rendering capabilities, which directly address the requirement for a fast, static-feeling site with "islands of interactivity," and provides a pragmatic comparison against the leading JavaScript alternative, Astro, from the perspective of a C\# developer.

### **1.1 The "Static-First" Philosophy with Blazor**

The release of.NET 8 marked a fundamental paradigm shift for the Blazor framework. It evolved from being primarily a Single Page Application (SPA) or stateful server-side framework into a full-spectrum web UI framework capable of delivering highly performant, content-focused websites. The most pivotal change is the adoption of Static Server-Side Rendering (SSR) as the default render mode for all components.1 This architectural decision aligns perfectly with the goal of creating a minimal, fast-loading user interface that feels like a static site.

Unlike previous Blazor models that required downloading a.NET runtime to the browser (WebAssembly) or maintaining a persistent WebSocket connection to the server (Blazor Server), static SSR delivers a non-interactive page with zero client-side overhead.1 This "static-first" approach is ideal for a search engine where initial page load speed and SEO are paramount. It favors high-speed content delivery and can be further optimized with standard web performance techniques like output caching, which instructs the web server to cache the rendered HTML for a specified duration, dramatically reducing server load for frequently accessed pages.1

The request-response cycle for a static Blazor component is simple and efficient. When a user requests a page, the component's C\# code executes on the server. This code can fetch data directly from the SQLite database or perform other server-side logic. The component is then rendered into a pure HTML string, which is sent to the browser in the HTTP response. Any user interactions, such as submitting a search form, are handled through standard HTML form submissions. This triggers a full-page request, and the component is re-rendered on the server with the new state.2 This baseline behavior ensures maximum performance and broad compatibility, as it relies on nothing more than standard web protocols. All components in a Blazor Web App adhere to this static rendering model unless interactivity is explicitly enabled.

### **1.2 Implementing Islands of Interactivity**

The true power of modern Blazor lies in its ability to selectively "hydrate" static components with rich interactivity, creating the "islands of interactivity" that are central to this project's requirements. This is achieved through the @rendermode directive, a mechanism that gives developers surgical control over how and where components become interactive, allowing for a finely tuned balance between performance and functionality.2 This choice is not a project-wide setting but a granular, per-component or even per-instance decision, which is a powerful architectural concept. It means a single page can host a completely static header and footer, a server-interactive search bar, and a WebAssembly-powered graph visualization, isolating the performance cost of interactivity only to the components that absolutely require it.

#### **Detailed Breakdown of Interactive Modes**

There are three primary interactive render modes, each with distinct characteristics and use cases:

* **@rendermode="InteractiveServer"**: This mode enables interactivity over a persistent SignalR connection. When a component with this render mode is requested, it is first pre-rendered on the server and sent to the browser as static HTML, ensuring a fast initial load. Simultaneously, a WebSocket connection is established. Subsequent user interactions (e.g., button clicks, input changes) are sent to the server over this connection, the component's state is updated, and the resulting UI changes are efficiently diffed and sent back to the client to update the DOM. This is a low-friction option for adding interactivity, especially for components that need access to server-side resources, as the component logic remains on the server.2  
* **@rendermode="InteractiveWebAssembly"**: This mode delivers the richest client-side experience by downloading a.NET runtime, compiled to WebAssembly (WASM), to the browser. The component and its dependencies are then executed entirely on the client's machine.2 This is ideal for components with complex, CPU-intensive UI logic, those that need to function offline, or applications that aim to offload processing from the server. Adopting this mode requires creating a separate client project within the solution to contain the components designated to run on WASM.2  
* **@rendermode="InteractiveAuto"**: This hybrid mode offers an optimized "best of both worlds" experience. On a user's first visit to a page with an "Auto" component, it behaves like InteractiveServer, providing instant interactivity while the WebAssembly runtime and necessary assets are downloaded in the background. On subsequent visits, the application automatically switches to InteractiveWebAssembly, executing the component on the client side for a faster, more responsive experience that reduces server load.2

#### **Project Structure and Configuration**

Setting up a Blazor Web App to use these modes is straightforward. When using InteractiveWebAssembly or InteractiveAuto, the solution structure must be split into two main projects: a server project that acts as the host and handles static SSR, and a client project that contains the components intended to run in the browser.5 This separation is a necessary architectural constraint to ensure that the client project contains only WASM-compatible code and dependencies.4

Configuration is handled in the server project's Program.cs file by adding the required services. builder.Services.AddRazorComponents().AddInteractiveServerComponents() enables the server render mode, while AddInteractiveWebAssemblyComponents() is added to support the WASM and Auto modes.2

| Render Mode | Description | Render Location | Interactivity | Initial Load Speed | Optimal Use Case |
| :---- | :---- | :---- | :---- | :---- | :---- |
| **Static Server** | Renders static HTML on the server in response to a request. No client-side interactivity. | Server | ❌ | Fastest | Content-heavy pages, blogs, product listings, or any component that does not require real-time user interaction. The default for all components. |
| **Interactive Server** | Renders on the server and handles UI updates over a SignalR (WebSocket) connection. | Server | ✔️ | Fast (pre-rendered) | Components with moderate interactivity that need access to server-side resources (e.g., database, file system) without exposing a full API. |
| **Interactive WebAssembly** | Renders on the client using a.NET runtime compiled to WebAssembly. | Client | ✔️ | Slower (runtime DL) | Complex, CPU-intensive components with heavy UI logic, applications requiring offline functionality, or progressive web apps (PWAs). |
| **Interactive Auto** | Uses Interactive Server on the first visit for speed, then downloads WASM in the background and uses it for subsequent visits. | Server, then Client | ✔️ | Fast (pre-rendered) | The ideal default for interactive components. Provides the fast initial load of Server mode with the long-term benefits of WASM. |

### **1.3 A Pragmatic Comparison: Blazor vs. Astro**

While the primary focus is on a.NET solution, it is valuable to compare Blazor's modern architecture with a leading JavaScript framework that champions a similar philosophy: Astro. Both frameworks are built around the "islands architecture," a pattern that prioritizes shipping minimal-to-zero JavaScript by default and selectively hydrating interactive components.6 Astro uses

client:\* directives to define its islands, while Blazor uses @rendermode.6 This convergence on the same architectural pattern from different language stacks is significant; it demonstrates that Microsoft has deliberately evolved Blazor to compete directly with state-of-the-art JavaScript frameworks, ensuring that C\# developers do not have to compromise on modern web architecture.9

#### **The Case for Astro**

A strong case can be made for Astro, particularly for projects deeply embedded in the JavaScript ecosystem. Astro is a mature, framework-agnostic static site generator renowned for its best-in-class performance on content-heavy websites.8 It allows developers to use components from various frameworks like React, Vue, and Svelte on the same page, offering unparalleled flexibility.11 Its laser focus on the islands model can lead to a highly intuitive developer experience for those already fluent in JavaScript and its tooling.13

#### **The Overwhelming Case for Blazor for a C\# Developer**

Despite Astro's strengths, the benefits of a unified.NET stack for a developer proficient in C\# are immense and make Blazor the superior choice for this project.

* **Single Language and Toolchain:** The most compelling advantage is the ability to use a single language, C\#, for the entire application stack—from the frontend UI components to the backend API logic and the data access layer.15 This eliminates the cognitive overhead and context-switching inherent in managing separate C\# backend and JavaScript/TypeScript frontend projects.  
* **Shared Logic and Models:** A unified stack allows for the creation of shared libraries containing data models, validation logic, and business rules. These libraries can be referenced by both the server project (for API validation and database operations) and the client project (for client-side form validation in an interactive component), ensuring consistency and eliminating code duplication.  
* **Mature and Integrated Ecosystem:** While the NPM ecosystem for frontend packages is vast, Blazor leverages the extensive NuGet ecosystem of over 350,000 packages for any backend or shared logic requirement.13 Furthermore, the Blazor UI component ecosystem is highly mature, with enterprise-grade component suites from vendors like Telerik and Syncfusion, and a robust open-source community led by libraries like MudBlazor, providing a wealth of pre-built, feature-rich components.16  
* **Modernized Development Experience:** Earlier criticisms of Blazor's developer experience, such as a convoluted process for static site generation, have been addressed.13 The.NET 8 Blazor Web App model makes static-first rendering the default and most intuitive development path, streamlining the entire process.

In conclusion, while Astro is an excellent technology that validates the architectural direction of the modern web, the pattern it champions is now a first-class citizen in Blazor. The significant productivity gains, architectural simplicity, and type safety of a unified.NET stack make Blazor the definitive choice for this project and its target developer profile.

## **Section 2: Backend and API Services \- The ASP.NET Core Foundation**

This section details the architecture of the backend services, built upon the solid and performant foundation of ASP.NET Core. The focus is on designing a clean, scalable public API and implementing efficient data handling strategies, which are critical for supporting the application's data-intensive and interactive features.

### **2.1 Designing the Public API**

A well-designed public API is crucial for the search engine's extensibility and potential future integrations. The recommended approach is to use ASP.NET Core Minimal APIs, a modern, low-ceremony framework for building HTTP endpoints. This approach reduces boilerplate code, encourages a clean, functional style of endpoint definition, and delivers exceptional performance, making it ideal for this project.13

The API should be structured with clear, resource-oriented endpoints. A logical structure would include:

* GET /api/games: Returns a paginated list of all games in the database.  
* GET /api/games/{id}: Retrieves the detailed information for a single game.  
* POST /api/search/text: Accepts a JSON payload with a text query and returns a list of matching games.  
* POST /api/search/image: Accepts a multipart/form-data request with an image file and returns a list of semantically similar games.  
* GET /api/games/{id}/related: Returns a paginated list of games that are most similar to the specified game, which will power the interactive graph visualization.

To ensure the API is easily discoverable and consumable by third-party developers, it is essential to integrate OpenAPI/Swagger documentation. This is seamlessly supported by Minimal APIs and provides an interactive UI for exploring endpoints and testing requests.

### **2.2 API Data Handling and Pagination**

For endpoints that can return large sets of data, such as the list of all games or the related games for the graph, returning the entire dataset in a single response is not feasible. It would result in poor performance, high memory consumption, and a slow user experience.19 Therefore, implementing a robust pagination strategy is a mandatory architectural requirement. The design of the API's pagination directly enables a smooth and responsive frontend experience, such as an "infinite scroll" or "load more" feature for the graph visualization. Providing metadata like

HasNext and TotalPages in the API response is what allows the frontend to intelligently request more data without guessing or making unnecessary calls.

A proven and effective method for implementing pagination is to use the repository pattern combined with LINQ's deferred execution capabilities. This approach cleanly separates the data access logic from the API controller's responsibilities.

The implementation involves several key components:

1. **A Parameter Class:** A class, such as QueryStringParameters, is created to encapsulate the pagination parameters (PageNumber and PageSize). This class should include logic to set default values and enforce a maximum page size to prevent abuse.19  
2. **Repository Logic:** Within the data repository layer, the Skip() and Take() LINQ extension methods are used to construct the database query. Because LINQ queries are deferred, these operations are translated directly into efficient SQL that fetches only the required "slice" of data from the database, minimizing data transfer and processing overhead.19  
3. **A Paginated List Wrapper:** A generic wrapper class, PaginatedList\<T\>, is used to structure the response. This class holds not only the list of items for the current page but also crucial metadata: TotalCount, CurrentPage, TotalPages, HasNext, and HasPrevious. This metadata is essential for the client application to build a proper pagination UI.19  
4. **API Response Formatting:** Following RESTful API best practices, the paginated data (the list of items) is returned in the JSON response body. The pagination metadata, however, is serialized (e.g., to a JSON string) and added to a custom HTTP response header, such as X-Pagination.19 This cleanly separates the resource data from the metadata about the collection.

This structured approach to pagination ensures that the API is not only performant and scalable but also provides a rich, predictable contract for any client application that consumes it.

## **Section 3: Core Feature Implementation \- Multi-Modal Vector Search**

This section addresses the most technically advanced aspect of the project: the implementation of a multi-modal search engine powered by vector similarity search directly within the SQLite database. It covers the selection of the core vector search technology, its integration into the.NET stack, and the step-by-step process for enabling image-based search using a state-of-the-art machine learning model.

### **3.1 Vector Search in SQLite: The sqlite-vec Advantage**

Modern AI-powered search operates on the principle of vector embeddings. Text, images, and other data are converted by a machine learning model into numerical vectors, where semantically similar items are located close to each other in a high-dimensional space. A search then becomes a mathematical operation to find the "nearest neighbors" to a query vector.21

For this project, the definitive choice for enabling this capability within SQLite is the sqlite-vec extension. Its predecessor, sqlite-vss, was a pioneering effort but suffered from critical limitations that made it unsuitable for a production.NET application: it lacked official Windows support, was notoriously difficult to compile, and stored all vectors in memory, limiting scalability.24 The development of

sqlite-vec as its successor is a critical enabling factor for this project. It is distributed as a single, dependency-free C file, making it extremely portable and ensuring it works flawlessly on Windows, which is essential for a standard.NET development environment.24 This evolution makes lightweight, embedded vector search a first-class citizen for cross-platform.NET applications.

To utilize sqlite-vec, several core concepts must be understood:

* **Virtual Tables:** Vectors are stored and indexed in a special virtual table created with a specific schema. For example, CREATE VIRTUAL TABLE vss\_games USING vec0(embedding FLOAT); creates a table named vss\_games designed to index 768-dimension floating-point vectors stored in an embedding column.27  
* **Distance Functions:** Similarity search is performed using SQL functions provided by the extension. For instance, vec\_distance\_cosine(vector1, vector2) calculates the cosine similarity between two vectors, a common metric for semantic search.27 The results can be ordered by this distance to find the closest matches.  
* **Hybrid Search:** For the most relevant results, a powerful technique is hybrid search. This approach combines the results of a vector similarity search with the results of a traditional keyword-based full-text search (using SQLite's FTS5 extension). The rankings from both search types are then fused using an algorithm like Reciprocal Rank Fusion (RRF) to produce a single, more accurate and comprehensive list of results.32

### **3.2.NET Integration Strategies for sqlite-vec**

There are two primary strategies for integrating sqlite-vec into a.NET application, each with its own trade-offs between abstraction and control.

1. **High-Level Abstraction (Microsoft Semantic Kernel):** Microsoft's Semantic Kernel, an open-source SDK for building AI applications, now includes an official connector for sqlite-vec, available via the Microsoft.SemanticKernel.Connectors.SqliteVec NuGet package.34 This is the most straightforward integration path. It allows developers to register the vector store through dependency injection (  
   builder.Services.AddSqliteVectorStore(...)) and define data models using C\# attributes like and. The connector handles the underlying SQL and virtual table creation, abstracting away the low-level details.  
2. **Low-Level Control (Direct P/Invoke and LoadExtension):** The fundamental method for using any SQLite extension in.NET is to load the compiled library (vec0.dll on Windows) into the active SQLite connection. With the Microsoft.Data.Sqlite provider, this can be done simply by calling connection.LoadExtension("vec0") after opening the connection.35 For other SQLite libraries like  
   sqlite-net-pcl, this may require using P/Invoke to call the native sqlite3\_load\_extension function directly.36 This approach provides maximum control, has no external dependencies beyond the extension itself, and avoids the overhead of the Semantic Kernel framework.

For this project, the recommended approach is to **begin with the Semantic Kernel connector** due to its simplicity and alignment with modern.NET dependency injection patterns. It will significantly accelerate initial development. If, during performance tuning, the abstraction layer proves to be a bottleneck, migrating to the direct LoadExtension method is a viable and well-defined optimization path.

| Integration Method | Key NuGet Package | Abstraction Level | Ease of Use | Dependencies | Granularity of Control | Best For |
| :---- | :---- | :---- | :---- | :---- | :---- | :---- |
| **Semantic Kernel Connector** | Microsoft.SemanticKernel.Connectors.SqliteVec | High | Very Easy | Semantic Kernel core libraries | Limited; controlled via connector options and attributes. | Rapid development, applications already using Semantic Kernel, projects where simplicity is prioritized. |
| **Direct P/Invoke/LoadExtension** | Microsoft.Data.Sqlite | Low | Moderate | None (besides the vec0.dll extension file) | Full; allows for raw SQL, custom pragmas, and direct API use. | Performance-critical applications, minimizing dependencies, or when needing advanced sqlite-vec features. |

### **3.3 Implementing Image Search with CLIP and ONNX Runtime**

The screenshot search feature requires a model that can understand both images and text. The CLIP (Contrastive Language-Image Pre-Training) model is perfectly suited for this, as it is trained to map images and their corresponding text descriptions to the same high-dimensional vector space.23 This means the vector embedding generated from a user's screenshot can be directly compared against the text-based vector embeddings of games stored in the database to find the most visually similar results.

To execute this Python-trained model within a C\# application, the ONNX (Open Neural Network Exchange) Runtime is the essential bridge technology. It acts as a high-performance inference engine that can run models from various frameworks (like PyTorch or TensorFlow) natively in.NET.39 This democratizes access to state-of-the-art ML models for.NET developers and eliminates the architectural complexity and operational overhead of maintaining a separate Python microservice for model inference.

The implementation process is as follows:

1. **Project Setup:** Add the Microsoft.ML.OnnxRuntime NuGet package for model inference and SixLabors.ImageSharp for advanced image manipulation.41  
2. **Model Acquisition:** Download a pre-trained CLIP model that has been converted to the .onnx format. These are often available on model-sharing platforms like Hugging Face. The model file should be included in the project assets.  
3. **Loading the Model:** In the C\# service responsible for image processing, create an InferenceSession by passing the path to the .onnx model file. This session object should be treated as a singleton and reused for subsequent inferences, as its initialization is an expensive operation.41  
4. **Image Preprocessing:** When a user uploads a screenshot, it must be preprocessed to match the exact input format the CLIP model was trained on. Using SixLabors.ImageSharp, this involves two key steps:  
   * **Center-Cropping:** The image is cropped to a square aspect ratio from its center.41  
   * **Resizing:** The cropped square image is resized to 224x224 pixels.41  
5. **Tensor Creation:** The preprocessed image's pixel data is then converted into a DenseTensor\<float\>. This is not just a simple conversion of pixel values. Each color channel (Red, Green, Blue) for each pixel must be normalized by subtracting a specific mean and dividing by a specific standard deviation. These precise numerical constants are part of the CLIP model's specification and are critical for accurate results.41  
6. **Inference Execution:** The input tensor is wrapped in a list of NamedOnnxValue objects and passed to the session.Run() method. The ONNX Runtime executes the model and returns the output, which is the final vector embedding for the image.40  
7. **Database Query:** The resulting float array (the image vector) is then used as a parameter in a SQL query against the sqlite-vec virtual table to find the games with the most similar embeddings, thus completing the image search operation.

### **3.4 Building the Search UI**

The UI for multi-modal search will consist of several key components, each of which is an excellent candidate for being an "island of interactivity" rendered using @rendermode="InteractiveServer" or @rendermode="InteractiveAuto".

* **Screenshot Upload:** To handle the image upload, the Toolbelt.Blazor.FileDropZone component is a highly recommended utility.42 It provides a simple and elegant way to wrap Blazor's standard  
  \<InputFile\> component, transforming it into a fully-featured, stylable area that accepts files via drag-and-drop. The component adds a .hover CSS class dynamically, allowing for clear visual feedback when a user drags a file over the zone.42  
* **Searchable Game List:** For the feature allowing users to search from a predefined list of games, a component like the MudBlazor Autocomplete is ideal.18 This component can be configured to call a backend API endpoint as the user types, fetching a filtered list of game titles in real-time. This provides a fast and responsive type-ahead search experience without needing to load the entire list of games into the browser.18

These interactive components, combined with a standard HTML text input for text-based search, form the complete multi-modal search interface.

## **Section 4: Core Feature Implementation \- Interactive Graph Visualization**

This section details the end-to-end implementation of the interactive graph that visualizes relationships between games. The process begins with the backend data calculation, moves to the selection of a frontend rendering library, and concludes with best practices for creating a performant and user-friendly visualization.

### **4.1 Calculating Nearest Neighbors for Graph Data**

The "related games" graph is not a static, predefined data structure. Instead, it is a dynamic, real-time view of the semantic space defined by the game embeddings. The relationships are determined by proximity within this high-dimensional vector space. When a user views a specific game, the backend must calculate its nearest neighbors to generate the graph data.

This calculation is performed as a k-Nearest Neighbor (k-NN) search against the sqlite-vec virtual table. The backend service takes the vector embedding of the currently viewed game and uses it as a query parameter. The SQL query to find the top 10 most similar games would be structured as follows:

SQL

SELECT  
  g.Id,  
  g.Title,  
  v.distance  
FROM vss\_games AS v  
JOIN games AS g ON g.rowid \= v.rowid  
WHERE vss\_search(  
  embedding,  
  (SELECT embedding FROM games WHERE Id \= :currentGameId)  
)  
LIMIT 10;

This query uses a sub-select to fetch the query vector for the current game and then uses the vss\_search function to find the rows in the virtual table with the smallest distance to that vector.25 The results—a list of related game IDs, titles, and their similarity scores—are then exposed via the paginated

GET /api/games/{id}/related API endpoint for the frontend to consume. This approach ensures that the graph is always up-to-date, reflecting the latest data in the database.

### **4.2 Choosing a Visualization Library: Native Blazor vs. JS Interop**

There are three primary avenues for rendering the graph visualization in the Blazor frontend, each with distinct advantages and trade-offs.

1. **Native Blazor Components:** A pure C\# solution can be built using a native Blazor library like Blazor.Diagrams.43 This library is fully customizable and extensible within the Blazor component model, offers good performance (especially in WebAssembly), and completely avoids the complexity of JavaScript interoperability. It is an excellent choice for standard graph requirements like displaying nodes and links, and handling panning and zooming.  
2. **JavaScript Interoperability:** For the ultimate level of control and the ability to create bespoke, highly interactive visualizations, leveraging a powerful JavaScript library via Blazor's JS Interop mechanism is the recommended path. A library like D3.js is renowned for its power in crafting custom, data-driven layouts, such as the force-directed graphs that are often ideal for visualizing relationship networks.15 This approach has a higher initial setup cost but provides unparalleled flexibility.  
3. **Commercial Component Suites:** A middle ground is offered by commercial vendors like Syncfusion and Telerik. They provide feature-rich, professionally supported, and highly optimized chart and diagram components for Blazor that can significantly accelerate development.16

For this project, the recommendation is to **use a JavaScript library like D3.js via JS Interop**. While native components are rapidly maturing, the JavaScript ecosystem for complex, interactive, and aesthetically refined graph visualization remains more advanced. The power and flexibility of a library like D3.js to create a custom, force-directed layout is likely worth the one-time investment in setting up the JS Interop bridge.

| Approach | Example Library/Tool | Customizability | Performance | Ease of Integration | Ecosystem Maturity |
| :---- | :---- | :---- | :---- | :---- | :---- |
| **Native Blazor Component** | Blazor.Diagrams | High; customizable through C\# and Blazor component model. | Good, especially in WebAssembly. Avoids JS Interop overhead. | Very Easy; works entirely within the.NET ecosystem. | Growing, but smaller than the JS ecosystem for specialized visualizations. |
| **JS Interop** | D3.js, G6.js, Chart.js | Extremely High; allows for completely bespoke visualizations limited only by the JS library's capabilities. | Can be excellent, but depends on efficient data transfer and a well-written JS library. JS Interop calls have a small overhead. | Moderate; requires setting up the interop bridge and managing a separate JavaScript file/module. | Very Mature; access to decades of powerful, battle-tested JavaScript visualization libraries. |
| **Commercial Component Suite** | Syncfusion, Telerik | Moderate to High; configurable through C\# APIs, but full customization may be limited. | Excellent; components are typically highly optimized for performance by the vendor. | Easy; provides pre-built Blazor components with extensive documentation and professional support. | Mature; vendors provide a wide range of well-supported and feature-rich components. |

### **4.3 A Practical Guide to Blazor-JavaScript Interoperability**

Successfully integrating a JavaScript visualization library requires a disciplined approach to JS Interop. The architecture should treat the JavaScript code not as a collection of loose scripts, but as a well-defined service with a clear API contract. This prevents common pitfalls and leads to a robust, maintainable solution.

The core of JS Interop is the IJSRuntime service, which is injected into a Blazor component.46 The interaction is bidirectional:

* **Calling JavaScript from C\#:** To render the graph, the Blazor component will fetch the related games data from the backend API and then pass this data to a JavaScript function. This is done via an asynchronous call: await JSRuntime.InvokeVoidAsync("graphVisualizer.render", graphData);. The graphData object will be automatically serialized to JSON.47  
* **Calling C\# from JavaScript:** To handle user interactions within the graph, such as clicking on a node to navigate to that game's page, the JavaScript code must call back into C\#. This is achieved by creating a .NET object reference in the Blazor component and passing it to the JavaScript module during initialization. The JavaScript code can then invoke public methods on this reference: dotNetHelper.invokeMethodAsync('OnNodeClicked', nodeId);. The corresponding C\# method must be decorated with the \`\` attribute to be accessible from JavaScript.47

A critical best practice is to **avoid direct DOM manipulation from JavaScript on elements managed by Blazor**. The JavaScript library should be given a dedicated container element (e.g., a \<div\>) that Blazor creates but does not otherwise interact with. The JavaScript code should render its entire visualization inside this container. Any state changes or events that need to affect the rest of the application must be communicated back to Blazor through the \`\` callback mechanism. Attempting to modify Blazor-rendered DOM from JavaScript can corrupt Blazor's internal representation of the UI, leading to unpredictable behavior and errors.46

### **4.4 Best Practices for Graph Visualization**

Creating an effective graph visualization goes beyond simply rendering nodes and links. The following principles should be applied to ensure the feature is performant, intuitive, and accessible.

* **Performance and Data Handling:** For very large graphs, performance is key. Pre-process and aggregate data on the server whenever possible. Implement "lazy loading" or virtualization techniques to only render the visible portion of the graph, loading more data as the user pans or zooms. The paginated API endpoint for related games is the first step in this process.15  
* **User Experience (UX):**  
  * **Simplicity and Clarity:** The primary goal is to convey relationships clearly. Avoid visual clutter from excessive labels, colors, or overlapping elements. Use whitespace and logical groupings to enhance readability.15  
  * **Meaningful Interactivity:** Interactivity should simplify data exploration. Use tooltips on hover to show game details, and implement drill-down functionality (clicking a node) to navigate to that game's page. Smooth panning and zooming are essential for navigating the graph.15  
* **Accessibility:** Ensure the visualization is usable by a broader audience. Use high-contrast color schemes, support keyboard navigation for interacting with nodes, and provide text alternatives for screen readers to describe the graph's structure and content.15

## **Section 5: Recommendations and Architectural Blueprint**

This final section consolidates the architectural decisions and presents a holistic blueprint for the game search engine, providing a clear path forward for development.

### **5.1 Consolidated Recommendations**

Based on the detailed analysis, the following technology stack and architectural patterns are recommended for the successful implementation of the game search engine:

* **Frontend Framework:** **Blazor Web App**. This provides the ideal foundation, using Static Server-Side Rendering (SSR) by default for maximum performance and SEO-friendliness.  
* **Interactivity Model:** **"Islands of Interactivity"** using Blazor's @rendermode directive. Use InteractiveServer for components requiring moderate interactivity and server access (like the search inputs) and consider InteractiveAuto for more complex client-side components like the graph visualization.  
* **Backend Framework:** **ASP.NET Core Minimal APIs**. This offers a high-performance, low-ceremony approach to building the required public API endpoints.  
* **Database:** **SQLite**. Its lightweight, file-based nature is perfect for this application's scope.  
* **Vector Search Engine:** **sqlite-vec Extension**. This is the definitive choice for enabling vector search within SQLite on a.NET stack, offering portability, performance, and no external dependencies.  
* **Image Search Model:** **CLIP (ONNX format)**. A state-of-the-art model for multi-modal search.  
* **Model Inference Engine:** **ONNX Runtime for.NET**. This allows for high-performance, in-process execution of the CLIP model directly within the C\# backend.  
* **Graph Visualization Library:** **A mature JavaScript library (e.g., D3.js)** integrated via Blazor's **JS Interop** mechanism. This provides the greatest flexibility and power for creating a custom, interactive, and visually appealing graph.

### **5.2 Final Architectural Blueprint**

The complete system architecture can be visualized as a cohesive, end-to-end.NET solution.

1. **User Interaction:** A user interacts with the application in a web browser. The initial page load is a fast, static HTML response generated by Blazor's SSR engine running on the ASP.NET Core server.  
2. **Frontend Components:** The Blazor frontend is composed of mostly static components. Specific "islands" are made interactive.  
   * The **Search Component** (containing text input, file drop zone, and autocomplete) is rendered with an interactive mode (InteractiveServer). User input triggers C\# event handlers that call the backend API.  
   * The **Graph Visualization Component** is also rendered interactively. It calls the backend API to fetch related game data and then uses JS Interop to pass this data to a JavaScript library (D3.js) for rendering within a dedicated \<div\>. Clicks on graph nodes are passed from JavaScript back to C\# via JS Interop.  
3. **Backend API:** The ASP.NET Core application hosts the Blazor UI and exposes the public Minimal API. API endpoints handle requests for searching, retrieving game details, and calculating graph data.  
4. **Machine Learning Inference:** When a screenshot search request is received, the API calls a dedicated C\# service. This service uses the **ONNX Runtime** to load the **CLIP model**. It preprocesses the user's image, executes the model to generate a vector embedding, and returns this vector.  
5. **Database and Vector Search:** All API services interact with a single **SQLite** database file. For vector search operations (both text and image), the services execute SQL queries that leverage the **sqlite-vec** extension. These queries perform k-Nearest Neighbor searches against the indexed game embeddings to find the most relevant results.

This blueprint illustrates a modern, performant, and maintainable architecture that leverages the full power of the.NET ecosystem to deliver a sophisticated, AI-powered application.

#### **Works cited**

1. Blazor and .NET 8: How I Built a Fast and Flexible Website | Fritz on the Web, accessed August 31, 2025, [https://jeffreyfritz.com/2024/02/blazor-and-net-8-how-i-build-a-fast-and-flexible-website/](https://jeffreyfritz.com/2024/02/blazor-and-net-8-how-i-build-a-fast-and-flexible-website/)  
2. ASP.NET Core Blazor render modes | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-9.0)  
3. Implementing and understanding a Blazor Static Server-Side Rendered Counter Page, accessed August 31, 2025, [https://stackoverflow.com/questions/77568579/implementing-and-understanding-a-blazor-static-server-side-rendered-counter-page](https://stackoverflow.com/questions/77568579/implementing-and-understanding-a-blazor-static-server-side-rendered-counter-page)  
4. Exploring Blazor Changes in .NET 8 \- Interactive Components using Blazor Server, accessed August 31, 2025, [https://jonhilton.net/blazor-interactive-islands-server/](https://jonhilton.net/blazor-interactive-islands-server/)  
5. Blazor Webassembly interactivity creates two projects. What's the point of the server one? \- Reddit, accessed August 31, 2025, [https://www.reddit.com/r/Blazor/comments/1i79jul/blazor\_webassembly\_interactivity\_creates\_two/](https://www.reddit.com/r/Blazor/comments/1i79jul/blazor_webassembly_interactivity_creates_two/)  
6. Island Architecture in Astro: A Example with an Interactive Pricing Calculator \- Medium, accessed August 31, 2025, [https://medium.com/@ignatovich.dm/island-architecture-in-astro-a-example-with-an-interactive-pricing-calculator-785a218d1902](https://medium.com/@ignatovich.dm/island-architecture-in-astro-a-example-with-an-interactive-pricing-calculator-785a218d1902)  
7. Islands architecture | Docs, accessed August 31, 2025, [https://docs.astro.build/en/concepts/islands/](https://docs.astro.build/en/concepts/islands/)  
8. Exploring the Features and Flexibility of Astro \- Apiumhub, accessed August 31, 2025, [https://apiumhub.com/tech-blog-barcelona/features-flexibility-astro/](https://apiumhub.com/tech-blog-barcelona/features-flexibility-astro/)  
9. Blazor brought component-based UI architecture to ASP.NET, and now Astro is b... \- DEV Community, accessed August 31, 2025, [https://dev.to/manigandham/comment/1hcb7](https://dev.to/manigandham/comment/1hcb7)  
10. A first look at Astro, astronomical results \- DEV Community, accessed August 31, 2025, [https://dev.to/dailydevtips1/a-first-look-at-astro-astronomical-results-l29](https://dev.to/dailydevtips1/a-first-look-at-astro-astronomical-results-l29)  
11. Intro to Astro—A Web Framework for Content-Driven Websites, accessed August 31, 2025, [https://www.telerik.com/blogs/introduction-astro-web-framework-content-driven-websites](https://www.telerik.com/blogs/introduction-astro-web-framework-content-driven-websites)  
12. Astro vs Next JS: Which Should You Use and Why \- Bluebird International, accessed August 31, 2025, [https://bluebirdinternational.com/astro-vs-next-js/](https://bluebirdinternational.com/astro-vs-next-js/)  
13. The current state of Blazor. Thinking about using Blazor in ... \- Medium, accessed August 31, 2025, [https://medium.com/@jakobarsement/the-current-state-of-blazor-c38c93e27fab](https://medium.com/@jakobarsement/the-current-state-of-blazor-c38c93e27fab)  
14. Astro 1.0 – a web framework for building fast, content-focused websites | Hacker News, accessed August 31, 2025, [https://news.ycombinator.com/item?id=32401159](https://news.ycombinator.com/item?id=32401159)  
15. Blazor Graph Visualization Techniques \- Tom Sawyer Software \- Blog, accessed August 31, 2025, [https://blog.tomsawyer.com/blazor-graph-visualization-techniques](https://blog.tomsawyer.com/blazor-graph-visualization-techniques)  
16. Blazor Charts | Interactive Live Charts \- Syncfusion, accessed August 31, 2025, [https://www.syncfusion.com/blazor-components/blazor-charts](https://www.syncfusion.com/blazor-components/blazor-charts)  
17. Blazor Charts and Graphs \- Overview \- Demos, accessed August 31, 2025, [https://demos.telerik.com/blazor-ui/chart/overview](https://demos.telerik.com/blazor-ui/chart/overview)  
18. Autocomplete \- MudBlazor \- Blazor Component Library, accessed August 31, 2025, [https://mudblazor.com/components/autocomplete](https://mudblazor.com/components/autocomplete)  
19. Paging in ASP.NET Core Web API \- Code Maze, accessed August 31, 2025, [https://code-maze.com/paging-aspnet-core-webapi/](https://code-maze.com/paging-aspnet-core-webapi/)  
20. Pagination in .Net Api \- DEV Community, accessed August 31, 2025, [https://dev.to/drsimplegraffiti/pagination-in-net-api-4opp](https://dev.to/drsimplegraffiti/pagination-in-net-api-4opp)  
21. Using Vector Databases to Extend LLM Capabilities \- .NET | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/dotnet/ai/conceptual/vector-databases](https://learn.microsoft.com/en-us/dotnet/ai/conceptual/vector-databases)  
22. Your Own Vector Search in 5 Minutes with SQLite, OpenAI Embeddings, and Node.js, accessed August 31, 2025, [https://markus.oberlehner.net/blog/your-own-vector-search-in-5-minutes-with-sqlite-openai-embeddings-and-nodejs](https://markus.oberlehner.net/blog/your-own-vector-search-in-5-minutes-with-sqlite-openai-embeddings-and-nodejs)  
23. Implement unified text and image search with a CLIP model using Amazon SageMaker and Amazon OpenSearch Service | Artificial Intelligence, accessed August 31, 2025, [https://aws.amazon.com/blogs/machine-learning/implement-unified-text-and-image-search-with-a-clip-model-using-amazon-sagemaker-and-amazon-opensearch-service/](https://aws.amazon.com/blogs/machine-learning/implement-unified-text-and-image-search-with-a-clip-model-using-amazon-sagemaker-and-amazon-opensearch-service/)  
24. SQLite Gets Into Vector Search \- I Programmer, accessed August 31, 2025, [https://www.i-programmer.info/news/84-database/17458-sqlite-gets-into-vector-search.html](https://www.i-programmer.info/news/84-database/17458-sqlite-gets-into-vector-search.html)  
25. asg017/sqlite-vss: A SQLite extension for efficient vector search, based on Faiss\! \- GitHub, accessed August 31, 2025, [https://github.com/asg017/sqlite-vss](https://github.com/asg017/sqlite-vss)  
26. Introducing sqlite-vss: A SQLite Extension for Vector Search \- Simon Willison's Weblog, accessed August 31, 2025, [https://simonwillison.net/2023/Feb/10/sqlite-vss/](https://simonwillison.net/2023/Feb/10/sqlite-vss/)  
27. How sqlite-vec Works for Storing and Querying Vector Embeddings | by Stephen Collins, accessed August 31, 2025, [https://medium.com/@stephenc211/how-sqlite-vec-works-for-storing-and-querying-vector-embeddings-165adeeeceea](https://medium.com/@stephenc211/how-sqlite-vec-works-for-storing-and-querying-vector-embeddings-165adeeeceea)  
28. How to use sqlite-vec to store and query vector embeddings \- DEV Community, accessed August 31, 2025, [https://dev.to/stephenc222/how-to-use-sqlite-vec-to-store-and-query-vector-embeddings-58mf](https://dev.to/stephenc222/how-to-use-sqlite-vec-to-store-and-query-vector-embeddings-58mf)  
29. How to Use sqlite-vec to Store and Query Vector Embeddings \- Stephen Collins.tech, accessed August 31, 2025, [https://stephencollins.tech/posts/how-to-use-sqlite-vec-to-store-and-query-vector-embeddings](https://stephencollins.tech/posts/how-to-use-sqlite-vec-to-store-and-query-vector-embeddings)  
30. Using sqlite-vec with embeddings in sqlite-utils and Datasette \- Simon Willison: TIL, accessed August 31, 2025, [https://til.simonwillison.net/sqlite/sqlite-vec](https://til.simonwillison.net/sqlite/sqlite-vec)  
31. API Reference | sqlite-vec \- Alex Garcia, accessed August 31, 2025, [https://alexgarcia.xyz/sqlite-vec/api-reference.html](https://alexgarcia.xyz/sqlite-vec/api-reference.html)  
32. Hybrid full-text search and vector search with SQLite \- Simon Willison's Weblog, accessed August 31, 2025, [https://simonwillison.net/2024/Oct/4/hybrid-full-text-search-and-vector-search-with-sqlite/](https://simonwillison.net/2024/Oct/4/hybrid-full-text-search-and-vector-search-with-sqlite/)  
33. SQLite-Vec — llama-stack documentation, accessed August 31, 2025, [https://llama-stack.readthedocs.io/en/v0.2.11/providers/vector\_io/sqlite-vec.html](https://llama-stack.readthedocs.io/en/v0.2.11/providers/vector_io/sqlite-vec.html)  
34. Using the Semantic Kernel SQLite Vector Store connector (Preview ..., accessed August 31, 2025, [https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/sqlite-connector](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/sqlite-connector)  
35. .NET install support · Issue \#193 · asg017/sqlite-vec \- GitHub, accessed August 31, 2025, [https://github.com/asg017/sqlite-vec/issues/193](https://github.com/asg017/sqlite-vec/issues/193)  
36. How to Wrap a Real Native Library \- JacksonDunstan.com, accessed August 31, 2025, [https://www.jacksondunstan.com/articles/5117](https://www.jacksondunstan.com/articles/5117)  
37. Adding extensions to sqlite-net | Damian Mehers' blog, accessed August 31, 2025, [https://damian.fyi/xamarin/2025/04/19/adding-extensions-to-sqlite-net.html](https://damian.fyi/xamarin/2025/04/19/adding-extensions-to-sqlite-net.html)  
38. openai/CLIP: CLIP (Contrastive Language-Image Pretraining), Predict the most relevant text snippet given an image \- GitHub, accessed August 31, 2025, [https://github.com/openai/CLIP](https://github.com/openai/CLIP)  
39. Inference with C\# | onnxruntime, accessed August 31, 2025, [https://onnxruntime.ai/docs/tutorials/csharp/](https://onnxruntime.ai/docs/tutorials/csharp/)  
40. C\# | onnxruntime, accessed August 31, 2025, [https://onnxruntime.ai/docs/get-started/with-csharp.html](https://onnxruntime.ai/docs/get-started/with-csharp.html)  
41. OpenAI's CLIP inference in C\# using ONNX Runtime · Bart Broere, accessed August 31, 2025, [https://bartbroere.eu/2023/07/29/openai-clip-csharp-onnx/](https://bartbroere.eu/2023/07/29/openai-clip-csharp-onnx/)  
42. jsakamoto/Toolbelt.Blazor.FileDropZone: Surround an ... \- GitHub, accessed August 31, 2025, [https://github.com/jsakamoto/Toolbelt.Blazor.FileDropZone](https://github.com/jsakamoto/Toolbelt.Blazor.FileDropZone)  
43. A fully customizable and extensible all-purpose diagrams library for Blazor \- GitHub, accessed August 31, 2025, [https://github.com/Blazor-Diagrams/Blazor.Diagrams](https://github.com/Blazor-Diagrams/Blazor.Diagrams)  
44. D3 by Observable | The JavaScript library for bespoke data visualization, accessed August 31, 2025, [https://d3js.org/](https://d3js.org/)  
45. Explore Interactive Drill-Down Charts in Blazor for Deeper Data Insights | Syncfusion Blogs, accessed August 31, 2025, [https://www.syncfusion.com/blogs/post/drill-down-charts-in-blazor/amp](https://www.syncfusion.com/blogs/post/drill-down-charts-in-blazor/amp)  
46. ASP.NET Core Blazor JavaScript interoperability (JS interop) | Microsoft Learn, accessed August 31, 2025, [https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-9.0](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-9.0)  
47. Using JavaScript Interop in Blazor | Chris Sainty, accessed August 31, 2025, [https://chrissainty.com/using-javascript-interop-in-razor-components-and-blazor/](https://chrissainty.com/using-javascript-interop-in-razor-components-and-blazor/)  
48. Guide to Blazor JavaScript Interop \- Imaginet Blog, accessed August 31, 2025, [https://imaginet.com/2021/guide-blazor-javascript-interop/](https://imaginet.com/2021/guide-blazor-javascript-interop/)  
49. JavaScript interop \- JetBrains Guide, accessed August 31, 2025, [https://www.jetbrains.com/guide/dotnet/tutorials/blazor-essentials/js-interop/](https://www.jetbrains.com/guide/dotnet/tutorials/blazor-essentials/js-interop/)  
50. Intro to JS Interop in Blazor \- DEV Community, accessed August 31, 2025, [https://dev.to/rasheedmozaffar/intro-to-js-interop-in-blazor-46mo](https://dev.to/rasheedmozaffar/intro-to-js-interop-in-blazor-46mo)  
51. Blazor, get Input value from Javascript created DOM Element \- Stack Overflow, accessed August 31, 2025, [https://stackoverflow.com/questions/58301986/blazor-get-input-value-from-javascript-created-dom-element](https://stackoverflow.com/questions/58301986/blazor-get-input-value-from-javascript-created-dom-element)