// Solodoc PWA Service Worker — offline support
// Caches: app shell (static assets) + API responses

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const apiCacheName = 'solodoc-api-cache';
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// API paths to cache for offline use (GET requests only)
const apiCachePaths = [
    '/api/projects',
    '/api/jobs',
    '/api/checklists/templates',
    '/api/checklists/instances',
    '/api/contacts',
    '/api/chemicals',
    '/api/equipment',
    '/api/locations',
    '/api/employees',
    '/api/deviations',
    '/api/hours',
    '/api/dashboard/summary',
    '/api/hours/active-clock',
    '/api/checkin/status',
    '/api/checkin/all-sites',
    '/api/checkin/log',
    '/api/auth/tenants',
    '/api/marketplace'
];

const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    const url = new URL(event.request.url);

    // ── API requests: network-first with cache fallback ──
    if (event.request.method === 'GET' && url.pathname.startsWith('/api/')) {
        // Check if this API path should be cached
        var shouldCache = apiCachePaths.some(path => url.pathname.startsWith(path));

        if (shouldCache) {
            try {
                // Try network first
                var networkResponse = await fetch(event.request.clone());
                if (networkResponse.ok) {
                    // Cache the successful response
                    var cache = await caches.open(apiCacheName);
                    cache.put(event.request, networkResponse.clone());
                    return networkResponse;
                }
                // Non-OK response — try cache
                var cached = await caches.open(apiCacheName).then(c => c.match(event.request));
                return cached || networkResponse;
            } catch (e) {
                // Network error (offline) — serve from cache
                var cached = await caches.open(apiCacheName).then(c => c.match(event.request));
                if (cached) return cached;
                // No cache — return offline JSON response
                return new Response(JSON.stringify({ error: 'Offline', offline: true }), {
                    status: 503,
                    headers: { 'Content-Type': 'application/json' }
                });
            }
        }

        // Non-cacheable API request — just fetch
        return fetch(event.request);
    }

    // ── Static assets: cache-first ──
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For SPA navigation (e.g. /projects/123), serve cached index.html
        const isNavigate = event.request.mode === 'navigate';
        const isApiOrAsset = url.pathname.startsWith('/api/') || url.pathname.startsWith('/_');
        const shouldServeIndexHtml = isNavigate && !isApiOrAsset;

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    // If offline and no cache hit, try index.html as last resort for navigations
    if (!cachedResponse && event.request.mode === 'navigate') {
        try {
            return await fetch(event.request);
        } catch (e) {
            const cache = await caches.open(cacheName);
            return await cache.match('index.html') || new Response('Offline', { status: 503 });
        }
    }

    return cachedResponse || fetch(event.request);
}
