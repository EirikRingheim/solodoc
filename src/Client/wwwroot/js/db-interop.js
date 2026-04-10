// ═══════════════════════════════════════════════════════
// Solodoc IndexedDB Interop — offline storage layer
// ═══════════════════════════════════════════════════════

window.solodocDb = {
    _db: null,
    _dbName: 'solodoc-offline',
    _version: 2,

    // ── Open / Initialize ──────────────────────────────
    open: function () {
        return new Promise((resolve, reject) => {
            if (this._db) { resolve(true); return; }

            var request = indexedDB.open(this._dbName, this._version);

            request.onupgradeneeded = function (event) {
                var db = event.target.result;

                // Sync queue — operations waiting to be sent to server
                if (!db.objectStoreNames.contains('syncQueue')) {
                    var syncStore = db.createObjectStore('syncQueue', { keyPath: 'id', autoIncrement: true });
                    syncStore.createIndex('timestamp', 'timestamp', { unique: false });
                    syncStore.createIndex('entityType', 'entityType', { unique: false });
                }

                // Entity cache — cached API responses
                if (!db.objectStoreNames.contains('entityCache')) {
                    var cacheStore = db.createObjectStore('entityCache', { keyPath: 'key' });
                    cacheStore.createIndex('entityType', 'entityType', { unique: false });
                    cacheStore.createIndex('cachedAt', 'cachedAt', { unique: false });
                }

                // Metadata — last sync time, offline settings
                if (!db.objectStoreNames.contains('metadata')) {
                    db.createObjectStore('metadata', { keyPath: 'key' });
                }

                // Local app state — pending clock-in, check-in, etc.
                if (!db.objectStoreNames.contains('localState')) {
                    db.createObjectStore('localState', { keyPath: 'key' });
                }

                // Offline photos — blobs stored for later upload
                if (!db.objectStoreNames.contains('offlinePhotos')) {
                    var photoStore = db.createObjectStore('offlinePhotos', { keyPath: 'id', autoIncrement: true });
                    photoStore.createIndex('entityType', 'entityType', { unique: false });
                }
            };

            request.onsuccess = (event) => {
                this._db = event.target.result;
                resolve(true);
            };

            request.onerror = (event) => {
                console.error('IndexedDB open error:', event.target.error);
                reject(event.target.error);
            };
        });
    },

    // ── Sync Queue Operations ──────────────────────────

    /**
     * Add an operation to the sync queue.
     * @param {string} entityType - e.g., "timeEntry", "deviation", "checklistItem"
     * @param {string} action - "POST", "PUT", "PATCH", "DELETE"
     * @param {string} url - API endpoint URL
     * @param {object|null} payload - JSON body
     * @param {number|null} latitude
     * @param {number|null} longitude
     * @returns {number} The queue entry ID
     */
    addToSyncQueue: async function (entityType, action, url, payload, latitude, longitude) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('syncQueue', 'readwrite');
            var store = tx.objectStore('syncQueue');
            var entry = {
                entityType: entityType,
                action: action,
                url: url,
                payload: payload,
                latitude: latitude,
                longitude: longitude,
                timestamp: Date.now(),
                retryCount: 0,
                status: 'pending' // pending, sending, failed
            };
            var request = store.add(entry);
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },

    /**
     * Get all pending sync queue entries.
     * @returns {Array} Queue entries sorted by timestamp
     */
    getSyncQueue: async function () {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('syncQueue', 'readonly');
            var store = tx.objectStore('syncQueue');
            var index = store.index('timestamp');
            var request = index.getAll();
            request.onsuccess = () => resolve(request.result.filter(e => e.status !== 'completed'));
            request.onerror = () => reject(request.error);
        });
    },

    /**
     * Get the count of pending items in the sync queue.
     * @returns {number}
     */
    getSyncQueueCount: async function () {
        var items = await this.getSyncQueue();
        return items.length;
    },

    /**
     * Update a sync queue entry (e.g., mark as sending, failed, completed).
     * @param {number} id
     * @param {object} updates - Fields to update
     */
    updateSyncQueueEntry: async function (id, updates) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('syncQueue', 'readwrite');
            var store = tx.objectStore('syncQueue');
            var getReq = store.get(id);
            getReq.onsuccess = () => {
                var entry = getReq.result;
                if (!entry) { resolve(false); return; }
                Object.assign(entry, updates);
                var putReq = store.put(entry);
                putReq.onsuccess = () => resolve(true);
                putReq.onerror = () => reject(putReq.error);
            };
            getReq.onerror = () => reject(getReq.error);
        });
    },

    /**
     * Remove a sync queue entry.
     * @param {number} id
     */
    removeSyncQueueEntry: async function (id) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('syncQueue', 'readwrite');
            var store = tx.objectStore('syncQueue');
            var request = store.delete(id);
            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    },

    /**
     * Clear all completed entries from the sync queue.
     */
    clearCompletedSyncEntries: async function () {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('syncQueue', 'readwrite');
            var store = tx.objectStore('syncQueue');
            var request = store.openCursor();
            var deleted = 0;
            request.onsuccess = (event) => {
                var cursor = event.target.result;
                if (cursor) {
                    if (cursor.value.status === 'completed') {
                        cursor.delete();
                        deleted++;
                    }
                    cursor.continue();
                } else {
                    resolve(deleted);
                }
            };
            request.onerror = () => reject(request.error);
        });
    },

    // ── Entity Cache Operations ────────────────────────

    /**
     * Cache an API response.
     * @param {string} key - Cache key (usually the API URL)
     * @param {string} entityType - e.g., "projects", "checklists", "contacts"
     * @param {object} data - The response data to cache
     */
    cacheEntity: async function (key, entityType, data) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('entityCache', 'readwrite');
            var store = tx.objectStore('entityCache');
            var entry = {
                key: key,
                entityType: entityType,
                data: data,
                cachedAt: Date.now()
            };
            var request = store.put(entry);
            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    },

    /**
     * Get a cached entity.
     * @param {string} key - Cache key
     * @param {number} maxAgeMs - Maximum age in milliseconds (default: 1 hour)
     * @returns {object|null} The cached data, or null if not found/expired
     */
    getCachedEntity: async function (key, maxAgeMs) {
        await this.open();
        maxAgeMs = maxAgeMs || 3600000; // 1 hour default
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('entityCache', 'readonly');
            var store = tx.objectStore('entityCache');
            var request = store.get(key);
            request.onsuccess = () => {
                var entry = request.result;
                if (!entry) { resolve(null); return; }
                if (Date.now() - entry.cachedAt > maxAgeMs) { resolve(null); return; }
                resolve(entry.data);
            };
            request.onerror = () => reject(request.error);
        });
    },

    /**
     * Get all cached entities of a type.
     * @param {string} entityType
     * @returns {Array}
     */
    getCachedByType: async function (entityType) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('entityCache', 'readonly');
            var store = tx.objectStore('entityCache');
            var index = store.index('entityType');
            var request = index.getAll(entityType);
            request.onsuccess = () => resolve(request.result.map(e => e.data));
            request.onerror = () => reject(request.error);
        });
    },

    /**
     * Clear all cached entities of a type.
     * @param {string} entityType
     */
    clearCacheByType: async function (entityType) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('entityCache', 'readwrite');
            var store = tx.objectStore('entityCache');
            var index = store.index('entityType');
            var request = index.openCursor(entityType);
            request.onsuccess = (event) => {
                var cursor = event.target.result;
                if (cursor) {
                    cursor.delete();
                    cursor.continue();
                } else {
                    resolve(true);
                }
            };
            request.onerror = () => reject(request.error);
        });
    },

    // ── Metadata Operations ────────────────────────────

    /**
     * Set a metadata value.
     * @param {string} key
     * @param {*} value
     */
    setMeta: async function (key, value) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('metadata', 'readwrite');
            var store = tx.objectStore('metadata');
            var request = store.put({ key: key, value: value });
            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    },

    /**
     * Get a metadata value.
     * @param {string} key
     * @returns {*} The value, or null
     */
    getMeta: async function (key) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('metadata', 'readonly');
            var store = tx.objectStore('metadata');
            var request = store.get(key);
            request.onsuccess = () => resolve(request.result ? request.result.value : null);
            request.onerror = () => reject(request.error);
        });
    },

    // ── Connectivity ───────────────────────────────────

    /**
     * Check if browser is online.
     * @returns {boolean}
     */
    isOnline: function () {
        return navigator.onLine;
    },

    /**
     * Register callback for online/offline changes.
     * @param {DotNetObjectReference} dotnetRef
     * @param {string} methodName
     */
    registerConnectivityHandler: function (dotnetRef, methodName) {
        var handler = function () {
            dotnetRef.invokeMethodAsync(methodName, navigator.onLine);
        };
        window.addEventListener('online', handler);
        window.addEventListener('offline', handler);

        // Store for cleanup
        window._solodocConnectivityHandler = handler;
        window._solodocConnectivityRef = dotnetRef;
    },

    /**
     * Unregister connectivity handler.
     */
    unregisterConnectivityHandler: function () {
        if (window._solodocConnectivityHandler) {
            window.removeEventListener('online', window._solodocConnectivityHandler);
            window.removeEventListener('offline', window._solodocConnectivityHandler);
            window._solodocConnectivityHandler = null;
            window._solodocConnectivityRef = null;
        }
    },

    // ── Local State ────────────────────────────────────

    setLocalState: async function (key, value) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('localState', 'readwrite');
            var store = tx.objectStore('localState');
            var request = store.put({ key: key, value: value, updatedAt: Date.now() });
            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    },

    getLocalState: async function (key) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('localState', 'readonly');
            var store = tx.objectStore('localState');
            var request = store.get(key);
            request.onsuccess = () => resolve(request.result ? request.result.value : null);
            request.onerror = () => reject(request.error);
        });
    },

    removeLocalState: async function (key) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('localState', 'readwrite');
            var store = tx.objectStore('localState');
            var request = store.delete(key);
            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    },

    // ── Offline Photos ─────────────────────────────────

    saveOfflinePhoto: async function (entityType, entityId, fileName, base64Data, contentType) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('offlinePhotos', 'readwrite');
            var store = tx.objectStore('offlinePhotos');
            var entry = {
                entityType: entityType,
                entityId: entityId,
                fileName: fileName,
                base64Data: base64Data,
                contentType: contentType,
                savedAt: Date.now()
            };
            var request = store.add(entry);
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },

    getOfflinePhotos: async function () {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('offlinePhotos', 'readonly');
            var store = tx.objectStore('offlinePhotos');
            var request = store.getAll();
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },

    removeOfflinePhoto: async function (id) {
        await this.open();
        return new Promise((resolve, reject) => {
            var tx = this._db.transaction('offlinePhotos', 'readwrite');
            var store = tx.objectStore('offlinePhotos');
            var request = store.delete(id);
            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    }
};
