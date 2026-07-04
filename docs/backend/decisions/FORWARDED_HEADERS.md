# Architectural Decision: Handling X-Forwarded-For and Rate Limiting

## Context
In modern web applications, the API is almost never exposed directly to the public internet. Instead, it sits behind a Reverse Proxy, such as Nginx, Cloudflare, or a managed load balancer from a cloud provider (e.g., Azure Container Apps Ingress).
*(Note: **Nginx** is one of the most popular open-source web servers in the world, widely used as a reverse proxy to securely route traffic to internal applications).*

When the proxy intercepts a client request and forwards it to our API, the `HttpContext.Connection.RemoteIpAddress` property at the TCP level will contain the IP address of the proxy server (the load balancer), not the actual user's IP.

To pass the real user IP address, the proxy adds a special HTTP header: `X-Forwarded-For`. Our application uses the `ForwardedHeadersMiddleware` to read this header and substitute the `RemoteIpAddress` with the true IP.

This is critical for the Rate Limiter because if it sees the load balancer's IP, it will share the request limit across all users of the site simultaneously, quickly blocking access for everyone.

### What is an Ingress?
In modern cloud architectures (like Kubernetes or Azure Container Apps), an **Ingress** acts as the "front door" for your application. It is a specialized reverse proxy (often powered by software like Envoy or Nginx) that sits at the edge of the cloud network. 
*(Note: **Kubernetes** is essentially a smart manager for your containers. Instead of you manually starting Docker containers on a server, you tell Kubernetes: "Keep 3 copies of my API running." If a container crashes, Kubernetes automatically restarts it; if a server dies, it moves the container to a healthy server. Azure Container Apps runs Kubernetes under the hood to do all this work for you, which is why they share concepts like "Ingress").*

Its main jobs are:
1. **SSL Termination**: It handles the HTTPS certificates so your app doesn't have to.
2. **Routing**: It looks at the URL (e.g., `/api`) and routes the traffic to the correct internal container.
3. **Network Isolation**: It protects your containers from being accessed directly from the outside world.

Because the Ingress intercepts every single request, it is the one responsible for adding the `X-Forwarded-For` header before passing the request down to your API.
## The Problem: IP Spoofing
The `X-Forwarded-For` header is just a string of text. Any attacker can add it to their request:
`X-Forwarded-For: 127.0.0.1`

If our server blindly trusts this header from *anyone*, an attacker could bypass the Rate Limiter by constantly changing the value of this header, or even impersonate an administrator.

Because of this, by default, ASP.NET Core trusts this header **only when the request comes from the local host (127.0.0.1)**. It does not trust any other proxy servers, and the header is ignored.

## The Solution: Two Approaches to Proxy Trust

There are two fundamental approaches to solving this problem, depending on the type of infrastructure:

### Approach 1: Trusted IP List (Traditional Hosting)
*Used when deploying the application on a self-hosted server (VPS, Ubuntu) with Nginx/Caddy.*
*(Note: **Caddy** is another popular modern web server, similar to Nginx, known for automatically managing HTTPS certificates).*

In this environment, the application port (e.g., 5000) might accidentally or intentionally be exposed to the internet. Therefore, we must explicitly specify the static IP address of our Nginx in the configuration (e.g., `127.0.0.1` or `192.168.1.5`).

ASP.NET Core checks who is knocking directly on its door. If it is our Nginx, it reads the `X-Forwarded-For` header. If a hacker knocks directly on port 5000 with a spoofed header, ASP.NET Core sees that their IP is not in the trusted list and ignores the header.

### Approach 2: Trust-All + Network Isolation (Cloud Native / PaaS)
*Used in Azure Container Apps (our case), AWS ECS, GCP Cloud Run.*

In managed cloud services, the IP address of the load balancer **is dynamic** (it can change). Therefore, hardcoding it is impossible and impractical.

However, the architecture of Azure Container Apps (ACA) guarantees full network isolation. The container physically has no access to the open internet; the only way to reach it is exclusively through the managed Ingress (Envoy proxy) provided by Azure.

How this protects against IP Spoofing:
1. An attacker sends a request with a spoofed header `X-Forwarded-For: 8.8.8.8` to the Ingress.
2. The Ingress sees the real IP of the attacker (e.g., `200.100.50.25`).
3. The Ingress does not replace the header, but **appends** the real IP to the end. The result that goes to the API is:
   `X-Forwarded-For: 8.8.8.8, 200.100.50.25`
4. In ASP.NET Core, the `ForwardLimit` property defaults to `1`.
5. Because we configured it to "trust all networks" (`KnownNetworks.Clear()`), ASP.NET Core starts reading the `X-Forwarded-For` header **from right to left**.
6. It takes only **one** address (`200.100.50.25`), sets it as the client IP, and stops. The spoofed address `8.8.8.8` is safely ignored!

## Our Decision

For deployment in Azure Container Apps, we use **Approach 2 (Trust-All)**.

In `Program.cs`, we clear the lists of trusted proxies if the `Proxy:TrustedProxies` variable is not set:
```csharp
options.KnownNetworks.Clear();
options.KnownProxies.Clear();
```

This is secure because:
1. Requests can physically only come from the managed Azure Ingress.
2. Azure Ingress always correctly appends the real client IP to the end of the list.
3. `ForwardLimit = 1` ensures that we take exactly the client IP (the last one in the list), discarding any spoofing attempts.

## How to Talk About This in an Interview

If asked about security and IP address resolution:
> *"Our API is deployed in Azure Container Apps. Since the Azure Ingress IP address is dynamic, we don't hardcode KnownProxies. Instead, we rely on the Network Isolation of the platform itself: the container cannot be reached bypassing the load balancer. The Azure Envoy Ingress is guaranteed to append the real TCP IP of the client to the end of the X-Forwarded-For header. Because our ForwardLimit is 1, the Middleware parses the header from right to left and takes the single reliable address, completely eliminating the risk of IP Spoofing. This is a standard and recommended pattern for Cloud Native (PaaS) environments."*
