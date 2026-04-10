window.mapInterop = {
    _maps: {},

    /**
     * Show a map for an address using Nominatim geocoding.
     * @param {string} elementId - Container element ID
     * @param {string} address - Address to geocode and show
     * @param {number} zoom - Zoom level (default 15)
     */
    showAddress: async function (elementId, address, zoom) {
        var container = document.getElementById(elementId);
        if (!container) return;

        // Clean up existing map
        if (this._maps[elementId]) {
            this._maps[elementId].remove();
            delete this._maps[elementId];
        }

        zoom = zoom || 15;

        // Default to Norway center while loading
        var map = L.map(elementId).setView([60.39, 5.32], 5);
        this._maps[elementId] = map;

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap',
            maxZoom: 19
        }).addTo(map);

        // Geocode the address via Nominatim
        try {
            var query = encodeURIComponent(address + ', Norway');
            var response = await fetch('https://nominatim.openstreetmap.org/search?format=json&q=' + query + '&limit=1', {
                headers: { 'Accept-Language': 'no' }
            });
            var results = await response.json();

            if (results && results.length > 0) {
                var lat = parseFloat(results[0].lat);
                var lon = parseFloat(results[0].lon);
                map.setView([lat, lon], zoom);
                L.marker([lat, lon]).addTo(map)
                    .bindPopup('<b>' + address + '</b>')
                    .openPopup();
            }
        } catch (e) {
            console.warn('Geocoding failed:', e);
        }

        // Fix leaflet rendering in hidden/dynamic containers
        setTimeout(function () { map.invalidateSize(); }, 200);
    },

    /**
     * Remove a map instance.
     * @param {string} elementId
     */
    destroy: function (elementId) {
        if (this._maps[elementId]) {
            this._maps[elementId].remove();
            delete this._maps[elementId];
        }
    }
};
