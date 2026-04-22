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
     * Initialize a geofence editor map.
     * @param {string} elementId - Container element ID
     * @param {number|null} lat - Center latitude (null = default Norway)
     * @param {number|null} lng - Center longitude
     * @param {string|null} geojson - Existing geofence GeoJSON
     * @param {number|null} radius - Existing radius (meters)
     * @param {object} dotnetRef - DotNet object reference for callbacks
     */
    initGeofenceEditor: function (elementId, lat, lng, geojson, radius, dotnetRef) {
        var container = document.getElementById(elementId);
        if (!container) return;

        if (this._maps[elementId]) {
            this._maps[elementId].remove();
            delete this._maps[elementId];
        }

        var center = (lat && lng) ? [lat, lng] : [60.39, 5.32];
        var zoom = (lat && lng) ? 15 : 5;
        var map = L.map(elementId).setView(center, zoom);
        this._maps[elementId] = map;

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap',
            maxZoom: 19
        }).addTo(map);

        var drawnItems = new L.FeatureGroup();
        map.addLayer(drawnItems);

        // Load existing geofence (supports FeatureCollection with circles)
        if (geojson) {
            try {
                var parsed = JSON.parse(geojson);
                var features = parsed.type === 'FeatureCollection' ? parsed.features : [parsed];
                features.forEach(function (f) {
                    if (f.properties && f.properties.radius && f.geometry.type === 'Point') {
                        var c = f.geometry.coordinates;
                        drawnItems.addLayer(L.circle([c[1], c[0]], {
                            radius: f.properties.radius, color: '#4361EE', fillOpacity: 0.2
                        }));
                    } else {
                        L.geoJSON(f, { style: { color: '#4361EE', fillOpacity: 0.2 } })
                            .eachLayer(function (l) { drawnItems.addLayer(l); });
                    }
                });
                if (drawnItems.getLayers().length > 0)
                    map.fitBounds(drawnItems.getBounds().pad(0.2));
            } catch (e) { console.warn('Failed to load geofence:', e); }
        } else if (lat && lng && radius) {
            var circle = L.circle([lat, lng], { radius: radius, color: '#4361EE', fillOpacity: 0.2 });
            drawnItems.addLayer(circle);
        }

        // Add marker at center
        if (lat && lng) {
            L.marker([lat, lng]).addTo(map).bindPopup('Prosjektlokasjon');
        }

        // Drawing controls
        var drawControl = new L.Control.Draw({
            edit: { featureGroup: drawnItems },
            draw: {
                polygon: { shapeOptions: { color: '#4361EE', fillOpacity: 0.2 } },
                circle: { shapeOptions: { color: '#4361EE', fillOpacity: 0.2 } },
                rectangle: { shapeOptions: { color: '#4361EE', fillOpacity: 0.2 } },
                polyline: false,
                marker: false,
                circlemarker: false
            }
        });
        map.addControl(drawControl);

        function notifyAll() {
            var features = [];
            var allLat = [], allLng = [];
            drawnItems.eachLayer(function (layer) {
                if (layer instanceof L.Circle) {
                    var c = layer.getLatLng();
                    allLat.push(c.lat); allLng.push(c.lng);
                    features.push({
                        type: 'Feature',
                        properties: { radius: layer.getRadius() },
                        geometry: { type: 'Point', coordinates: [c.lng, c.lat] }
                    });
                } else {
                    var gj = layer.toGeoJSON();
                    features.push(gj);
                    if (gj.geometry && gj.geometry.coordinates) {
                        var coords = gj.geometry.type === 'Polygon' ? gj.geometry.coordinates[0] : gj.geometry.coordinates;
                        coords.forEach(function (c) { if (Array.isArray(c)) { allLng.push(c[0]); allLat.push(c[1]); } });
                    }
                }
            });
            var fc = features.length > 0 ? JSON.stringify({ type: 'FeatureCollection', features: features }) : null;
            var centerLat = allLat.length > 0 ? allLat.reduce(function(a,b){return a+b;},0) / allLat.length : null;
            var centerLng = allLng.length > 0 ? allLng.reduce(function(a,b){return a+b;},0) / allLng.length : null;
            console.log('Geofence changed:', { features: features.length, fc: fc ? fc.substring(0, 100) : null, centerLat, centerLng });
            if (dotnetRef) dotnetRef.invokeMethodAsync('OnGeofenceChanged', fc, null, centerLat, centerLng);
        }

        map.on(L.Draw.Event.CREATED, function (e) {
            drawnItems.addLayer(e.layer);
            notifyAll();
        });

        map.on(L.Draw.Event.EDITED, function () {
            notifyAll();
        });

        map.on(L.Draw.Event.DELETED, function () {
            notifyAll();
        });

        setTimeout(function () { map.invalidateSize(); }, 200);
    },

    _extractGeofenceData: function (layer) {
        if (layer instanceof L.Circle) {
            var c = layer.getLatLng();
            return { geojson: null, radius: layer.getRadius(), lat: c.lat, lng: c.lng };
        }
        return { geojson: JSON.stringify(layer.toGeoJSON()), radius: null, lat: null, lng: null };
    },

    /**
     * Geocode an address and pan the map to it with a marker.
     */
    geocodeAndPan: async function (elementId, address) {
        var map = this._maps[elementId];
        if (!map || !address) return;
        try {
            var query = encodeURIComponent(address + ', Norway');
            var response = await fetch('https://nominatim.openstreetmap.org/search?format=json&q=' + query + '&limit=1', {
                headers: { 'Accept-Language': 'no' }
            });
            var results = await response.json();
            if (results && results.length > 0) {
                var lat = parseFloat(results[0].lat);
                var lon = parseFloat(results[0].lon);
                map.setView([lat, lon], 15);
                L.marker([lat, lon]).addTo(map).bindPopup('<b>' + address + '</b>').openPopup();
            }
        } catch (e) { console.warn('Geocoding failed:', e); }
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
